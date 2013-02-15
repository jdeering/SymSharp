using System;

namespace Symitar
{
    public struct ReportInfo
    {
        public int Sequence;
        public string Title;

        public ReportInfo(int sequence, string title)
        {
            if (sequence <= 0) throw new ArgumentOutOfRangeException("sequence", "Invalid report sequence number");
            if (title == null) throw new ArgumentNullException("title");

            Sequence = sequence;
            Title = title;
        }
    }
}