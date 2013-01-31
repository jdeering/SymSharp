using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                Symitar.File file = new Symitar.File(_socket.Server, SymDirectory.ToString(), i.ToString(), Symitar.FileType.Report, DateTime.Now, 0);
                string contents = FileRead(file);
                contents = contents.Substring(contents.IndexOf("Name of Posting: ") + 17);
                if (contents.StartsWith(title))
                    return i;
            }
            return -1;
        }
        //------------------------------------------------------------------------
        public ReportInfo FMRun(string inpTitle, FileMaintenanceType fmtype, FileRun_Status callStatus, int queue)
        {
            callStatus(1, "Initializing...");
            ISymCommand cmd;
            string outTitle = "PwrIDE FM - " + new Random().Next(8388608).ToString("D7");

            _socket.Write("mm0\u001B");
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            callStatus(2, "Writing Commands...");
            _socket.Write("1\r");
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            _socket.Write("24\r"); //Misc. Processing
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            _socket.Write("5\r"); //Batch FM
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            _socket.Write(((int)fmtype).ToString() + "\r"); //FM File Type
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            _socket.Write("0\r"); //Undo a Posting? (NO)
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            _socket.Write(inpTitle + "\r"); //Title of Batch Report Output to Use as FM Script
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            _socket.Write("1\r"); //Number of Search Days? (1)
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            if (fmtype == FileMaintenanceType.Account)
            {
                _socket.Write("1\r"); //Record FM History (YES)
                cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();
            }

            _socket.Write(outTitle + "\r"); //Name of Posting (needed to lookup later)
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            _socket.Write("1\r"); //Produce Empty Report If No Exceptions? (YES)
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            _socket.Write("0\r"); //Batch Options? (NO)

            //get queues
            callStatus(4, "Getting Queue List");
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

            //get queue counts
            callStatus(5, "Getting Queue Counts");
            cmd = new SymCommand("Misc");
            cmd.Set("InfoType", "BatchQueues");
            _socket.Write(cmd);

            cmd = _socket.ReadCommand();
            while (!cmd.HasParameter("Done"))
            {
                if ((cmd.Get("Action") == "QueueEntry") && (cmd.Get("Stat") == "Running"))
                    queAvailable[int.Parse(cmd.Get("Queue"))]++;
                cmd = _socket.ReadCommand();
            }

            if (queue == -1) //auto select lowest pending queue, or last available Zero queue
            {
                queue = 0;
                foreach (KeyValuePair<int, int> Q in queAvailable)
                    if (Q.Value <= queAvailable[queue])
                        queue = Q.Key;
            }

            callStatus(7, "Writing Final Commands");
            _socket.Write(queue.ToString() + "\r"); //write queue
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            _socket.Write("1\r"); //Okay (to Proceed)? (YES)
            cmd = _socket.ReadCommand(); while (cmd.Command != "Input") cmd = _socket.ReadCommand();

            //get queues again
            callStatus(8, "Finding FM Sequence");
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
            return new ReportInfo(sequenceNo, outTitle);
        }
    }
}
