using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Symitar.Interfaces
{
    public interface ISymSocket
    {
        bool Connected { get; }
        bool Active { get; }
        string Error { get; }
        string Server { get; set; }
        int Port { get; set; }

        bool Connect();
        bool Connect(string server, int port);
        void Disconnect();

        void WakeUp();
        void KeepAliveStart();
        void KeepAliveStop();

        void Write(byte[] data);
        void Write(string data);
        void Write(ISymCommand symCommand);

        void Write(byte[] data, int timeout);
        void Write(string data, int timeout);
        void Write(ISymCommand symCommand, int timeout);

        byte[] Read(int timeout);

        byte[] ReadUntil(byte[] match, int timeout);
        byte[] ReadUntil(List<byte[]> matchers, int timeout);
        byte[] ReadUntil(string match, int timeout);
        byte[] ReadUntil(List<string> matchers, int timeout);

        ISymCommand ReadCommand();
        ISymCommand ReadCommand(int timeout);
    }
}
