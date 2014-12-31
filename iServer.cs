using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace iServer
{
    // Base server class
    class iServer
    {
        private string code = "200 OK";
        private Hashtable respHeader = new Hashtable();
        private const int DEFAULT_PORT = 57475;
        private bool goon = true;
        private TcpListener iListener;
        private int iPort;
        private Thread th;

        // Dump everything out to the browser through the socket
        public void SendToBrowser(string data, Socket iSocket)
        {
            try
            {
                data = makeHeader() + data;
                byte[] sendBytes = Encoding.UTF8.GetBytes(data);
                if (iSocket.Connected == false) { throw new Exception("No socket."); }
                if (iSocket.Send(sendBytes) == -1) { throw new Exception("Could not send."); }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // This is the function that inherited classes will override to parse data and handle requests
        public virtual void Command(string path, string args, Hashtable headers, string type, Socket iSocket)
        {
            string html = "<h2>iServer is working...</h2>";
            respHeader.Add("Server", "iServer");
            respHeader.Add("Date", DateTime.Now.ToUniversalTime().ToString());
            respHeader.Add("Content-Type", "text/html");
            respHeader.Add("Content-Length", html.Length.ToString());
            this.SendToBrowser(html, iSocket);
        }

        // Public access to add headers
        public void addHeader(string k, string v)
        {
            if (respHeader.ContainsKey(k))
            {
                respHeader[k] = v.ToString();
            }
            else
            {
                respHeader.Add(k, v);
            }
        }

        // Set the HTTP code
        public void setCode(string code)
        {
            this.code = code;
        }

        // Build a header from data within the class
        protected string makeHeader()
        {
            string header = String.Format("HTTP/1.0 {0}\r\n", this.code);
            foreach (object key in respHeader.Keys)
            {
                header = header + String.Format("{0}: {1}\r\n", (string)key, respHeader[key].ToString());
            }
            header = header + "\r\n";
            return header;
        }

        // The main loop where the listener listens for connections.
        // Only handles one connection at a time for now.
        protected void Listen()
        {
            this.iListener = new TcpListener(IPAddress.Any, this.iPort);
            this.iListener.Start();

            while (goon == true)
            {
                try
                {
                    if (this.iListener.Pending() == false)
                    {
                        continue;
                    }

                    Socket iSocket = this.iListener.AcceptSocket();
                    iSocket.ReceiveTimeout = 2000;
                    iSocket.SendTimeout = 2000;

                    if (iSocket.Connected == true)
                    {
                        // Ugly to wait for all the data to pipe through.
                        Thread.Sleep(100);

                        // The Recieve right below needs to be in a loop to read until the length is 0.
                        Byte[] reqRecv = new Byte[iSocket.ReceiveBufferSize];
                        int recvLen = iSocket.Receive(reqRecv, SocketFlags.None);
                        string reqBuffer = Encoding.UTF8.GetString(reqRecv);
                        Hashtable headers = parseHeader(reqBuffer);
                        string reqMethod = reqBuffer.Substring(0, 4).ToUpper().Replace(" ", "");
                        string args = "";
                        string path = "";

                        if (reqMethod == "GET")
                        {
                            int methodLen = reqMethod.Length + 1;
                            int endPos = reqBuffer.IndexOf(" HTTP") - methodLen;
                            path = reqBuffer.Substring(methodLen, endPos);
                            string pathTemp = System.Uri.UnescapeDataString(path);
                            if (reqBuffer.IndexOf("?") > -1)
                            {
                                char[] sp = { '?' };
                                string[] parts = path.Split(sp, 2);
                                path = parts[0];
                                args = parts[1];
                            }
                        }
                        else if (reqMethod == "POST")
                        {
                            int methodLen = reqMethod.Length + 1;
                            int endPos = reqBuffer.IndexOf(" HTTP") - methodLen;
                            if (reqBuffer.IndexOf("?") > -1)
                            {
                                endPos = reqBuffer.IndexOf("?") - methodLen;
                            }
                            path = reqBuffer.Substring(methodLen, endPos);
                            int contentLen = Int16.Parse(headers["Content-Length"].ToString());
                            args = this.parseRequest(reqBuffer, contentLen);
                        }
                        else
                        {
                            iSocket.Close();
                            iSocket.Shutdown(SocketShutdown.Both);
                            iSocket.Dispose();
                            continue;
                        }

                        // Call the function to handle the request
                        Command(path, args, headers, reqMethod, iSocket);

                        // Close the socket and clear the header hash
                        iSocket.Close();
                        iSocket.Dispose();
                        respHeader.Clear();
                    }
                }
                catch (Exception ex)
                {
                    //throw new Exception(ex.Message);
                }
            }

            this.iListener.Stop();
        }

        // Parse the incoming request header
        protected Hashtable parseHeader(string header)
        {
            Hashtable vals = new Hashtable();
            string[] headers = header.Replace("\r", "").Split('\n');
            int cnt = 0;

            foreach (string line in headers)
            {
                if (cnt > 0)
                {
                    if (line.Length == 0) { break; }
                    string[] strsep = { ": ", ":" };
                    string[] parts = line.Split(strsep, 2, System.StringSplitOptions.None);
                    if (parts.Length == 2) { vals.Add(parts[0], parts[1]); }
                }

                cnt++;
            }

            return vals;
        }

        // If the request method is POST use this to get the data
        protected string parseRequest(string header, int length)
        {
            string data = "";
            string[] headers = header.Replace("\r", "").Split('\n');
            int keeploop = 0;

            foreach (string line in headers)
            {

                if (keeploop == 0)
                {
                    if (line.Length == 0)
                    {
                        keeploop = 1;
                    }
                }
                else if (keeploop == 1)
                {
                    data = line.Substring(0, length);
                }
                else
                {
                    break;
                }
            }

            return data;
        }

        // Start the server
        public void startServer()
        {
            th = new Thread(new ThreadStart(this.Listen));
            th.Start();
        }

        // Stop the server
        public void stopServer()
        {
            this.goon = false;
        }

        // constructors
        public iServer(int port) { iPort = port; }
        public iServer() { iPort = DEFAULT_PORT; }
    }
}