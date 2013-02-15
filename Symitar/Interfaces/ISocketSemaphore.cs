namespace Symitar.Interfaces
{
    public interface ISocketSemaphore
    {
        bool WaitOne(int timeout);
        void Release();
    }
}