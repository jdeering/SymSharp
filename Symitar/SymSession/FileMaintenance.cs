using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Symitar.Interfaces;

namespace Symitar
{
    public partial class SymSession
    {
        public int GetFileMaintenanceSequence(string title)
        {
            List<int> seqs = GetPrintSequences("MISCFMPOST");
            foreach (int i in seqs)
            {
                var file = new File(_socket.Server, SymDirectory.ToString(), i.ToString(), FileType.Report, DateTime.Now,
                                    0);
                string contents = FileRead(file);
                contents = contents.Substring(contents.IndexOf("Name of Posting: ") + 17);
                if (contents.StartsWith(title))
                    return i;
            }
            return -1;
        }

        public ReportInfo FMRun(string reportName, FileMaintenanceType fmtype, FileRunStatus callStatus, int queue, RunWorkerCompletedEventHandler Notify = null)
        {
            callStatus(RunState.Initializing, reportName);
            ISymCommand cmd;
            string outTitle = "FM - " + new Random().Next(8388608).ToString("D7");

            _socket.Write("mm0\u001B");
            cmd = WaitForCommand("Input");

            _socket.Write("1\r");
            cmd = WaitForCommand("Input");

            _socket.Write("24\r"); //Misc. Processing
            cmd = WaitForCommand("Input");

            _socket.Write("5\r"); //Batch FM
            cmd = WaitForCommand("Input");

            _socket.Write(((int) fmtype).ToString() + "\r"); //FM File Type
            cmd = WaitForCommand("Input");

            _socket.Write("0\r"); //Undo a Posting? (NO)
            cmd = WaitForCommand("Input");

            _socket.Write(reportName + "\r"); //Title of Batch Report Output to Use as FM Script
            cmd = WaitForCommand("Input");

            _socket.Write("1\r"); //Number of Search Days? (1)
            cmd = WaitForCommand("Input");

            if (fmtype == FileMaintenanceType.Account)
            {
                _socket.Write("1\r"); //Record FM History (YES)
                cmd = WaitForCommand("Input");
            }

            _socket.Write(outTitle + "\r"); //Name of Posting (needed to lookup later)
            cmd = WaitForCommand("Input");

            _socket.Write("1\r"); //Produce Empty Report If No Exceptions? (YES)
            cmd = WaitForCommand("Input");

            _socket.Write("0\r"); //Batch Options? (NO)

            if (queue < 0)
            {
                queue = GetOpenQueue(null);
            }

            _socket.Write(queue.ToString() + "\r"); //write queue
            cmd = WaitForCommand("Input");

            _socket.Write("1\r"); //Okay (to Proceed)? (YES)
            cmd = WaitForCommand("Input");

            //get queues again
            callStatus(RunState.Initializing, "Finding FM Sequence");
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

                    string[] tokens = timeStr.Split(':');
                    string seconds = tokens[2], minutes = tokens[1], hours = tokens[0];

                    currTime = int.Parse(seconds);
                    currTime += 60*int.Parse(minutes);
                    currTime += 3600*int.Parse(hours);

                    if (currTime >= newestTime)
                    {
                        newestTime = currTime;
                        sequenceNo = int.Parse(cmd.Get("Seq"));
                    }
                }
                cmd = _socket.ReadCommand();
            }

            callStatus(RunState.Running, sequenceNo);
            
            if (Notify != null)
            {
                var worker = new BackgroundWorker();

                worker.DoWork += (sender, eventArgs) =>
                {
                    while (IsFileRunning(sequenceNo))
                    {
                        Thread.Sleep(15000);
                    }

                    eventArgs.Result = GetFileMaintenanceSequence(reportName);
                };

                worker.RunWorkerCompleted += Notify;

                worker.RunWorkerAsync();
            }

            return new ReportInfo(sequenceNo, outTitle);
        }

        private ISymCommand WaitForCommand(string command)
        {
            while (true)
            {
                ISymCommand cmd = _socket.ReadCommand();
                if (cmd.Command == command)
                {
                    _socket.Clear();
                    return cmd;
                }
            }
        }

        private ISymCommand WaitForPrompt(string prompt)
        {
            while (true)
            {
                ISymCommand cmd = _socket.ReadCommand();
                if (cmd.Get("Prompt").Contains(prompt))
                {
                    _socket.Clear();
                    return cmd;
                }
            }
        }

        private int GetOpenQueue(Dictionary<int, int> availableQueues)
        {
            int queue;
            ISymCommand cmd = new SymCommand();

            if (availableQueues == null || availableQueues.Count == 0)
                availableQueues = GetQueueList(cmd);

            //get queue counts
            cmd = new SymCommand("Misc");
            cmd.Set("InfoType", "BatchQueues");
            _socket.Write(cmd);

            cmd = _socket.ReadCommand();
            while (!cmd.HasParameter("Done"))
            {
                if ((cmd.Get("Action") == "QueueEntry") && (cmd.Get("Stat") == "Running"))
                    availableQueues[int.Parse(cmd.Get("Queue"))]++;
                cmd = _socket.ReadCommand();
            }

            // Get queue with lowest pending
            int lowestPending = 0;
            queue = 0;
            foreach (var Q in availableQueues)
            {
                if (Q.Value == 0) return Q.Key;

                if (Q.Value < lowestPending)
                {
                    lowestPending = Q.Value;
                    queue = Q.Key;
                }
            }

            return queue;
        }

        private Dictionary<int, int> GetQueueList(ISymCommand cmd)
        {
            var availableQueues = new Dictionary<int, int>();

            while (!(cmd.Get("Action") == "DisplayLine" && cmd.Get("Text").Contains("Batch Queues Available:")))
            {
                cmd = _socket.ReadCommand();
            }
            string line = cmd.Get("Text");
            string[] strQueues = line.Substring(line.IndexOf(':') + 1).Split(new[] {','});
            for (int i = 0; i < strQueues.Length; i++)
            {
                strQueues[i] = strQueues[i].Trim();
                if (strQueues[i].Contains("-"))
                {
                    int pos = strQueues[i].IndexOf('-');
                    int start = int.Parse(strQueues[i].Substring(0, pos));
                    int end = int.Parse(strQueues[i].Substring(pos + 1));
                    for (int c = start; c <= end; c++)
                        availableQueues.Add(c, 0);
                }
                else
                    availableQueues.Add(int.Parse(strQueues[i]), 0);
            }

            return availableQueues;
        }
    }
}