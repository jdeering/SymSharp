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

            Thread dataThread = new Thread(UpdateTextBox);
            dataThread.Start();
        }

        private void UpdateTextBox()
        {
            while (true)
            {
                if (_socket != null && _socket.Connected)
                {
                    var data = _socket.Read();
                    if(!string.IsNullOrEmpty(data))
                        responseBox.Invoke(() => responseBox.Text += data);
                }
            }
        }

        private SymSocket _socket;

        private void startButton_Click(object sender, EventArgs e)
        {
            var adapter = new SocketAdapter();
            _socket = new SymSocket(adapter, "symitar", 23);
            _socket.Connect();

            _socket.Write(new byte[] { 0xFF, 0xFB, 0x18 });
            _socket.Write(new byte[] { 0xFF, 0xFA, 0x18, 0x00, 0x61, 0x69, 0x78, 0x74, 0x65, 0x72, 0x6D, 0xFF, 0xF0 });
            _socket.Write(new byte[] { 0xFF, 0xFD, 0x01 });
            _socket.Write(new byte[] { 0xFF, 0xFD, 0x03, 0xFF, 0xFC, 0x1F, 0xFF, 0xFC, 0x01 });
            
            var data = _socket.Read();
            responseBox.Text += data + "\n";
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            var message = messageBox.Text;
            messageBox.Text = "";

            _socket.Write(message + '\r');
            var data = _socket.Read();
            responseBox.Text += data + "\n";
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
