using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using NP2P = Notus.P2P;
namespace Notus.P2P
{
    public class Manager : IDisposable
    {
        private ConcurrentDictionary<string, NP2P.Connection> Peers = new();
        private Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private Action<string> onReceive;

        public Manager(IPEndPoint localEndPoint, int port, Action<string> onReceive)
        {
            this.listener.Bind(localEndPoint);
            this.listener.Listen(port);
            this.listener.BeginAccept(new AsyncCallback(this.AcceptCallback), this.listener);
            this.onReceive = onReceive;
        }

        public void AcceptCallback(IAsyncResult ar)
        {
            var listener = (Socket)ar.AsyncState;
            var handler = listener.EndAccept(ar);
            var peerEndPoint = (IPEndPoint)handler.RemoteEndPoint;
            var peerId = peerEndPoint.ToString();
            var peer = new NP2P.Connection(peerId, handler, this.onReceive);
            this.Peers.TryAdd(peerId, peer);
            Console.WriteLine("Peer connected: " + peerId);
            this.listener.BeginAccept(new AsyncCallback(this.AcceptCallback), this.listener);
        }

        public void AddPeer(string peerId, IPEndPoint peerEndPoint)
        {
            if (this.Peers.ContainsKey(peerId))
                return;

            var peer = new NP2P.Connection(peerId, peerEndPoint, this.onReceive);
            this.Peers.TryAdd(peerId, peer);
        }

        public void RemovePeer(string peerId)
        {
            if (this.Peers.ContainsKey(peerId))
            {
                this.Peers.TryRemove(peerId, out _);
            }
        }

        public void Send(string peerId, string message)
        {
            if (this.Peers.ContainsKey(peerId))
            {
                if (this.Peers[peerId].Send(message) == false)
                {
                    Console.WriteLine("Error : " + peerId);
                    this.RemovePeer(peerId);
                }
                else
                {
                    Console.WriteLine("Success from " + peerId);
                }

            }
        }

        public void SendAll(string message)
        {
            foreach (var peer in this.Peers)
            {
                if (peer.Value.Send(message) == false)
                {
                    this.RemovePeer(peer.Key);
                    Console.WriteLine("Error : " + peer.Key);
                }
                else
                {
                    Console.WriteLine("Success from " + peer.Key);
                }

            }
        }

        public void Dispose()
        {
            this.listener.Dispose();
            foreach (var peer in this.Peers)
            {
                peer.Value.Dispose();
            }
        }
    }
}
