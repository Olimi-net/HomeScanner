using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scaner
{
    delegate void ScanerDelegate();

    public partial class Form1 : Form
    {
        WiaScan _wia;
        ScanServer _server;
        string lastImage = "";
        int lastStatus = 0;

        ScanerDelegate _del;
        string dir = "";

        public Form1()
        {
            InitializeComponent();

            


            var fbd = new FolderBrowserDialog();
            var res = fbd.ShowDialog();

            

            if (res == System.Windows.Forms.DialogResult.OK)
            {
                dir = fbd.SelectedPath;
            }


            _server = new ScanServer(dir);
            _server.ServerEvent += Server_Event;

            _wia = new WiaScan();
            _wia.ScanerEvent += WiaScan_Event;
            _wia.Init(dir);
            _del = Scan2;
            
        }

        private void Server_Event(object sender, ServerArg e)
        {
            if (e.Cmd == ScanerCmd.Scan && lastStatus == 1)
            {
                this.Invoke(_del);                
            }
        }

        private void Scan2()
        {
            _wia.Scan();
        }

        private void WiaScan_Event(object sender, ScanArg e)
        {
            lastStatus = e.Status;
           this.Text = ("Status " + e.Status + ". Message " + e.Message);

           if (e.Status == 1)
           {
               lastImage = e.Message;
           }

           _server.SetStatus(e.Status, e.Message, lastImage);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (lastStatus != 1) return;

            string s = _wia.Scan();

            if(string.IsNullOrEmpty(s))
            {
                pictureBox1.Image = null; 
            }
            else
            {
                string path = Path.Combine(dir, s);
                if (File.Exists(path))
                {
                    using (var bmp = Bitmap.FromFile(path))
                        pictureBox1.Image = bmp;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _server.Dispose();
        }
    }
}
