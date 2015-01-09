namespace Symitar.Interfaces
{
    public interface ISymCommand
    {
        string Command { get; }
        string Data { get; }

        string GetFileData();

        bool HasParameter(string param);
        void Set(string param, string value);
        string Get(string param);
        string ToString();
    }
}