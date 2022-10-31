using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Notus.Communication.Sync
{
    public class Server : IDisposable
    {
        public System.Net.Sockets.Socket? listener;
        //public System.Net.Sockets.Socket? listener = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public void Start()
        {
            bu sync soket düzenlenecek...
            bu sync soket düzenlenecek...
            bu sync soket düzenlenecek...
            bu sync soket düzenlenecek...
            bu sync soket düzenlenecek...

            listener = new System.Net.Sockets.Socket(
                AddressFamily.InterNetwork, 
                SocketType.Stream, 
                ProtocolType.Tcp
            );
            byte[] bytes = new Byte[1024000];
            String content = String.Empty;

            IPAddress ipAddress = IPAddress.Parse("192.168.1.2");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 53100);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (true)
                {
                    //// Program is suspended while waiting for an incoming connection.
                    Socket handler = listener.Accept();
                    string replyData = string.Empty;

                    // An incoming connection needs to be processed.
                    while (true)
                    {
                        bytes = new byte[1024000];
                        int bytesRec = handler.Receive(bytes);
                        string incomeData = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        /*
                        strBuilder.Append();
                        if (strBuilder.Length > 1)
                        {
                            content = strBuilder.ToString();
                            byte[] xdata = Convert.FromBase64String(content);
                            using (var mStream = new MemoryStream(xdata, 0, xdata.Length))
                            {
                                pictureBox1.Image = Image.FromStream(mStream, true);
                            }
                        }
                        */
                    }
                    byte[] msg = Encoding.ASCII.GetBytes(replyData);

                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
        }
        public Server()
        {
        }
        ~Server()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (listener != null)
            {
                listener.Close();
                listener.Dispose();
            }
        }

    }
}
