using System;
using System.Text;

namespace Symitar
{
    public class SpecfileError
    {
        public SymFile fileSrc;
        public string  fileErr;
        public string  error;
        public int     line;
        public int     column;
        public int     installedSize;
        public bool    any;

        public SpecfileError(SymFile Source, string ErrFile, string ErrMsg, int Row, int Col)
        {
            fileSrc = Source;
            fileErr = ErrFile;
            error   = ErrMsg;
            line    = Row;
            column  = Col;
            any     = true;
        }

        public static SpecfileError None()
        {
            SpecfileError ret = new SpecfileError(null, "", "", 0, 0);
            ret.any = false;
            return ret;
        }

        public static SpecfileError None(int size)
        {
            SpecfileError ret = new SpecfileError(null, "", "", 0, 0);
            ret.any = false;
            ret.installedSize = size;
            return ret;
        }
    }
}
