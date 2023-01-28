using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
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
        private bool DebugActivated = true;

        public Manager(IPEndPoint localEndPoint, int port, Action<string> onReceive, bool debugActivated = true)
        {
            DebugActivated = debugActivated;
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
            if (peer.connected == true)
            {
                this.Peers.TryAdd(peerId, peer);
                if (DebugActivated == true)
                {
                    NP.Info("Connected To Peer : " + peerId);
                }
                this.listener.BeginAccept(new AsyncCallback(this.AcceptCallback), this.listener);
            }
            else
            {
                if (DebugActivated == true)
                {
                    NP.Danger("Connection Error -> " + peerId);
                }
            }
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

            IPAddress ipObj = IPAddress.Parse(ipAddress);
            IPEndPoint localEndPoint = new IPEndPoint(ipObj, Notus.Network.Node.GetP2PPort());
            bool result = this.Peers.TryAdd(peerId, new NP2P.Connection(peerId, localEndPoint, this.onReceive));
            if (result == true)
            {
                if (DebugActivated == true)
                {
                    NP.Success("Peer Started -> " + peerId.Substring(0, 7) + "..." + peerId.Substring(peerId.Length - 7));
                }
            }
        }

        public void RemoveAll()
        {
            if (DebugActivated == true)
            {
                NP.Info("All P2P Connection Is Closing");
            }
            List<string> tmpRemoveList = new();
            foreach (var item in this.Peers)
            {
                tmpRemoveList.Add(item.Key);
            }
            for (int i = 0; i < tmpRemoveList.Count; i++)
            {
                this.RemovePeer(tmpRemoveList[i], false);
            }
            if (DebugActivated == true)
            {
                NP.Success("All P2P Connection Cleared");
            }
        }
        public void RemovePeer(string peerId, bool showMoreDetails = true)
        {
            if (this.Peers.ContainsKey(peerId))
            {
                this.Peers.TryRemove(peerId, out _);
                if (showMoreDetails == true)
                {
                    NP.Warning("P2P Connection Closed ->" + peerId.Substring(0, 7) + "..." + peerId.Substring(peerId.Length - 7));
                }
            }
        }

        public bool IsStarted(string peerId)
        {
            return this.Peers.ContainsKey(peerId);
        }
        public void SendWithTask(NVS.NodeQueueInfo nodeItem, string messageText)
        {
            if (nodeItem.Status != NVS.NodeStatus.Online)
                return;

            if (string.Equals(nodeItem.IP.Wallet, NVG.Settings.Nodes.My.IP.Wallet) == true)
                return;

            Task.Run(() =>
            {
                NVG.Settings.PeerManager.Send(nodeItem.IP.Wallet, messageText);
            });
        }
        public bool Send(string peerId, string message, bool removePeerIfOffline = true)
        {
            //Console.WriteLine("Peer Send : " + peerId + " -> " + message);
            if (this.Peers.ContainsKey(peerId))
            {
                if (this.Peers[peerId].Send(message) == true)
                {
                    return true;
                }
                if (removePeerIfOffline == true)
                {
                    this.RemovePeer(peerId);
                }
            }
            return false;
        }

        public void SendAll(string message, bool removePeerIfOffline = true)
        {
            foreach (var peer in this.Peers)
            {
                if (peer.Value.Send(message) == false)
                {
                    if (removePeerIfOffline == true)
                    {
                        this.RemovePeer(peer.Key);
                    }
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
