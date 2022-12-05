using System.Text.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using NP = Notus.Print;
using NP2P = Notus.P2P;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.P2P
{
    public class Manager : IDisposable
    {
        public ConcurrentDictionary<ulong, NVS.PeerDetailStruct> Old = new();
        public ConcurrentDictionary<ulong, NVS.PeerDetailStruct> Now = new();
        public ConcurrentDictionary<ulong, NVS.PeerDetailStruct> Next = new();

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

        public void StopOldPeers()
        {
            foreach (var outerItem in NVG.Settings.PeerManager.Old)
            {
                bool closePeer = true;
                foreach (var innerItem in NVG.Settings.PeerManager.Now)
                {
                    if (string.Equals(outerItem.Value.WalletId, innerItem.Value.WalletId))
                    {
                        closePeer = false;
                    }
                }
                if (closePeer == true)
                {
                    NVG.Settings.PeerManager.RemovePeer(outerItem.Value.WalletId);
                }
            }
            foreach (var outerItem in NVG.Settings.PeerManager.Old)
            {
                bool closePeer = true;
                foreach (var innerItem in NVG.Settings.PeerManager.Next)
                {
                    if (string.Equals(outerItem.Value.WalletId, innerItem.Value.WalletId))
                    {
                        closePeer = false;
                    }
                }
                if (closePeer == true)
                {
                    NVG.Settings.PeerManager.RemovePeer(outerItem.Value.WalletId);
                }
            }
        }
        public void MovePeerList()
        {
            /*
           

            foreach (var outerItem in NVG.Settings.PeerManager.Old)
            {
                bool closePeer = true;
                foreach (var innerItem in NVG.Settings.PeerManager.Next)
                {
                    if (string.Equals(outerItem.Value.WalletId, innerItem.Value.WalletId))
                    {
                        closePeer = false;
                    }
                }
                if (closePeer == true)
                {
                    NVG.Settings.PeerManager.RemovePeer(outerItem.Value.WalletId);
                }
            }
            Console.WriteLine("Location-02");
            Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.PeerManager.Old));
            Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.PeerManager.Now));
            Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.PeerManager.Next));
            */

            NVG.Settings.PeerManager.Old.Clear();
            foreach (var item in NVG.Settings.PeerManager.Now)
            {
                NVG.Settings.PeerManager.Old.TryAdd(item.Key, item.Value);
            }
            NVG.Settings.PeerManager.Now.Clear();

            foreach (var item in NVG.Settings.PeerManager.Next)
            {
                NVG.Settings.PeerManager.Now.TryAdd(item.Key, item.Value);
            }
            NVG.Settings.PeerManager.Next.Clear();
            Console.WriteLine("Location-02");
            Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.PeerManager.Old));
            Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.PeerManager.Now));
            Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.PeerManager.Next));
        }

        public void StartAllPeers()
        {
            foreach (var item in NVG.Settings.PeerManager.Now)
            {
                NVG.Settings.PeerManager.AddPeer(item.Value.WalletId, item.Value.IpAddress);
            }
            foreach (var item in NVG.Settings.PeerManager.Next)
            {
                NVG.Settings.PeerManager.AddPeer(item.Value.WalletId, item.Value.IpAddress);
            }
        }
        public void AddPeer(string peerId, string ipAddress)
        {
            if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, peerId) == true)
                return;

            if (this.Peers.ContainsKey(peerId))
                return;

            bool result = this.Peers.TryAdd(peerId,
                new NP2P.Connection(
                    peerId,
                    new IPEndPoint(
                        IPAddress.Parse(ipAddress),
                        Notus.Network.Node.GetP2PPort()
                    ),
                    this.onReceive
                )
            );
            if (result == true)
                NP.Success("Peer Startted -> " + peerId);
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
