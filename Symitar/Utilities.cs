using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Symitar
{
    public static class Utilities
    {
        private static readonly string[] FileTypeDescriptor = { "RepWriter", "Letter", "Help", "Report" };
        private static readonly string[] FileFolder = { "REPWRITERSPECS", "LetterSPECS", "HelpFILES", "REPORTS" };

        public static DateTime ParseSystemTime(string date, string time)
        {
            while (time.Length < 4)
                time = '0' + time;

            while (date.Length < 8)
                date = '0' + date;

            return new DateTime(
                Int32.Parse(date.Substring(4, 4)),
                Int32.Parse(date.Substring(0, 2)),
                Int32.Parse(date.Substring(2, 2)),
                Int32.Parse(time.Substring(0, 2)),
                Int32.Parse(time.Substring(2, 2)),
                0
            );
        }

        public static string FileTypeString(FileType type)
        {
            return FileTypeDescriptor[(int)type];
        }

        public static string ContainingFolder(string sym, FileType type)
        {
            return String.Format("/SYM/SYM{0}/{1}", sym, FileFolder[(int)type]);
        }

        public static string ContainingFolder(int sym, FileType type)
        {
            string symString = sym.ToString().PadLeft(3, '0');
            return String.Format("/SYM/SYM{0}/{1}", symString, FileFolder[(int)type]);
        }
    }
}
