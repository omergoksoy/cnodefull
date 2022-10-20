using Notus.Encryption;
using Notus.Network;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NGF = Notus.Variable.Globals.Functions;
using NVC = Notus.Variable.Constant;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
using ND = Notus.Date;
using NP = Notus.Print;
namespace Notus.Validator
{
    public class Queue : IDisposable
    {
        private bool StartingTimeAfterEnoughNode_Arrived = false;
        private DateTime StartingTimeAfterEnoughNode;

        private bool WaitForEnoughNode_Val = true;
        public bool WaitForEnoughNode
        {
            get { return WaitForEnoughNode_Val; }
            set { WaitForEnoughNode_Val = value; }
        }

        public bool NotEnoughNode_Printed = false;
        public bool NotEnoughNode_Val = true;
        public bool NotEnoughNode
        {
            get { return NotEnoughNode_Val; }
        }
        public int ActiveNodeCount_Val = 0;
        public int ActiveNodeCount
        {
            get { return ActiveNodeCount_Val; }
        }

        private bool Val_Ready = false;
        public bool Ready
        {
            get { return Val_Ready; }
        }
        private bool MyTurn_Val = false;
        public bool MyTurn
        {
            get { return MyTurn_Val; }
            set { MyTurn_Val = value; }
        }
        private bool SyncReady = true;

        private SortedDictionary<string, NVS.IpInfo> MainAddressList = new SortedDictionary<string, NVS.IpInfo>();
        private string MainAddressListHash = string.Empty;

        private ConcurrentDictionary<string, NVS.NodeQueueInfo>? PreviousNodeList = new ConcurrentDictionary<string, NVS.NodeQueueInfo>();
        public ConcurrentDictionary<string, NVS.NodeQueueInfo>? SyncNodeList
        {
            get { return PreviousNodeList; }
            set { PreviousNodeList = value; }
        }
        private ConcurrentDictionary<string, int> NodeTurnCount = new ConcurrentDictionary<string, int>();
        private ConcurrentDictionary<string, NVS.NodeQueueInfo> NodeList = new ConcurrentDictionary<string, NVS.NodeQueueInfo>();
        private ConcurrentDictionary<int, string> NodeOrderList = new ConcurrentDictionary<int, string>();
        private ConcurrentDictionary<string, DateTime> NodeTimeBasedOrderList = new ConcurrentDictionary<string, DateTime>();

        private Notus.Mempool ObjMp_NodeList;
        private bool ExitFromLoop = false;
        private string LastHashForStoreList = "#####";
        private string NodeListHash = "#";

        private DateTime LastPingTime;

        public System.Func<Notus.Variable.Class.BlockData, bool>? Func_NewBlockIncome = null;

        //empty blok için kontrolü yapacak olan node'u seçen fonksiyon
        public NVE.ValidatorOrder EmptyTimer()
        {
            return NVE.ValidatorOrder.Primary;
        }

        //oluşturulacak blokları kimin oluşturacağını seçen fonksiyon
        public void Distrubute(long blockRowNo, int blockType = 0)
        {
            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
            {
                if (string.Equals(NVG.Settings.Nodes.My.HexKey, entry.Key) == false && entry.Value.Status == NVS.NodeStatus.Online)
                {
                    NP.Info(NVG.Settings,
                        "Distrubuting " +
                        blockRowNo.ToString() + "[ " +
                        blockType.ToString() +
                        " ] . Block To " +
                        entry.Value.IP.IpAddress + ":" +
                        entry.Value.IP.Port.ToString()
                    );
                    SendMessage(entry.Value.IP.IpAddress, entry.Value.IP.Port,
                        "<block>" + blockRowNo.ToString() + ":" +
                        NVG.Settings.NodeWallet.WalletKey + "</block>",
                        true
                    );
                }
            }
        }

        private DateTime RefreshNtpTime()
        {
            DateTime tmpNtpTime = NGF.GetUtcNowFromNtp();
            const ulong secondPointConst = 1000;

            DateTime afterMiliSecondTime = tmpNtpTime.AddMilliseconds(
                secondPointConst + (secondPointConst - (ND.ToLong(tmpNtpTime) % secondPointConst))
            );
            double secondVal = NVC.NodeStartingSync +
                (NVC.NodeStartingSync -
                    (
                        ulong.Parse(
                            afterMiliSecondTime.ToString("ss")
                        ) %
                        NVC.NodeStartingSync
                    )
                );
            return afterMiliSecondTime.AddSeconds(secondVal);
        }
        private void PingOtherNodes()
        {
            NP.Info(NVG.Settings, "Waiting For Node Sync", false);
            bool tmpExitWhileLoop = false;
            while (tmpExitWhileLoop == false)
            {
                KeyValuePair<string, NVS.IpInfo>[]? tmpMainList = MainAddressList.ToArray();
                if (tmpMainList != null)
                {
                    for (int i = 0; i < tmpMainList.Length && tmpExitWhileLoop == false; i++)
                    {
                        if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                        {
                            tmpExitWhileLoop = Notus.Toolbox.Network.PingToNode(tmpMainList[i].Value);
                        }
                    }
                }
                if (tmpExitWhileLoop == false)
                {
                    Thread.Sleep(5500);
                }
            }
        }
        private string CalculateMainAddressListHash()
        {
            List<UInt64> tmpAllWordlTimeList = new List<UInt64>();
            foreach (KeyValuePair<string, NVS.IpInfo> entry in MainAddressList)
            {
                tmpAllWordlTimeList.Add(UInt64.Parse(entry.Key, NumberStyles.AllowHexSpecifier));
            }
            tmpAllWordlTimeList.Sort();
            return new Notus.Hash().CommonHash("sha1", JsonSerializer.Serialize(tmpAllWordlTimeList));
        }
        public List<NVS.IpInfo> GiveMeNodeList()
        {
            List<NVS.IpInfo> tmpNodeList = new List<NVS.IpInfo>();
            foreach (KeyValuePair<string, NVS.IpInfo> entry in MainAddressList)
            {
                if (string.Equals(entry.Key, NVG.Settings.Nodes.My.HexKey) == false)
                {
                    tmpNodeList.Add(new NVS.IpInfo()
                    {
                        IpAddress = entry.Value.IpAddress,
                        Port = entry.Value.Port
                    });
                }
            }
            return tmpNodeList;
        }
        private void AddToMainAddressList(string ipAddress, int portNo, bool storeToDb = true)
        {
            string tmpHexKeyStr = Notus.Toolbox.Network.IpAndPortToHex(ipAddress, portNo);
            if (MainAddressList.ContainsKey(tmpHexKeyStr) == false)
            {
                MainAddressList.Add(tmpHexKeyStr, new NVS.IpInfo()
                {
                    IpAddress = ipAddress,
                    Port = portNo,
                });
                if (storeToDb == true)
                {
                    StoreNodeListToDb();
                }
                MainAddressListHash = CalculateMainAddressListHash();
            }
        }
        private void AddToNodeList(NVS.NodeQueueInfo NodeQueueInfo)
        {
            if (NodeList.ContainsKey(NodeQueueInfo.HexKey))
            {
                NodeList[NodeQueueInfo.HexKey] = NodeQueueInfo;
            }
            else
            {
                NodeList.TryAdd(NodeQueueInfo.HexKey, NodeQueueInfo);
            }
            AddToMainAddressList(NodeQueueInfo.IP.IpAddress, NodeQueueInfo.IP.Port);
        }
        private string CalculateMyNodeListHash()
        {
            Dictionary<string, NVS.NodeQueueInfo>? tmpNodeList = JsonSerializer.Deserialize<Dictionary<string, NVS.NodeQueueInfo>>(JsonSerializer.Serialize(NodeList));
            if (tmpNodeList == null)
            {
                return string.Empty;
            }

            List<string> tmpAllAddressList = new List<string>();
            List<string> tmpAllWalletList = new List<string>();
            List<long> tmpAllWordlTimeList = new List<long>();
            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in tmpNodeList)
            {
                string tmpAddressListHex = Notus.Toolbox.Network.IpAndPortToHex(entry.Value.IP);
                if (tmpAllAddressList.IndexOf(tmpAddressListHex) < 0)
                {
                    tmpAllAddressList.Add(tmpAddressListHex);
                    if (entry.Value.IP.Wallet.Length == 0)
                    {
                        tmpAllWalletList.Add("#");
                    }
                    else
                    {
                        tmpAllWalletList.Add(entry.Value.IP.Wallet);
                    }
                }
            }
            tmpAllAddressList.Sort();
            tmpAllWalletList.Sort();
            tmpAllWordlTimeList.Sort();

            NodeListHash = new Notus.Hash().CommonHash("sha1",
                JsonSerializer.Serialize(tmpAllAddressList) + ":" +
                JsonSerializer.Serialize(tmpAllWalletList) + ":" +
                JsonSerializer.Serialize(tmpAllWordlTimeList)
            );

            return NodeListHash;
        }
        private void StoreNodeListToDb()
        {
            bool storeList = true;
            string tmpNodeListStr = ObjMp_NodeList.Get("ip_list", "");
            if (tmpNodeListStr.Length > 0)
            {
                SortedDictionary<string, NVS.IpInfo>? tmpDbNodeList = JsonSerializer.Deserialize<SortedDictionary<string, NVS.IpInfo>>(tmpNodeListStr);
                if (
                    string.Equals(
                        JsonSerializer.Serialize(tmpDbNodeList),
                        JsonSerializer.Serialize(MainAddressList)
                    )
                )
                {
                    storeList = false;
                }
            }
            if (storeList)
            {
                ObjMp_NodeList.Set("ip_list", JsonSerializer.Serialize(MainAddressList), true);
            }
        }
        private bool CheckXmlTag(string rawDataStr, string tagName)
        {
            return ((rawDataStr.IndexOf("<" + tagName + ">") >= 0 && rawDataStr.IndexOf("</" + tagName + ">") >= 0) ? true : false);
        }
        private string GetPureText(string rawDataStr, string tagName)
        {
            rawDataStr = rawDataStr.Replace("<" + tagName + ">", "");
            return rawDataStr.Replace("</" + tagName + ">", "");
        }
        public string Process(NVS.HttpRequestDetails incomeData)
        {
            string reponseText = ProcessIncomeData(incomeData.PostParams["data"]);
            NodeIsOnline(incomeData.UrlList[2].ToLower());
            return reponseText;
        }
        private string ProcessIncomeData(string incomeData)
        {
            if (CheckXmlTag(incomeData, "block"))
            {
                string incomeDataStr = GetPureText(incomeData, "block");
                if (incomeDataStr.IndexOf(":") < 0)
                {
                    return "error-msg";
                }

                string[] tmpArr = incomeDataStr.Split(":");
                long tmpBlockNo = long.Parse(tmpArr[0]);
                string tmpNodeWalletKey = tmpArr[1];
                string tmpIpAddress = string.Empty;
                int tmpPortNo = 0;
                foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
                {
                    if (string.Equals(entry.Value.IP.Wallet, tmpNodeWalletKey))
                    {
                        tmpIpAddress = entry.Value.IP.IpAddress;
                        tmpPortNo = entry.Value.IP.Port;
                    }
                    if (
                        entry.Value.Status == NVS.NodeStatus.Online &&
                        entry.Value.Ready == true
                    //&& entry.Value.ErrorCount == 0
                    )
                    {

                    }
                }
                if (tmpPortNo == 0)
                {
                    return "fncResult-port-zero";
                }
                Variable.Class.BlockData? tmpBlockData =
                    Notus.Toolbox.Network.GetBlockFromNode(tmpIpAddress, tmpPortNo, tmpBlockNo, NVG.Settings);
                if (tmpBlockData == null)
                {
                    return "tmpError-true";
                }
                if (Func_NewBlockIncome != null)
                {
                    bool fncResult = Func_NewBlockIncome(tmpBlockData);
                    if (fncResult == true)
                    {
                        return "done";
                    }
                }
                return "fncResult-false";
            }

            if (CheckXmlTag(incomeData, "when"))
            {
                StartingTimeAfterEnoughNode = ND.ToDateTime(GetPureText(incomeData, "when"));
                NVG.NodeQueue.Starting = Notus.Time.DateTimeToUlong(StartingTimeAfterEnoughNode);
                NVG.NodeQueue.OrderCount = 1;
                NVG.NodeQueue.Begin = true;
                StartingTimeAfterEnoughNode_Arrived = true;
                return "done";
            }
            if (CheckXmlTag(incomeData, "hash"))
            {
                incomeData = GetPureText(incomeData, "hash");
                string[] tmpHashPart = incomeData.Split(':');
                if (string.Equals(tmpHashPart[0], MainAddressListHash.Substring(0, 20)) == false)
                {
                    return "1";
                }

                if (string.Equals(tmpHashPart[1], NodeListHash.Substring(0, 20)) == false)
                {
                    return "2";
                }
                return "0";
            }
            if (CheckXmlTag(incomeData, "lhash"))
            {
                incomeData = GetPureText(incomeData, "lhash");
                return (string.Equals(incomeData, MainAddressListHash) == true ? "1" : "0");
            }
            if (CheckXmlTag(incomeData, "nList"))
            {
                incomeData = GetPureText(incomeData, "nList");
                SortedDictionary<string, NVS.IpInfo>? tmpNodeList = JsonSerializer.Deserialize<SortedDictionary<string, NVS.IpInfo>>(incomeData);
                if (tmpNodeList == null)
                {
                    return "0";
                }
                foreach (KeyValuePair<string, NVS.IpInfo> entry in tmpNodeList)
                {
                    AddToMainAddressList(entry.Value.IpAddress, entry.Value.Port, true);
                }
                return "1";
            }


            if (CheckXmlTag(incomeData, "ready"))
            {
                incomeData = GetPureText(incomeData, "ready");
                Console.WriteLine("Ready Income : " + incomeData);
                foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
                {
                    if (string.Equals(entry.Value.IP.Wallet, incomeData) == true)
                    {
                        NodeList[entry.Key].Ready = true;
                    }
                }
                return "done";
            }
            if (CheckXmlTag(incomeData, "rNode"))
            {
                return "<node>" + JsonSerializer.Serialize(NodeList[NVG.Settings.Nodes.My.HexKey]) + "</node>";
            }
            if (CheckXmlTag(incomeData, "node"))
            {
                incomeData = GetPureText(incomeData, "node");
                try
                {
                    NVS.NodeQueueInfo? tmpNodeQueueInfo =
                        JsonSerializer.Deserialize<NVS.NodeQueueInfo>(incomeData);
                    if (tmpNodeQueueInfo != null)
                    {
                        AddToNodeList(tmpNodeQueueInfo);
                        return "1";
                    }
                }
                catch { }
                return "0";
                //return "<node>" + JsonSerializer.Serialize(NodeList[NVG.Settings.Nodes.My.HexKey]) + "</node>";
            }
            if (CheckXmlTag(incomeData, "list"))
            {
                incomeData = GetPureText(incomeData, "list");
                SortedDictionary<string, NVS.IpInfo>? tmpNodeList = JsonSerializer.Deserialize<SortedDictionary<string, NVS.IpInfo>>(incomeData);
                if (tmpNodeList == null)
                {
                    return "<err>1</err>";
                }
                foreach (KeyValuePair<string, NVS.IpInfo> entry in tmpNodeList)
                {
                    AddToMainAddressList(entry.Value.IpAddress, entry.Value.Port, true);
                }
                return "<list>" + JsonSerializer.Serialize(MainAddressList) + "</list>";
            }
            return "<err>1</err>";
        }
        private void NodeError(string nodeHexText)
        {
            if (NodeList.ContainsKey(nodeHexText) == true)
            {
                //NodeList[nodeHexText].ErrorCount++;
                NodeList[nodeHexText].Status = NVS.NodeStatus.Offline;
                NodeList[nodeHexText].Ready = false;
                //NodeList[nodeHexText].ErrorTime = Notus.Time.NowNtpTimeToUlong();

                //NodeList[nodeHexText].Time.Error = DateTime.Now;
            }
        }
        private void NodeIsOnline(string nodeHexText)
        {
            if (NodeList.ContainsKey(nodeHexText) == true)
            {
                NodeList[nodeHexText].Status = NVS.NodeStatus.Online;
            }
        }
        private string SendMessage(NVS.NodeInfo receiverIp, string messageText, bool executeErrorControl)
        {
            return SendMessage(receiverIp.IpAddress, receiverIp.Port, messageText, executeErrorControl);
        }
        private string SendMessage(string receiverIpAddress, int receiverPortNo, string messageText, bool executeErrorControl)
        {

            string tmpNodeHexStr = Notus.Toolbox.Network.IpAndPortToHex(receiverIpAddress, receiverPortNo);
            //TimeSpan tmpErrorDiff = DateTime.Now - NodeList[tmpNodeHexStr].Time.Error;
            //if (tmpErrorDiff.TotalSeconds > 60)
            if (100 > 60)
            {
                string urlPath =
                    Notus.Network.Node.MakeHttpListenerPath(receiverIpAddress, receiverPortNo) +
                    "queue/node/" + tmpNodeHexStr;
                (bool worksCorrent, string incodeResponse) = Notus.Communication.Request.PostSync(
                    urlPath,
                    new Dictionary<string, string>()
                    {
                        { "data",messageText }
                    },
                    2,
                    true,
                    false
                );
                //Console.WriteLine("Sending : " + urlPath);
                //Console.WriteLine(worksCorrent);
                //Console.WriteLine(incodeResponse);
                if (worksCorrent == true)
                {
                    //NodeList[tmpNodeHexStr].ErrorCount = 0;
                    NodeList[tmpNodeHexStr].Status = NVS.NodeStatus.Online;
                    //NodeList[tmpNodeHexStr].Time.Error = NVC.DefaultTime;
                    return incodeResponse;
                }
                NodeError(tmpNodeHexStr);
            }
            return string.Empty;
        }
        private string SendMessageED(string nodeHex, string receiverIpAddress, int receiverPortNo, string messageText)
        {
            (bool worksCorrent, string incodeResponse) = Notus.Communication.Request.PostSync(
                Notus.Network.Node.MakeHttpListenerPath(receiverIpAddress, receiverPortNo) +
                "queue/node/" + nodeHex,
                new Dictionary<string, string>()
                {
                    { "data",messageText }
                },
                2,
                true,
                false
            );
            if (worksCorrent == true)
            {
                return incodeResponse;
            }
            return string.Empty;
        }
        private string Message_Hash_ViaSocket(string _ipAddress, int _portNo)
        {
            return SendMessage(
                _ipAddress,
                _portNo,
                "<hash>" +
                    MainAddressListHash.Substring(0, 20) + ":" + NodeListHash.Substring(0, 20) +
                "</hash>",
                true
            );
        }
        private void Message_Node_ViaSocket(string _ipAddress, int _portNo, string _nodeHex = "")
        {
            string responseStr = SendMessage(_ipAddress, _portNo,
                "<node>" + JsonSerializer.Serialize(NodeList[NVG.Settings.Nodes.My.HexKey]) + "</node>",
                true
            );
            if (string.Equals("err", responseStr) == false)
            {
                ProcessIncomeData(responseStr);
            }
        }
        private void Message_List_ViaSocket(string _ipAddress, int _portNo, string _nodeHex = "")
        {
            if (_nodeHex == "")
            {
                _nodeHex = Notus.Toolbox.Network.IpAndPortToHex(_ipAddress, _portNo);
            }
            string _nodeKeyText = _nodeHex + "list";
            string tmpReturnStr = SendMessage(_ipAddress, _portNo, "<list>" + JsonSerializer.Serialize(MainAddressList) + "</list>", true);
            if (string.Equals("err", tmpReturnStr) == false)
            {
                ProcessIncomeData(tmpReturnStr);
            }
        }
        private void MainLoop()
        {
            while (ExitFromLoop == false)
            {
                //burası belirli periyotlarda hash gönderiminin yapıldığı kod grubu
                if ((DateTime.Now - LastPingTime).TotalSeconds > 20 || SyncReady == false)
                {
                    bool innerControlLoop = false;
                    string tmpData = string.Empty;
                    while (innerControlLoop == false)
                    {
                        try
                        {
                            tmpData = JsonSerializer.Serialize(MainAddressList);
                            innerControlLoop = true;
                        }
                        catch { }
                    }
                    SortedDictionary<string, NVS.IpInfo>? tmpMainAddressList =
                        JsonSerializer.Deserialize<SortedDictionary<string, NVS.IpInfo>>(tmpData);
                    bool tmpRefreshNodeDetails = false;
                    if (tmpMainAddressList != null)
                    {
                        foreach (KeyValuePair<string, NVS.IpInfo> entry in tmpMainAddressList)
                        {
                            string tmpNodeHexStr = Notus.Toolbox.Network.IpAndPortToHex(entry.Value);
                            if (string.Equals(NVG.Settings.Nodes.My.HexKey, tmpNodeHexStr) == false)
                            {
                                string tmpReturnStr = Message_Hash_ViaSocket(entry.Value.IpAddress, entry.Value.Port);
                                if (tmpReturnStr == "1") // list not equal
                                {
                                    Message_List_ViaSocket(entry.Value.IpAddress, entry.Value.Port, tmpNodeHexStr);
                                }

                                if (tmpReturnStr == "2") // list equal but node hash different
                                {
                                    tmpRefreshNodeDetails = true;
                                }

                                if (tmpReturnStr == "0") // list and node hash are equal
                                {
                                }

                                if (tmpReturnStr == "err") // socket comm error
                                {
                                    tmpRefreshNodeDetails = true;
                                }
                            }
                        }
                    }
                    if (tmpRefreshNodeDetails == true)
                    {
                        foreach (KeyValuePair<string, NVS.IpInfo> entry in tmpMainAddressList)
                        {
                            string tmpNodeHexStr = Notus.Toolbox.Network.IpAndPortToHex(entry.Value);
                            if (string.Equals(NVG.Settings.Nodes.My.HexKey, tmpNodeHexStr) == false)
                            {
                                Message_Node_ViaSocket(entry.Value.IpAddress, entry.Value.Port, tmpNodeHexStr);
                            }
                        }
                    }
                    LastPingTime = DateTime.Now;
                }

                // burada durumu bilinmeyen nodeların bilgilerinin sorgulandığı kısım
                foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
                {
                    bool tmpRefreshNodeDetails = false;
                    string tmpCheckHex = Notus.Toolbox.Network.IpAndPortToHex(entry.Value.IP);
                    if (entry.Value.Status == NVS.NodeStatus.Unknown)
                    {
                        tmpRefreshNodeDetails = true;
                    }
                    if (tmpRefreshNodeDetails == true)
                    {
                        Message_Node_ViaSocket(entry.Value.IP.IpAddress, entry.Value.IP.Port, tmpCheckHex);
                    }
                }

                //NodeList[NVG.Settings.Nodes.My.HexKey].NodeHash = CalculateMyNodeListHash();
                int nodeCount = 0;
                SyncReady = true;
                //burada eğer nodeların hashleri farklı ise senkron olacağı kısım
                foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
                {
                    if (entry.Value.Status == NVS.NodeStatus.Online /* && entry.Value.ErrorCount == 0 */)
                    {
                        nodeCount++;
                        string tmpCheckHex = Notus.Toolbox.Network.IpAndPortToHex(entry.Value.IP);
                        if (string.Equals(NVG.Settings.Nodes.My.HexKey, tmpCheckHex) == false)
                        {

                            //burası beklemeye alındı
                            /*
                            if (NodeListHash != entry.Value.NodeHash)
                            {
                                SyncReady = false;
                                Message_Node_ViaSocket(entry.Value.IP.IpAddress, entry.Value.IP.Port, tmpCheckHex);
                            }
                            */
                        }
                    }
                }
                //Console.WriteLine("nodeCount : " + nodeCount.ToString());
                if (nodeCount == 0)
                {
                    SyncReady = false;
                }

                //Console.WriteLine(SyncReady);
                if (SyncReady == true)
                {
                    // Console.WriteLine(NtpTime);
                    // Console.WriteLine(NextQueueValidNtpTime);
                    if (LastHashForStoreList != NodeListHash)
                    {
                        /*
                        if (NtpTime > NextQueueValidNtpTime)
                        {
                            CheckNodeCount();
                        }
                        */
                        StoreNodeListToDb();
                    }
                }
            }
        }
        public void Start()
        {
            if (NVG.Settings.LocalNode == false)
            {
                Task.Run(() =>
                {
                    MainLoop();
                });
            }
        }

        public void GenerateNodeQueue(
            ulong biggestSyncNo, 
            ulong syncStaringTime, 
            SortedDictionary<BigInteger, string> nodeWalletList
        )
        {
            ulong tmpSyncNo = syncStaringTime;

            bool exitFromInnerWhile = false;
            int firstListcount = 0;

            // her node için ayrılan süre
            int queueTimePeriod = NVC.BlockListeningForPoolTime + NVC.BlockGeneratingTime + NVC.BlockDistributingTime;

            while (exitFromInnerWhile == false)
            {
                foreach (KeyValuePair<BigInteger, string> outerEntry in nodeWalletList)
                {
                    foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
                    {
                        if (string.Equals(entry.Value.IP.Wallet, outerEntry.Value))
                        {
                            if (exitFromInnerWhile == false)
                            {
                                //Console.WriteLine(firstListcount);
                                NVG.Settings.Nodes.Queue.Add(tmpSyncNo, new NVS.NodeInfo()
                                {
                                    IpAddress = entry.Value.IP.IpAddress,
                                    Port = entry.Value.IP.Port,
                                    Wallet = entry.Value.IP.Wallet
                                });
                                tmpSyncNo = ND.AddMiliseconds(tmpSyncNo, queueTimePeriod);
                                firstListcount++;
                                if (firstListcount == 6)
                                {
                                    exitFromInnerWhile = true;
                                }
                            }
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
            {
                if (entry.Value.Status == NVS.NodeStatus.Online && entry.Value.SyncNo == biggestSyncNo)
                {
                    NodeList[entry.Key].SyncNo = syncStaringTime;
                }
            }
            /*
            if (biggestSyncNo > 0)
            {
                Console.WriteLine(JsonSerializer.Serialize(NodeList, NVC.JsonSetting));
                Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.Nodes.Queue, NVC.JsonSetting));
            }
            */
        }
        public SortedDictionary<BigInteger, string> MakeOrderToNode(ulong biggestSyncNo, ulong seedForQueue)
        {
            SortedDictionary<BigInteger, string> resultList = new SortedDictionary<BigInteger, string>();
            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
            {
                if (entry.Value.Status == NVS.NodeStatus.Online && entry.Value.SyncNo == biggestSyncNo)
                {
                    bool exitInnerWhileLoop = false;
                    int innerCount = 1;
                    while (exitInnerWhileLoop == false)
                    {
                        BigInteger intWalletNo = BigInteger.Parse(
                            "0" +
                            new Notus.Hash().CommonHash("sha1",
                                entry.Value.IP.Wallet +
                                NVC.CommonDelimeterChar +
                                entry.Value.Begin.ToString() +
                                NVC.CommonDelimeterChar +
                                seedForQueue.ToString() +
                                NVC.CommonDelimeterChar +
                                innerCount.ToString()
                            ),
                            NumberStyles.AllowHexSpecifier
                        );
                        if (resultList.ContainsKey(intWalletNo) == false)
                        {
                            resultList.Add(intWalletNo, entry.Value.IP.Wallet);
                            exitInnerWhileLoop = true;
                        }
                        else
                        {
                            innerCount++;
                        }
                    }
                }
            }
            return resultList;
        }
        public void PreStart()
        {
            if (NVG.Settings.LocalNode == true)
                return;
            if (NVG.Settings.GenesisCreated == true)
                return;
            foreach (NVS.IpInfo defaultNodeInfo in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
            {
                AddToMainAddressList(defaultNodeInfo.IpAddress, defaultNodeInfo.Port, false);
            }

            string tmpNodeListStr = ObjMp_NodeList.Get("ip_list", "");
            if (tmpNodeListStr.Length == 0)
            {
                StoreNodeListToDb();
            }
            else
            {
                SortedDictionary<string, NVS.IpInfo>? tmpDbNodeList = JsonSerializer.Deserialize<SortedDictionary<string, NVS.IpInfo>>(tmpNodeListStr);
                if (tmpDbNodeList != null)
                {
                    foreach (KeyValuePair<string, NVS.IpInfo> entry in tmpDbNodeList)
                    {
                        AddToMainAddressList(entry.Value.IpAddress, entry.Value.Port);
                    }
                }
            }
            AddToNodeList(new NVS.NodeQueueInfo()
            {
                Ready = true,
                Status = NVS.NodeStatus.Online,
                HexKey = NVG.Settings.Nodes.My.HexKey,
                Begin = NVG.Settings.Nodes.My.Begin,
                SyncNo = 0,
                Tick = ND.ToLong(NGF.GetUtcNowFromNtp()),
                IP = new NVS.NodeInfo()
                {
                    IpAddress = NVG.Settings.Nodes.My.IP.IpAddress,
                    Port = NVG.Settings.Nodes.My.IP.Port,
                    Wallet = NVG.Settings.NodeWallet.WalletKey
                },
                JoinTime = 0
            });

            foreach (KeyValuePair<string, NVS.IpInfo> entry in MainAddressList)
            {
                if (string.Equals(NVG.Settings.Nodes.My.HexKey, entry.Key) == false)
                {
                    AddToNodeList(new NVS.NodeQueueInfo()
                    {
                        Ready = false,
                        Status = NVS.NodeStatus.Unknown,
                        Begin = 0,
                        Tick = 0,
                        SyncNo = 0,
                        HexKey = Notus.Toolbox.Network.IpAndPortToHex(entry.Value.IpAddress, entry.Value.Port),
                        IP = new NVS.NodeInfo()
                        {
                            IpAddress = entry.Value.IpAddress,
                            Port = entry.Value.Port,
                            Wallet = "#"
                        },
                        JoinTime = 0
                    });
                }
            }

            NP.Info(NVG.Settings, "Node Sync Starting", false);

            //listedekilere ping atıyor, eğer 1 adet node aktif ise çıkış yapıyor...
            PingOtherNodes();

            // mevcut node ile diğer nodeların listeleri senkron hale getiriliyor
            SyncListWithNode();

            // diğer node'lara bizim kim olduğumuz söyleniyor...
            TellThemWhoTheNodeIs();

            //bu fonksyion ile amaç en çok sayıda olan sync no bulunacak
            ulong biggestSyncNo = FindBiggestSyncNo();
            StartingTimeAfterEnoughNode_Arrived = false;
            if (biggestSyncNo == 0)
            {
                NP.Info(NVG.Settings, "Active Node Count : " + NodeList.Count.ToString());
                //cüzdanların hashleri alınıp sıraya koyuluyor.
                SortedDictionary<BigInteger, string> tmpWalletList = MakeOrderToNode(biggestSyncNo, 0);

                //birinci sırada ki cüzdan seçiliyor...
                string tmpFirstWalletId = tmpWalletList.First().Value;
                if (string.Equals(tmpFirstWalletId, NVG.Settings.Nodes.My.IP.Wallet))
                {
                    Thread.Sleep(5000);
                    StartingTimeAfterEnoughNode = RefreshNtpTime();
                    ulong syncStaringTime = ND.ToLong(StartingTimeAfterEnoughNode);
                    GenerateNodeQueue(biggestSyncNo, syncStaringTime, tmpWalletList);

                    NP.Info(NVG.Settings,
                        "I'm Sending Starting (When) Time / Current : " +
                        StartingTimeAfterEnoughNode.ToString("HH:mm:ss.fff") +
                        " / " + NGF.GetUtcNowFromNtp().ToString("HH:mm:ss.fff")
                    );

                    NVG.NodeQueue.Starting = syncStaringTime;
                    NVG.NodeQueue.OrderCount = 1;
                    NVG.NodeQueue.Begin = true;

                    // diğer nodelara belirlediğimiz zaman bilgisini gönderiyoruz
                    foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
                    {
                        if (string.Equals(entry.Key, NVG.Settings.Nodes.My.HexKey) == false)
                        {
                            if (entry.Value.Status == NVS.NodeStatus.Online)
                            {
                                if (entry.Value.SyncNo == syncStaringTime)
                                {
                                    bool sendedToNode = false;
                                    while (sendedToNode == false)
                                    {
                                        string tmpResult = SendMessage(entry.Value.IP, "<when>" + syncStaringTime + "</when>", true);
                                        if (string.Equals(tmpResult, "done"))
                                        {
                                            sendedToNode = true;
                                        }
                                        else
                                        {
                                            Console.WriteLine("when-error-a-01");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    while (StartingTimeAfterEnoughNode_Arrived == false)
                    {
                        Thread.Sleep(10);
                    }

                    GenerateNodeQueue(biggestSyncNo, NVG.NodeQueue.Starting, tmpWalletList);
                    NP.Info(NVG.Settings,
                        "I'm Waiting Starting (When) Time / Current : " +
                        StartingTimeAfterEnoughNode.ToString("HH:mm:ss.fff") +
                        " /  " +
                        NGF.GetUtcNowFromNtp().ToString("HH:mm:ss.fff")
                    );
                }
            }
            else
            {
                Console.WriteLine("There Is Biggest Sync No");
                NP.ReadLine();
            }

            //Console.WriteLine(JsonSerializer.Serialize(NodeList, NVC.JsonSetting));
        }
        public void ReOrderNodeQueue(ulong currentQueueTime)
        //public void ReOrderNodeQueue(ulong currentsyncNo,ulong seedForNewQueue)
        {
            ulong biggestSyncNo = FindBiggestSyncNo();
            //Console.WriteLine("currentQueueTime : " + currentQueueTime.ToString());
            //Console.WriteLine("biggestSyncNo    : " + biggestSyncNo.ToString());
            //Console.WriteLine("sira guncellemeye geldi");
            //Console.WriteLine(JsonSerializer.Serialize(NodeList, NVC.JsonSetting));
            //NP.ReadLine();
            SortedDictionary<BigInteger, string> tmpWalletList = MakeOrderToNode(biggestSyncNo, currentQueueTime);
            GenerateNodeQueue(currentQueueTime, ND.AddMiliseconds(currentQueueTime, 1500), tmpWalletList);
            NVG.NodeQueue.OrderCount++;
        }
        private ulong FindBiggestSyncNo()
        {
            ulong resultVal = 0;
            Dictionary<ulong, int> syncNoCount = new Dictionary<ulong, int>();
            foreach (var iEntry in NodeList)
            {

                if (syncNoCount.ContainsKey(iEntry.Value.SyncNo) == false)
                {
                    syncNoCount.Add(iEntry.Value.SyncNo, 0);
                }
                syncNoCount[iEntry.Value.SyncNo]++;
            }
            int zeroCount = 0;
            int biggestCount = 0;
            ulong biggestSyncNo = 0;
            foreach (var iEntry in syncNoCount)
            {
                if (iEntry.Key == 0)
                {
                    zeroCount = zeroCount + iEntry.Value;
                }
                else
                {
                    if (iEntry.Key > biggestSyncNo)
                    {
                        biggestSyncNo = iEntry.Key;
                        biggestCount = iEntry.Value;
                    }
                    else
                    {
                        if (iEntry.Key == biggestSyncNo)
                        {
                            Console.WriteLine("Ayni SyncNo sayısına sahip node'lar var.");
                        }
                    }
                }
            }


            //eğer büyük sayılardan hiç yok ise, olan node'lar kendi aralarında birinci belirleyecek
            if (biggestCount == 0)
            {
                return 0;
            }


            /*
            if (zeroCount > biggestCount)
            {

            }

            Console.WriteLine(zeroCount);
            Console.WriteLine(biggestCount);
            NP.ReadLine();
            Console.WriteLine("biggestSyncNo [xx]: " + biggestSyncNo.ToString());
            Console.WriteLine(JsonSerializer.Serialize(syncNoCount, NVC.JsonSetting));
            */
            return biggestSyncNo;
        }
        private void TellThemWhoTheNodeIs()
        {
            KeyValuePair<string, NVS.IpInfo>[]? tmpMainList = MainAddressList.ToArray();
            if (tmpMainList != null)
            {

                ulong exactTimeLong = ND.ToLong(NGF.GetUtcNowFromNtp());
                string myNodeDataText = "<node>" + JsonSerializer.Serialize(NodeList[NVG.Settings.Nodes.My.HexKey]) + "</node>";

                bool allDone = false;
                while (allDone == false)
                {

                    // her 30 saniyede bir diğer node'ları kim olduğumu söylüyor.
                    for (int i = 0; i < tmpMainList.Length; i++)
                    {
                        bool refreshNodeInfo = false;
                        if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                        {
                            if (NodeList.ContainsKey(tmpMainList[i].Key))
                            {
                                if (NodeList[tmpMainList[i].Key].Tick == 0)
                                {
                                    refreshNodeInfo = true;
                                }
                                else
                                {
                                    long tickDiff = Math.Abs((long)(exactTimeLong - NodeList[tmpMainList[i].Key].Tick));
                                    if (tickDiff > 30000)
                                    {
                                        refreshNodeInfo = true;
                                    }

                                }
                            }
                        }
                        if (refreshNodeInfo == true)
                        {
                            string responseStr = SendMessageED(tmpMainList[i].Key,
                                tmpMainList[i].Value.IpAddress, tmpMainList[i].Value.Port, myNodeDataText
                            );
                            if (responseStr == "1")
                            {
                                NodeList[tmpMainList[i].Key].Status = NVS.NodeStatus.Online;
                            }
                        }
                    }

                    //eğer bende bilgisi olmayan node varsa bilgisini istiyor
                    bool tmpAllCheck = true;
                    for (int i = 0; i < tmpMainList.Length; i++)
                    {
                        if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                        {
                            if (NodeList.ContainsKey(tmpMainList[i].Key))
                            {
                                if (NodeList[tmpMainList[i].Key].Tick == 0)
                                {
                                    string responseStr = SendMessageED(tmpMainList[i].Key,
                                        tmpMainList[i].Value.IpAddress,
                                        tmpMainList[i].Value.Port,
                                        "<rNode>1</rNode>"
                                    );
                                    ProcessIncomeData(responseStr);
                                    tmpAllCheck = false;
                                }
                            }
                        }
                    }
                    if (tmpAllCheck == true)
                    {
                        allDone = true;
                    }
                }
            }
        }
        private void SyncListWithNode()
        {
            KeyValuePair<string, NVS.IpInfo>[]? tmpMainList = MainAddressList.ToArray();
            if (tmpMainList != null)
            {
                bool exitSyncLoop = false;
                while (exitSyncLoop == false)
                {
                    for (int i = 0; i < tmpMainList.Length; i++)
                    {
                        if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                        {
                            bool exitListSendingLoop = false;
                            while (exitListSendingLoop == false)
                            {
                                string innerResponseStr = SendMessageED(tmpMainList[i].Key,
                                    tmpMainList[i].Value.IpAddress,
                                    tmpMainList[i].Value.Port,
                                    "<nList>" + JsonSerializer.Serialize(MainAddressList) + "</nList>"
                                );
                                if (innerResponseStr == "1")
                                {
                                    exitListSendingLoop = true;
                                }
                                else
                                {
                                    Thread.Sleep(3500);
                                }
                            }
                        }
                    }

                    bool allListSyncWithNode = true;

                    for (int i = 0; i < tmpMainList.Length; i++)
                    {
                        if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                        {
                            string innerResponseStr = SendMessageED(tmpMainList[i].Key,
                                tmpMainList[i].Value.IpAddress,
                                tmpMainList[i].Value.Port,
                                "<lhash>" + MainAddressListHash + "</lhash>"
                            );
                            if (innerResponseStr == "0")
                            {
                                allListSyncWithNode = false;
                            }
                        }
                    }
                    if (allListSyncWithNode == true)
                    {
                        exitSyncLoop = true;
                    }
                }
            }
        }
        private void Message_Ready_ViaSocket(string _ipAddress, int _portNo, string _nodeHex = "")
        {
            if (_nodeHex == "")
            {
                _nodeHex = Notus.Toolbox.Network.IpAndPortToHex(_ipAddress, _portNo);
            }
            string _nodeKeyText = _nodeHex + "ready";

            string responseStr = SendMessage(_ipAddress, _portNo,
                "<ready>" + NodeList[NVG.Settings.Nodes.My.HexKey].IP.Wallet + "</ready>",
                true
            );
            if (string.Equals("done", responseStr.Trim()) == true)
            {
                ProcessIncomeData(responseStr);
            }
            else
            {
                NP.Danger(NVG.Settings, "Ready Signal Doesnt Received From Node -> Queue -> Line 998");
            }
        }
        public void MyNodeIsReady()
        {
            NodeList[NVG.Settings.Nodes.My.HexKey].Ready = true;
            Val_Ready = true;
            if (ActiveNodeCount_Val > 1)
            {
                NP.Info(NVG.Settings, "Sending Ready Signal To Other Nodes");
                NodeList[NVG.Settings.Nodes.My.HexKey].Ready = true;
                foreach (KeyValuePair<string, NVS.IpInfo> entry in MainAddressList)
                {
                    string tmpNodeHexStr = Notus.Toolbox.Network.IpAndPortToHex(entry.Value);
                    if (string.Equals(NVG.Settings.Nodes.My.HexKey, tmpNodeHexStr) == false)
                    {
                        Message_Ready_ViaSocket(entry.Value.IpAddress, entry.Value.Port, tmpNodeHexStr);
                    }
                }
            }
        }
        public Queue()
        {
            NodeList.Clear();
            LastPingTime = NVC.DefaultTime;
            ObjMp_NodeList = new Notus.Mempool("node_pool_list");
            ObjMp_NodeList.AsyncActive = false;
        }
        ~Queue()
        {
            Dispose();
        }
        public void Dispose()
        {
            ExitFromLoop = true;
            if (ObjMp_NodeList != null)
            {
                ObjMp_NodeList.Dispose();
            }
        }
    }
}
//bitiş noktası 1400.satır