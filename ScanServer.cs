using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <author>
/// Lada 
/// </author>
/// <created>
/// 2019.05.28
/// </created>

namespace Scaner
{
    class ScanServer : IDisposable
    {
        Thread thread;
        public static string Dir = "";
        TcpListener Listener;
        private string _message;
        private int _status;
        public EventHandler<ServerArg> ServerEvent;
        private string _lastImage = "";

        public ScanServer(string dir = "")
        {
            Dir = dir;
            Listener = new TcpListener(IPAddress.Any, 80);
            Listener.Start();

            thread = new Thread(WaitingClient);
            thread.Start();            
        }

        private void WaitingClient()
        {
            try
            {
                while (true)
                {
                    var tcpClient = Listener.AcceptTcpClient();
                    var get = GetClientGet(tcpClient);
                    if (get.IsPost && _status == 1)
                    {
                        if (ServerEvent != null)
                        {
                            ServerEvent.BeginInvoke(this, new ServerArg(ScanerCmd.Scan), ServerCallback, this);
                        }                        
                    }
                    new Client(tcpClient, _status, _message, _lastImage, get);
                }
            }
            catch (Exception ex)
            {
                if (ServerEvent != null)
                {
                    ServerEvent.BeginInvoke(this, new ServerArg(0, ex.Message), ServerCallback, this);
                }  
            }
        }

        private void ServerCallback(IAsyncResult ar)
        {
            
        }


        private ClientGet GetClientGet(TcpClient Client)
        {
            string Request = "";
            byte[] RequestBuffer = new byte[100];
            int Count;
            if ((Count = Client.GetStream().Read(RequestBuffer, 0, RequestBuffer.Length)) > 0)
            {
                Request += Encoding.ASCII.GetString(RequestBuffer, 0, Count);
            }

            return new ClientGet(Request);
        }


        public void Dispose()
        {
            try
            {
                if (Listener != null)
                {
                    Listener.Stop();
                    Listener = null;
                }

                thread.Abort();
            }
            catch (Exception ex)
            {

            }
        }

        internal void SetStatus(int p1, string p2, string p3)
        {
            _status = p1;
            _message = p2;
            _lastImage = p3;
        }
    }

    class ClientGet
    {
        public bool IsPost;
        public ClientGet() { }

        public bool IsGet { get; set; }
        public int Key { get; set; }
        public string Last { get; set; }

        public ClientGet(string s)
        {
            var get = s.Split('\n');
            if (get.Length == 0) return;

            var res = get[0].Split(' ');
            if (res.Length > 1)
            {
                IsGet = (res[0].Equals("GET", StringComparison.CurrentCultureIgnoreCase));
                IsPost = (res[0].Equals("POST", StringComparison.CurrentCultureIgnoreCase));
                var a = res[1].Split('?');
                if(a.Length > 1)
                {
                    var b = a[1].Split('=');
                    if (b.Length > 1)
                    {
                        int k = 0;
                        if(Int32.TryParse(b[1], out k))
                        {
                            Key = k;
                        }
                    }
                }

                if (a.Length > 0)
                {
                    var c = a[0].Split('/');
                    if (c.Length > 0)
                    {
                        Last = c[c.Length - 1];
                    }
                }
            }
        }
    }

    class Client
    {
        const string header = "<head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/><title>Scaner</title><style>.red{background:red;} .yellow{background:yellow;} .green{background:green;} form,h1{display:inline-block} h1,input{font-size:1.5em; padding:0 1em;} img{width: auto;height: auto;max-width: 100%;max-height: 100%;} </style></head>";
        const string statusFormat = "<h1 class=\"{0}\">Status {1}</h1>";
        const string sendForm = "<form method=\"POST\" action=\"\"> <input type=\"submit\"></form>";
        TcpClient _client;
        int _state;
        string _msg;
        string _img;

        public Client(TcpClient Client, int state, string msg, string img, ClientGet get)
        {
            _client = Client;
            _state = state;
            _msg = msg;
            _img = img;
            
            if (!string.IsNullOrEmpty(get.Last))
            {
                SendFile(get.Last);   
            }
            else
            {
                SendHtml();
            }
            Thread.Sleep(1000);
            Client.Close();
        }


        private string GetDefaultBody()
        {
            string status = "";
            switch (_state)
            {
                case 1: status += string.Format(statusFormat, "green", "ready"); break;
                case 3: status += string.Format(statusFormat, "yellow", "scanning"); break;
                default: status += string.Format(statusFormat, "red", "error:" + _msg); break;
            }

            if (_state == 1)
                status += sendForm;
            
            string pic = (string.IsNullOrEmpty(_img)) ? "" : "<img src=\"" + _img + "\">";
            return "<html>" + header + "<body><nav>" + status + "</nav><div>" + pic + "</div></body></html>";
        }

        private void WriteHeader(string contentType, long length)
        {
            string str = "HTTP/1.1 200 OK\nContent-type: " + contentType
                   + "\nContent-Length:" + length
                   + "\n\n";
            _client.GetStream().Write(Encoding.ASCII.GetBytes(str), 0, str.Length);
        }

        private string GetContentType(string file)
        {
            string s = Path.GetExtension(file).ToUpper();

            if(s.Equals(".HTML")) return "text/html";
            if(s.Equals(".HTM")) return "text/html";
            if(s.Equals(".TXT")) return "text/plain";
            if(s.Equals(".GIF")) return "image/gif";
            if(s.Equals(".JPG")) return "image/jpeg";
            if(s.Equals(".JPEG")) return "image/jpeg";
            if(s.Equals(".JS")) return "text/javascript";

            return "application/unknown";
        }


        private void SendFile(string fileName)
        {
            string filepath = Path.Combine(ScanServer.Dir, fileName);
            if (File.Exists(filepath))
            {
                using (FileStream file = File.Open(filepath, FileMode.Open))
                {
                    WriteHeader(GetContentType(fileName), file.Length);
                    byte[] buf = new byte[1024];
                    int len;
                    while ((len = file.Read(buf, 0, 1024)) != 0)
                    {
                        _client.GetStream().Write(buf, 0, len);
                    }
                }
            }
        }

        private void SendHtml()
        {
            var html = Encoding.UTF8.GetBytes(GetDefaultBody());
            WriteHeader("text/html", html.Length);
            _client.GetStream().Write(html, 0, html.Length);
        }
    } 
}
