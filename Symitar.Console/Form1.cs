using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Symitar.Console
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void UpdateTextBox()
        {
            while (true)
            {
                if (_socket != null && _socket.Connected)
                {
                    string msg;
                    var cmd = _socket.ReadCommand();

                    msg = cmd.Command == "" ? _socket.Read() : cmd.ToString();

                    if(!string.IsNullOrEmpty(msg))
                        this.Invoke(() =>
                            {
                                responseBox.Text += msg + "\n";
                                responseBox.SelectionStart = responseBox.Text.Length;
                                responseBox.ScrollToCaret();
                            });
                } 
            }
        }

        private SymSession _session;
        private SymSocket _socket;

        private void startButton_Click(object sender, EventArgs e)
        {
            if (_session != null)
            {
                _socket.Disconnect();
                _socket = null;
            }

            _socket = new SymSocket("symitar", 23);
            _session = new SymSession(_socket, 670);
            while (!_session.LoggedIn)
            {
                _session.Disconnect();
                _session.Connect("symitar", 23);
                _session.Login("jdeering", "h3dd0#mon", "083ch#ckb00k");
            }

            Thread dataThread = new Thread(UpdateTextBox);
            //dataThread.Start();
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            var message = messageBox.Text;
            messageBox.Text = "";

            //FileInstallTest(message);
            //FileReadTest(message, FileType.RepGen);
            RunReportTest(message);
        }

        private void RunReportTest(string fileName)
        {
            var file = new File() {Name = fileName, Type = FileType.RepGen};
            _session.FileRun(file,
                             (code, description) => responseBox.Text += string.Format("{0}: {1}", code, description),
                             prompt => { return ""; }, 
                             3);
        }

        private void FileReadTest(string fileName, FileType fileType)
        {
            var contents = _session.FileRead(fileName, fileType);

            LogResponse(contents);
        }

        private void FileInstallTest(string fileName)
        {
            try
            {
                var file = new File() { Name = fileName, Type = FileType.RepGen };
                var result = _session.FileInstall(file);
                LogResponse(_session.Log.Aggregate((a, b) => a + "\n" + b));

                LogResponse(result.PassedCheck);
                LogResponse(result.FileWithError);
                LogResponse(result.ErrorMessage);
            }
            catch (Exception ex)
            {
                LogResponse(_session.Log.Aggregate((a, b) => a + "\n" + b));
                LogResponse(ex.Message);
            }
            finally
            {
                _session.Log.Clear();
            }
        }

        private void LogResponse(object message)
        {
            responseBox.Text += message + "\n";
            responseBox.SelectionStart = responseBox.Text.Length;
            responseBox.ScrollToCaret();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _session.Disconnect();
            _session = null;
            base.OnClosing(e);
        }
    }

    internal static class ControlExtensions
    {
        public static void Invoke<TControl>(this TControl control, Action action)
            where TControl : Control
        {
            if (!control.InvokeRequired)
            {
                action();
            }
            else
            {
                control.Invoke(action);
            }
        }
    }
}
