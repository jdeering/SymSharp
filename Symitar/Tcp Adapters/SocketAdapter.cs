using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Symitar.Interfaces;

namespace Symitar
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class SocketAdapter : ITcpAdapter
    {
        private Socket _socket;
        private IPEndPoint _endPoint;
        private string _address;
        private int _port;

        private List<byte> _log; 
        private string _workingData;
        private byte[] _buffer;

        public bool Connected
        {
            get { return _socket != null && _socket.Connected; }
        }

        public void Connect(string server, int port)
        {
            _address = server;
            _port = port;
            _log = new List<byte>(256);
            _buffer = new byte[256];

            var ipHost = Dns.GetHostEntry(_address);
            var primaryAddress = ipHost.AddressList[0];

            // Try a blocking connection to the server
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _endPoint = new IPEndPoint(primaryAddress, _port);
            _socket.Connect(_endPoint);

            // If the connect worked, setup a callback to start listening for incoming data
            var recieveData = new AsyncCallback(OnRecievedData);
            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, recieveData, _socket);
        }

        private void OnRecievedData(IAsyncResult ar)
        {
            // Get The connection socket from the callback
            var sock = (Socket)ar.AsyncState;

            int bytesReceived;

            // Get The data , if any
            try
            {
                bytesReceived = sock.EndReceive(ar);
            }
            catch(ObjectDisposedException ode)
            {
                return; // Gracefully handle disposed socket
            }

            if (bytesReceived > 0)
            {
                // Decode the received data
                var workingData = CleanDisplay(Encoding.ASCII.GetString(_buffer, 0, bytesReceived));

                // Write out the data
                //if (workingData.IndexOf("[c") != -1) Negotiate(1);
                //if (workingData.IndexOf("[6n") != -1) Negotiate(2);
                _log.AddRange(_buffer);
                _workingData += workingData;

                // Launch another callback to listen for data
                var recieveData = new AsyncCallback(OnRecievedData);
                sock.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, recieveData, sock);
            }
            else
            {
                // If no data was recieved then the connection is probably dead
                Console.WriteLine("Disconnected {0}", sock.RemoteEndPoint);
                sock.Shutdown(SocketShutdown.Both);
                sock.Close();
            }
        }

        private void Negotiate(int part)
        {
            StringBuilder x;
            string neg;
            if (part == 1)
            {
                x = new StringBuilder();
                x.Append((char)27);
                x.Append((char)91);
                x.Append((char)63);
                x.Append((char)49);
                x.Append((char)59);
                x.Append((char)50);
                x.Append((char)99);
                neg = x.ToString();
            }
            else
            {
                x = new StringBuilder();
                x.Append((char)27);
                x.Append((char)91);
                x.Append((char)50);
                x.Append((char)52);
                x.Append((char)59);
                x.Append((char)56);
                x.Append((char)48);
                x.Append((char)82);
                neg = x.ToString();
            }
            Write(neg);
        }

        private string CleanDisplay(string input)
        {

            input = input.Replace("(0x (B", "|");
            input = input.Replace("(0 x(B", "|");
            input = input.Replace(")0=>", "");
            input = input.Replace("[0m>", "");
            input = input.Replace("7[7m", "[");
            input = input.Replace("[0m*8[7m", "]");
            input = input.Replace("[0m", "");
            return input;
        }

        public void Close()
        {
            if (_socket.Connected) 
                _socket.Close();
        }

        public void Write(string data)
        {
            Write(Encoding.ASCII.GetBytes(data));
        }

        public void Write(byte[] data)
        {
            _socket.Send(data, 0, data.Length, SocketFlags.None);
        }

        public string Read()
        {
            string data = _workingData;
            ClearWorkingData();
            return data;
        }

        public string ReadTo(string data)
        {
            if (_workingData.IndexOf(data) < 0) 
                return string.Empty;

            var end = _workingData.IndexOf(data) + data.Length;
            
            string result = _workingData.Substring(0, end);
            if (_workingData.Length > end)
            {
                _log = _log.Skip(end).ToList();
                _workingData = _workingData.Substring(end);
            }
            else
            {
                ClearWorkingData();
            }
            return result;
        }

        public string ReadTo(byte[] data)
        {
            int end = -1;
            for (var i = 0; i < _log.Count - data.Length; i++)
            {
                if (_log.GetRange(i, data.Length).ToArray() == data)
                {
                    end = i + data.Length - 1;
                    break;
                }
            }

            if (end < 0) return string.Empty;

            string result = _workingData.Substring(0, end);
            if (_workingData.Length > end)
            {
                _log = _log.Skip(end).ToList();
                _workingData = _workingData.Substring(end);
            }
            else
            {
                ClearWorkingData();
            }
            return result;
        }

        public bool Find(string data)
        {
            return _workingData.Contains(data);
        }

        private void ClearWorkingData()
        {
            _log.Clear();
            _workingData = "";
        }
    }
}
