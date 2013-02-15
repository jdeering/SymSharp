using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Symitar.Interfaces;

namespace Symitar
{
    [ExcludeFromCodeCoverage]
    public class SocketLock : ISocketSemaphore
    {
        private readonly Semaphore _lock;

        public SocketLock()
        {
            _lock = new Semaphore(1, 1);
        }

        public SocketLock(int maximumCount)
        {
            _lock = new Semaphore(maximumCount, maximumCount);
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