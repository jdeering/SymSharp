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
    internal class SymSocket : ISymSocket
    {
        private const int DefaultTimeout = 5000;
        private const int KeepAliveInterval = 45000;

        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _keepAliveThread;
        private DateTime _lastActivity;
        private bool _keepAliveActive;
        private Semaphore _clientLock;

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

        public SymSocket(string server, int port)
        {
            Initialize();
            Server = server;
            Port = port;
        }

        private void Initialize()
        {
            _client = null;
            _stream = null;
            _keepAliveThread = null;
            _keepAliveActive = false;

            Server = "";
            _lastError = "";
            _active = false;
            _clientLock = new Semaphore(1, 1);
        }

        public bool Connect()
        {
            return Connect(Server, Port);
        }

        public bool Connect(string server, int port)
        {
            if(Connected) 
                throw new InvalidOperationException("Client is already connected");

            Server = server;
            Port = port;
            _lastError = "";

            try
            {
                LockSocket(5000);
                _client = new TcpClient();
                _client.Connect(IPAddress.Parse(Server), Port);
                _stream = _client.GetStream();
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

            return Connected;
        }

        public void Disconnect()
        {
            KeepAliveStop();
            try { _stream.Close(); } catch { }
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

        public void Write(byte[] buff, int off, int size, int timeout)
        {
            LockSocket(DefaultTimeout);

            int currTimeout = _stream.WriteTimeout;

            _stream.WriteTimeout = timeout;
            _stream.Write(buff, off, size);

            _stream.WriteTimeout = currTimeout;

            UnlockSocket();
            _lastActivity = DateTime.Now;
        }

        public void Write(byte[] buff, int timeout)
        {
            Write(buff, 0, buff.Length, timeout);
        }

        public void Write(string str, int timeout)
        {
            Write(Utilities.EncodeString(str), timeout);
        }

        public void Write(ISymCommand cmd, int timeout)
        {
            Write(cmd.ToString(), timeout);
        }

        public void Write(byte[] buff)
        {
            Write(buff, DefaultTimeout);
        }

        public void Write(string str)
        {
            Write(Utilities.EncodeString(str), DefaultTimeout);
        }

        public void Write(ISymCommand cmd)
        {
            Write(cmd.ToString(), DefaultTimeout);
        }

        public void WakeUp()
        {
            try
            {
                Write(new SymCommand("WakeUp"), 1000);
            }
            catch (Exception) { }
        }

        public byte[] Read(int size, int timeout)
        {
            LockSocket(5000);
            int oto = _stream.ReadTimeout;
            _stream.ReadTimeout = timeout;
            DateTime begin = DateTime.Now;

            int read = 0;
            byte[] buff = new byte[size];

            while (read < size)
            {
                if ((DateTime.Now - begin).Milliseconds > timeout)
                {
                    UnlockSocket();
                    Disconnect();
                    throw new Exception("Socket Read Timeout");
                }
                read += _stream.Read(buff, read, size - read);
            }

            _stream.ReadTimeout = oto;
            UnlockSocket();
            _lastActivity = DateTime.Now;

            return buff;
        }

        public byte[] Read(int size)
        {
            return Read(size, DefaultTimeout);
        }

        public byte[] ReadUntil(byte[] match, int timeout)
        {
            LockSocket(5000);
            int oto = _stream.ReadTimeout;
            _stream.ReadTimeout = timeout;
            DateTime begin = DateTime.Now;

            bool hit = false;
            byte[] test = new byte[match.Length];
            List<byte> buff = new List<byte>();

            while (!hit)
            {
                if ((DateTime.Now - begin).Milliseconds > timeout)
                {
                    UnlockSocket();
                    Disconnect();
                    throw new Exception("Socket Read Timeout");
                }
                for (int i = 0; i < (test.Length - 1); i++) test[i] = test[i + 1];
                _stream.Read(test, test.Length - 1, 1);
                buff.Add(test[test.Length - 1]);
                hit = true;
                for (int c = 0; c < test.Length; c++)
                {
                    if (test[c] != match[c])
                    {
                        hit = false;
                        break;
                    }
                }
            }

            _stream.ReadTimeout = oto;
            UnlockSocket();
            _lastActivity = DateTime.Now;
            return buff.ToArray();
        }
        //------------------------------------------------------------------------
        public byte[] ReadUntil(string match, int timeout) { return ReadUntil(Utilities.EncodeString(match), timeout); }
        public string ReadUntilString(byte[] match, int timeout) { return Utilities.DecodeString(ReadUntil(match, timeout)); }
        public string ReadUntilString(string match, int timeout) { return Utilities.DecodeString(ReadUntil(match, timeout)); }
        public byte[] ReadUntil(byte[] match) { return ReadUntil(match, DefaultTimeout); }
        public byte[] ReadUntil(string match) { return ReadUntil(match, DefaultTimeout); }
        public string ReadUntilString(byte[] match) { return ReadUntilString(match, DefaultTimeout); }
        public string ReadUntilString(string match) { return ReadUntilString(match, DefaultTimeout); }
        
        public ISymCommand ReadCommand()
        {
            return ReadCommand(DefaultTimeout);
        }

        public ISymCommand ReadCommand(int timeout)
        {
            ReadUntil(new byte[] { 0x1B, 0xFE }, timeout);
            string data = ReadUntilString(new byte[] { 0xFC }, timeout);

            ISymCommand cmd = SymCommand.Parse(data.Substring(0, data.Length - 1));
            if ((cmd.Command == "MsgDlg") && (cmd.HasParameter("Text")))
                if (cmd.Get("Text").IndexOf("From PID") != -1)
                    cmd = ReadCommand(timeout);
            return cmd;
        }

        public byte[] ReadUntil(List<byte[]> matchers, int timeout)
        {
            List<string> stringMatchers = matchers.Select(matcher => Utilities.DecodeString(matcher)).ToList();

            return ReadUntil(stringMatchers, timeout);
        }

        public byte[] ReadUntil(List<string> matchers, int timeout)
        {
            if(timeout <= 0)
                throw new ArgumentOutOfRangeException("timeout");

            if (matchers.Count == 0)
                throw new ArgumentException("matchers", "One or more matchers must be provided.");
            
            byte[] buffer = new byte[1024]; // 1 MB buffer for read
            int bytesRead = 0;
            string data = "";

            LockSocket(5000);
            int currTimeout = _stream.ReadTimeout;
            _stream.ReadTimeout = timeout;

            bool matchFound = false;
            DateTime startTime = DateTime.Now;
            while (!matchFound)
            {
                if ((DateTime.Now - startTime).Milliseconds > timeout)
                {
                    UnlockSocket();
                    Disconnect();
                    throw new TimeoutException();
                }

                bytesRead = _stream.Read(buffer, bytesRead, 1);
                data += Utilities.DecodeString(buffer);

                if (matchers.Any(matcher => data.Contains(matcher)))
                {
                    matchFound = true;
                }
            }

            _stream.ReadTimeout = currTimeout;
            UnlockSocket();
            _lastActivity = DateTime.Now;

            return Utilities.EncodeString(data);
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

            /***** this was seriously slowing down application exit, waiting 2s per connection ****
            //attempt to let thread die naturally
            _keepAliveActive = false;
            Thread.Sleep(2000);
            */

            //now force thread to terminate
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
        //------------------------------------------------------------------------
        private void KeepAlive()
        {
            int oto = _stream.WriteTimeout;
            byte[] wakeCmd = Utilities.EncodeString((new SymCommand("WakeUp")).ToString());

            try
            {
                while (Connected && _keepAliveActive)
                {
                    if ((DateTime.Now - _lastActivity).TotalMilliseconds >= KeepAliveInterval)
                    {
                        try
                        {
                            LockSocket(5000);
                            try
                            {
                                _stream.WriteTimeout = 1000;
                                _stream.Write(wakeCmd, 0, wakeCmd.Length);
                                _stream.WriteTimeout = oto;
                                UnlockSocket();
                                _lastActivity = DateTime.Now;
                            }
                            catch (Exception)
                            {
                                Disconnect();
                            }
                        }
                        catch (Exception)
                        {
                            //failed in LockSocket, must be in use.
                            //no need to complain
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
