namespace Symitar.Interfaces
{
    public interface ITcpAdapter
    {
        bool Connected { get; }

        void Connect(string server, int port);
        void Close();
        void Write(byte[] data);
        byte[] Read();

        bool Find(byte[] matcher);
    }
}