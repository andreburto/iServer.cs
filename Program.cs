using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace iServer
{
    class Program
    {
        static void Main(string[] args)
        {
            MyServer i = new MyServer(8899);
            i.startServer();
            Console.WriteLine("Starting...");
            Thread.Sleep(15000);
            Console.WriteLine("Stopping...");
            i.stopServer();

            Console.Read();
        }
    }

    // TEST CLASS
    class MyServer : iServer
    {
        public override void Command(string path, string args, Hashtable headers, string type, Socket iSocket)
        {
            string html = String.Format("{0} method calling the {1} path", type, path);
            this.addHeader("Server", "iServer");
            this.addHeader("Date", DateTime.Now.ToUniversalTime().ToString());
            this.addHeader("Content-Type", "text/plain");
            this.addHeader("Content-Length", html.Length.ToString());
            this.SendToBrowser(html, iSocket);
        }

        public MyServer(int port) : base(port) { }
    }
}
