using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Symitar.Interfaces;

namespace Symitar
{
    public partial class SymSession
    {
        public delegate void FileRunStatus(int code, string description);
        public delegate string FileRunPrompt(string prompt);

        public bool IsFileRunning(int sequence)
        {
            if(sequence <= 0)
                throw new ArgumentOutOfRangeException("sequence");

            ISymCommand cmd;
            bool running = false;

            cmd = new SymCommand("Misc");
            cmd.Set("InfoType", "BatchQueues");
            _socket.Write(cmd);

            cmd = _socket.ReadCommand();
            while (!cmd.HasParameter("Done"))
            {
                if ((cmd.Get("Action") == "QueueEntry") && (int.Parse(cmd.Get("Seq")) == sequence))
                    running = true;
                cmd = _socket.ReadCommand();
            }

            return running;
        }

        private List<int> GetPrintSequences(string which)
        {
            List<int> seqs = new List<int>();
            ISymCommand cmd;

            cmd = new SymCommand("File");
            cmd.Set("Action", "List");
            cmd.Set("MaxCount", "50");
            cmd.Set("Query", "LAST 20 \"+" + which + "+\"");
            cmd.Set("Type", "Report");
            _socket.Write(cmd);

            cmd = _socket.ReadCommand();
            while (!cmd.HasParameter("Done"))
            {
                if (cmd.HasParameter("Sequence"))
                    seqs.Add(int.Parse(cmd.Get("Sequence")));
                cmd = _socket.ReadCommand();
            }

            seqs.Sort();
            seqs.Reverse();
            return seqs;
        }

        public int GetReportSequence(string repName, int time)
        {
            List<int> seqs = GetPrintSequences("REPWRITER");
            foreach (int i in seqs)
            {
                Symitar.File file = new Symitar.File(_socket.Server, SymDirectory.ToString(), i.ToString(), Symitar.FileType.Report, DateTime.Now, 0);
                string contents = FileRead(file);
                int beganIndex = contents.IndexOf("Processing begun on");
                if (beganIndex != -1)
                {
                    contents = contents.Substring(beganIndex + 41);
                    string timeStr = contents.Substring(0, 8);
                    string[] tokens = timeStr.Split(':');
                    string seconds = tokens[2], minutes = tokens[1], hours = tokens[0];

                    int currTime = int.Parse(seconds);
                    currTime += 60 * int.Parse(minutes);
                    currTime += 3600 * int.Parse(hours);
                    contents = contents.Substring(contents.IndexOf("(newline when done):") + 21);

                    string name = contents.Substring(0, contents.IndexOf('\n'));
                    if (name == repName && Math.Abs(time - currTime) <= 1)
                        return i;
                }
            }
            return -1;
        }

        public RepgenRunResult FileRun(Symitar.File file, FileRunStatus callStatus, FileRunPrompt callPrompt, int queue)
        {
            if (file.Type != Symitar.FileType.RepGen)
                throw new InvalidOperationException("Cannot run a " + file.FileTypeString() + " file");

            ISymCommand cmd;
            callStatus(0, "Initializing...");

            _socket.Write("mm0\u001B");
            cmd = _socket.ReadCommand();
            while (cmd.Command != "Input")
                cmd = _socket.ReadCommand();
            callStatus(1, "Writing Commands...");

            _socket.Write("1\r");
            cmd = _socket.ReadCommand();
            while (cmd.Command != "Input")
                cmd = _socket.ReadCommand();
            callStatus(2, "Writing Commands...");

            _socket.Write("11\r");
            cmd = _socket.ReadCommand();
            while (cmd.Command != "Input")
                cmd = _socket.ReadCommand();
            callStatus(3, "Writing Commands...");

            _socket.Write(file.Name + "\r");
            bool erroredOut = false;
            while (true)
            {
                cmd = _socket.ReadCommand();

                if ((cmd.Command == "Input") && (cmd.Get("HelpCode") == "20301"))
                    break;
                if (cmd.Command == "Input")
                {
                    callStatus(4, "Please Enter Prompts");

                    string result = callPrompt(cmd.Get("Prompt"));
                    if (result == null) //cancelled
                    {
                        _socket.Write("\u001B");
                        cmd = _socket.ReadCommand();
                        while (cmd.Command != "Input")
                            cmd = _socket.ReadCommand();
                        return RepgenRunResult.Cancelled();
                    }
                    else
                        _socket.Write(result.Trim() + '\r');
                }
                else if (cmd.Command == "Bell")
                    callStatus(4, "Invalid Prompt Input, Please Re-Enter");
                else if ((cmd.Command == "Batch") && (cmd.Get("Text") == "No such file or directory"))
                {
                    cmd = _socket.ReadCommand();
                    while (cmd.Command != "Input")
                        cmd = _socket.ReadCommand();
                    return RepgenRunResult.FileNotFound();
                }
                else if (cmd.Command == "SpecfileErr")
                    erroredOut = true;
                else if (erroredOut && (cmd.Command == "Batch") && (cmd.Get("Action") == "DisplayLine"))
                {
                    string err = cmd.Get("Text");
                    cmd = _socket.ReadCommand();
                    while (cmd.Command != "Input")
                        cmd = _socket.ReadCommand();
                    return RepgenRunResult.Error(err);
                }
                else if ((cmd.Command == "Batch") && (cmd.Get("Action") == "DisplayLine"))
                    callStatus(5, cmd.Get("Text"));
            }

            _socket.Write("\r");
            cmd = _socket.ReadCommand();
            while (cmd.Command != "Input")
                cmd = _socket.ReadCommand();

            callStatus(6, "Getting Queue List");
            _socket.Write("0\r");
            cmd = _socket.ReadCommand();
            Dictionary<int, int> queAvailable = new Dictionary<int, int>();
            while (cmd.Command != "Input")
            {
                if ((cmd.Get("Action") == "DisplayLine") && (cmd.Get("Text").Contains("Batch Queues Available:")))
                {
                    string line = cmd.Get("Text");
                    string[] strQueues = line.Substring(line.IndexOf(':') + 1).Split(new char[] { ',' });
                    for (int i = 0; i < strQueues.Length; i++)
                    {
                        strQueues[i] = strQueues[i].Trim();
                        if (strQueues[i].Contains("-"))
                        {
                            int pos = strQueues[i].IndexOf('-');
                            int start = int.Parse(strQueues[i].Substring(0, pos));
                            int end = int.Parse(strQueues[i].Substring(pos + 1));
                            for (int c = start; c <= end; c++)
                                queAvailable.Add(c, 0);
                        }
                        else
                            queAvailable.Add(int.Parse(strQueues[i]), 0);
                    }
                }
                cmd = _socket.ReadCommand();
            }

            if (queue < 0)
                queue = GetQueue(callStatus);

            _socket.Write(queue.ToString() + "\r");
            cmd = _socket.ReadCommand();
            while (cmd.Command != "Input")
                cmd = _socket.ReadCommand();

            callStatus(8, "Getting Sequence Numbers");
            _socket.Write("1\r");
            cmd = _socket.ReadCommand();
            while (cmd.Command != "Input")
                cmd = _socket.ReadCommand();

            cmd = new SymCommand("Misc");
            cmd.Set("InfoType", "BatchQueues");
            _socket.Write(cmd);

            int newestTime = 0;
            int sequenceNo = -1;
            cmd = _socket.ReadCommand();
            while (!cmd.HasParameter("Done"))
            {
                if (cmd.Get("Action") == "QueueEntry")
                {
                    int currTime = 0;
                    string timeStr = cmd.Get("Time");
                    currTime = int.Parse(timeStr.Substring(timeStr.LastIndexOf(':') + 1));
                    currTime += 60 * int.Parse(timeStr.Substring(timeStr.IndexOf(':') + 1, 2));
                    currTime += 3600 * int.Parse(timeStr.Substring(0, timeStr.IndexOf(':')));
                    if (currTime >= newestTime)
                    {
                        newestTime = currTime;
                        sequenceNo = int.Parse(cmd.Get("Seq"));
                    }
                }
                cmd = _socket.ReadCommand();
            }

            callStatus(9, "Running..");
            return RepgenRunResult.Okay(sequenceNo, newestTime);
        }
    }
}
