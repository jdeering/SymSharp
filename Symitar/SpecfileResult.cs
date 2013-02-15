using System;

namespace Symitar
{
    public class SpecfileResult
    {
        private readonly int _errorColumn;
        private readonly int _errorLine;
        private readonly string _errorMessage;
        private readonly string _fileWithError;
        private readonly File _sourceFile;

        public SpecfileResult(File specfile, string fileWithError, string message, int line, int col)
        {
            _sourceFile = specfile;
            _fileWithError = fileWithError;
            _errorMessage = message;
            _errorLine = line;
            _errorColumn = col;
            InvalidInstall = !string.IsNullOrEmpty(fileWithError) || !string.IsNullOrEmpty(message) ||
                             (line > 0 && col > 0);
        }

        public File Specfile
        {
            get { return _sourceFile; }
        }

        public string FileWithError
        {
            get { return _fileWithError; }
        }

        public string ErrorMessage
        {
            get { return _errorMessage; }
        }

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

        public static SpecfileResult Success()
        {
            var result = new SpecfileResult(null, "", "", 0, 0);
            return result;
        }

        public static SpecfileResult Success(int size)
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException("size", "Specfile install size cannot be less than 0.");

            var result = new SpecfileResult(null, "", "", 0, 0);
            result.InstallSize = size;

            if (size == 0)
                result.InvalidInstall = true;

            return result;
        }
    }
}