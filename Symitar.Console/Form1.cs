using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Symitar.Console
{
    public partial class Form1 : Form
    {
        private SymSession _session;
        private SymSocket _socket;

        public Form1()
        {
            InitializeComponent();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            Thread.CurrentThread.Name = "Main Thread";
            if (_session != null)
            {
                _socket.Disconnect();
                _socket = null;
            }

            _socket = new SymSocket("symitar", 23);
            _session = new SymSession(_socket, 670);
            Login();
        }

        private void Login()
        {
            while (!_session.LoggedIn)
            {
                _session.Disconnect();
                _session.Connect("symitar", 23);
                _session.Login("jdeering", "h3dd0#mon", "083ch#ckb00k");

                if (_session.LoggedIn)
                {
                    responseBox.Text += @"Logged in";
                    responseBox.Text += "\n\n";
                }
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            Login();
            string message = messageBox.Text;
            messageBox.Text = "";

            //FileInstallTest(message);

            //FileReadTest(message, FileType.RepGen);

            //var filename = message.Split(',')[0];
            //var runtime = message.Split(',')[1];
            //GetSequenceTest(filename, runtime);

            RunReportTest(message);
        }

        private void GetSequenceTest(params string[] args)
        {
            var sequence = _session.GetBatchOutputSequence(args[0], int.Parse(args[1]));

            responseBox.Text += String.Format("Batch output found at sequence {0}\n", sequence);
            foreach (var seq in _session.GetReportSequences(sequence))
            {
                responseBox.Text += String.Format("\tGenerated Report Seq: {0}\n", seq);
            }
        }

        private void RunReportTest(string fileName)
        {
            var file = new File {Name = fileName, Type = FileType.RepGen};
            RepgenRunResult result = _session.FileRun(file,
                                                      (code, description) =>
                                                      responseBox.Text += string.Format("{0}: {1}\n", code, description),
                                                      prompt => "",
                                                      3,
                                                      JobComplete);

        }

        private void JobComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
                responseBox.Text += "Job error: " + e.Error.Message + "\n";
            else
                responseBox.Text += "Job complete: " + e.Result;
        }

        private void FileReadTest(string fileName, FileType fileType)
        {
            string contents = _session.FileRead(fileName, fileType);

            LogResponse(contents);
        }

        private void FileInstallTest(string fileName)
        {
            try
            {
                var file = new File {Name = fileName, Type = FileType.RepGen};
                SpecfileResult result = _session.FileInstall(file);
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