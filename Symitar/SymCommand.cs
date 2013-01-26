using System;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Symitar
{
    public class SymCommand
    {
        public string Command = "";
        public string Data = "";
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();

        private static int _msgId = 10000;
        private const char Delimiter = '~';

        public SymCommand()
        {
            Parameters.Add("MsgId", _msgId.ToString());
            _msgId++;
        }

        public SymCommand(string cmd)
        {
            Command = cmd;
            Parameters.Add("MsgId", _msgId.ToString());
            _msgId++;
        }

        public SymCommand(string cmd, Dictionary<string, string> parms)
        {
            Command = cmd;
            Parameters = parms;
            Parameters.Add("MsgId", _msgId.ToString());
            _msgId++;
        }

        public SymCommand(string cmd, Dictionary<string, string> parms, string data)
        {
            Command = cmd;
            Parameters = parms;
            Data = data;
            Parameters.Add("MsgId", _msgId.ToString());
            _msgId++;
        }

        public static SymCommand Parse(string message)
        {
            SymCommand symCommand = new SymCommand();

            if (!message.Contains("~") || message.Contains("\u00FD"))
            {
                symCommand.Command = message;
            }
            else
            {
                string[] tokens = message.Split(Delimiter);
                symCommand.Command = tokens[0];

                for (int i = 1; i < tokens.Length; i++)
                {
                    List<string> pair = tokens[i].Split('=').ToList();

                    // Add default 'value' for parameter
                    // if not specified
                    if(pair.Count == 1) 
                        pair.Add(""); 

                    symCommand.Set(pair[0], pair[1]);
                }
            }
      
            return symCommand;
        }

        public string GetFileData()
        {
            int fd = Data.IndexOf('\u00FD');
            int fe = Data.IndexOf('\u00FE');

            if((fd != -1) && (fe != -1))
                return Data.Substring(fd + 1, fe - fd - 1);
            return "";
        }

        public override string ToString()
        {
            string commandParams = Parameters
                            .Select(
                                x =>
                                    {
                                        if (x.Value == "")
                                            return x.Key;

                                        return x.Key + "=" + x.Value;
                                    })
                            .Aggregate((a, b) => a + "~" + b);

            string data = Command + "~" + commandParams;
            string dataHeader = String.Format("{0}{1}", '\u0007', data.Length);

            return String.Format("{0}\r{1}", dataHeader, data);
        }

        public void Set(string parameter, string value)
        {
            if (Parameters.ContainsKey(parameter))
                Parameters[parameter] = value;
            else
                Parameters.Add(parameter, value);
        }

        public bool HasParameter(string parameter)
        {
            return Parameters.ContainsKey(parameter);
        }

        public string GetParam(string key)
        {
            return HasParameter(key) ? Parameters[key] : "";
        }
    }
}
