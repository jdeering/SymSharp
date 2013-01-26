using System;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Symitar
{
  public class SymCommand
  {
    //========================================================================
    public string command = "";
    public string data    = "";
    public Dictionary<string, string> parameters = new Dictionary<string, string>();
    private static Regex cmdPattern = new Regex("(.*?)~.*");
    private static int currMsgId = 10000;
    //========================================================================
    public SymCommand()
    {
      parameters.Add("MsgId", currMsgId.ToString());
      currMsgId++;
    }
    //------------------------------------------------------------------------
    public SymCommand(string cmd)
    {
      command = cmd;
      parameters.Add("MsgId", currMsgId.ToString());
      currMsgId++;
    }
    //------------------------------------------------------------------------
    public SymCommand(string cmd, Dictionary<string, string>parms, string dat)
    {
      command    = cmd;
      parameters = parms;
      data       = dat;
      parameters.Add("MsgId", currMsgId.ToString());
      currMsgId++;
    }
    //========================================================================
    public static SymCommand Parse(string dat)
    {
      char[] seperators = {'~'};
      SymCommand cmd = new SymCommand();
      cmd.data = dat;
      
      if((dat.IndexOf("~") != -1) && (dat.IndexOf('\u00FD') == -1))
      {
        Match match = cmdPattern.Match(dat);
        cmd.command = match.Groups[1].Value;
        
        string[] sep = dat.Substring(cmd.command.Length + 1).Split(seperators);
        for(int i=0; i<sep.Length; i++)
        {
          int eqPos = sep[i].IndexOf("=");
          if(eqPos != -1)
            cmd.SetParam(sep[i].Substring(0,eqPos), sep[i].Substring(eqPos+1));
          else
            cmd.SetParam(sep[i],"");
        }
      }
      else
        cmd.command = dat;
      
      return cmd;
    }
    //------------------------------------------------------------------------
    public string GetFileData()
    {    
      int fd = data.IndexOf('\u00FD');
      int fe = data.IndexOf('\u00FE');
      if((fd != -1) && (fe != -1))
        return data.Substring(fd+1, fe-fd-1);
      return "";
    }
    //------------------------------------------------------------------------
    public string Packet()
    {
      string dat = command + '~';
      foreach(KeyValuePair<string, string> param in parameters)
      {
        dat += param.Key;
        if(param.Value != "")
          dat += '=' + param.Value;
        dat += '~';
      }
      
      dat = dat.Substring(0, dat.Length - 1);
      return '\u0007' + dat.Length.ToString() + '\r' + dat;  
    }
    //========================================================================
    public void SetParam(string prm, string value)
    {
      if (parameters.ContainsKey(prm) == true)
        parameters[prm] = value;
      else
        parameters.Add(prm, value);
    }
    //------------------------------------------------------------------------
    public bool HasParam(string prm)
    {
      return parameters.ContainsKey(prm);
    }
    //------------------------------------------------------------------------
    public string GetParam(string prm)
    {
      if (parameters.ContainsKey(prm) == true)
        return parameters[prm];
      return "";
    }
    //------------------------------------------------------------------------
  }
}
