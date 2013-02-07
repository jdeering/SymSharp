using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Symitar.Interfaces;

namespace Symitar
{
    public partial class SymSession
    {
        public bool FileExists(Symitar.File file)
        {
            return (FileList(file.Name, file.Type).Count > 0);
        }

        public bool FileExists(string filename, Symitar.FileType type)
        {
            return (FileList(filename, type).Count > 0);
        }

        public List<Symitar.File> FileList(string pattern, Symitar.FileType type)
        {
            List<Symitar.File> files = new List<Symitar.File>();

            ISymCommand cmd = new SymCommand("File");
            cmd.Set("Type", Utilities.FileTypeString(type));
            cmd.Set("Name", pattern);
            cmd.Set("Action", "List");
            _socket.Write(cmd);

            while (true)
            {
                cmd = _socket.ReadCommand();
                if (cmd.HasParameter("Status"))
                    break;
                if (cmd.HasParameter("Name"))
                {
                    files.Add(new Symitar.File(_socket.Server, SymDirectory.ToString(), cmd.Get("Name"), type,
                                               cmd.Get("Date"), cmd.Get("Time"),
                                               int.Parse(cmd.Get("Size"))));
                }
                if (cmd.HasParameter("Done"))
                    break;
            }
            return files;
        }

        public Symitar.File FileGet(string filename, Symitar.FileType type)
        {
            List<Symitar.File> files = FileList(filename, type);
            if (files.Count < 1)
                throw new FileNotFoundException();
            return files[0];
        }

        public void FileRename(Symitar.File file, string newName)
        {
            FileRename(file.Name, newName, file.Type);
        }

        public void FileRename(string oldName, string newName, Symitar.FileType type)
        {
            ISymCommand cmd = new SymCommand("File");
            cmd.Set("Action", "Rename");
            cmd.Set("Type", Utilities.FileTypeString(type));
            cmd.Set("Name", oldName);
            cmd.Set("NewName", newName);
            _socket.Write(cmd);

            cmd = _socket.ReadCommand();
            if (cmd.HasParameter("Status"))
            {
                if (cmd.Get("Status").IndexOf("No such file or directory") != -1)
                    throw new FileNotFoundException();
                else
                    throw new Exception("Filename Too Long");
            }

            if (cmd.HasParameter("Done"))
                return;

            throw new Exception("Unknown Renaming Error");
        }

        public void FileDelete(Symitar.File file)
        {
            FileDelete(file.Name, file.Type);
        }

        public void FileDelete(string name, Symitar.FileType type)
        {
            ISymCommand cmd = new SymCommand("File");
            cmd.Set("Action", "Delete");
            cmd.Set("Type", Utilities.FileTypeString(type));
            cmd.Set("Name", name);
            _socket.Write(cmd);

            cmd = _socket.ReadCommand();
            if (cmd.HasParameter("Status"))
            {
                if (cmd.Get("Status").IndexOf("No such file or directory") != -1)
                    throw new FileNotFoundException();
                else
                    throw new Exception("Filename Too Long");
            }
            if (cmd.HasParameter("Done"))
                return;

            throw new Exception("Unknown Deletion Error");
        }

        public string FileRead(Symitar.File file)
        {
            return FileRead(file.Name, file.Type);
        }

        public string FileRead(string name, Symitar.FileType type)
        {
            StringBuilder content = new StringBuilder();

            ISymCommand cmd = new SymCommand("File");
            cmd.Set("Action", "Retrieve");
            cmd.Set("Type", Utilities.FileTypeString(type));
            cmd.Set("Name", name);
            _socket.Write(cmd);

            while (true)
            {
                cmd = _socket.ReadCommand();

                if (cmd.HasParameter("Status"))
                {
                    string status = cmd.Get("Status");
                    if (status.Contains("No such file or directory"))
                        throw new FileNotFoundException();

                    if (status.Contains("Cannot view a blank report"))
                        return "";

                    throw new Exception("Filename Too Long");
                }

                string chunk = cmd.GetFileData();
                if (chunk.Length > 0)
                {
                    content.Append(chunk);
                    if (type == Symitar.FileType.Report)
                        content.Append('\n');
                }

                if (cmd.HasParameter("Done")) break;
            }
            return content.ToString();
        }

        public void FileWrite(Symitar.File file, string content)
        {
            FileWrite(file.Name, file.Type, content);
        }

        public void FileWrite(string name, Symitar.FileType type, string content)
        {
            int chunkMax = 1024; // 1 MB max file size by default

            ISymCommand cmd = new SymCommand("File");
            cmd.Set("Action", "Store");
            cmd.Set("Type", Utilities.FileTypeString(type));
            cmd.Set("Name", name);
            _socket.WakeUp();
            _socket.Write(cmd);

            cmd = _socket.ReadCommand();
            int wtf_is_this = 0;
            while (!cmd.Data.Contains("BadCharList"))
            {
                cmd = _socket.ReadCommand();
                wtf_is_this++;
                if (wtf_is_this > 5)
                    throw new NullReferenceException();
            }

            if (cmd.Data.Contains("MaxBuff"))
                chunkMax = int.Parse(cmd.Get("MaxBuff"));

            if (content.Length > (999 * chunkMax))
                throw new FileLoadException("File too large");

            if (cmd.Get("Status").Contains("Filename is too long"))
                throw new Exception("Filename Too Long");

            content = SanitizeFileContent(content, cmd.Get("BadCharList").Split(','));

            int bytesSent = 0;
            int block = 0;
            string blockStr;
            byte[] response;

            while (bytesSent < content.Length)
            {
                int chunkSize = (content.Length - bytesSent);
                if (chunkSize > chunkMax)
                    chunkSize = chunkMax;
                string chunk = content.Substring(bytesSent, chunkSize);
                string chunkStr = chunkSize.ToString("D5");
                blockStr = block.ToString("D3");

                response = new byte[] { 0x4E, 0x4E, 0x4E, 0x4E, 0x4E, 0x4E, 0x4E, 0x4E, 0x4E, 0x4E, 0x4E };
                while (response[7] == 0x4E)
                {
                    _socket.Write("PROT" + blockStr + "DATA" + chunkStr);
                    _socket.Write(chunk);
                    response = Convert.FromBase64String(_socket.Read());
                }

                block++;
                bytesSent += chunkSize;
            }

            blockStr = block.ToString("D3");
            _socket.Write("PROT" + blockStr + "EOF\u0020\u0020\u0020\u0020\u0020\u0020");
            _socket.Read();

            _socket.ReadCommand();
            _socket.WakeUp();
        }

        private string SanitizeFileContent(string content, string[] invalidCharacters)
        {
            for (int i = 0; i < invalidCharacters.Length; i++)
                content = content.Replace(((char)int.Parse(invalidCharacters[i])) + "", "");

            return content;
        }

        public SpecfileResult FileCheck(Symitar.File file)
        {
            if (file.Type != Symitar.FileType.RepGen)
                throw new Exception("Cannot check a " + file.FileTypeString() + " file");

            _socket.Write("mm3\u001B"); _socket.ReadCommand();
            _socket.Write("7\r"); _socket.ReadCommand(); _socket.ReadCommand();
            _socket.Write(file.Name + '\r');

            ISymCommand cmd = _socket.ReadCommand();
            if (cmd.HasParameter("Warning") || cmd.HasParameter("Error"))
            {
                _socket.ReadCommand();
                throw new FileNotFoundException();
            }
            if (cmd.Get("Action") == "NoError")
            {
                _socket.ReadCommand();
                return SpecfileResult.Success();
            }

            int errRow = 0, errCol = 0;
            string errFile = "", errText = "";

            if (cmd.Get("Action") == "Init")
            {
                errFile = cmd.Get("FileName");
                cmd = _socket.ReadCommand();
                while (cmd.Get("Action") != "DisplayEdit")
                {
                    if (cmd.Get("Action") == "FileInfo")
                    {
                        errRow = int.Parse(cmd.Get("Line").Replace(",", ""));
                        errCol = int.Parse(cmd.Get("Col").Replace(",", ""));
                    }
                    else if (cmd.Get("Action") == "ErrText")
                    {
                        errText += cmd.Get("Line") + " ";
                    }
                    cmd = _socket.ReadCommand();
                }
                _socket.ReadCommand();

                return new SpecfileResult(file, errFile, errText, errRow, errCol);
            }

            throw new Exception("An unknown error occurred.");
        }

        public SpecfileResult FileInstall(Symitar.File file)
        {
            if (file.Type != Symitar.FileType.RepGen)
                throw new Exception("Cannot Install a " + file.FileTypeString() + " File");

            _socket.Write("mm3\u001B"); _socket.ReadCommand();
            _socket.Write("8\r"); _socket.ReadCommand(); _socket.ReadCommand();
            _socket.Write(file.Name + '\r');

            ISymCommand cmd = _socket.ReadCommand();
            if (cmd.HasParameter("Warning") || cmd.HasParameter("Error"))
            {
                _socket.ReadCommand();
                throw new FileNotFoundException();
            }

            if (cmd.Command == "SpecfileData")
            {
                _socket.ReadCommand();
                _socket.Write("1\r");
                _socket.ReadCommand(); _socket.ReadCommand();
                return SpecfileResult.Success(int.Parse(cmd.Get("Size").Replace(",", "")));
            }

            int errRow = 0, errCol = 0;
            string errFile = "", errText = "";
            if (cmd.Get("Action") == "Init")
            {
                errFile = cmd.Get("FileName");
                cmd = _socket.ReadCommand();
                while (cmd.Get("Action") != "DisplayEdit")
                {
                    if (cmd.Get("Action") == "FileInfo")
                    {
                        errRow = int.Parse(cmd.Get("Line").Replace(",", ""));
                        errCol = int.Parse(cmd.Get("Col").Replace(",", ""));
                    }
                    else if (cmd.Get("Action") == "ErrText")
                        errText += cmd.Get("Line") + " ";
                    cmd = _socket.ReadCommand();
                }
                _socket.ReadCommand();

                return new SpecfileResult(file, errFile, errText, errRow, errCol);
            }

            throw new Exception("Unknown Install Error");
        }
    }
}
