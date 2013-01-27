using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Symitar
{
    public static class Utilities
    {
        private static readonly string[] FileTypeDescriptor = { "RepWriter", "Letter", "Help", "Report" };
        private static readonly string[] FileFolder = { "REPWRITERSPECS", "LETTERSPECS", "HELPFILES", "REPORTS" };

        public static DateTime ParseSystemTime(string date, string time)
        {
            if(string.IsNullOrEmpty(date))
                throw new ArgumentNullException("date", "Date cannot be blank.");

            int day, month, year, hour, minutes;

            if (string.IsNullOrEmpty(time))
            {
                hour = 0;
                minutes = 0;
            }
            else
            {
                while (time.Length < 4)
                {
                    time = '0' + time;
                }
                hour = Int32.Parse(time.Substring(0, 2));
                minutes = Int32.Parse(time.Substring(2, 2));
            }

            while (date.Length < 8)
                date = '0' + date;

            year = Int32.Parse(date.Substring(4, 4));
            month = Int32.Parse(date.Substring(0, 2));
            day = Int32.Parse(date.Substring(2, 2));

            return new DateTime(
                year,
                month,
                day,
                hour,
                minutes,
                0
            );
        }

        public static string FileTypeString(FileType type)
        {
            return FileTypeDescriptor[(int)type];
        }

        public static string ContainingFolder(int sym, FileType type)
        {
            if (sym < 0)
                throw new ArgumentOutOfRangeException("sym");

            return ContainingFolder(sym.ToString(), type);
        }

        public static string ContainingFolder(string sym, FileType type)
        {
            if(string.IsNullOrEmpty(sym)) throw new ArgumentNullException("sym");
            if(sym.Length > 3) throw new ArgumentOutOfRangeException("sym");

            sym = sym.PadLeft(3, '0');
            return String.Format("/SYM/SYM{0}/{1}", sym, FileFolder[(int)type]);
        }
    }
}
