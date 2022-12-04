/*
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
            if (portNo == 0)
            {
                portNo = Notus.Network.Node.GetNetworkPort() + 10;
            }
            IPEndPoint connnectionPoint = new IPEndPoint(IPAddress.Parse(ipAddress), portNo);
            try
            {
                sender.Connect(connnectionPoint);
                sender.ReceiveTimeout = 2000;
                NP.Info("Message Node connected to "+ connnectionPoint.ToString());
                return true;
            }
            catch (Exception err)
            {
                NP.Danger("Message Node Connecting Error [l1]:  " + connnectionPoint.ToString() + " -> " + err.Message);
            }
            return false;
        }
        public string Send(string messageText)
        {
            int bytesSent = 0;
            try
            {
                bytesSent = sender.Send(Encoding.ASCII.GetBytes(messageText));
            }
            catch (Exception err){
                Console.WriteLine("Subscriber.cs -> Line 42");
                Console.WriteLine("Message Sending Error : " + err.Message);
            }
            if (bytesSent > 0)
            {
                int bytesArrLen = 0;
                try
                {
                    bytesArrLen = sender.Receive(byteArr);
                }
                catch (Exception err2){
                    Console.WriteLine("Subscriber.cs -> Line 58");
                    Console.WriteLine("Message Receivin Error : " + err2.Message);
                }

                if (bytesArrLen == 0)
                {
                    return string.Empty;
                }
                return Encoding.UTF8.GetString(byteArr, 0, bytesArrLen);
            }
            return string.Empty;
        }
        public Subscriber()
        {
        }
        ~Subscriber()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (sender != null)
            {
                try
                {

                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                    sender.Dispose();
                }
                catch
                {

                }
            }
        }

    }
}
*/