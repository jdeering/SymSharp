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
        string ReadTo(string data); // Reads data stream before and up to the specified data
        string ReadTo(byte[] data); // Reads data stream before and up to the specified data

        bool Find(string data);
    }
}
