using System;

namespace Symitar
{
    public class File
    {
        public File()
        {
            Server = Sym = Name = "";
            TimeStamp = DateTime.Now;
            Size = 0;
            Type = FileType.RepGen;
        }

        public File(string server, string sym, string name, FileType fileType, string date, string time, int fileSize)
        {
            Server = server;
            Sym = sym;
            Name = name;
            TimeStamp = Utilities.ParseSystemTime(date, time);
            Size = fileSize;
            Type = fileType;
        }

        public File(string server, string sym, string name, FileType fileType, DateTime date, int fileSize)
        {
            Server = server;
            Sym = sym;
            Name = name;
            TimeStamp = date;
            Size = fileSize;
            Type = fileType;
        }

        public string Server { get; set; }
        public string Sym { get; set; }
        public string Name { get; set; }
        public DateTime TimeStamp { get; set; }
        public int Size { get; set; }
        public FileType Type { get; set; }
        public int Sequence { get; set; }
        public string Title { get; set; }
        public int PageCount { get; set; }

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