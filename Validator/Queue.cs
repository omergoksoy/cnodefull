using Notus.Encryption;
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

namespace Notus.Validator
{
    public class Queue : IDisposable
    {
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
                    Notus.Print.Info(NVG.Settings,
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
                secondPointConst + (secondPointConst - (Notus.Date.ToLong(tmpNtpTime) % secondPointConst))
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
            Notus.Print.Info(NVG.Settings, "Waiting For Node Sync", false);
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
                    //tmpAllWordlTimeList.Add(entry.Value.Time.World.Ticks);
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
            Console.WriteLine("Queue.Cs - Line 281");
            Console.WriteLine(JsonSerializer.Serialize(NodeList, NVC.JsonSetting));
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
                //Console.WriteLine("When = Is Come");
                StartingTimeAfterEnoughNode = Notus.Date.ToDateTime(GetPureText(incomeData, "when"));

                NVG.NodeQueue.Starting = Notus.Time.DateTimeToUlong(StartingTimeAfterEnoughNode);
                NVG.NodeQueue.OrderCount = 1;
                NVG.NodeQueue.Begin = true;
                //Console.WriteLine(StartingTimeAfterEnoughNode);
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
            if (CheckXmlTag(incomeData, "node"))
            {
                incomeData = GetPureText(incomeData, "node");
                try
                {
                    NVS.NodeQueueInfo? tmpNodeQueueInfo = JsonSerializer.Deserialize<NVS.NodeQueueInfo>(incomeData);
                    if (tmpNodeQueueInfo != null)
                    {
                        AddToNodeList(tmpNodeQueueInfo);
                    }
                }
                catch { }
                return "<node>" + JsonSerializer.Serialize(NodeList[NVG.Settings.Nodes.My.HexKey]) + "</node>";
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
        private string SendMessageED(string nodeHex,string receiverIpAddress, int receiverPortNo, string messageText)
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
        private string Message_Hash_ViaSocket(string _ipAddress, int _portNo, string _nodeHex = "")
        {
            if (_nodeHex == "")
            {
                _nodeHex = Notus.Toolbox.Network.IpAndPortToHex(_ipAddress, _portNo);
            }
            string _nodeKeyText = _nodeHex + "hash";
            return SendMessage(
                _ipAddress,
                _portNo,
                "<hash>" +
                    MainAddressListHash.Substring(0, 20) + ":" + NodeListHash.Substring(0, 20) +
                "</hash>",
                true
            );
            /*
            if (MessageTimeListAvailable(_nodeKeyText, 1))
            {
                AddToMessageTimeList(_nodeKeyText);
            }
            */
            return "b";
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
            /*
            if (MessageTimeListAvailable(_nodeKeyText, 2))
            {
                AddToMessageTimeList(_nodeKeyText);
            }
            */
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
                                string tmpReturnStr = Message_Hash_ViaSocket(entry.Value.IpAddress, entry.Value.Port, "hash");
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

        private void CheckNodeCount()
        {
            int nodeCount = 0;
            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
            {
                if (
                    entry.Value.Status == NVS.NodeStatus.Online &&
                    //entry.Value.ErrorCount == 0 &&
                    entry.Value.Ready == true
                )
                {
                    nodeCount++;
                }
            }
            ActiveNodeCount_Val = nodeCount;
            if (ActiveNodeCount_Val > 1 && Val_Ready == true)
            {
                if (NodeList[NVG.Settings.Nodes.My.HexKey].Ready == false)
                {
                    Console.WriteLine("Control-Point-2");
                    MyNodeIsReady();
                }
                if (NotEnoughNode_Val == true) // ilk aşamada buraya girecek
                {
                    //Notus.Print.Basic(NVG.Settings, "Notus.Validator.Queue -> Line 820");
                    Notus.Print.Info(NVG.Settings, "Active Node Count : " + ActiveNodeCount_Val.ToString());
                    SortedDictionary<BigInteger, string> tmpWalletList = new SortedDictionary<BigInteger, string>();
                    foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
                    {
                        if (entry.Value.Status == NVS.NodeStatus.Online /* && entry.Value.ErrorCount == 0 */)
                        {
                            BigInteger walletNo = BigInteger.Parse(
                                new Notus.Hash().CommonHash("sha1", entry.Value.IP.Wallet),
                                NumberStyles.AllowHexSpecifier
                            );
                            if (tmpWalletList.ContainsKey(walletNo) == false)
                            {
                                tmpWalletList.Add(walletNo, entry.Value.IP.Wallet);
                            }
                        }
                    }
                    string tmpFirstWallet = tmpWalletList.First().Value;
                    if (string.Equals(tmpFirstWallet, NVG.Settings.NodeWallet.WalletKey))
                    {

                        StartingTimeAfterEnoughNode = RefreshNtpTime();
                        Notus.Print.Info(NVG.Settings,
                            "I'm Sending Starting (When) Time / Current : " +
                            StartingTimeAfterEnoughNode.ToString("HH:mm:ss.fff") +
                            " / " + NGF.GetUtcNowFromNtp().ToString("HH:mm:ss.fff")
                        );
                        NVG.NodeQueue.Starting = Notus.Time.DateTimeToUlong(StartingTimeAfterEnoughNode);
                        NVG.NodeQueue.OrderCount = 1;
                        NVG.NodeQueue.Begin = true;
                        foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NodeList)
                        {
                            if (entry.Value.Status == NVS.NodeStatus.Online /* && entry.Value.ErrorCount == 0 */)
                            {
                                SendMessage(
                                    entry.Value.IP.IpAddress,
                                    entry.Value.IP.Port,
                                    "<when>" +
                                        StartingTimeAfterEnoughNode.ToString(
                                            NVC.DefaultDateTimeFormatText
                                        ) +
                                    "</when>",
                                    true
                                );
                            }
                        }
                    }
                    else
                    {
                        //listen and wait
                        for (int x = 0; x < 100; x++)
                        {
                            Thread.Sleep(20);
                        }
                        Notus.Print.Info(NVG.Settings,
                            "I'm Waiting Starting (When) Time / Current : " +
                            StartingTimeAfterEnoughNode.ToString("HH:mm:ss.fff") +
                            " /  " +
                            NGF.GetUtcNowFromNtp().ToString("HH:mm:ss.fff")
                        );
                    }
                }
                if (NGF.GetUtcNowFromNtp() > StartingTimeAfterEnoughNode)
                {
                    OrganizeQueue();
                }
                NotEnoughNode_Val = false;
                NotEnoughNode_Printed = false;
            }
            else
            {
                NVG.NodeQueue.Starting = 0;
                NVG.NodeQueue.Begin = false;

                NotEnoughNode_Val = true;
                WaitForEnoughNode_Val = true;
                if (NotEnoughNode_Printed == false)
                {
                    NotEnoughNode_Printed = true;
                    Notus.Print.Basic(NVG.Settings, "Waiting For Enough Node");
                }
            }
        }

        private void OrganizeQueue()
        {
            /*
            Console.WriteLine(
                JsonSerializer.Serialize(NodeList, NVC.JsonSetting)
            );
            */

            //önce geçerli node listesinin bir yedeği alınıyor ve önceki node listesi değişkeninde tutuluyor.
            PreviousNodeList = JsonSerializer.Deserialize<ConcurrentDictionary<string, NVS.NodeQueueInfo>>(
                JsonSerializer.Serialize(NodeList)
            );
            LastHashForStoreList = NodeListHash;

            Dictionary<BigInteger, string> tmpNodeTimeList = new Dictionary<BigInteger, string>();
            Dictionary<BigInteger, string> tmpWalletList = new Dictionary<BigInteger, string>();
            List<BigInteger> tmpWalletOrder = new List<BigInteger>();
            SortedDictionary<string, string> tmpWalletHashList = new SortedDictionary<string, string>();
            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in PreviousNodeList)
            {
                if (/* entry.Value.ErrorCount == 0 && */ entry.Value.Ready == true)
                {
                    BigInteger walletNo = Notus.Convert.FromBase58(entry.Value.IP.Wallet);
                    tmpWalletList.Add(walletNo, entry.Value.IP.Wallet);
                    //tmpNodeTimeList.Add(walletNo, entry.Value.Time.World.ToString(NVC.DefaultDateTimeFormatText));

                    tmpWalletOrder.Add(walletNo);
                }
            }

            tmpWalletOrder.Sort();
            string tmpSalt = new Notus.Hash().CommonHash("md5", string.Join("#", tmpWalletOrder.ToArray()));
            for (int i = 0; i < tmpWalletOrder.Count; i++)
            {
                string tmpWalletHash = new Notus.Hash().CommonHash("md5",
                    tmpSalt + tmpWalletList[tmpWalletOrder[i]] + tmpNodeTimeList[tmpWalletOrder[i]]
                );

                tmpWalletHashList.Add(tmpWalletHash, tmpWalletList[tmpWalletOrder[i]]);
            }
            int counter = 0;
            NodeOrderList.Clear();

            foreach (KeyValuePair<string, string> entry in tmpWalletHashList)
            {
                counter++;
                NodeOrderList.TryAdd(counter, entry.Value);
            }

            NodeTurnCount.Clear();
            foreach (KeyValuePair<int, string> entry in NodeOrderList)
            {
                if (NodeTurnCount.ContainsKey(entry.Value) == false)
                {
                    NodeTurnCount.TryAdd(entry.Value, 0);
                }
            }
            /*

            empty blok sayısı toplanacak


            empty blok + transaction sayısı + blok sayısı
            ---------------------------------------------= İşlem başına ödül miktarı
                             ödül miktarı


            toplam ödül miktarından vakıf payı çıkarılacak ( % 2 )
            ayrıca 10 blok ödülü seçilecek bir kişiye verilecek


            */
            int myRewardCount = NodeTurnCount[NodeOrderList[1]];
            int minRewardCount = int.MaxValue;
            int maxRewardCount = 0;
            foreach (KeyValuePair<string, int> entry in NodeTurnCount)
            {
                if (entry.Value > maxRewardCount)
                {
                    maxRewardCount = entry.Value;
                }
                if (minRewardCount > entry.Value)
                {
                    minRewardCount = entry.Value;
                }
            }

            /*
            burada işlem yapılacak
            süreler dağıtılacak
            */

            //NVG.NodeOrderList.Clear();
            int islemSuresi = 200;
            int olusturmaSuresi = 100;
            int dagitmaSuresi = 200;
            int toplamZamanAraligi = islemSuresi + olusturmaSuresi + dagitmaSuresi;

            /*
            sıralar burada oluşturuldu ama 
            her sıranın süresi belirtilmedi
            süre sonrası belirtilen aralıkta node'lar işlemlerini yapacak
            */
            NGF.RefreshNodeQueueTime();
            if (NodeOrderList.Count == 2)
            {
                //NVG.NodeQueue.OrderCount
                NVG.NodeQueue.NodeOrder.Add(1, NodeOrderList[1]);
                NVG.NodeQueue.NodeOrder.Add(2, NodeOrderList[2]);

                NVG.NodeQueue.NodeOrder.Add(3, NodeOrderList[1]);
                NVG.NodeQueue.NodeOrder.Add(4, NodeOrderList[2]);

                NVG.NodeQueue.NodeOrder.Add(5, NodeOrderList[1]);
                NVG.NodeQueue.NodeOrder.Add(6, NodeOrderList[2]);

                //NVG.NodeQueue.Starting = Notus.Time.DateTimeToUlong(StartingTimeAfterEnoughNode);

                NVG.NodeQueue.TimeBaseWalletList.Add(
                    NVG.NodeQueue.Starting,
                    NodeOrderList[1]
                );
                NVG.NodeQueue.TimeBaseWalletList.Add(
                    Notus.Time.DateTimeToUlong(
                        Notus.Date.ToDateTime(NVG.NodeQueue.Starting).AddMilliseconds(toplamZamanAraligi)
                    , true),
                    NodeOrderList[2]
                );
                NVG.NodeQueue.TimeBaseWalletList.Add(
                    Notus.Time.DateTimeToUlong(
                        Notus.Date.ToDateTime(NVG.NodeQueue.Starting).AddMilliseconds(toplamZamanAraligi * 2)
                    , true),
                    NodeOrderList[1]
                );
                NVG.NodeQueue.TimeBaseWalletList.Add(
                    Notus.Time.DateTimeToUlong(
                        Notus.Date.ToDateTime(NVG.NodeQueue.Starting).AddMilliseconds(toplamZamanAraligi * 3)
                    , true),
                    NodeOrderList[2]
                );
                NVG.NodeQueue.TimeBaseWalletList.Add(
                    Notus.Time.DateTimeToUlong(
                        Notus.Date.ToDateTime(NVG.NodeQueue.Starting).AddMilliseconds(toplamZamanAraligi * 4)
                    , true),
                    NodeOrderList[1]
                );
                NVG.NodeQueue.TimeBaseWalletList.Add(
                    Notus.Time.DateTimeToUlong(
                        Notus.Date.ToDateTime(NVG.NodeQueue.Starting).AddMilliseconds(toplamZamanAraligi * 55)
                    , true),
                    NodeOrderList[2]
                );
            }

            if (NVG.NodeListPrinted == false)
            {
                NVG.NodeListPrinted = true;
                Console.WriteLine(JsonSerializer.Serialize(NVG.NodeQueue));
                //Console.WriteLine(JsonSerializer.Serialize(NVG.NodeQueue.TimeBaseWalletList));
                //Console.WriteLine(JsonSerializer.Serialize(NVG.NodeQueue.NodeOrder));
                //NVG.NodeQueue.OrderCount
                //NVG.NodeQueue.NodeOrder.Add(1, NodeOrderList[1]);

                //Console.WriteLine(JsonSerializer.Serialize());

            }
            //Console.WriteLine();

            /*
            if (NodeOrderList.Count == 3)
            {
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[1],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[2],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[3],
                        Begin = NVG.StartingTime
                    }
                );

                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[1],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[2],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[3],
                        Begin = NVG.StartingTime
                    }
                );

            }

            if (NodeOrderList.Count == 4)
            {
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[1],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[2],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[3],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[4],
                        Begin = NVG.StartingTime
                    }
                );

                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[1],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[2],
                        Begin = NVG.StartingTime
                    }
                );
            }

            if (NodeOrderList.Count == 5)
            {
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[1],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[2],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[3],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[4],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[5],
                        Begin = NVG.StartingTime
                    }
                );

                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[1],
                        Begin = NVG.StartingTime
                    }
                );
            }

            if (NodeOrderList.Count > 5)
            {
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[1],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[2],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[3],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[4],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[5],
                        Begin = NVG.StartingTime
                    }
                );
                NVG.NodeTimeBasedOrderList.Add(
                    new Globals.Variable.NodeOrderStruct()
                    {
                        Wallet = NodeOrderList[6],
                        Begin = NVG.StartingTime
                    }
                );
            }
            */
            // Console.WriteLine("+++++++++++++++++++++++++++++++++++++++++");
            // Console.WriteLine(JsonSerializer.Serialize(NodeOrderList));
            MyTurn_Val = (string.Equals(NVG.Settings.NodeWallet.WalletKey, NodeOrderList[1]));
            //NodeTimeBasedOrderList
            //Console.WriteLine("+++++++++++++++++++++++++++++++++++++++++");
            //Console.WriteLine(Notus.Time.NowNtpTime().ToString("HH:mm:ss fff"));

            if (MyTurn_Val == true)
            {
                /*
                //Notus.Print.Info(NVG.Settings, "My Turn");
                NextQueueValidNtpTime = RefreshNtpTime(NVC.NodeSortFrequency);
                foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in PreviousNodeList)
                {
                    if (
                        entry.Value.ErrorCount == 0 &&
                        entry.Value.Status == NodeStatus.Online &&
                        entry.Value.Ready == true &&
                        string.Equals(entry.Value.Wallet, NVG.Settings.NodeWallet.WalletKey) == false
                    )
                    {
                        SendMessage(
                            entry.Value.IP,
                            "<time>" + Notus.Date.ToString(NextQueueValidNtpTime) + "</time>",
                            true
                        );
                    }
                }
                */
            }
            else
            {

                //Console.WriteLine(JsonSerializer.Serialize(NodeList, NVC.JsonSetting));

                //Console.WriteLine(JsonSerializer.Serialize(NodeOrderList, NVC.JsonSetting));
                //Notus.Print.Info(NVG.Settings, "Waiting For Turn");
            }
            //NodeList[NVG.Settings.Nodes.My.HexKey].Time.Node = DateTime.Now;
            //NodeList[NVG.Settings.Nodes.My.HexKey].Time.World = NGF.GetUtcNowFromNtp();
            WaitForEnoughNode_Val = false;
        }
        public void FirstHandShake()
        {
            foreach (KeyValuePair<string, NVS.IpInfo> entry in MainAddressList)
            {
                if (string.Equals(entry.Key, NVG.Settings.Nodes.My.HexKey) == false)
                {
                    /*
                    tmpNodeList.Add(new NVS.IpInfo()
                    {
                        IpAddress = entry.Value.IpAddress,
                        Port = entry.Value.Port
                    });
                    */
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
                IP = new NVS.NodeInfo()
                {
                    IpAddress = NVG.Settings.Nodes.My.IP.IpAddress,
                    Port = NVG.Settings.Nodes.My.IP.Port,
                    Wallet = NVG.Settings.NodeWallet.WalletKey
                },
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
                        HexKey = Notus.Toolbox.Network.IpAndPortToHex(entry.Value.IpAddress, entry.Value.Port),
                        IP = new NVS.NodeInfo()
                        {
                            IpAddress = entry.Value.IpAddress,
                            Port = entry.Value.Port,
                            Wallet = "#"
                        },
                    });
                }
            }

            PingOtherNodes();
            KeyValuePair<string, NVS.NodeQueueInfo>[]? tmpNodeList = NodeList.ToArray();
            if (tmpNodeList != null)
            {
                for(int i=0;i<tmpNodeList.Length;i++)
                {
                    if (string.Equals(tmpNodeList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                    {
                        string responseStr = SendMessageED(tmpNodeList[i].Key,
                            tmpNodeList[i].Value.IP.IpAddress,
                            tmpNodeList[i].Value.IP.Port,
                            "<node>" + 
                                JsonSerializer.Serialize(NodeList[NVG.Settings.Nodes.My.HexKey]) + 
                            "</node>"
                        );
                        Console.WriteLine("Queue.Cs -> Line 1271");
                        Console.WriteLine(responseStr);
                    }
                    Console.WriteLine(tmpNodeList[i].Key);
                    Console.WriteLine(JsonSerializer.Serialize(NodeList[tmpNodeList[i].Key], NVC.JsonSetting));
                    Console.WriteLine("Node Bilgini gönder");
                    Console.WriteLine("Gönderdiğin node'dan, elindeki node listesini iste");
                }
            }
            
            //Console.WriteLine("Validator.Queue -> Line 1270");
            //Console.WriteLine(JsonSerializer.Serialize(MainAddressList));
            //Console.WriteLine("----------------------------");
            //Console.WriteLine(JsonSerializer.Serialize(NodeList));
            Notus.Print.ReadLine();
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
                Notus.Print.Danger(NVG.Settings, "Ready Signal Doesnt Received From Node -> Queue -> Line 998");
            }
        }
        public void MyNodeIsReady()
        {
            NodeList[NVG.Settings.Nodes.My.HexKey].Ready = true;
            Val_Ready = true;
            if (ActiveNodeCount_Val > 1)
            {
                Notus.Print.Info(NVG.Settings, "Sending Ready Signal To Other Nodes");
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