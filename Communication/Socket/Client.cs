using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NVC = Notus.Variable.Constant;
namespace Notus.Communication.Sync.Socket
{
    //socket-exception
    public class Client : IDisposable
    {
        private byte[] byteArr = new byte[8192];
        System.Net.Sockets.Socket sender = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //public System.Net.Sockets.Socket? listener = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public void Start(string ipAddress)
        {
            try
            {
                sender.Connect(
                    new IPEndPoint(
                        IPAddress.Parse(ipAddress),
                        NVC.DefaultMessagePortNo
                    )
                );

                Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
        }
        public void Send(string messageText)
        {
            byte[] msg = Encoding.ASCII.GetBytes(messageText + "<theend>");
            int bytesSent = sender.Send(msg);
            Console.WriteLine("Sended data size {0}", bytesSent);
            int bytesArrLen = sender.Receive(byteArr);
            Console.WriteLine("The Server says : {0}", Encoding.ASCII.GetString(byteArr, 0, bytesArrLen));
        }
        public Client(string ipAddress = "")
        {
            if (ipAddress.Length > 0)
            {
                Start(ipAddress);
            }
        }
        ~Client()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (sender != null)
            {
                sender.Shutdown(SocketShutdown.Both);
                sender.Close();
                sender.Dispose();
            }
        }

    }
}
