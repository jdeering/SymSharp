using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Symitar.Interfaces;

namespace Symitar
{
    // Wraps TCP Client to connect
    // to Symitar. Used by SymSession.
    public class SymSocket : ISymSocket
    {
        private const int DefaultTimeout = 5000;
        private const int KeepAliveInterval = 45000;
        private ITcpAdapter _client;

        private int _commandIndex;
        private List<ISymCommand> _commands;
        private string _data;
        private bool _keepAliveActive;
        private Thread _keepAliveThread;
        private DateTime _lastActivity;

        private string _lastError;

        public SymSocket()
        {
            Initialize();
        }

        public SymSocket(ITcpAdapter tcpClient)
        {
            Initialize(tcpClient);
        }

        public SymSocket(ITcpAdapter tcpClient, string server, int port)
        {
            Initialize(tcpClient);
            Server = server;
            Port = port;
        }

        public ITcpAdapter TcpClient { get { return _client; } }
        public string Server { get; set; }
        public int Port { get; set; }

        public string Error
        {
            get { return _lastError; }
        }

        public bool Connected
        {
            get { return _client != null && _client.Connected; }
        }

        public bool Connect()
        {
            return Connect(Server, Port);
        }

        public bool Connect(string server, int port)
        {
            if (string.IsNullOrEmpty(server))
                throw new ArgumentNullException("server");

            if (port <= 0)
                throw new ArgumentOutOfRangeException("port");

            if (Connected)
                throw new InvalidOperationException("Client is already connected");

            Server = server;
            Port = port;
            _lastError = "";

            try
            {
                _client.Connect(server, port);
            }
            catch (Exception ex)
            {
                _lastError = "Unable to Connect to Server\n" + ex.Message;
                return false;
            }

            _lastActivity = DateTime.Now;

            return true;
        }

        public void Disconnect()
        {
            KeepAliveStop();
            try
            {
                _client.Close();
            }
            catch
            {
            }
            Initialize(); // Reset everything
        }

        public void Write(string data)
        {
            _client.Write(Encoding.ASCII.GetBytes(data));
        }

        public void Write(ISymCommand cmd)
        {
            Write(cmd.ToString());
        }

        public void Write(byte[] buff)
        {
            _client.Write(buff);
        }

        public void WakeUp()
        {
            try
            {
                Write(new SymCommand("WakeUp"));
            }
            catch (Exception)
            {
            }
        }

        public string Read()
        {
            var data = new List<byte>();
            while (true)
            {
                byte[] newData = _client.Read();
                if (newData.Length == 0)
                    return Encoding.ASCII.GetString(data.ToArray());
                data.AddRange(newData);
            }
        }

        public void Clear()
        {
            _commands.RemoveRange(0, _commandIndex + 1);
            _commandIndex = -1;
        }

        public ISymCommand ReadCommand()
        {
            _data += Read();

            if (!string.IsNullOrEmpty(_data))
            {
                string commandStart = Encoding.ASCII.GetString(new byte[] {0x1b, 0xfe});
                string commandEnd = Encoding.ASCII.GetString(new byte[] {0xfc});

                int start = _data.IndexOf(commandStart, StringComparison.Ordinal);
                int end = _data.IndexOf(commandEnd, start + commandStart.Length, StringComparison.Ordinal);
                while (start >= 0 && end >= start + commandStart.Length)
                {
                    int commandLength = end - start - commandStart.Length;

                    string commandString = _data.Substring(start + commandStart.Length, commandLength);
                    SymCommand newCommand = SymCommand.Parse(commandString);
                    _data = _data.Substring(start + commandStart.Length + commandString.Length);
                    int dataLength = _data.Length;
                    if (newCommand.Command == "File")
                    {
                        int nextStart = _data.IndexOf(commandStart, StringComparison.Ordinal);
                        if (nextStart > 2)
                        {
                            newCommand.Data = _data.Substring(0, nextStart - 2);
                            _data = _data.Substring(nextStart);
                        }
                    }

                    _commands.Add(newCommand);

                    start = _data.IndexOf(commandStart, StringComparison.Ordinal);
                    end = _data.IndexOf(commandEnd, start + commandStart.Length, StringComparison.Ordinal);
                }
            }

            if (_commandIndex + 1 == _commands.Count)
            {
                _commands.Clear();
                _commandIndex = -1;
            }

            if (_commands.Count == 0) return SymCommand.Parse("");

            ISymCommand cmd = _commands[++_commandIndex];
            if ((cmd.Command == "MsgDlg") && (cmd.HasParameter("Text")))
                if (cmd.Get("Text").IndexOf("From PID", StringComparison.Ordinal) != -1)
                    cmd = ReadCommand();
            return cmd;
        }

        public int WaitFor(params string[] matchers)
        {
            if (matchers.Length == 0)
                throw new ArgumentException("matchers");

            DateTime startTime = DateTime.Now;

            while (true)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > DefaultTimeout)
                {
                    throw new TimeoutException("Timed out waiting for " + matchers);
                }

                for (int i = 0; i < matchers.Length; i++)
                {
                    if (_client.Find(Encoding.ASCII.GetBytes(matchers[i])))
                        return i;
                }
            }
        }

        public void KeepAliveStart()
        {
            if (!Connected) return;
            if (_keepAliveThread != null)
                KeepAliveStop();

            _keepAliveActive = true;
            _keepAliveThread = new Thread(KeepAlive);
            try
            {
                _keepAliveThread.Start();
            }
            catch (Exception ex)
            {
                _keepAliveActive = false;
                throw new Exception("Error Starting Keep-Alive Thread\n" + ex.Message);
            }
        }

        public void KeepAliveStop()
        {
            if (_keepAliveThread == null) return;

            try
            {
                _keepAliveThread.Abort();
            }
            catch (Exception)
            {
                //thread died on it's own
            }

            _keepAliveThread = null;
        }

        private void Initialize(ITcpAdapter tcpClient = null)
        {
            _data = "";
            _commandIndex = -1;
            _commands = new List<ISymCommand>();
            _client = tcpClient;
            _keepAliveThread = null;
            _keepAliveActive = false;

            Server = "";
            _lastError = "";
        }

        private void KeepAlive()
        {
            byte[] wakeCmd = Utilities.EncodeString((new SymCommand("WakeUp")).ToString());

            try
            {
                while (Connected && _keepAliveActive)
                {
                    if ((DateTime.Now - _lastActivity).TotalMilliseconds >= KeepAliveInterval)
                    {
                        try
                        {
                            try
                            {
                                _client.Write(Encoding.ASCII.GetBytes(wakeCmd.ToString()));
                                _lastActivity = DateTime.Now;
                            }
                            catch (Exception)
                            {
                                Disconnect();
                            }
                        }
                        catch (Exception)
                        {
                            // Failed to lock, must be in use.
                        }
                    }

                    Thread.Sleep(1000);
                }
            }
            catch (ThreadAbortException)
            {
                //we were forcefully killed
            }
        }
    }
}