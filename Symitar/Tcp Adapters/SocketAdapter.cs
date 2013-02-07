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

            // Get The data , if any
            var bytesReceived = sock.EndReceive(ar);

            if (bytesReceived > 0)
            {
                // Decode the received data
                var workingData = CleanDisplay(Encoding.ASCII.GetString(_buffer, 0, bytesReceived));

                // Write out the data
                if (workingData.IndexOf("[c") != -1) Negotiate(1);
                if (workingData.IndexOf("[6n") != -1) Negotiate(2);

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
            _workingData = "";
            return data;
        }
    }
}
