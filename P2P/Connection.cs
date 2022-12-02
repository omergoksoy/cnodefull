using System.Net;
using System.Net.Sockets;
using NPM = Notus.P2P.Manager;
namespace Notus.P2P
{
    internal class Connection : IDisposable
    {
        private string peerId;
        private Socket socket;
        private Action<string> messageReceivedCallback;

        public void Send(string message)
        {
            this.socket.Send(System.Text.Encoding.UTF8.GetBytes(message));
        }

        private void Receive()
        {
            var buffer = new byte[1024];
            var bytesReceived = this.socket.Receive(buffer);
            var message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            this.messageReceivedCallback(message);
        }

        public Connection(string peerId, Socket handler, Action<string> messageReceivedCallback)
        {
            this.peerId = peerId;
            this.messageReceivedCallback = messageReceivedCallback;
            this.socket = handler;

            var thread = new Thread(this.Receive);
            thread.Start();
        }

        public Connection(string peerId, IPEndPoint peerEndPoint, Action<string> messageReceivedCallback)
        {
            this.peerId = peerId;
            this.messageReceivedCallback = messageReceivedCallback;
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.socket.Connect(peerEndPoint);

            var thread = new Thread(this.Receive);
            thread.Start();
        }

        public void Dispose()
        {
            this.socket.Dispose();
        }
    }
}