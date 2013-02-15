using System;

namespace Symitar
{
    public class RepgenRunResult
    {
        public RepgenRunResult()
        {
            Sequence = -1;
            RunTime = -1;
        }

        public RunStatus Status { get; set; }
        public int Sequence { get; set; }
        public int RunTime { get; set; }
        public string ErrorMessage { get; set; }

        public static RepgenRunResult Okay(int sequence, int runTime)
        {
            return new RepgenRunResult
                {
                    Status = RunStatus.Okay,
                    Sequence = sequence,
                    RunTime = runTime
                };
        }

        public static RepgenRunResult Cancelled()
        {
            return new RepgenRunResult
                {
                    Status = RunStatus.Cancelled,
                    Sequence = -1,
                    RunTime = -1
                };
        }

        public static RepgenRunResult Error(string message)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException("message", "Error message must be specified.");

            return new RepgenRunResult
                {
                    Status = RunStatus.Error,
                    Sequence = -1,
                    RunTime = -1,
                    ErrorMessage = message
                };
        }

        public static RepgenRunResult FileNotFound()
        {
            return new RepgenRunResult
                {
                    Status = RunStatus.FileNotFound,
                    Sequence = -1,
                    RunTime = -1
                };
        }
    }
}