using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NVC = Notus.Variable.Constant;
using NP = Notus.Print;
namespace Notus.Message
{
    public class Subscriber : IDisposable
    {
        private byte[] byteArr = new byte[8192];
        System.Net.Sockets.Socket sender = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public bool Start(string ipAddress, int portNo = 0)
        {
            try
            {
                if (portNo == 0)
                {
                    portNo = Notus.Network.Node.GetNetworkPort() + 10;
                }
                sender.Connect(new IPEndPoint(IPAddress.Parse(ipAddress), portNo));
                control-POİNT
                Console.WriteLine("Socket connected to {0}", sender.RemoteEndPoint.ToString());
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.ToString());
            }
            return false;
        }
        public string Send(string messageText)
        {
            int bytesSent = sender.Send(Encoding.ASCII.GetBytes(messageText));
            if (bytesSent > 0)
            {
                NP.Info("Control-Point-3-For - Receive");
                int bytesArrLen = sender.Receive(byteArr);
                NP.Info("Control-Point-4-For - Receive");
                return Encoding.UTF8.GetString(byteArr, 0, bytesArrLen);
            }
            return string.Empty;
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
