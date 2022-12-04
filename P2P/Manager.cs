using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using NP = Notus.Print;
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
            NP.Info("Connected To Peer : " + peerId);
            this.listener.BeginAccept(new AsyncCallback(this.AcceptCallback), this.listener);
        }

        public void AddPeer(string peerId, string ipAddress)
        {
            if (this.Peers.ContainsKey(peerId))
                return;

            this.Peers.TryAdd(peerId,
                new NP2P.Connection(
                    peerId,
                    new IPEndPoint(
                        IPAddress.Parse(ipAddress),
                        Notus.Network.Node.GetP2PPort()
                    ),
                    this.onReceive
                )
            );
        }

        public void RemoveAll()
        {
            NP.Basic("All P2P Connection Cleared");
            foreach (var item in this.Peers)
            {
                this.RemovePeer(item.Key);
            }
        }
        public void RemovePeer(string peerId)
        {
            NP.Warning(peerId + " -> P2P Connection Closed");
            if (this.Peers.ContainsKey(peerId))
            {
                this.Peers.TryRemove(peerId, out _);
            }
        }

        public bool Send(string peerId, string message)
        {
            if (this.Peers.ContainsKey(peerId))
            {
                if (this.Peers[peerId].Send(message) == true)
                {
                    return true;
                }
                this.RemovePeer(peerId);
            }
            return false;
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
