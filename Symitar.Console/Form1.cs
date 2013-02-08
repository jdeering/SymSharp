using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
                if (_session != null && _session.Socket != null && _session.Socket.Connected)
                {
                    var data = _session.Socket.Read();
                    if(!string.IsNullOrEmpty(data))
                        responseBox.Invoke(() => responseBox.Text += data);
                }
            }
        }

        private SymSession _session;

        private void startButton_Click(object sender, EventArgs e)
        {
            if (_session != null)
            {
                _session.Disconnect();
                _session = null;
            }

            _session = new SymSession(670);
            _session.Connect("symitar", 23);
            _session.Login("jdeering", "h3dd0#mon", "083ch#ckb00k");

            if (!_session.LoggedIn)
            {
                responseBox.Text += "LOGIN FAILED\n\n";
            }
            else
            {
                responseBox.Text += "LOGIN PASSED\n\n";
            }

            //Thread dataThread = new Thread(UpdateTextBox);
            //dataThread.Start();
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            var message = messageBox.Text;
            messageBox.Text = "";

            var files = _session.FileList(message, FileType.RepGen);

            foreach (var f in files)
            {
                responseBox.Text += f.Name + "\n";
            }
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
