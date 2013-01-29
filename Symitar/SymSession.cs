using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Symitar
{
    public class SymSession
    {
        private SymSocket _socket;

        public int SymDirectory { get; set; }

        private string _error;
        public string Error
        {
            get { return _error; }
        }

        private bool _loggedIn;
        public bool LoggedIn
        {
            get { return _loggedIn; }
        }
        
        public bool Connect(string server, int port)
        {
            _socket = new SymSocket(server, port);
            return _socket.Connect();
        }

        public void Disconnect()
        {
            _socket.Disconnect();
        }

        public bool LoginTest(string username, string password)
        {
            _loggedIn = false;
            if(!_socket.Connected)
            {
                _error = "Socket not connected";
                return false;
            }
      
            //Telnet Handshake
            try
            {
                _socket.Write(new byte[] {0xFF, 0xFB, 0x18} , 1000);
                _socket.Write(new byte[] { 0xFF, 0xFA, 0x18, 0x00, 0x61, 0x69, 0x78, 0x74, 0x65, 0x72, 0x6D, 0xFF, 0xF0 }, 1000);
                _socket.Write(new byte[] { 0xFF, 0xFD, 0x01 }, 1000);
                _socket.Write(new byte[] { 0xFF, 0xFD, 0x03, 0xFF, 0xFC, 0x1F, 0xFF, 0xFC, 0x01 }, 1000);
            }
            catch(Exception ex)
            {
                _error = "Connection failed";
                Disconnect();
                return false;
            }
      
            //AIX Login
            try
            {
                string stat;
                byte[] data;

                _socket.Write(username+'\r', 1000);

                data = _socket.ReadUntil(new List<string>{ "Password:", "[c" }, 1000);
                stat = Utilities.DecodeString(data);

                if(stat.IndexOf("[c") == -1)
                {
                    if(stat.IndexOf("invalid login") != -1)
                    {
                        _error = "Invalid AIX Login";
                        return false;
                    }

                    _socket.Write(password + '\r', 1000);
                    data = _socket.ReadUntil(":", 1000);
                    stat = Utilities.DecodeString(data);

                    if(stat.IndexOf("invalid login") != -1)
                    {
                        _error = "Invalid AIX Login";
                        return false;
                    }

                    _socket.ReadUntil("[c", 1000);
                }
            }
            catch(Exception ex)
            {
                return false;
            }

            _loggedIn = true;
            return _loggedIn;
        }

        public bool Login(string aixUsername, string aixPassword, int directory, string symUserId)
        {
            return Login(aixUsername, aixPassword, directory, symUserId, 0);
        }

        public bool Login(string aixUsername, string aixPassword, int directory, string symUserId, int stage)
        {
            if (!_socket.Connected)
            {
                _error = "Socket not connected";
                return false;
            }

            _error = "";
            SymDirectory = directory;


            //Telnet Handshake
            try
            {
                _socket.Write(new byte[] { 0xFF, 0xFB, 0x18 }, 1000);
                _socket.Write(new byte[] { 0xFF, 0xFA, 0x18, 0x00, 0x61, 0x69, 0x78, 0x74, 0x65, 0x72, 0x6D, 0xFF, 0xF0 }, 1000);
                _socket.Write(new byte[] { 0xFF, 0xFD, 0x01 }, 1000);
                _socket.Write(new byte[] { 0xFF, 0xFD, 0x03, 0xFF, 0xFC, 0x1F, 0xFF, 0xFC, 0x01 }, 1000);
            }
            catch (Exception ex)
            {
                _error = "Telnet communication failed: " + ex.Message;
                Disconnect();
                return false;
            }

            if (stage < 2)
            {
                if (!AixLogin(aixUsername, aixPassword))
                    return false;

                if (!SymLogin(SymDirectory, symUserId))
                    return false;
            }

            try
            {
                _socket.KeepAliveStart();
            }
            catch (Exception)
            {
                return false;
            }
            _loggedIn = true;
            return true;
        }

        private bool AixLogin(string username, string password)
        {
            //AIX Login
            try
            {
                string stat;
                byte[] data;

                _socket.Write(username + '\r', 1000);

                data = _socket.ReadUntil(new List<string> { "Password:", "[c" }, 1000);
                stat = Utilities.DecodeString(data);

                if (stat.IndexOf("[c") == -1)
                {
                    if (stat.IndexOf("invalid login") != -1)
                    {
                        _error = "Invalid AIX Login";
                        return false;
                    }

                    _socket.Write(password + '\r', 1000);
                    data = _socket.ReadUntil(":", 1000);
                    stat = Utilities.DecodeString(data);

                    if (stat.IndexOf("invalid login") != -1)
                    {
                        _error = "Invalid AIX Login";
                        return false;
                    }

                    _socket.ReadUntil("[c", 1000);
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            
            return true;
        }

        private bool SymLogin(int symDir, string userPassword)
        {
            _socket.Write("WINDOWSLEVEL=3\n", 1000);
            _socket.ReadUntil("$ ", 1000);
            _socket.Write(
                String.Format("sym {0}\r", symDir), 
                1000);

            SymCommand cmd = _socket.ReadCommand(2000);

            while (cmd.Command != "Input")
            {
                if (cmd.Command == "SymLogonError" && cmd.GetParam("Text").IndexOf("Too Many Invalid Password Attempts") > -1)
                {
                    _error = "Too Many Invalid Password Attempts";
                    return false;
                }

                cmd = _socket.ReadCommand(2000);
                if ((cmd.Command == "Input") && (cmd.GetParam("HelpCode") == "10025"))
                {
                    _socket.Write("$WinHostSync$\r");
                    cmd = _socket.ReadCommand(2000);
                }
            }

            _socket.Write(String.Format("{0}\r", symDir), 1000);
            cmd = _socket.ReadCommand(2000);
            if (cmd.Command == "SymLogonInvalidUser")
            {
                _error = "Invalid Sym User";
                _socket.Write("\r", 1000);
                _socket.ReadCommand(2000);
                return false;
            }
            if (cmd.Command == "SymLogonError" && cmd.GetParam("Text").IndexOf("Too Many Invalid Password Attempts") > -1)
            {
                _error = "Too Many Invalid Password Attempts";
                return false;
            }

            _socket.Write("\r", 1000); _socket.ReadCommand(2000);
            _socket.Write("\r", 1000); _socket.ReadCommand(2000);

            return true;
        }

        //========================================================================
        public List<Symitar.File> FileList(string pattern, Symitar.FileType type)
        {
        List<Symitar.File> files = new List<Symitar.File>();
      
        SymCommand cmd = new SymCommand("File");
        cmd.Set("Type", Utilities.FileTypeString(type));
        cmd.Set("Name", pattern);
        cmd.Set("Action", "List");
        _socket.Write(cmd);
      
        while(true)
        {
            cmd = _socket.ReadCommand(2000);
        if(cmd.HasParameter("Status"))
        break;
        if(cmd.HasParameter("Name"))
            files.Add(new Symitar.File(_socket.Server, SymDirectory.ToString(), cmd.GetParam("Name"), type, cmd.GetParam("Date"), cmd.GetParam("Time"), int.Parse(cmd.GetParam("Size"))));
        if(cmd.HasParameter("Done"))
        break;
        }
        return files;
        }
        //========================================================================
        public bool FileExists(Symitar.File file)
        {
        return (FileList(file.Name, file.Type).Count > 0);
        }
        //------------------------------------------------------------------------
        public bool FileExists(string filename, Symitar.FileType type)
        {
        return (FileList(filename, type).Count > 0);
        }
        //========================================================================
        public Symitar.File FileGet(string filename, Symitar.FileType type)
        {
        List<Symitar.File> files = FileList(filename, type);
        if(files.Count < 1)
        throw new FileNotFoundException("File \""+filename+"\" Not Found");
        return files[0];
        }
        //========================================================================
        public void FileRename(string oldName, Symitar.FileType type, string newName)
        {
        SymCommand cmd = new SymCommand("File");
        cmd.Set("Action" , "Rename");
        cmd.Set("Type"   , Utilities.FileTypeString(type));
        cmd.Set("Name"   , oldName);
        cmd.Set("NewName", newName);
        _socket.Write(cmd);

        cmd = _socket.ReadCommand(2000);
        if(cmd.HasParameter("Status"))
        {
        if(cmd.GetParam("Status").IndexOf("No such file or directory") != -1)
        throw new FileNotFoundException("File \""+oldName+"\" Not Found");
        else
        throw new Exception("Filename Too Long");
        }
        else if(cmd.HasParameter("Done"))
        return;
        else
        throw new Exception("Unknown Renaming Error");
        }
        //------------------------------------------------------------------------
        public void FileRename(Symitar.File file, string newName) { FileRename(file.Name, file.Type, newName); }
        //========================================================================
        public void FileDelete(string name, Symitar.FileType type)
        {
        SymCommand cmd = new SymCommand("File");
        cmd.Set("Action", "Delete");
        cmd.Set("Type", Utilities.FileTypeString(type));
        cmd.Set("Name"  , name);
        _socket.Write(cmd);

        cmd = _socket.ReadCommand(2000);
        if(cmd.HasParameter("Status"))
        {
        if(cmd.GetParam("Status").IndexOf("No such file or directory") != -1)
        throw new FileNotFoundException("File \""+name+"\" Not Found");
        else
        throw new Exception("Filename Too Long");
        }
        else if(cmd.HasParameter("Done"))
        return;
        else
        throw new Exception("Unknown Deletion Error");
        }
        //------------------------------------------------------------------------
        public void FileDelete(Symitar.File file) { FileDelete(file.Name, file.Type); }
        //========================================================================
        public string FileRead(string name, Symitar.FileType type)
        {
        StringBuilder content = new StringBuilder();
      
        SymCommand cmd = new SymCommand("File");
        cmd.Set("Action", "Retrieve");
        cmd.Set("Type", Utilities.FileTypeString(type));
        cmd.Set("Name"  , name);
        _socket.Write(cmd);
      
        while(true)
        {
            cmd = _socket.ReadCommand();
        if(cmd.HasParameter("Status"))
        {
        if(cmd.GetParam("Status").IndexOf("No such file or directory") != -1)
        throw new FileNotFoundException("File \""+name+"\" Not Found");
        else if(cmd.GetParam("Status").IndexOf("Cannot view a blank report") != -1)
        return "";
        else
        throw new Exception("Filename Too Long");
        }

        string chunk = cmd.GetFileData();
        if(chunk.Length > 0)
        {
        content.Append(chunk);
        if(type==Symitar.FileType.Report)
        content.Append('\n');
        }

        if(cmd.HasParameter("Done"))
        break;
        }
        return content.ToString();
        }
        //------------------------------------------------------------------------
        public string FileRead(Symitar.File file) { return FileRead(file.Name, file.Type); }
        //========================================================================
        public void FileWrite(string name, Symitar.FileType type, string content)
        {
        int chunkMax = 1024;
      
        SymCommand cmd = new SymCommand("File");
        cmd.Set("Action", "Store");
        cmd.Set("Type"  , Utilities.FileTypeString(type));
        cmd.Set("Name"  , name);
        _socket.WakeUp();
        _socket.Write(cmd);

        cmd = _socket.ReadCommand();
        int wtf_is_this = 0;
        while(cmd.Data.IndexOf("BadCharList") == -1)
        {
            cmd = _socket.ReadCommand();
        wtf_is_this++;
        if(wtf_is_this > 5)
        throw new Exception("Null Pointer");
        }
      
        if(cmd.Data.IndexOf("MaxBuff") > -1)
        chunkMax = int.Parse(cmd.GetParam("MaxBuff"));
        if(content.Length > (999*chunkMax))
        throw new Exception("File Too Large");
      
        if(cmd.GetParam("Status").IndexOf("Filename is too long") != -1)
        throw new Exception("Filename Too Long");
      
        string[] badChars = cmd.GetParam("BadCharList").Split(new char[] { ',' });
        for(int i=0; i<badChars.Length; i++)
        content = content.Replace(((char)int.Parse(badChars[i]))+"", "");
      
        int sent=0, block=0; string blockStr; byte[] resp;
        while(sent < content.Length)
        {
        int chunkSize = (content.Length - sent);
        if(chunkSize > chunkMax)
        chunkSize = chunkMax;
        string chunk = content.Substring(sent, chunkSize);
        string chunkStr = chunkSize.ToString("D5");
        blockStr = block.ToString("D3");
        
        resp = new byte[]{0x4E,0x4E,0x4E,0x4E,0x4E,0x4E,0x4E,0x4E,0x4E,0x4E,0x4E};
        while(resp[7] == 0x4E)
        {
            _socket.Write("PROT" + blockStr + "DATA" + chunkStr);
            _socket.Write(chunk);
            resp = _socket.Read(16);
        }

        block++;
        sent += chunkSize;
        }
      
        blockStr = block.ToString("D3");
        _socket.Write("PROT" + blockStr + "EOF\u0020\u0020\u0020\u0020\u0020\u0020");
        resp = _socket.Read(16);

        cmd = _socket.ReadCommand();
        _socket.WakeUp();
        }
        //------------------------------------------------------------------------
        public void FileWrite(Symitar.File file, string content) { FileWrite(file.Name, file.Type, content); }
        //========================================================================
        public SpecfileError FileCheck(Symitar.File file)
        {  
        if(file.Type != Symitar.FileType.RepGen)
        throw new Exception("Cannot Check a "+file.FileTypeString()+" File");

        _socket.Write("mm3\u001B"); _socket.ReadCommand();
        _socket.Write("7\r"); _socket.ReadCommand(); _socket.ReadCommand();
        _socket.Write(file.Name + '\r');

        SymCommand cmd = _socket.ReadCommand();
        if(cmd.HasParameter("Warning") || cmd.HasParameter("Error"))
        {
            _socket.ReadCommand();
        throw new Exception("File \""+file.Name+"\" Not Found");
        }
        if(cmd.GetParam("Action")=="NoError")
        {
            _socket.ReadCommand();
        return SpecfileError.None();
        }
      
        int errRow=0, errCol=0;
        string errFile="", errText="";
        if(cmd.GetParam("Action")=="Init")
        {
        errFile = cmd.GetParam("FileName");
        cmd = _socket.ReadCommand();
        while(cmd.GetParam("Action")!="DisplayEdit")
        {
        if(cmd.GetParam("Action")=="FileInfo")
        {
        errRow = int.Parse(cmd.GetParam("Line").Replace(",", ""));
        errCol = int.Parse(cmd.GetParam("Col" ).Replace(",", ""));
        }
        else if(cmd.GetParam("Action")=="ErrText")
        errText += cmd.GetParam("Line")+" ";
        cmd = _socket.ReadCommand();
        }
        _socket.ReadCommand();

        return new SpecfileError(file, errFile, errText, errRow, errCol);
        }
      
        throw new Exception("Unknown Checking Error");
        }
        //========================================================================
        public SpecfileError FileInstall(Symitar.File file)
        {
        if(file.Type != Symitar.FileType.RepGen)
        throw new Exception("Cannot Install a "+file.FileTypeString()+" File");

        _socket.Write("mm3\u001B"); _socket.ReadCommand();
        _socket.Write("8\r"); _socket.ReadCommand(); _socket.ReadCommand();
        _socket.Write(file.Name + '\r');

        SymCommand cmd = _socket.ReadCommand();
        if(cmd.HasParameter("Warning") || cmd.HasParameter("Error"))
        {
            _socket.ReadCommand();
        throw new Exception("File \""+file.Name+"\" Not Found");
        }

        if(cmd.Command=="SpecfileData")
        {
            _socket.ReadCommand();
            _socket.Write("1\r");
            _socket.ReadCommand(); _socket.ReadCommand();
        return SpecfileError.None(int.Parse(cmd.GetParam("Size").Replace(",","")));
        }

        int errRow = 0, errCol = 0;
        string errFile = "", errText = "";
        if (cmd.GetParam("Action") == "Init")
        {
        errFile = cmd.GetParam("FileName");
        cmd = _socket.ReadCommand();
        while (cmd.GetParam("Action") != "DisplayEdit")
        {
        if (cmd.GetParam("Action") == "FileInfo")
        {
        errRow = int.Parse(cmd.GetParam("Line").Replace(",", ""));
        errCol = int.Parse(cmd.GetParam("Col").Replace(",", ""));
        }
        else if (cmd.GetParam("Action") == "ErrText")
        errText += cmd.GetParam("Line") + " ";
        cmd = _socket.ReadCommand();
        }
        _socket.ReadCommand();

        return new SpecfileError(file, errFile, errText, errRow, errCol);
        }
      
        throw new Exception("Unknown Install Error");
        }
        //========================================================================
        // Report Running Stuff
        //========================================================================
        public delegate void   FileRun_Status(int code, string description);
        public delegate string FileRun_Prompt(string prompt);
        //------------------------------------------------------------------------
        public bool IsFileRunning(int sequence)
        {
        SymCommand cmd;
        bool running = false;

        cmd = new SymCommand("Misc");
        cmd.Set("InfoType", "BatchQueues");
        _socket.Write(cmd);

        cmd = _socket.ReadCommand();
        while(!cmd.HasParameter("Done"))
        {
        if((cmd.GetParam("Action")=="QueueEntry") && (int.Parse(cmd.GetParam("Seq")) == sequence))
        running = true;
        cmd = _socket.ReadCommand();
        }

        return running;
        }
        //------------------------------------------------------------------------
        private List<int> GetPrintSequences(string which)
        {
        List<int> seqs = new List<int>();
        SymCommand cmd;

        cmd = new SymCommand("File");
        cmd.Set("Action"  , "List");
        cmd.Set("MaxCount", "50");
        cmd.Set("Query"   , "LAST 20 \"+"+which+"+\"");
        cmd.Set("Type"    , "Report");
        _socket.Write(cmd);

        cmd = _socket.ReadCommand();
        while(!cmd.HasParameter("Done"))
        {
        if(cmd.HasParameter("Sequence"))
        seqs.Add(int.Parse(cmd.GetParam("Sequence")));
        cmd = _socket.ReadCommand();
        }

        seqs.Sort();
        seqs.Reverse();
        return seqs;
        }
        //------------------------------------------------------------------------
        public int GetReportSequence(string repName, int time)
        {
        List<int> seqs = GetPrintSequences("REPWRITER");
        foreach(int i in seqs)
        {
        Symitar.File file = new Symitar.File(_socket.Server, SymDirectory.ToString(), i.ToString(), Symitar.FileType.Report, DateTime.Now, 0);
        string contents = FileRead(file);
        int beganIndex = contents.IndexOf("Processing begun on");
        if(beganIndex != -1)
        {
        contents = contents.Substring(beganIndex+41);
        string timeStr = contents.Substring(0, 8);
        int currTime =     int.Parse(timeStr.Substring(timeStr.LastIndexOf(':')+1));
        currTime +=   60 * int.Parse(timeStr.Substring(timeStr.IndexOf(':')+1, 2));
        currTime += 3600 * int.Parse(timeStr.Substring(0, timeStr.IndexOf(':')));
        contents = contents.Substring(contents.IndexOf("(newline when done):") + 21);

        string name = contents.Substring(0, contents.IndexOf('\n'));
        if(name == repName)
        if((time+1==currTime) || (time==currTime) || (time-1==currTime))
        return i;
        }
        }
        return -1;
        }
        //------------------------------------------------------------------------
        public RepgenRunResult FileRun(Symitar.File file, FileRun_Status callStatus, FileRun_Prompt callPrompt, int queue)
        {
        if (file.Type != Symitar.FileType.RepGen)
        throw new Exception("Cannot Run a " + file.FileTypeString() + " File");

        SymCommand cmd;
        callStatus(0,"Initializing...");
      
        _socket.Write("mm0\u001B");
        cmd = _socket.ReadCommand();
        while(cmd.Command != "Input")
        cmd = _socket.ReadCommand();
        callStatus(1,"Writing Commands...");

        _socket.Write("1\r");
        cmd = _socket.ReadCommand();
        while(cmd.Command != "Input")
        cmd = _socket.ReadCommand();
        callStatus(2,"Writing Commands...");

        _socket.Write("11\r");
        cmd = _socket.ReadCommand();
        while(cmd.Command != "Input")
        cmd = _socket.ReadCommand();
        callStatus(3,"Writing Commands...");

        _socket.Write(file.Name + "\r");
        bool erroredOut = false;
        while(true)
        {
        cmd = _socket.ReadCommand();

        if((cmd.Command == "Input") && (cmd.GetParam("HelpCode")=="20301"))
        break;
        if(cmd.Command == "Input")
        {
        callStatus(4,"Please Enter Prompts");

        string result = callPrompt(cmd.GetParam("Prompt"));
        if(result == null) //cancelled
        {
        _socket.Write("\u001B");
        cmd = _socket.ReadCommand();
        while(cmd.Command != "Input")
        cmd = _socket.ReadCommand();
        return RepgenRunResult.Cancelled();
        }
        else
        _socket.Write(result.Trim()+'\r');
        }
        else if(cmd.Command == "Bell")
        callStatus(4, "Invalid Prompt Input, Please Re-Enter");
        else if((cmd.Command == "Batch") && (cmd.GetParam("Text")=="No such file or directory"))
        {
        cmd = _socket.ReadCommand();
        while(cmd.Command != "Input")
        cmd = _socket.ReadCommand();
        return RepgenRunResult.FileNotFound();
        }
        else if(cmd.Command == "SpecfileErr")
        erroredOut = true;
        else if(erroredOut && (cmd.Command == "Batch") && (cmd.GetParam("Action") == "DisplayLine"))
        {
        string err = cmd.GetParam("Text");
        cmd = _socket.ReadCommand();
        while (cmd.Command != "Input")
        cmd = _socket.ReadCommand();
        return RepgenRunResult.Error(err);
        }
        else if((cmd.Command == "Batch") && (cmd.GetParam("Action") == "DisplayLine"))
        callStatus(5, cmd.GetParam("Text"));
        }
      
        _socket.Write("\r");
        cmd = _socket.ReadCommand();
        while(cmd.Command != "Input")
        cmd = _socket.ReadCommand();
      
        callStatus(6, "Getting Queue List");
        _socket.Write("0\r");
        cmd = _socket.ReadCommand();
        Dictionary<int,int> queAvailable = new Dictionary<int,int>();
        while(cmd.Command != "Input")
        {
        if((cmd.GetParam("Action") == "DisplayLine") && (cmd.GetParam("Text").Contains("Batch Queues Available:")))
        {
        string line = cmd.GetParam("Text");
        string[] strQueues = line.Substring(line.IndexOf(':')+1).Split(new char[]{','});
        for(int i=0; i<strQueues.Length; i++)
        {
        strQueues[i] = strQueues[i].Trim();
        if(strQueues[i].Contains("-"))
        {
        int pos = strQueues[i].IndexOf('-');
        int start = int.Parse(strQueues[i].Substring(0,pos));
        int end   = int.Parse(strQueues[i].Substring(pos+1));
        for(int c=start; c<=end; c++)
        queAvailable.Add(c,0);
        }
        else
        queAvailable.Add(int.Parse(strQueues[i]),0);
        }
        }
        cmd = _socket.ReadCommand();
        }
      
        callStatus(7, "Getting Queue Counts");
        cmd = new SymCommand("Misc");
        cmd.Set("InfoType", "BatchQueues");
        _socket.Write(cmd);
      
        cmd = _socket.ReadCommand();
        while(!cmd.HasParameter("Done"))
        {
        if((cmd.GetParam("Action") == "QueueEntry") && (cmd.GetParam("Stat") == "Running"))
        queAvailable[int.Parse(cmd.GetParam("Queue"))]++;
        cmd = _socket.ReadCommand();
        }
      
        if(queue == -1) //auto select lowest pending queue, or last available Zero queue
        {
        queue = 0;
        foreach(KeyValuePair<int, int> Q in queAvailable)
        if(Q.Value <= queAvailable[queue])
        queue = Q.Key;
        }
      
        _socket.Write(queue.ToString()+"\r");
        cmd = _socket.ReadCommand();
        while(cmd.Command != "Input")
        cmd = _socket.ReadCommand();
      
        callStatus(8, "Getting Sequence Numbers");
        _socket.Write("1\r");
        cmd = _socket.ReadCommand();
        while(cmd.Command != "Input")
        cmd = _socket.ReadCommand();
      
        cmd = new SymCommand("Misc");
        cmd.Set("InfoType", "BatchQueues");
        _socket.Write(cmd);
      
        int newestTime = 0;
        int sequenceNo = -1;
        cmd = _socket.ReadCommand();
        while(!cmd.HasParameter("Done"))
        {
        if(cmd.GetParam("Action") == "QueueEntry")
        {
        int currTime = 0;
        string timeStr = cmd.GetParam("Time");
        currTime =         int.Parse(timeStr.Substring(timeStr.LastIndexOf(':')+1));
        currTime +=   60 * int.Parse(timeStr.Substring(timeStr.IndexOf(':')+1, 2));
        currTime += 3600 * int.Parse(timeStr.Substring(0, timeStr.IndexOf(':')));
        if(currTime >= newestTime)
        {
        newestTime = currTime;
        sequenceNo = int.Parse(cmd.GetParam("Seq"));
        }
        }
        cmd = _socket.ReadCommand();
        }
      
        callStatus(9, "Running..");
        return RepgenRunResult.Okay(sequenceNo, newestTime);
        }
        //========================================================================
        // FM Running Stuff
        //========================================================================
        public int GetFMSequence(string title)
        {
        List<int> seqs = GetPrintSequences("MISCFMPOST");
        foreach(int i in seqs)
        {
            Symitar.File file = new Symitar.File(_socket.Server, SymDirectory.ToString(), i.ToString(), Symitar.FileType.Report, DateTime.Now, 0);
        string contents = FileRead(file);
        contents = contents.Substring(contents.IndexOf("Name of Posting: ")+17);
        if(contents.StartsWith(title))
        return i;
        }
        return -1;
        }
        //------------------------------------------------------------------------
        public ReportInfo FMRun(string inpTitle, FileMaintenanceType fmtype, FileRun_Status callStatus, int queue)
        {
        callStatus(1,"Initializing...");
        SymCommand cmd;
        string outTitle = "PwrIDE FM - " + new Random().Next(8388608).ToString("D7");
      
        _socket.Write("mm0\u001B");
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();

        callStatus(2,"Writing Commands...");
        _socket.Write("1\r");
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();

        _socket.Write("24\r"); //Misc. Processing
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();
      
        _socket.Write("5\r"); //Batch FM
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();
      
        _socket.Write(((int)fmtype).ToString()+"\r"); //FM File Type
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();
      
        _socket.Write("0\r"); //Undo a Posting? (NO)
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();
      
        _socket.Write(inpTitle+"\r"); //Title of Batch Report Output to Use as FM Script
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();
      
        _socket.Write("1\r"); //Number of Search Days? (1)
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();
      
        if(fmtype == FileMaintenanceType.Account)
        {
        _socket.Write("1\r"); //Record FM History (YES)
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();
        }
      
        _socket.Write(outTitle+"\r"); //Name of Posting (needed to lookup later)
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();
      
        _socket.Write("1\r"); //Produce Empty Report If No Exceptions? (YES)
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();
      
        _socket.Write("0\r"); //Batch Options? (NO)

        //get queues
        callStatus(4, "Getting Queue List");
        cmd = _socket.ReadCommand();
        Dictionary<int,int> queAvailable = new Dictionary<int,int>();
        while(cmd.Command != "Input")
        {
        if((cmd.GetParam("Action") == "DisplayLine") && (cmd.GetParam("Text").Contains("Batch Queues Available:")))
        {
        string line = cmd.GetParam("Text");
        string[] strQueues = line.Substring(line.IndexOf(':')+1).Split(new char[]{','});
        for(int i=0; i<strQueues.Length; i++)
        {
        strQueues[i] = strQueues[i].Trim();
        if(strQueues[i].Contains("-"))
        {
        int pos = strQueues[i].IndexOf('-');
        int start = int.Parse(strQueues[i].Substring(0,pos));
        int end   = int.Parse(strQueues[i].Substring(pos+1));
        for(int c=start; c<=end; c++)
        queAvailable.Add(c,0);
        }
        else
        queAvailable.Add(int.Parse(strQueues[i]),0);
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
        while(!cmd.HasParameter("Done"))
        {
        if((cmd.GetParam("Action") == "QueueEntry") && (cmd.GetParam("Stat") == "Running"))
        queAvailable[int.Parse(cmd.GetParam("Queue"))]++;
        cmd = _socket.ReadCommand();
        }
      
        if(queue == -1) //auto select lowest pending queue, or last available Zero queue
        {
        queue = 0;
        foreach(KeyValuePair<int, int> Q in queAvailable)
        if(Q.Value <= queAvailable[queue])
        queue = Q.Key;
        }
      
        callStatus(7, "Writing Final Commands");
        _socket.Write(queue.ToString()+"\r"); //write queue
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();
      
        _socket.Write("1\r"); //Okay (to Proceed)? (YES)
        cmd=_socket.ReadCommand(); while(cmd.Command!="Input") cmd=_socket.ReadCommand();
      
        //get queues again
        callStatus(8, "Finding FM Sequence");
        cmd = new SymCommand("Misc");
        cmd.Set("InfoType", "BatchQueues");
        _socket.Write(cmd);
      
        int newestTime = 0;
        int sequenceNo = -1;
        cmd = _socket.ReadCommand();
        while(!cmd.HasParameter("Done"))
        {
        if(cmd.GetParam("Action") == "QueueEntry")
        {
        int currTime = 0;
        string timeStr = cmd.GetParam("Time");
        currTime =         int.Parse(timeStr.Substring(timeStr.LastIndexOf(':')+1));
        currTime +=   60 * int.Parse(timeStr.Substring(timeStr.IndexOf(':')+1, 2));
        currTime += 3600 * int.Parse(timeStr.Substring(0, timeStr.IndexOf(':')));
        if(currTime >= newestTime)
        {
        newestTime = currTime;
        sequenceNo = int.Parse(cmd.GetParam("Seq"));
        }
        }
        cmd = _socket.ReadCommand();
        }
      
        callStatus(9, "Running..");
        return new ReportInfo(sequenceNo, outTitle);
        }
        //========================================================================
    }
}
