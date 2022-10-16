using Notus.Encryption;
using Notus.Variable.Struct;
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
using NVG = Notus.Variable.Globals;

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

        private SortedDictionary<string, IpInfo> MainAddressList = new SortedDictionary<string, IpInfo>();
        private string MainAddressListHash = string.Empty;

        private ConcurrentDictionary<string, NodeQueueInfo>? PreviousNodeList = new ConcurrentDictionary<string, NodeQueueInfo>();
        public ConcurrentDictionary<string, NodeQueueInfo>? SyncNodeList
        {
            get { return PreviousNodeList; }
            set { PreviousNodeList = value; }
        }
        private ConcurrentDictionary<string, int> NodeTurnCount = new ConcurrentDictionary<string, int>();
        private ConcurrentDictionary<string, NodeQueueInfo> NodeList = new ConcurrentDictionary<string, NodeQueueInfo>();
        private ConcurrentDictionary<int, string> NodeOrderList = new ConcurrentDictionary<int, string>();
        private ConcurrentDictionary<string, DateTime> NodeTimeBasedOrderList = new ConcurrentDictionary<string, DateTime>();

        private Notus.Mempool ObjMp_NodeList;
        private bool ExitFromLoop = false;
        private string LastHashForStoreList = "#####";
        private string NodeListHash = "#";

        private int MyPortNo = 6500;
        private string MyNodeHexKey = "#";
        private string MyWallet = "#";
        private string MyIpAddress = "#";

        private DateTime LastPingTime;

        public System.Func<Notus.Variable.Class.BlockData, bool>? Func_NewBlockIncome = null;

        //empty blok için kontrolü yapacak olan node'u seçen fonksiyon
        public Notus.Variable.Enum.ValidatorOrder EmptyTimer()
        {
            return Notus.Variable.Enum.ValidatorOrder.Primary;
        }

        //oluşturulacak blokları kimin oluşturacağını seçen fonksiyon
        public void Distrubute(long blockRowNo, int blockType = 0)
        {
            foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
            {
                if (string.Equals(MyNodeHexKey, entry.Key) == false && entry.Value.Status == NodeStatus.Online)
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
            double secondVal = Notus.Variable.Constant.NodeStartingSync +
                (Notus.Variable.Constant.NodeStartingSync -
                    (
                        ulong.Parse(
                            afterMiliSecondTime.ToString("ss")
                        ) %
                        Notus.Variable.Constant.NodeStartingSync
                    )
                );
            return afterMiliSecondTime.AddSeconds(secondVal);
        }
        /*
        private bool MessageTimeListAvailable(string _keyName, int timeOutSecond)
        {
            if (MessageTimeList.ContainsKey(_keyName) == false)
            {
                return true;
            }
            if ((DateTime.Now - MessageTimeList[_keyName]).TotalSeconds > timeOutSecond)
            {
                return true;
            }
            return false;
        }
        private void AddToMessageTimeList(string _keyName)
        {
            if (MessageTimeList.ContainsKey(_keyName) == false)
            {
                MessageTimeList.TryAdd(_keyName, DateTime.Now);
            }
            else
            {
                MessageTimeList[_keyName] = DateTime.Now;
            }
        }
        */
        public void PingOtherNodes()
        {
            Notus.Print.Info(NVG.Settings, "Waiting For Node Sync", false);
            bool tmpExitWhileLoop = false;
            while (tmpExitWhileLoop == false)
            {
                KeyValuePair<string, IpInfo>[]? tmpMainList = MainAddressList.ToArray();
                if (tmpMainList != null)
                {
                    for(int i=0;i<tmpMainList.Length && tmpExitWhileLoop == false; i++)
                    {
                        if (string.Equals(tmpMainList[i].Key, MyNodeHexKey) == false)
                        {
                            tmpExitWhileLoop = Notus.Toolbox.Network.PingToNode(tmpMainList[i].Value);
                        }
                    }
                }

                if(tmpExitWhileLoop == false)
                {
                    Thread.Sleep(5500);
                }
            }
            Thread.Sleep(500);
        }
        private string CalculateMainAddressListHash()
        {
            List<UInt64> tmpAllWordlTimeList = new List<UInt64>();
            foreach (KeyValuePair<string, IpInfo> entry in MainAddressList)
            {
                tmpAllWordlTimeList.Add(UInt64.Parse(entry.Key, NumberStyles.AllowHexSpecifier));
            }
            tmpAllWordlTimeList.Sort();
            return new Notus.Hash().CommonHash("sha1", JsonSerializer.Serialize(tmpAllWordlTimeList));
        }
        public List<IpInfo> GiveMeNodeList()
        {
            List<IpInfo> tmpNodeList = new List<IpInfo>();
            foreach (KeyValuePair<string, IpInfo> entry in MainAddressList)
            {
                if (string.Equals(entry.Key, MyNodeHexKey) == false)
                {
                    tmpNodeList.Add(new IpInfo()
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
                MainAddressList.Add(tmpHexKeyStr, new IpInfo()
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
        private void AddToNodeList(NodeQueueInfo NodeQueueInfo)
        {
            string tmpNodeHexStr = Notus.Toolbox.Network.IpAndPortToHex(NodeQueueInfo.IP);
            if (NodeList.ContainsKey(tmpNodeHexStr))
            {
                NodeList[tmpNodeHexStr] = NodeQueueInfo;
            }
            else
            {
                NodeList.TryAdd(tmpNodeHexStr, NodeQueueInfo);
            }

            if (NVG.Settings.LocalNode == true)
            {
                NodeList[tmpNodeHexStr].InTheCode = true;
            }
            else
            {
                NodeList[tmpNodeHexStr].InTheCode = false;
                foreach (IpInfo entry in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
                {
                    if (string.Equals(entry.IpAddress, NodeQueueInfo.IP.IpAddress) && NodeQueueInfo.IP.Port == entry.Port)
                    {
                        NodeList[tmpNodeHexStr].InTheCode = true;
                    }
                }
            }

            AddToMainAddressList(NodeQueueInfo.IP.IpAddress, NodeQueueInfo.IP.Port);
        }
        private string CalculateMyNodeListHash()
        {
            Dictionary<string, NodeQueueInfo>? tmpNodeList = JsonSerializer.Deserialize<Dictionary<string, NodeQueueInfo>>(JsonSerializer.Serialize(NodeList));
            if (tmpNodeList == null)
            {
                return string.Empty;
            }

            List<string> tmpAllAddressList = new List<string>();
            List<string> tmpAllWalletList = new List<string>();
            List<long> tmpAllWordlTimeList = new List<long>();
            foreach (KeyValuePair<string, NodeQueueInfo> entry in tmpNodeList)
            {
                string tmpAddressListHex = Notus.Toolbox.Network.IpAndPortToHex(entry.Value.IP);
                if (tmpAllAddressList.IndexOf(tmpAddressListHex) < 0)
                {
                    tmpAllAddressList.Add(tmpAddressListHex);
                    if (entry.Value.Wallet.Length == 0)
                    {
                        tmpAllWalletList.Add("#");
                    }
                    else
                    {
                        tmpAllWalletList.Add(entry.Value.Wallet);
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
                SortedDictionary<string, IpInfo>? tmpDbNodeList = JsonSerializer.Deserialize<SortedDictionary<string, IpInfo>>(tmpNodeListStr);
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
        public string Process(Notus.Variable.Struct.HttpRequestDetails incomeData)
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
                foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                {
                    if (string.Equals(entry.Value.Wallet, tmpNodeWalletKey))
                    {
                        tmpIpAddress = entry.Value.IP.IpAddress;
                        tmpPortNo = entry.Value.IP.Port;
                    }
                    if (
                        entry.Value.Status == NodeStatus.Online &&
                        entry.Value.Ready == true &&
                        entry.Value.ErrorCount == 0
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
                foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                {
                    if (string.Equals(entry.Value.Wallet, incomeData) == true)
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
                    NodeQueueInfo? tmpNodeQueueInfo = JsonSerializer.Deserialize<NodeQueueInfo>(incomeData);
                    if (tmpNodeQueueInfo != null)
                    {
                        AddToNodeList(tmpNodeQueueInfo);
                    }
                }
                catch{}
                return "<node>" + JsonSerializer.Serialize(NodeList[MyNodeHexKey]) + "</node>";
            }
            if (CheckXmlTag(incomeData, "list"))
            {
                incomeData = GetPureText(incomeData, "list");
                SortedDictionary<string, IpInfo>? tmpNodeList = JsonSerializer.Deserialize<SortedDictionary<string, IpInfo>>(incomeData);
                if (tmpNodeList == null)
                {
                    return "<err>1</err>";
                }
                foreach (KeyValuePair<string, IpInfo> entry in tmpNodeList)
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
                NodeList[nodeHexText].ErrorCount++;
                NodeList[nodeHexText].Status = NodeStatus.Offline;
                NodeList[nodeHexText].Ready = false;
                NodeList[nodeHexText].ErrorTime = Notus.Time.NowNtpTimeToUlong();

                //NodeList[nodeHexText].Time.Error = DateTime.Now;
            }
        }
        private void NodeIsOnline(string nodeHexText)
        {
            if (NodeList.ContainsKey(nodeHexText) == true)
            {
                NodeList[nodeHexText].ErrorCount = 0;
                NodeList[nodeHexText].Status = NodeStatus.Online;
                //NodeList[nodeHexText].Time.Error = Notus.Variable.Constant.DefaultTime;
            }
        }
        private string SendMessage(string receiverIpAddress, int receiverPortNo, string messageText, bool executeErrorControl)
        {
            
            string tmpNodeHexStr = Notus.Toolbox.Network.IpAndPortToHex(receiverIpAddress, receiverPortNo);
            //TimeSpan tmpErrorDiff = DateTime.Now - NodeList[tmpNodeHexStr].Time.Error;
            //if (tmpErrorDiff.TotalSeconds > 60)
            if (100> 60)
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
                    NodeList[tmpNodeHexStr].ErrorCount = 0;
                    NodeList[tmpNodeHexStr].Status = NodeStatus.Online;
                    //NodeList[tmpNodeHexStr].Time.Error = Notus.Variable.Constant.DefaultTime;
                    return incodeResponse;
                }
                NodeError(tmpNodeHexStr);
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
            if (_nodeHex == "")
            {
                _nodeHex = Notus.Toolbox.Network.IpAndPortToHex(_ipAddress, _portNo);
            }
            string _nodeKeyText = _nodeHex + "node";
            string responseStr = SendMessage(_ipAddress, _portNo,
                "<node>" + JsonSerializer.Serialize(NodeList[MyNodeHexKey]) + "</node>",
                true
            );
            if (string.Equals("err", responseStr) == false)
            {
                ProcessIncomeData(responseStr);
            }
            /*
            if (MessageTimeListAvailable(_nodeKeyText, 2))
            {
                AddToMessageTimeList(_nodeKeyText);
            }
            */
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
                        catch{}
                    }
                    SortedDictionary<string, IpInfo>? tmpMainAddressList = JsonSerializer.Deserialize<SortedDictionary<string, IpInfo>>(tmpData);
                    bool tmpRefreshNodeDetails = false;
                    if (tmpMainAddressList != null)
                    {
                        foreach (KeyValuePair<string, IpInfo> entry in tmpMainAddressList)
                        {
                            string tmpNodeHexStr = Notus.Toolbox.Network.IpAndPortToHex(entry.Value);
                            if (string.Equals(MyNodeHexKey, tmpNodeHexStr) == false)
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
                        foreach (KeyValuePair<string, IpInfo> entry in tmpMainAddressList)
                        {
                            string tmpNodeHexStr = Notus.Toolbox.Network.IpAndPortToHex(entry.Value);
                            if (string.Equals(MyNodeHexKey, tmpNodeHexStr) == false)
                            {
                                Message_Node_ViaSocket(entry.Value.IpAddress, entry.Value.Port, tmpNodeHexStr);
                            }
                        }
                    }
                    LastPingTime = DateTime.Now;
                }

                // burada durumu bilinmeyen nodeların bilgilerinin sorgulandığı kısım
                foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                {
                    bool tmpRefreshNodeDetails = false;
                    string tmpCheckHex = Notus.Toolbox.Network.IpAndPortToHex(entry.Value.IP);
                    if (entry.Value.Status == NodeStatus.Unknown)
                    {
                        tmpRefreshNodeDetails = true;
                    }
                    if (tmpRefreshNodeDetails == true)
                    {
                        Message_Node_ViaSocket(entry.Value.IP.IpAddress, entry.Value.IP.Port, tmpCheckHex);
                    }
                }

                NodeList[MyNodeHexKey].NodeHash = CalculateMyNodeListHash();
                int nodeCount = 0;
                SyncReady = true;
                //burada eğer nodeların hashleri farklı ise senkron olacağı kısım
                foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                {
                    if (entry.Value.Status == NodeStatus.Online && entry.Value.ErrorCount == 0)
                    {
                        nodeCount++;
                        string tmpCheckHex = Notus.Toolbox.Network.IpAndPortToHex(entry.Value.IP);
                        if (string.Equals(MyNodeHexKey, tmpCheckHex) == false)
                        {
                            if (NodeListHash != entry.Value.NodeHash)
                            {
                                SyncReady = false;
                                Message_Node_ViaSocket(entry.Value.IP.IpAddress, entry.Value.IP.Port, tmpCheckHex);
                            }
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
            foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
            {
                if (
                    entry.Value.Status == NodeStatus.Online &&
                    entry.Value.ErrorCount == 0 &&
                    entry.Value.Ready == true
                )
                {
                    nodeCount++;
                }
            }
            ActiveNodeCount_Val = nodeCount;
            if (ActiveNodeCount_Val > 1 && Val_Ready == true)
            {
                if (NodeList[MyNodeHexKey].Ready == false)
                {
                    Console.WriteLine("Control-Point-2");
                    MyNodeIsReady();
                }
                if (NotEnoughNode_Val == true) // ilk aşamada buraya girecek
                {
                    //Notus.Print.Basic(NVG.Settings, "Notus.Validator.Queue -> Line 820");
                    Notus.Print.Info(NVG.Settings, "Active Node Count : " + ActiveNodeCount_Val.ToString());
                    SortedDictionary<BigInteger, string> tmpWalletList = new SortedDictionary<BigInteger, string>();
                    foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                    {
                        if (entry.Value.Status == NodeStatus.Online && entry.Value.ErrorCount == 0)
                        {
                            BigInteger walletNo = BigInteger.Parse(
                                new Notus.Hash().CommonHash("sha1", entry.Value.Wallet),
                                NumberStyles.AllowHexSpecifier
                            );
                            if (tmpWalletList.ContainsKey(walletNo) == false)
                            {
                                tmpWalletList.Add(walletNo, entry.Value.Wallet);
                            }
                        }
                    }
                    string tmpFirstWallet = tmpWalletList.First().Value;
                    if (string.Equals(tmpFirstWallet, MyWallet))
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
                        foreach (KeyValuePair<string, NodeQueueInfo> entry in NodeList)
                        {
                            if (entry.Value.Status == NodeStatus.Online && entry.Value.ErrorCount == 0)
                            {
                                SendMessage(
                                    entry.Value.IP.IpAddress,
                                    entry.Value.IP.Port,
                                    "<when>" +
                                        StartingTimeAfterEnoughNode.ToString(
                                            Notus.Variable.Constant.DefaultDateTimeFormatText
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
                JsonSerializer.Serialize(NodeList, Notus.Variable.Constant.JsonSetting)
            );
            */

            //önce geçerli node listesinin bir yedeği alınıyor ve önceki node listesi değişkeninde tutuluyor.
            PreviousNodeList = JsonSerializer.Deserialize<ConcurrentDictionary<string, NodeQueueInfo>>(
                JsonSerializer.Serialize(NodeList)
            );
            LastHashForStoreList = NodeListHash;

            Dictionary<BigInteger, string> tmpNodeTimeList = new Dictionary<BigInteger, string>();
            Dictionary<BigInteger, string> tmpWalletList = new Dictionary<BigInteger, string>();
            List<BigInteger> tmpWalletOrder = new List<BigInteger>();
            SortedDictionary<string, string> tmpWalletHashList = new SortedDictionary<string, string>();
            foreach (KeyValuePair<string, NodeQueueInfo> entry in PreviousNodeList)
            {
                if (entry.Value.ErrorCount == 0 && entry.Value.Ready == true)
                {
                    BigInteger walletNo = Notus.Convert.FromBase58(entry.Value.Wallet);
                    tmpWalletList.Add(walletNo, entry.Value.Wallet);
                    //tmpNodeTimeList.Add(walletNo, entry.Value.Time.World.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText));

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
                    ,true),
                    NodeOrderList[2]
                );
                NVG.NodeQueue.TimeBaseWalletList.Add(
                    Notus.Time.DateTimeToUlong(
                        Notus.Date.ToDateTime(NVG.NodeQueue.Starting).AddMilliseconds(toplamZamanAraligi*2)
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
            MyTurn_Val = (string.Equals(MyWallet, NodeOrderList[1]));
            //NodeTimeBasedOrderList
            //Console.WriteLine("+++++++++++++++++++++++++++++++++++++++++");
            //Console.WriteLine(Notus.Time.NowNtpTime().ToString("HH:mm:ss fff"));

            if (MyTurn_Val == true)
            {
                /*
                //Notus.Print.Info(NVG.Settings, "My Turn");
                NextQueueValidNtpTime = RefreshNtpTime(Notus.Variable.Constant.NodeSortFrequency);
                foreach (KeyValuePair<string, NodeQueueInfo> entry in PreviousNodeList)
                {
                    if (
                        entry.Value.ErrorCount == 0 &&
                        entry.Value.Status == NodeStatus.Online &&
                        entry.Value.Ready == true &&
                        string.Equals(entry.Value.Wallet, MyWallet) == false
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

                //Console.WriteLine(JsonSerializer.Serialize(NodeList, Notus.Variable.Constant.JsonSetting));

                //Console.WriteLine(JsonSerializer.Serialize(NodeOrderList, Notus.Variable.Constant.JsonSetting));
                //Notus.Print.Info(NVG.Settings, "Waiting For Turn");
            }
            //NodeList[MyNodeHexKey].Time.Node = DateTime.Now;
            //NodeList[MyNodeHexKey].Time.World = NGF.GetUtcNowFromNtp();
            WaitForEnoughNode_Val = false;
        }
        public void Start()
        {
            if (NVG.Settings.LocalNode == false)
            {
                if (NVG.Settings.GenesisCreated == false)
                {
                    PreStart();
                    PingOtherNodes();
                }
                Notus.Print.Info(NVG.Settings, "Getting UTC Time From NTP Server");
                Task.Run(() =>
                {
                    MainLoop();
                });
            }
        }

        public void PreStart()
        {
            MyPortNo = Notus.Toolbox.Network.GetNetworkPort();

            MyIpAddress = (NVG.Settings.LocalNode == true ? NVG.Settings.IpInfo.Local : NVG.Settings.IpInfo.Public);
            MyNodeHexKey = Notus.Toolbox.Network.IpAndPortToHex(MyIpAddress, MyPortNo);
            //Notus.Print.Basic(NVG.Settings, "My Node Hex Key : " + MyNodeHexKey);
            if (NVG.Settings.LocalNode == true)
            {
                AddToMainAddressList(NVG.Settings.IpInfo.Local, MyPortNo, false);
            }
            else
            {
                foreach (IpInfo defaultNodeInfo in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
                {
                    AddToMainAddressList(defaultNodeInfo.IpAddress, defaultNodeInfo.Port, false);
                }
            }

            MyWallet = NVG.Settings.NodeWallet.WalletKey;

            string tmpNodeListStr = ObjMp_NodeList.Get("ip_list", "");
            if (tmpNodeListStr.Length == 0)
            {
                StoreNodeListToDb();
            }
            else
            {
                SortedDictionary<string, IpInfo>? tmpDbNodeList = JsonSerializer.Deserialize<SortedDictionary<string, IpInfo>>(tmpNodeListStr);
                if (tmpDbNodeList != null)
                {
                    foreach (KeyValuePair<string, IpInfo> entry in tmpDbNodeList)
                    {
                        AddToMainAddressList(entry.Value.IpAddress, entry.Value.Port);
                    }
                }
            }
            AddToNodeList(new NodeQueueInfo()
            {
                ErrorCount = 0,
                Ready = false,
                NodeHash = "#",
                Status = NodeStatus.Online,
                
                //sync-disable-exception
                //Time = new NodeQueueInfo_Time()
                //{
                //Node = Notus.Variable.Constant.DefaultTime,
                //World = Notus.Variable.Constant.DefaultTime,
                //Error = Notus.Variable.Constant.DefaultTime
                //},
                Begin = Notus.Variable.Constant.DefaultTime,

                Wallet = MyWallet,
                IP = new IpInfo()
                {
                    IpAddress = MyIpAddress,
                    Port = MyPortNo
                },
                //sync-disable-exception
                //LastRowNo = lastBlockRowNo,
                //LastSign = lastBlockSign,
                //LastUid = lastBlockUid,
                //LastPrev = lastBlockPrev
            });

            foreach (KeyValuePair<string, IpInfo> entry in MainAddressList)
            {
                if (string.Equals(MyNodeHexKey, Notus.Toolbox.Network.IpAndPortToHex(entry.Value)) == false)
                {
                    AddToNodeList(new NodeQueueInfo()
                    {
                        Ready = false,
                        ErrorCount = 0,
                        NodeHash = "#",
                        Status = NodeStatus.Unknown,

                        //sync-disable-exception
                        //Time = new NodeQueueInfo_Time()
                        //{
                        //Node = Notus.Variable.Constant.DefaultTime,
                        //World = Notus.Variable.Constant.DefaultTime,
                        //Error = Notus.Variable.Constant.DefaultTime
                        //},
                        Begin = Notus.Variable.Constant.DefaultTime,

                        Wallet = "#",
                        IP = new IpInfo()
                        {
                            IpAddress = entry.Value.IpAddress,
                            Port = entry.Value.Port,
                        },
                        //sync-disable-exception
                        //LastRowNo = 0,
                        //LastSign = string.Empty,
                        //LastPrev = string.Empty,
                        //LastUid = string.Empty
                    });
                }
            }
            NodeList[MyNodeHexKey].NodeHash = CalculateMyNodeListHash();
        }
        private void Message_Ready_ViaSocket(string _ipAddress, int _portNo, string _nodeHex = "")
        {
            if (_nodeHex == "")
            {
                _nodeHex = Notus.Toolbox.Network.IpAndPortToHex(_ipAddress, _portNo);
            }
            string _nodeKeyText = _nodeHex + "ready";

            string responseStr = SendMessage(_ipAddress, _portNo,
                "<ready>" + NodeList[MyNodeHexKey].Wallet + "</ready>",
                true
            );
            //Console.WriteLine("_ipAddress / _portNo: " + _ipAddress + " : "+ _portNo.ToString());
            //Console.WriteLine("responseStr : " + responseStr);
            if (string.Equals("done", responseStr.Trim()) == true)
            {
                ProcessIncomeData(responseStr);
            }
            else
            {
                Notus.Print.Danger(NVG.Settings, "Ready Signal Doesnt Received From Node -> Queue -> Line 998");
            }
            /*
            if (MessageTimeListAvailable(_nodeKeyText, 2))
            {
                AddToMessageTimeList(_nodeKeyText);
            }
            else
            {
                //Console.WriteLine("Ready Signal Were Sended Before");
            }
            */
        }
        public void MyNodeIsReady()
        {
            NodeList[MyNodeHexKey].Ready = true;
            Val_Ready = true;
            if (ActiveNodeCount_Val > 1)
            {
                Notus.Print.Info(NVG.Settings, "Sending Ready Signal To Other Nodes");
                NodeList[MyNodeHexKey].Ready = true;
                foreach (KeyValuePair<string, IpInfo> entry in MainAddressList)
                {
                    string tmpNodeHexStr = Notus.Toolbox.Network.IpAndPortToHex(entry.Value);
                    if (string.Equals(MyNodeHexKey, tmpNodeHexStr) == false)
                    {
                        Message_Ready_ViaSocket(entry.Value.IpAddress, entry.Value.Port, tmpNodeHexStr);
                    }
                }
            }
        }
        public Queue()
        {
            NodeList.Clear();
            LastPingTime = Notus.Variable.Constant.DefaultTime;
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