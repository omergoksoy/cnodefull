using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NVC = Notus.Variable.Constant;
namespace Notus.Message
{
    //socket-exception
    public class Subscriber : IDisposable
    {
        private byte[] byteArr = new byte[8192];
        System.Net.Sockets.Socket sender = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public bool Start(string ipAddress,int portNo=0)
        {
            try
            {
                if (portNo == 0)
                {
                    portNo = Notus.Network.Node.GetNetworkPort() + 10;
                }
                sender.Connect(new IPEndPoint( IPAddress.Parse(ipAddress),portNo));
                Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint.ToString());
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            return false;
        }
        public void Send(string messageText)
        {
            int bytesSent = sender.Send(Encoding.ASCII.GetBytes(messageText));
            if (bytesSent > 0)
            {
                int bytesArrLen = sender.Receive(byteArr);
            }
        }
        public Subscriber(string ipAddress = "")
        {
            if (ipAddress.Length > 0)
            {
                Start(ipAddress);
            }
        }
        ~Subscriber()
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
