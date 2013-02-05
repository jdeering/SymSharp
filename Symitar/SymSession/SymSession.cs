using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;
using Symitar.Interfaces;

namespace Symitar
{
    public partial class SymSession
    {
        private ISymSocket _socket;

        public int SymDirectory { get; set; }

        private string _error;
        public string Error
        {
            get { return _error; }
        }

        private bool _loggedIn;
        public bool LoggedIn
        {
            get { return _loggedIn; }
        }

        public bool Connected
        {
            get { return _socket.Connected; }
        }

        public SymSession()
        {
            Initialize();
        }

        public SymSession(int symDirectory)
        {
            Initialize();
            SymDirectory = symDirectory;
        }

        public SymSession(ISymSocket socket)
        {
            Initialize();
            _socket = socket;
        }

        public SymSession(ISymSocket socket, int symDirectory)
        {
            Initialize();
            _socket = socket;
            SymDirectory = symDirectory;
        }

        private void Initialize()
        {
            _error = "";
            _loggedIn = false;
        }
        
        public bool Connect(string server, int port)
        {
            if(string.IsNullOrEmpty(server)) throw new ArgumentNullException("server");
            if(port <= 0) throw new ArgumentOutOfRangeException("port");

            if (_socket == null)
            {
                _socket = new SymSocket(server, port);
                return _socket.Connect();
            }
            else
            {
                return _socket.Connect(server, port);
            }
        }

        public void Disconnect()
        {
            if(_socket != null)
                _socket.Disconnect();
        }

        public bool Login(string aixUsername, string aixPassword, string symUserId)
        {
            return Login(aixUsername, aixPassword, SymDirectory, symUserId, 0);
        }

        public bool Login(string aixUsername, string aixPassword, int directory, string symUserId)
        {
            return Login(aixUsername, aixPassword, directory, symUserId, 0);
        }

        public bool Login(string aixUsername, string aixPassword, int directory, string symUserId, int stage)
        {
            if (!_socket.Connected)
            {
                _error = "Socket not connected";
                return false;
            }

            if (string.IsNullOrEmpty(aixUsername)) throw new ArgumentNullException("aixUsername");
            if (string.IsNullOrEmpty(aixPassword)) throw new ArgumentNullException("aixPassword");
            if (directory < 0 || directory > 999) throw new ArgumentOutOfRangeException("directory");
            if (string.IsNullOrEmpty(symUserId)) throw new ArgumentNullException("symUserId");
            if (stage < 0) throw new ArgumentOutOfRangeException("stage");

            _error = "";
            SymDirectory = directory;

            //Telnet Handshake
            try
            {
                _socket.Write(new byte[] { 0xFF, 0xFB, 0x18 }, 1000);
                _socket.Write(new byte[] { 0xFF, 0xFA, 0x18, 0x00, 0x61, 0x69, 0x78, 0x74, 0x65, 0x72, 0x6D, 0xFF, 0xF0 }, 1000);
                _socket.Write(new byte[] { 0xFF, 0xFD, 0x01 }, 1000);
                _socket.Write(new byte[] { 0xFF, 0xFD, 0x03, 0xFF, 0xFC, 0x1F, 0xFF, 0xFC, 0x01 }, 1000);
            }
            catch (Exception ex)
            {
                _error = "Telnet communication failed: " + ex.Message;
                Disconnect();
                return false;
            }

            if (stage < 2)
            {
                if (!AixLogin(aixUsername, aixPassword))
                    return false;

                if (!SymLogin(SymDirectory, symUserId))
                    return false;
            }

            try
            {
                _socket.KeepAliveStart();
            }
            catch (Exception)
            {
                return false;
            }
            _loggedIn = true;
            return true;
        }

        private bool AixLogin(string username, string password)
        {
            //AIX Login
            try
            {
                string stat;
                byte[] data;

                _socket.Write(username + '\r', 1000);

                data = _socket.ReadUntil(new List<string> { "Password:", "[c" }, 1000);
                stat = Utilities.DecodeString(data);

                if (stat.IndexOf("[c") == -1)
                {
                    if (stat.IndexOf("invalid login") != -1)
                    {
                        _error = "Invalid AIX Login";
                        return false;
                    }

                    _socket.Write(password + '\r', 1000);
                    data = _socket.ReadUntil(":", 1000);
                    stat = Utilities.DecodeString(data);

                    if (stat.IndexOf("invalid login") != -1)
                    {
                        _error = "Invalid AIX Login";
                        return false;
                    }

                    _socket.ReadUntil("[c", 1000);
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            
            return true;
        }

        private bool SymLogin(int symDir, string userPassword)
        {
            _socket.Write("WINDOWSLEVEL=3\n", 1000);
            _socket.ReadUntil("$ ", 1000);
            _socket.Write(
                String.Format("sym {0}\r", symDir), 
                1000);

            ISymCommand cmd = _socket.ReadCommand(2000);

            while (cmd.Command != "Input")
            {
                if (cmd.Command == "SymLogonError" && cmd.Get("Text").IndexOf("Too Many Invalid Password Attempts") > -1)
                {
                    _error = "Too Many Invalid Password Attempts";
                    return false;
                }

                cmd = _socket.ReadCommand(2000);
                if ((cmd.Command == "Input") && (cmd.Get("HelpCode") == "10025"))
                {
                    _socket.Write("$WinHostSync$\r");
                    cmd = _socket.ReadCommand(2000);
                }
            }

            _socket.Write(String.Format("{0}\r", symDir), 1000);
            cmd = _socket.ReadCommand(2000);
            if (cmd.Command == "SymLogonInvalidUser")
            {
                _error = "Invalid Sym User";
                _socket.Write("\r", 1000);
                _socket.ReadCommand(2000);
                return false;
            }
            if (cmd.Command == "SymLogonError" && cmd.Get("Text").IndexOf("Too Many Invalid Password Attempts") > -1)
            {
                _error = "Too Many Invalid Password Attempts";
                return false;
            }

            _socket.Write("\r", 1000); _socket.ReadCommand(2000);
            _socket.Write("\r", 1000); _socket.ReadCommand(2000);

            return true;
        }
    }
}
