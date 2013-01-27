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
    //========================================================================
    public struct FMRunNfo
    {
      public int    sequence;
      public string title;
      
      public FMRunNfo(int Sequence, string Title)
      {
        sequence = Sequence;
        title    = Title;
      }
    }
    //------------------------------------------------------------------------
    public enum FMType
    {
      Account            = 0,
      Inventory          = 1,
      Payee              = 2,
      GLAccount          = 3,
      ReceivedItem       = 4,
      Participant        = 5,
      Participation      = 6,
      Dealer             = 7,
      User               = 8,
      Collateral         = 9,
      Member             =10,
      MemberAddress      =11,
      NonAccountName     =12,
      FinancialReporting =13,
      Wire               =14
    }
    //========================================================================
    private TcpClient     socket;
    private NetworkStream stream;
    private Thread        keepAlive;
    private DateTime      lastActive;
    private bool          isKeepAlive;
    private Semaphore     sockSem;
    //------------------------------------------------------------------------
    public string server;
    public string sym;
    public string error;
    public bool   isConnected;
    public bool   isLoggedIn;
    public bool   inUse;
    public int    defaultTimeout    =  5000;
    public int    keepAliveInterval = 45000;
    //========================================================================
    public delegate void SessionDebugCallback(string str);
    public SessionDebugCallback debug=null;
    //========================================================================
    public SymSession()
    {
      socket = null;
      stream = null;
      keepAlive = null;
      isKeepAlive = false;
      server = sym = error = "";
      isConnected = isLoggedIn = inUse = false; 
      sockSem = new Semaphore(1,1);
    }
    //========================================================================
    private void SockLock(int timeout)
    {
      if(sockSem.WaitOne(timeout)==false)
        throw new Exception("Unable to Obtain Socket Lock");
      inUse = true;
    }
    //------------------------------------------------------------------------
    private void SockUnlock()
    {
      inUse=false;
      sockSem.Release();
    }
    //------------------------------------------------------------------------
    public bool LockTest()
    {
      try
      {
        SockLock(2500);
        SockUnlock();
      }
      catch(Exception)
      {
        return false;
      }
      return true;
    }
    //========================================================================
    private void Write(byte[] buff, int off, int size, int timeout)
    {
      SockLock(5000);
      int oto = stream.WriteTimeout;
      stream.WriteTimeout = timeout;
      stream.Write(buff, off, size);
      stream.WriteTimeout = oto;
      SockUnlock();
      lastActive = DateTime.Now;
      if(debug != null)
      {
        debug("WRITE: ");
        for(int i=0; i<size; i++) debug(buff[off+i].ToString("X2")+", ");
        debug("\n");
      }
    }
    //------------------------------------------------------------------------
    private void Write(byte[] buff, int timeout)    { Write(buff, 0, buff.Length,     timeout); }
    private void Write(string str , int timeout)    { Write(EncodeString(str),        timeout); }
    private void Write(SymCommand cmd, int timeout) { Write(cmd.ToString(),             timeout); }
    private void Write(byte[] buff)                 { Write(buff,              defaultTimeout); }
    private void Write(string str)                  { Write(EncodeString(str), defaultTimeout); }
    private void Write(SymCommand cmd)              { Write(cmd.ToString(),      defaultTimeout); }
    //========================================================================
    private void WakeUp() { try{Write(new SymCommand("WakeUp"), 1000);} catch(Exception){}  }
    //========================================================================
    private byte[] Read(int size, int timeout)
    {
      SockLock(5000);
      int oto = stream.ReadTimeout;
      stream.ReadTimeout = timeout;
      DateTime begin = DateTime.Now;
      
      int read=0;
      byte[] buff = new byte[size];
      while(read < size)
      {
        if((DateTime.Now - begin).Milliseconds > timeout)
        {
          SockUnlock();
          Disconnect();
          throw new Exception("Socket Read Timeout");
        }
        read += stream.Read(buff,read,size-read);
      }
      
      stream.ReadTimeout = oto;
      SockUnlock();
      lastActive = DateTime.Now;
      
      if(debug != null)
      {
        debug("READ: ");
        for(int i=0; i<buff.Length; i++) debug(buff[i].ToString("X2")+", ");
        debug("\n");
      }
      return buff;
    }
    //------------------------------------------------------------------------
    private byte[] Read(int size) { return Read(size, defaultTimeout); }
    //------------------------------------------------------------------------
    private byte[] ReadUntil(byte[] match, int timeout)
    {
      SockLock(5000);
      int oto = stream.ReadTimeout;
      stream.ReadTimeout = timeout;
      DateTime begin = DateTime.Now;

      bool hit = false;
      byte[] test = new byte[match.Length];
      List<byte> buff = new List<byte>();
      while(!hit)
      {
        if((DateTime.Now - begin).Milliseconds > timeout)
        {
          SockUnlock();
          Disconnect();
          throw new Exception("Socket Read Timeout");
        }
        for(int i=0; i<(test.Length-1); i++) test[i]=test[i+1];
        stream.Read(test,test.Length-1,1);
        buff.Add(test[test.Length-1]);
        hit = true;
        for(int c=0; c<test.Length; c++)
        {
          if(test[c] != match[c])
          {
            hit=false;
            break;
          }
        }
      }
      stream.ReadTimeout = oto;
      SockUnlock();
      lastActive = DateTime.Now;
      
      if(debug != null)
      {
        byte[] dbg = buff.ToArray();
        debug("READ: ");
        for(int i=0; i<dbg.Length; i++) debug(dbg[i].ToString("X2")+", ");
        debug("\n");
      }
      return buff.ToArray();
    }
    //------------------------------------------------------------------------
    private byte[] ReadUntil(string match, int timeout)       { return ReadUntil(EncodeString(match), timeout);  }
    private string ReadUntilString(byte[] match, int timeout) { return DecodeString(ReadUntil(match,  timeout)); }
    private string ReadUntilString(string match, int timeout) { return DecodeString(ReadUntil(match,  timeout)); }
    private byte[] ReadUntil(byte[] match)                    { return ReadUntil(match,        defaultTimeout);  }
    private byte[] ReadUntil(string match)                    { return ReadUntil(match,        defaultTimeout);  }
    private string ReadUntilString(byte[] match)              { return ReadUntilString(match,  defaultTimeout);  }
    private string ReadUntilString(string match)              { return ReadUntilString(match,  defaultTimeout);  }
    //========================================================================
    private SymCommand ReadCommand(int timeout)
    {
      ReadUntil(new byte[] {0x1B, 0xFE}, timeout);
      string data = ReadUntilString(new byte[] {0xFC}, timeout);
      
      SymCommand cmd = SymCommand.Parse(data.Substring(0, data.Length-1));
      if((cmd.Command == "MsgDlg") && (cmd.HasParameter("Text")))
        if(cmd.Parameters["Text"].IndexOf("From PID") != -1)
          cmd = ReadCommand(timeout);
      return cmd;
    }
    //------------------------------------------------------------------------
    private SymCommand ReadCommand(){ return ReadCommand(defaultTimeout); }
    //========================================================================
    private byte[] ReadUntilOr(byte[] matchA, byte[] matchB, int timeout)
    {
      SockLock(5000);
      int oto = stream.ReadTimeout;
      stream.ReadTimeout = timeout;
      DateTime begin = DateTime.Now;
      
      bool hit = false;
      byte[] testA = new byte[matchA.Length];
      byte[] testB = new byte[matchB.Length];
      List<byte> buff = new List<byte>();
      while(!hit)
      {
        if((DateTime.Now - begin).Milliseconds > timeout)
        {
          SockUnlock();
          Disconnect();
          throw new Exception("Socket Read Timeout");
        }
        for(int i=0; i<(testA.Length-1); i++) testA[i]=testA[i+1];
        for(int j=0; j<(testB.Length-1); j++) testB[j]=testB[j+1];
        stream.Read(testA,testA.Length-1,1);
        testB[testB.Length-1] = testA[testA.Length-1];
        buff.Add(testA[testA.Length-1]);
        hit = true;
        for(int c=0; c<testA.Length; c++)
        {
          if(testA[c] != matchA[c])
          {
            hit=false;
            break;
          }
        }
        if(hit == false)
        {
          hit=true;
          for(int d=0; d<testB.Length; d++)
          {
            if(testB[d] != matchB[d])
            {
              hit=false;
              break;
            }
          }
        }
      }
      stream.ReadTimeout = oto;
      SockUnlock();
      lastActive = DateTime.Now;
      
      if(debug != null)
      {
        byte[] dbg = buff.ToArray();
        debug("READ: ");
        for(int i=0; i<dbg.Length; i++) debug(dbg[i].ToString("X2")+", ");
        debug("\n");
      }
      return buff.ToArray();
    }
    //------------------------------------------------------------------------
    private byte[] ReadUntilOr(string matchA, string matchB, int timeout)      { return ReadUntilOr(EncodeString(matchA),EncodeString(matchB),defaultTimeout); }
    private string ReadUntilOrString(string matchA, string matchB, int timeout){ return DecodeString(ReadUntilOr(matchA, matchB, timeout));                    }
    //========================================================================
    public string DecodeString(byte[] byt)
    {
      string ret="";
      for(int i=0; i<byt.Length; i++)
        ret += (char)byt[i];
      return ret;
    }
    //------------------------------------------------------------------------
    public byte[] EncodeString(string str)
    {
      byte[] ret = new byte[str.Length];
      for(int i=0; i<str.Length; i++)
        ret[i] = (byte)(str[i] & 0x00FF);
      return ret;
    }
    //========================================================================
    private void KeepAliveStart()
    {
      if(!isConnected) return;
      if(keepAlive != null) KeepAliveStop();

      isKeepAlive = true;
      keepAlive = new Thread(KeepAlive);
      try
      {
        keepAlive.Start();
      }
      catch(Exception ex)
      {
        isKeepAlive = false;
        throw new Exception("Error Starting Keep-Alive Thread\n"+ex.Message);
      }
    }
    //------------------------------------------------------------------------
    private void KeepAliveStop()
    {
      if(keepAlive == null) return;
      
      /***** this was seriously slowing down application exit, waiting 2s per connection ****
      //attempt to let thread die naturally
      isKeepAlive = false;
      Thread.Sleep(2000);
      */
      
      //now force thread to terminate
      try
      {
        keepAlive.Abort();
      }
      catch(Exception)
      {
        //thread died on it's own
      }
      
      keepAlive = null;
    }
    //------------------------------------------------------------------------
    private void KeepAlive()
    {
      int oto = stream.WriteTimeout;
      byte[] wakeCmd = EncodeString((new SymCommand("WakeUp")).ToString());

      try
      {
        while(isConnected && isKeepAlive)
        {
          if((DateTime.Now - lastActive).TotalMilliseconds >= keepAliveInterval)
          {
            try
            {
              SockLock(5000);
              try
              {
                stream.WriteTimeout = 1000;
                stream.Write(wakeCmd, 0, wakeCmd.Length);
                stream.WriteTimeout = oto;
                SockUnlock();
                lastActive = DateTime.Now;
              }
              catch(Exception)
              {
                Disconnect();
              }
            }
            catch(Exception)
            {
              //failed in SockLock, must be in use.
              //no need to complain
            }
          }
          
          Thread.Sleep(1000);
        }
      }
      catch(ThreadAbortException)
      {
        //we were forcefully killed
      }
    }
    //========================================================================
    public bool Connect(string Server, int Port)
    {
      error = "";
      server = Server;
      try
      {
        SockLock(5000);
        socket = new TcpClient();
        socket.Connect(IPAddress.Parse(server), Port);
        stream = socket.GetStream();
      }
      catch(Exception ex)
      {
        if(inUse) SockUnlock();
        error = "Unable to Connect to Server\n"+ex.Message;
        return false;
      }
      
      isConnected = true;
      lastActive = DateTime.Now;
      SockUnlock();
      return true;
    }
    //========================================================================
    public void Disconnect()
    {
      KeepAliveStop();
      try { stream.Close(); } catch (Exception) { }
      try { socket.Close(); } catch (Exception) { }
      stream = null; socket = null;
      isConnected = isLoggedIn = inUse = false;
    }
    //========================================================================
    public bool AIXTest(string usr, string pwd)
    {
      error = "";
      
      if(!isConnected)
      {
        error = "Not Connected to Host";
        return false;
      }
      
      //Telnet Handshake
      try
      {
        Write(new byte[] {0xFF, 0xFB, 0x18}                                                            , 1000);
        Write(new byte[] {0xFF, 0xFA, 0x18, 0x00, 0x61, 0x69, 0x78, 0x74, 0x65, 0x72, 0x6D, 0xFF, 0xF0}, 1000);
        Write(new byte[] {0xFF, 0xFD, 0x01}                                                            , 1000);
        Write(new byte[] {0xFF, 0xFD, 0x03, 0xFF, 0xFC, 0x1F, 0xFF, 0xFC, 0x01}                        , 1000);
      }
      catch(Exception ex)
      {
        error = "Error During Telnet Handshake\n"+ex.Message;
        return false;
      }
      
      //AIX Login
      try
      {
        string stat;
        Write(usr+'\r', 1000);
        stat = ReadUntilOrString("Password:", "[c", 1000);
        if(stat.IndexOf("[c") == -1)
        {
          if(stat.IndexOf("invalid login") != -1)
          {
            error = "Invalid AIX Login";
            return false;
          }
          Write(pwd+'\r', 1000);
          stat = ReadUntilString(":", 1000);
          if(stat.IndexOf("invalid login") != -1)
          {
            error = "Invalid AIX Login";
            return false;
          }
          stat = ReadUntilString("[c", 1000);
        }
      }
      catch(Exception ex)
      {
        error = "Error During AIX Login\n"+ex.Message;
        return false;
      }
      return true;
    }
    //========================================================================
    public bool Login(string usr, string pwd, string symdir, string symid)
    {
      return Login(usr, pwd, symdir, symid, 0);
    }
    public bool Login(string usr, string pwd, string symdir, string symid, int stage)
    {
      error = "";
      sym = symdir;
      SymCommand cmd;
      
      if(!isConnected)
      {
        error = "Not Connected to Host";
        return false;
      }
      
      if(stage <1)
      {
        //Telnet Handshake
        try
        {
          Write(new byte[] {0xFF, 0xFB, 0x18}                                                            , 1000);
          Write(new byte[] {0xFF, 0xFA, 0x18, 0x00, 0x61, 0x69, 0x78, 0x74, 0x65, 0x72, 0x6D, 0xFF, 0xF0}, 1000);
          Write(new byte[] {0xFF, 0xFD, 0x01}                                                            , 1000);
          Write(new byte[] {0xFF, 0xFD, 0x03, 0xFF, 0xFC, 0x1F, 0xFF, 0xFC, 0x01}                        , 1000);
        }
        catch(Exception ex)
        {
          error = "Error During Telnet Handshake\n"+ex.Message;
          return false;
        }
      }
      
      if(stage <2)
      {
        //AIX Login
        try
        {
          string stat;
          Write(usr+'\r', 1000);
          stat = ReadUntilOrString("Password:", "[c", 1000);
          if(stat.IndexOf("[c") == -1)
          {
            if(stat.IndexOf("invalid login") != -1)
            {
              error = "Invalid AIX Login";
              return false;
            }
            Write(pwd+'\r', 1000);
            stat = ReadUntilString(":", 1000);
            if(stat.IndexOf("invalid login") != -1)
            {
              error = "Invalid AIX Login";
              return false;
            }
            stat = ReadUntilString("[c", 1000);
          }
        }
        catch(Exception ex)
        {
          error = "Error During AIX Login\n"+ex.Message;
          return false;
        }
      }
      
      //Symitar Login
      try
      {
        if(stage <2)
        {
          Write("WINDOWSLEVEL=3\n", 1000);
          ReadUntil("$ ", 1000);
          Write("sym "+sym+'\r', 1000);
          cmd = ReadCommand(2000);
          while(cmd.Command != "Input")
          {
            if(cmd.Command == "SymLogonError")
              if(cmd.GetParam("Text").IndexOf("Too Many Invalid Password Attempts") > -1)
              {
                error = "Too Many Invalid Password Attempts";
                return false;
              }
            cmd = ReadCommand(2000);
            if((cmd.Command == "Input") && (cmd.GetParam("HelpCode") == "10025"))
            {
              Write("$WinHostSync$\r");
              cmd = ReadCommand(2000);
            }
          }
        }
        Write(symid+'\r', 1000);
        cmd = ReadCommand(2000);
        if(cmd.Command == "SymLogonInvalidUser")
        {
          error = "Invalid Sym User";
          Write("\r", 1000); ReadCommand(2000);
          return false;
        }
        if(cmd.Command == "SymLogonError")
          if(cmd.GetParam("Text").IndexOf("Too Many Invalid Password Attempts") > -1)
          {
            error = "Too Many Invalid Password Attempts";
            return false;
          }
        Write("\r", 1000); ReadCommand(2000);
        Write("\r", 1000); ReadCommand(2000);
      }
      catch(Exception ex)
      {
        error = "Error During Sym Login\n"+ex.Message;
        return false;
      }
      
      //Keep-Alive Thread
      try
      {
        KeepAliveStart();
      }
      catch(Exception ex)
      {
        error = ex.Message;
        return false;
      }
      
      isLoggedIn = true;
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
      Write(cmd);
      
      while(true)
      {
        cmd = ReadCommand(2000);
        if(cmd.HasParameter("Status"))
          break;
        if(cmd.HasParameter("Name"))
            files.Add(new Symitar.File(server, sym, cmd.GetParam("Name"), type, cmd.GetParam("Date"), cmd.GetParam("Time"), int.Parse(cmd.GetParam("Size"))));
        if(cmd.HasParameter("Done"))
          break;
      }
      return files;
    }
    //========================================================================
    public bool FileExists(Symitar.File file)
    {
      return (FileList(file.FileName, file.Type).Count > 0);
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
      Write(cmd);
      
      cmd = ReadCommand(2000);
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
    public void FileRename(Symitar.File file, string newName) { FileRename(file.FileName, file.Type, newName); }
    //========================================================================
    public void FileDelete(string name, Symitar.FileType type)
    {
      SymCommand cmd = new SymCommand("File");
      cmd.Set("Action", "Delete");
      cmd.Set("Type", Utilities.FileTypeString(type));
      cmd.Set("Name"  , name);
      Write(cmd);
      
      cmd = ReadCommand(2000);
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
    public void FileDelete(Symitar.File file) { FileDelete(file.FileName, file.Type); }
    //========================================================================
    public string FileRead(string name, Symitar.FileType type)
    {
      StringBuilder content = new StringBuilder();
      
      SymCommand cmd = new SymCommand("File");
      cmd.Set("Action", "Retrieve");
      cmd.Set("Type", Utilities.FileTypeString(type));
      cmd.Set("Name"  , name);
      Write(cmd);
      
      while(true)
      {
        cmd = ReadCommand();
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
    public string FileRead(Symitar.File file) { return FileRead(file.FileName, file.Type); }
    //========================================================================
    public void FileWrite(string name, Symitar.FileType type, string content)
    {
      int chunkMax = 1024;
      
      SymCommand cmd = new SymCommand("File");
      cmd.Set("Action", "Store");
      cmd.Set("Type"  , Utilities.FileTypeString(type));
      cmd.Set("Name"  , name);
      WakeUp();
      Write(cmd);
      
      cmd = ReadCommand();
      int wtf_is_this = 0;
      while(cmd.Data.IndexOf("BadCharList") == -1)
      {
        cmd = ReadCommand();
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
          Write("PROT"+blockStr+"DATA"+chunkStr);
          Write(chunk);
          resp = Read(16);
        }

        block++;
        sent += chunkSize;
      }
      
      blockStr = block.ToString("D3");
      Write("PROT"+blockStr+"EOF\u0020\u0020\u0020\u0020\u0020\u0020");
      resp = Read(16);
      
      cmd = ReadCommand();
      WakeUp();
    }
    //------------------------------------------------------------------------
    public void FileWrite(Symitar.File file, string content) { FileWrite(file.FileName, file.Type, content); }
    //========================================================================
    public SpecfileError FileCheck(Symitar.File file)
    {  
      if(file.Type != Symitar.FileType.RepGen)
        throw new Exception("Cannot Check a "+file.FileTypeString()+" File");
      
      Write("mm3\u001B");    ReadCommand();
      Write("7\r");          ReadCommand(); ReadCommand();
      Write(file.FileName+'\r');
      
      SymCommand cmd = ReadCommand();
      if(cmd.HasParameter("Warning") || cmd.HasParameter("Error"))
      {
        ReadCommand();
        throw new Exception("File \""+file.FileName+"\" Not Found");
      }
      if(cmd.GetParam("Action")=="NoError")
      {
        ReadCommand();
        return SpecfileError.None();
      }
      
      int errRow=0, errCol=0;
      string errFile="", errText="";
      if(cmd.GetParam("Action")=="Init")
      {
        errFile = cmd.GetParam("FileName");
        cmd = ReadCommand();
        while(cmd.GetParam("Action")!="DisplayEdit")
        {
          if(cmd.GetParam("Action")=="FileInfo")
          {
            errRow = int.Parse(cmd.GetParam("Line").Replace(",", ""));
            errCol = int.Parse(cmd.GetParam("Col" ).Replace(",", ""));
          }
          else if(cmd.GetParam("Action")=="ErrText")
            errText += cmd.GetParam("Line")+" ";
          cmd = ReadCommand();
        }
        ReadCommand();

        return new SpecfileError(file, errFile, errText, errRow, errCol);
      }
      
      throw new Exception("Unknown Checking Error");
    }
    //========================================================================
    public SpecfileError FileInstall(Symitar.File file)
    {
      if(file.Type != Symitar.FileType.RepGen)
        throw new Exception("Cannot Install a "+file.FileTypeString()+" File");
      
      Write("mm3\u001B");    ReadCommand();
      Write("8\r");          ReadCommand(); ReadCommand();
      Write(file.FileName+'\r');
      
      SymCommand cmd = ReadCommand();
      if(cmd.HasParameter("Warning") || cmd.HasParameter("Error"))
      {
        ReadCommand();
        throw new Exception("File \""+file.FileName+"\" Not Found");
      }

      if(cmd.Command=="SpecfileData")
      {
        ReadCommand();
        Write("1\r");
        ReadCommand(); ReadCommand();
        return SpecfileError.None(int.Parse(cmd.GetParam("Size").Replace(",","")));
      }

      int errRow = 0, errCol = 0;
      string errFile = "", errText = "";
      if (cmd.GetParam("Action") == "Init")
      {
        errFile = cmd.GetParam("FileName");
        cmd = ReadCommand();
        while (cmd.GetParam("Action") != "DisplayEdit")
        {
          if (cmd.GetParam("Action") == "FileInfo")
          {
            errRow = int.Parse(cmd.GetParam("Line").Replace(",", ""));
            errCol = int.Parse(cmd.GetParam("Col").Replace(",", ""));
          }
          else if (cmd.GetParam("Action") == "ErrText")
            errText += cmd.GetParam("Line") + " ";
          cmd = ReadCommand();
        }
        ReadCommand();

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
      Write(cmd);

      cmd = ReadCommand();
      while(!cmd.HasParameter("Done"))
      {
        if((cmd.GetParam("Action")=="QueueEntry") && (int.Parse(cmd.GetParam("Seq")) == sequence))
          running = true;
        cmd = ReadCommand();
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
      Write(cmd);

      cmd = ReadCommand();
      while(!cmd.HasParameter("Done"))
      {
        if(cmd.HasParameter("Sequence"))
          seqs.Add(int.Parse(cmd.GetParam("Sequence")));
        cmd = ReadCommand();
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
          Symitar.File file = new Symitar.File(server, sym, i.ToString(), Symitar.FileType.Report, DateTime.Now, 0);
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
      
      Write("mm0\u001B");
      cmd = ReadCommand();
      while(cmd.Command != "Input")
        cmd = ReadCommand();
      callStatus(1,"Writing Commands...");

      Write("1\r");
      cmd = ReadCommand();
      while(cmd.Command != "Input")
        cmd = ReadCommand();
      callStatus(2,"Writing Commands...");

      Write("11\r");
      cmd = ReadCommand();
      while(cmd.Command != "Input")
        cmd = ReadCommand();
      callStatus(3,"Writing Commands...");

      Write(file.FileName + "\r");
      bool erroredOut = false;
      while(true)
      {
        cmd = ReadCommand();

        if((cmd.Command == "Input") && (cmd.GetParam("HelpCode")=="20301"))
          break;
        if(cmd.Command == "Input")
        {
          callStatus(4,"Please Enter Prompts");

          string result = callPrompt(cmd.GetParam("Prompt"));
          if(result == null) //cancelled
          {
            Write("\u001B");
            cmd = ReadCommand();
            while(cmd.Command != "Input")
              cmd = ReadCommand();
            return RepgenRunResult.Cancelled();
          }
          else
            Write(result.Trim()+'\r');
        }
        else if(cmd.Command == "Bell")
          callStatus(4, "Invalid Prompt Input, Please Re-Enter");
        else if((cmd.Command == "Batch") && (cmd.GetParam("Text")=="No such file or directory"))
        {
          cmd = ReadCommand();
          while(cmd.Command != "Input")
            cmd = ReadCommand();
          return RepgenRunResult.FileNotFound();
        }
        else if(cmd.Command == "SpecfileErr")
          erroredOut = true;
        else if(erroredOut && (cmd.Command == "Batch") && (cmd.GetParam("Action") == "DisplayLine"))
        {
          string err = cmd.GetParam("Text");
          cmd = ReadCommand();
          while (cmd.Command != "Input")
            cmd = ReadCommand();
          return RepgenRunResult.Error(err);
        }
        else if((cmd.Command == "Batch") && (cmd.GetParam("Action") == "DisplayLine"))
          callStatus(5, cmd.GetParam("Text"));
      }
      
      Write("\r");
      cmd = ReadCommand();
      while(cmd.Command != "Input")
        cmd = ReadCommand();
      
      callStatus(6, "Getting Queue List");
      Write("0\r");
      cmd = ReadCommand();
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
        cmd = ReadCommand();
      }
      
      callStatus(7, "Getting Queue Counts");
      cmd = new SymCommand("Misc");
      cmd.Set("InfoType", "BatchQueues");
      Write(cmd);
      
      cmd = ReadCommand();
      while(!cmd.HasParameter("Done"))
      {
        if((cmd.GetParam("Action") == "QueueEntry") && (cmd.GetParam("Stat") == "Running"))
          queAvailable[int.Parse(cmd.GetParam("Queue"))]++;
        cmd = ReadCommand();
      }
      
      if(queue == -1) //auto select lowest pending queue, or last available Zero queue
      {
        queue = 0;
        foreach(KeyValuePair<int, int> Q in queAvailable)
          if(Q.Value <= queAvailable[queue])
            queue = Q.Key;
      }
      
      Write(queue.ToString()+"\r");
      cmd = ReadCommand();
      while(cmd.Command != "Input")
        cmd = ReadCommand();
      
      callStatus(8, "Getting Sequence Numbers");
      Write("1\r");
      cmd = ReadCommand();
      while(cmd.Command != "Input")
        cmd = ReadCommand();
      
      cmd = new SymCommand("Misc");
      cmd.Set("InfoType", "BatchQueues");
      Write(cmd);
      
      int newestTime = 0;
      int sequenceNo = -1;
      cmd = ReadCommand();
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
        cmd = ReadCommand();
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
          Symitar.File file = new Symitar.File(server, sym, i.ToString(), Symitar.FileType.Report, DateTime.Now, 0);
        string contents = FileRead(file);
        contents = contents.Substring(contents.IndexOf("Name of Posting: ")+17);
        if(contents.StartsWith(title))
          return i;
      }
      return -1;
    }
    //------------------------------------------------------------------------
    public FMRunNfo FMRun(string inpTitle, FMType fmtype, FileRun_Status callStatus, int queue)
    {
      callStatus(1,"Initializing...");
      SymCommand cmd;
      string outTitle = "PwrIDE FM - " + new Random().Next(8388608).ToString("D7");
      
      Write("mm0\u001B");
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();

      callStatus(2,"Writing Commands...");
      Write("1\r");
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();

      Write("24\r"); //Misc. Processing
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();
      
      Write("5\r"); //Batch FM
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();
      
      Write(((int)fmtype).ToString()+"\r"); //FM File Type
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();
      
      Write("0\r"); //Undo a Posting? (NO)
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();
      
      Write(inpTitle+"\r"); //Title of Batch Report Output to Use as FM Script
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();
      
      Write("1\r"); //Number of Search Days? (1)
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();
      
      if(fmtype == FMType.Account)
      {
        Write("1\r"); //Record FM History (YES)
        cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();
      }
      
      Write(outTitle+"\r"); //Name of Posting (needed to lookup later)
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();
      
      Write("1\r"); //Produce Empty Report If No Exceptions? (YES)
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();
      
      Write("0\r"); //Batch Options? (NO)

      //get queues
      callStatus(4, "Getting Queue List");
      cmd = ReadCommand();
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
        cmd = ReadCommand();
      }
      
      //get queue counts
      callStatus(5, "Getting Queue Counts");
      cmd = new SymCommand("Misc");
      cmd.Set("InfoType", "BatchQueues");
      Write(cmd);
      
      cmd = ReadCommand();
      while(!cmd.HasParameter("Done"))
      {
        if((cmd.GetParam("Action") == "QueueEntry") && (cmd.GetParam("Stat") == "Running"))
          queAvailable[int.Parse(cmd.GetParam("Queue"))]++;
        cmd = ReadCommand();
      }
      
      if(queue == -1) //auto select lowest pending queue, or last available Zero queue
      {
        queue = 0;
        foreach(KeyValuePair<int, int> Q in queAvailable)
          if(Q.Value <= queAvailable[queue])
            queue = Q.Key;
      }
      
      callStatus(7, "Writing Final Commands");
      Write(queue.ToString()+"\r"); //write queue
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();
      
      Write("1\r"); //Okay (to Proceed)? (YES)
      cmd=ReadCommand(); while(cmd.Command!="Input") cmd=ReadCommand();
      
      //get queues again
      callStatus(8, "Finding FM Sequence");
      cmd = new SymCommand("Misc");
      cmd.Set("InfoType", "BatchQueues");
      Write(cmd);
      
      int newestTime = 0;
      int sequenceNo = -1;
      cmd = ReadCommand();
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
        cmd = ReadCommand();
      }
      
      callStatus(9, "Running..");
      return new FMRunNfo(sequenceNo, outTitle);
    }
    //========================================================================
  }
}
