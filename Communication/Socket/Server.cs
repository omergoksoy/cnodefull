using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Notus.Communication.Sync.Socket
{
    //socket-exception
    public class Server : IDisposable
    {
        private bool closeSocket = false;
        private bool readyForDispose = false;
        public System.Net.Sockets.Socket? listener;
        //public System.Net.Sockets.Socket? listener = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public void Start(string ipAddress, int portNo)
        {
            listener = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            byte[] bytes = new byte[1048576];
            string content = string.Empty;

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), portNo);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (closeSocket == false)
                {
                    System.Net.Sockets.Socket handler = listener.Accept();
                    string replyData = string.Empty;
                    while (closeSocket == false)
                    {
                        int bytesRec = handler.Receive(bytes);
                        string incomeData = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        Console.WriteLine(incomeData);
                    }
                    handler.Send(System.Text.Encoding.ASCII.GetBytes("ok"));
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception ex)
            {

            }
            readyForDispose = true;
        }
        public Server()
        {
            closeSocket = false;
        }
        ~Server()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (listener != null)
            {
                closeSocket = true;
                while (readyForDispose == false)
                {
                    Thread.Sleep(5);
                }
                listener.Close();
                listener.Dispose();
            }
        }

    }
}
