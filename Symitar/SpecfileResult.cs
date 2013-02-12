using System;
using System.Text;

namespace Symitar
{
    public class SpecfileResult
    {
        private Symitar.File _sourceFile;
        public Symitar.File Specfile
        {
            get { return _sourceFile; }
        }

        private string _fileWithError;
        public string FileWithError
        {
            get { return _fileWithError; }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get { return _errorMessage; }
        }

        private int _errorLine;
        private int _errorColumn;

        public int Line
        {
            get { return _errorLine; }
        }

        public int Column
        {
            get { return _errorColumn; }
        }

        public int InstallSize { get; set; }
        public bool InvalidInstall { get; set; }

        public bool PassedCheck
        {
            get { return !InvalidInstall && _errorLine <= 0; }
        }

        public SpecfileResult(Symitar.File specfile, string fileWithError, string message, int line, int col)
        {
            _sourceFile = specfile;
            _fileWithError = fileWithError;
            _errorMessage = message;
            _errorLine = line;
            _errorColumn = col;
            InvalidInstall = !string.IsNullOrEmpty(fileWithError) || !string.IsNullOrEmpty(message) || (line > 0 && col > 0);
        }

        public static SpecfileResult Success()
        {
            SpecfileResult result = new SpecfileResult(null, "", "", 0, 0);
            return result;
        }

        public static SpecfileResult Success(int size)
        {
            if(size < 0)
                throw new ArgumentOutOfRangeException("size", "Specfile install size cannot be less than 0.");

            SpecfileResult result = new SpecfileResult(null, "", "", 0, 0);
            result.InstallSize = size;

            if (size == 0) 
                result.InvalidInstall = true;

            return result;
        }
    }
}
