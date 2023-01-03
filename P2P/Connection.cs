using System.Net;
using System.Net.Sockets;
using System.Text;
namespace Notus.P2P
{
    internal class Connection : IDisposable
    {
        private string peerId;
        private Socket socket;
        public bool connected;
        private bool exitLoop;
        private Action<string> messageReceivedCallback;

        public bool Send(string message)
        {
            if (message.Length == 0)
                return false;

            try
            {
                int bytesSent = this.socket.Send(Encoding.ASCII.GetBytes(message));
                if (bytesSent > 0)
                    return true;
            }
            catch
            {
            }
            return false;
        }

        private void Receive()
        {
            while (this.exitLoop == false)
            {
                try
                {
                    byte[] bytes = new byte[8192];
                    int bytesRec = this.socket.Receive(bytes);
                    if (bytesRec > 0)
                    {
                        string message = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        Console.WriteLine("Peer Receive : " + message);
                        this.messageReceivedCallback(message);
                    }
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e.ToString());
                }
            }
        }

        public Connection(string peerId, Socket handler, Action<string> messageReceivedCallback)
        {
            this.connected = true;
            this.exitLoop = false;
            this.peerId = peerId;
            this.messageReceivedCallback = messageReceivedCallback;
            this.socket = handler;

            var thread = new Thread(this.Receive);
            thread.Start();
        }

        public Connection(string peerId, IPEndPoint peerEndPoint, Action<string> messageReceivedCallback)
        {
            this.connected = false;
            this.exitLoop = false;
            try
            {
                this.peerId = peerId;
                this.messageReceivedCallback = messageReceivedCallback;
                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.socket.Connect(peerEndPoint);
                this.connected = true;
            }
            catch { }
            if (this.connected == true)
            {

                var thread = new Thread(this.Receive);
                thread.Start();
            }
        }

        public void Dispose()
        {
            this.exitLoop = true;
            this.socket.Dispose();
        }
    }
}