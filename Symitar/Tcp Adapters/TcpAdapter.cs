using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Symitar.Interfaces;

namespace Symitar
{
    // Wrapper for the native TcpClient.
    // Allows it to be mocked in unit tests.
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    internal class TcpAdapter : ITcpAdapter
    {
        private TcpClient _client;

        public TcpAdapter()
        {
            _client = new TcpClient();
        }

        public bool Connected
        {
            get { return _client.Connected; }
        }

        public void Connect(string server, int port)
        {
            _client.Connect(server, port);
        }

        public void Close()
        {
            _client.Close();
        }

        public void Write(string data)
        {
            throw new NotImplementedException();
        }

        public void Write(byte[] data)
        {
            throw new NotImplementedException();
        }

        public string Read()
        {
            throw new NotImplementedException();
        }

        public string ReadTo(string data)
        {
            throw new NotImplementedException();
        }

        public string ReadTo(byte[] data)
        {
            throw new NotImplementedException();
        }

        public bool Find(string data)
        {
            throw new NotImplementedException();
        }
    }
}
