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

        string Read();
        ISymCommand ReadCommand();

        int WaitFor(string matcher);
        int WaitFor(List<string> matchers);
    }
}
