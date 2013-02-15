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

        private SymSession _session;
        private SymSocket _socket;

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
            var message = messageBox.Text;
            messageBox.Text = "";

            //FileInstallTest(message);
            //FileReadTest(message, FileType.RepGen);
            RunReportTest(message);
        }

        private void RunReportTest(string fileName)
        {
            var file = new File() {Name = fileName, Type = FileType.RepGen};
            var result = _session.FileRun(file,
                             (code, description) => responseBox.Text += string.Format("{0}: {1}\n", code, description),
                             prompt => "", 
                             3);

            Thread waiter = new Thread(WaitForReport);
            waiter.Name = "Report Run Waiter";
            waiter.Start(new string[] { file.Name, result.Sequence.ToString(), result.RunTime.ToString() });
        }

        private void WaitForReport(object a)
        {
            var args = a as string[];
            while (_session.IsFileRunning(int.Parse(args[1])))
            {
                Thread.Sleep(60000);
            }
            this.Invoke(() => responseBox.Text += String.Format("Report completed: {0}", _session.GetReportSequence(args[0], int.Parse(args[2]))));
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
