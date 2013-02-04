using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Symitar.Interfaces;

namespace Symitar
{
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class SocketLock : ISocketSemaphore
    {
        private Semaphore _lock;

        public SocketLock()
        {
            _lock = new Semaphore(1, 1);
        }

        public SocketLock(int initialCount, int maximumCount)
        {
            _lock = new Semaphore(initialCount, maximumCount);
        }

        public bool WaitOne(int timeout)
        {
            return _lock.WaitOne(timeout);
        }

        public void Release()
        {
            _lock.Release();
        }
    }
}
