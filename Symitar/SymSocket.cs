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
            _client.Write(data);
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
            return _client.Read();
        }

        public ISymCommand ReadCommand()
        {
            _client.ReadTo(new byte[] { 0x1B, 0xFE });
            string data = _client.ReadTo(new byte[] { 0xFC });

            if(data.Length == 0) return SymCommand.Parse("");

            ISymCommand cmd = SymCommand.Parse(data.Substring(0, data.Length - 1));
            if ((cmd.Command == "MsgDlg") && (cmd.HasParameter("Text")))
                if (cmd.Get("Text").IndexOf("From PID") != -1)
                    cmd = ReadCommand();
            return cmd;
        }

        public int WaitFor(string matcher)
        {
            var startTime = DateTime.Now;

            var data = _client.ReadTo(matcher);
            while (string.IsNullOrEmpty(data))
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > DefaultTimeout)
                {
                    throw new TimeoutException("Timed out waiting for "+matcher);
                }

                data = _client.ReadTo(matcher);
            }

            return 0;
        }

        public int WaitFor(List<string> matchers)
        {
            var startTime = DateTime.Now;

            while (true)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > DefaultTimeout)
                {
                    throw new TimeoutException("Timed out waiting for " + matchers);
                }

                for (var i = 0; i < matchers.Count; i++)
                {
                    var data = _client.ReadTo(matchers[i]);
                    if (!string.IsNullOrEmpty(data)) 
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
                                _client.Write(wakeCmd.ToString());
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
