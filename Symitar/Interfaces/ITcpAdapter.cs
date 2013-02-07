using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Net;

namespace Symitar.Interfaces
{
    public interface ITcpAdapter
    {
        bool Connected { get; }

        void Connect(string server, int port);

        void Close();

        void Write(string data);
        void Write(byte[] data);
        string Read();
    }
}
