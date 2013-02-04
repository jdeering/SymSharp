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

        public void Connect(IPAddress server, int port)
        {
            _client.Connect(server, port);
        }

        public NetworkStream GetStream()
        {
            return _client.GetStream();
        }

        public void Close()
        {
            _client.Close();
        }
    }
}
