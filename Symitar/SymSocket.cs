using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        private ISocketSemaphore _clientLock;
        private ITcpAdapter _client;
        private Thread _keepAliveThread;
        private DateTime _lastActivity;
        private bool _keepAliveActive;

        private Queue<ISymCommand> _commands;

        public string Server { get; set; }
        public int Port { get; set; }

        private string _lastError;
        public string Error 
        { 
            get { return _lastError; }
        }

        private bool _active;
        public bool Active
        {
            get { return _active; }
        }

        public bool Connected
        {
            get { return _client != null && _client.Connected; }
        }

        public SymSocket()
        {
            Initialize();
        }

        public SymSocket(ITcpAdapter tcpClient)
        {
            Initialize(tcpClient);
        }

        public SymSocket(ITcpAdapter tcpClient, ISocketSemaphore socketLock)
        {
            Initialize(tcpClient, socketLock);
        }

        public SymSocket(string server, int port)
        {
            Initialize();
            Server = server;
            Port = port;
        }

        public SymSocket(ITcpAdapter tcpClient, string server, int port)
        {
            Initialize(tcpClient);
            Server = server;
            Port = port;
        }

        private void Initialize(ITcpAdapter tcpClient = null)
        {
            _commands = new Queue<ISymCommand>();
            _client = tcpClient;
            _keepAliveThread = null;
            _keepAliveActive = false;

            Server = "";
            _lastError = "";
            _active = false;
            _clientLock = new SocketLock();
        }

        private void Initialize(ITcpAdapter tcpClient, ISocketSemaphore semaphore)
        {
            _commands = new Queue<ISymCommand>();
            _client = tcpClient;
            _keepAliveThread = null;
            _keepAliveActive = false;

            Server = "";
            _lastError = "";
            _active = false;
            _clientLock = semaphore;
        }

        public bool Connect()
        {
            return Connect(Server, Port);
        }

        public bool Connect(string server, int port)
        {
            if(string.IsNullOrEmpty(server))
                throw new ArgumentNullException("server");

            if(port <= 0)
                throw new ArgumentOutOfRangeException("port");

            if(Connected) 
                throw new InvalidOperationException("Client is already connected");

            Server = server;
            Port = port;
            _lastError = "";

            try
            {
                IPAddress ipAddress;
                bool parseResult = IPAddress.TryParse(server, out ipAddress);

                LockSocket(5000);

                if(_client == null) // Use SocketAdapter implementation as default ITcpAdapter
                    _client = new SocketAdapter();
                
                _client.Connect(server, port);
            }
            catch (Exception ex)
            {
                if (Active) 
                    UnlockSocket();
                _lastError = "Unable to Connect to Server\n" + ex.Message;
                return false;
            }

            _lastActivity = DateTime.Now;
            UnlockSocket();

            return true;
        }

        public void Disconnect()
        {
            KeepAliveStop();
            try { _client.Close(); } catch { }
            Initialize(); // Reset everything
        }

        private void LockSocket(int timeout)
        {
            if(!_clientLock.WaitOne(timeout))
                throw new Exception("Unable to Obtain Socket Lock");
            _active = true;
        }

        private void UnlockSocket()
        {
            _active = false;
            _clientLock.Release();
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
            catch (Exception) { }
        }

        public string Read()
        {
            return Encoding.ASCII.GetString(_client.Read());
        }

        public ISymCommand ReadCommand()
        {
            var data = Read();
            if (!string.IsNullOrEmpty(data))
            {
                var commandStart = Encoding.ASCII.GetString(new byte[] {0x1b, 0xfe});
                var commandEnd = Encoding.ASCII.GetString(new byte[] {0xfc});

                int start = data.IndexOf(commandStart);
                while (start >= 0)
                {
                    var end = data.IndexOf(commandEnd, start + commandStart.Length);

                    var commandString = data.Substring(start + commandStart.Length, end - start - commandStart.Length);

                    var newCommand = SymCommand.Parse(commandString);
                    data = data.Substring(end + commandEnd.Length);

                    if (newCommand.Command == "File")
                    {
                        var nextStart = data.IndexOf(commandStart);
                        if (nextStart >= 0)
                        {
                            newCommand.Data = data.Substring(0, nextStart - 2);
                            data = data.Substring(nextStart);
                        }
                    }

                    _commands.Enqueue(newCommand);


                    start = data.IndexOf(commandStart);
                }
            }

            if (_commands.Count == 0) return SymCommand.Parse("");

            var cmd = _commands.Dequeue();
            if ((cmd.Command == "MsgDlg") && (cmd.HasParameter("Text")))
                if (cmd.Get("Text").IndexOf("From PID") != -1)
                    cmd = ReadCommand();
            return cmd;
        }

        public int WaitFor(params string[] matchers)
        {
            if(matchers.Length == 0)
                throw new ArgumentException("matchers");

            var startTime = DateTime.Now;

            while (true)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > DefaultTimeout)
                {
                    throw new TimeoutException("Timed out waiting for " + matchers);
                }

                for (var i = 0; i < matchers.Length; i++)
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
                                UnlockSocket();
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
