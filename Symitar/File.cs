using System;
using System.Text;

namespace Symitar
{
    public class File
    {
        public string Server { get; set; }
        public string Sym { get; set; }
        public string FileName { get; set; }
        public DateTime TimeStamp { get; set; }
        public int FileSize { get; set; }
        public FileType Type { get; set; }
        public int      Sequence;
        public string   Title;
        public int      PageCount;

        public File()
        {
            Server = Sym = FileName = "";
            TimeStamp = DateTime.Now;
            FileSize = 0;
            Type = FileType.RepGen;
        }

        public File(string server, string sym, string name, FileType fileType, string date, string time, int fileSize)
        {
            Server = server;
            Sym = sym;
            FileName = name;
            TimeStamp = Utilities.ParseSystemTime(date, time);
            FileSize = fileSize;
            Type = fileType;
        }

        public File(string server, string sym, string name, FileType fileType, DateTime date, int fileSize)
        {
            Server = server;
            Sym = sym;
            FileName = name;
            TimeStamp = date;
            FileSize = fileSize;
            Type = fileType;
        }

        public string FileTypeString()
        {
            return Utilities.FileTypeString(Type);
        }

        public string ContainingFolder()
        {
            return Utilities.ContainingFolder(Sym, Type);
        }
    }
}
