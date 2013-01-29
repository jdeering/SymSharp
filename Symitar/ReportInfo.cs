namespace Symitar
{
    public struct ReportInfo
    {
        public int Sequence;
        public string Title;

        public ReportInfo(int sequence, string title)
        {
            Sequence = sequence;
            Title = title;
        }
    }
}