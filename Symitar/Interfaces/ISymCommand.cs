using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
