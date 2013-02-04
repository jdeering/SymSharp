using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Symitar.Interfaces
{
    public interface ISocketSemaphore
    {
        bool WaitOne(int timeout);
        void Release();
    }
}
