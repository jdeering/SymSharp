using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Symitar.Interfaces;

namespace Symitar
{
    [ExcludeFromCodeCoverage]
    public class SocketAdapter : ITcpAdapter
    {
        private readonly Semaphore _lock;
        private string _address;
        private byte[] _buffer;
        private IPEndPoint _endPoint;
        private int _port;
        private Socket _socket;

        private List<byte> _workingData;

        public SocketAdapter()
        {
            _lock = new Semaphore(1, 1);
        }

        public bool Connected
        {
            get { return _socket != null && _socket.Connected; }
        }

        public void Connect(string server, int port)
        {
            _address = server;
            _port = port;
            _workingData = new List<byte>(2048);
            _buffer = new byte[2048];

            IPHostEntry ipHost = Dns.GetHostEntry(_address);
            IPAddress primaryAddress = ipHost.AddressList[0];

            // Try a blocking connection to the server
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _endPoint = new IPEndPoint(primaryAddress, _port);
            _socket.Connect(_endPoint);

            // If the connect worked, setup a callback to start listening for incoming data
            var recieveData = new AsyncCallback(OnRecievedData);
            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, recieveData, _socket);
        }

        public void Close()
        {
            if (_socket.Connected)
                _socket.Close();
        }

        public void Write(byte[] data)
        {
            _socket.Send(data, 0, data.Length, SocketFlags.None);

            Thread.Sleep(25); // Adding a 25 ms sleep to allow server to respond
        }

        public byte[] Read()
        {
            _lock.WaitOne(500);
            byte[] data = _workingData.ToArray();
            ClearWorkingData();
            _lock.Release();
            return data;
        }

        public bool Find(byte[] matcher)
        {
            return Encoding.ASCII.GetString(_workingData.ToArray()).Contains(Encoding.ASCII.GetString(matcher));
        }

        private void OnRecievedData(IAsyncResult ar)
        {
            // Get The connection socket from the callback
            var sock = (Socket) ar.AsyncState;

            int bytesReceived;

            // Get The data , if any
            try
            {
                bytesReceived = sock.EndReceive(ar);
            }
            catch (ObjectDisposedException ode)
            {
                return; // Gracefully handle disposed socket
            }

            if (bytesReceived > 0)
            {
                // Decode the received data
                string workingData = CleanDisplay(Encoding.ASCII.GetString(_buffer, 0, bytesReceived));

                // Write out the data
                //if (workingData.IndexOf("[c") != -1) Negotiate(1);
                //if (workingData.IndexOf("[6n") != -1) Negotiate(2);
                _lock.WaitOne(500);
                _workingData.AddRange(Encoding.ASCII.GetBytes(workingData));
                _lock.Release();
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
                x.Append((char) 27);
                x.Append((char) 91);
                x.Append((char) 63);
                x.Append((char) 49);
                x.Append((char) 59);
                x.Append((char) 50);
                x.Append((char) 99);
                neg = x.ToString();
            }
            else
            {
                x = new StringBuilder();
                x.Append((char) 27);
                x.Append((char) 91);
                x.Append((char) 50);
                x.Append((char) 52);
                x.Append((char) 59);
                x.Append((char) 56);
                x.Append((char) 48);
                x.Append((char) 82);
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

        public void Write(string data)
        {
            Write(Encoding.ASCII.GetBytes(data));
        }

        private void ClearWorkingData()
        {
            _workingData.Clear();
        }
    }
}