using System.Globalization;
using System.Numerics;
using System.Text.Json;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NH = Notus.Hash;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
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

        private bool SyncReady = true;

        private bool ExitFromLoop = false;
        private string LastHashForStoreList = "#####";
        private string NodeListHash = "#";

        private DateTime LastPingTime;

        public System.Func<Notus.Variable.Class.BlockData, bool>? Func_NewBlockIncome = null;

        private string fixedRowNoLength(long blockRowNo)
        {
            return blockRowNo.ToString().PadLeft(15, '_');
        }
        public void Distrubute(long blockRowNo, int blockType, ulong currentNodeStartingTime)
        {
            ulong totalQueuePeriod = (ulong)(NVC.BlockListeningForPoolTime + NVC.BlockGeneratingTime + NVC.BlockDistributingTime);
            ulong nextValidatorNodeTime = ND.AddMiliseconds(currentNodeStartingTime, totalQueuePeriod);


            // sonraki node'a doğrudan gönder,
            // 2 sonraki node task ile gönderebilirsin.

            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
            {
                if (string.Equals(NVG.Settings.Nodes.My.HexKey, entry.Key) == false && entry.Value.Status == NVS.NodeStatus.Online)
                {
                    /*
                    NP.Info("incomeResult : " + incomeResult);
                    ProcessIncomeData(incomeResult);
                    */


                    //kullanılan cüzdanlar burada liste olarak gönderilecek...
                    List<string> wList = new List<string>();
                    if (blockType != 300)
                    {
                        foreach (var iEntry in NGF.WalletUsageList)
                        {
                            wList.Add(iEntry.Key);
                        }
                        if (wList.Count > 0)
                        {
                            Console.WriteLine(JsonSerializer.Serialize(wList));
                        }
                    }

                    NP.Info(
                    "Distributing [ " +
                        fixedRowNoLength(blockRowNo) + " : " +
                        blockType.ToString() +
                        " ] To " +
                        entry.Value.IP.IpAddress + ":" +
                        entry.Value.IP.Port.ToString()
                    );

                    Task.Run(() =>
                    {
                        Console.WriteLine(JsonSerializer.Serialize(NVG.NodeList));
                    });
                    string incomeResult = NVG.Settings.MsgOrch.SendMsg(
                        entry.Value.IP.Wallet,
                        "<block>" +
                            blockRowNo.ToString() + ":" + NVG.Settings.NodeWallet.WalletKey +
                        "</block>"
                    );

                    //NP.Info(NVG.Settings, "Distrubute : " + ND.ToDateTime(NVG.NOW.Int).ToString("HH mm ss fff"));
                    //Console.WriteLine("incomeResult [ " + incomeResult.Length +  " ] : " + incomeResult);
                }
            }
        }

        private DateTime CalculateStartingTime()
        {
            DateTime tmpNtpTime = NVG.NOW.Obj;
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
            // listesinde eğer 1 adet çevrim içi node varsa 
            // döngüden çıkış yapacak
            NP.Info("Sending Ping To Nodes");
            bool exitInnerWhile = false;
            KeyValuePair<string, NVS.IpInfo>[]? tmpMainList = NGF.ValidatorList.ToArray();
            if (tmpMainList != null)
            {
                while (exitInnerWhile == false)
                {
                    for (int i = 0; i < tmpMainList.Length && exitInnerWhile == false; i++)
                    {
                        if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                        {
                            var nodeStatus = Notus.Toolbox.Network.PingToNode(tmpMainList[i].Value);
                            if (nodeStatus == NVS.NodeStatus.Online)
                            {
                                NGF.ValidatorList[tmpMainList[i].Key].Status = NVS.NodeStatus.Online;
                            }
                            if (nodeStatus == NVS.NodeStatus.Offline)
                            {
                                NGF.ValidatorList[tmpMainList[i].Key].Status = NVS.NodeStatus.Offline;
                            }
                            NGF.ValidatorList[tmpMainList[i].Key].Status = nodeStatus;
                            if (nodeStatus == NVS.NodeStatus.Online)
                            {
                                exitInnerWhile = true;
                            }
                        }
                    }
                    if (exitInnerWhile == false)
                    {
                        Thread.Sleep(300);
                    }
                }
            }
        }
        public List<NVS.IpInfo> GiveMeNodeList()
        {
            List<NVS.IpInfo> tmpNodeList = new List<NVS.IpInfo>();
            foreach (KeyValuePair<string, NVS.IpInfo> entry in NGF.ValidatorList)
            {
                if (string.Equals(entry.Key, NVG.Settings.Nodes.My.HexKey) == false)
                {
                    if (entry.Value.Status == NVS.NodeStatus.Online)
                    {
                        tmpNodeList.Add(new NVS.IpInfo()
                        {
                            IpAddress = entry.Value.IpAddress,
                            Port = entry.Value.Port
                        });
                    }
                }
            }
            return tmpNodeList;
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
        public string ProcessIncomeData(string incomeData)
        {
            if (CheckXmlTag(incomeData, "block"))
            {
                //sync-control
                /*
                bu değişken true olunca, öncelikle diğer node'dan 
                blok alınması işlemini tamamla,
                blok alma işi bitince yeni blok oluşturulsun
                */
                NVG.Settings.WaitForGeneratedBlock = true;
                NP.Info("NVG.Settings.WaitForGeneratedBlock = TRUE;");

                string incomeDataStr = GetPureText(incomeData, "block");
                NP.Info("Income Block Row No -> " + incomeDataStr);
                if (incomeDataStr.IndexOf(":") < 0)
                {
                    NVG.Settings.WaitForGeneratedBlock = false;
                    NP.Warning("NVG.Settings.WaitForGeneratedBlock = FALSE;");
                    return "error-msg";
                }

                string[] tmpArr = incomeDataStr.Split(":");
                NP.Info("Income Block Row No -> " + tmpArr[0] + ", Validator => " + tmpArr[1]);
                long tmpBlockNo = long.Parse(tmpArr[0]);
                string tmpNodeWalletKey = tmpArr[1];
                string tmpIpAddress = string.Empty;
                int tmpPortNo = 0;
                foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
                {
                    if (string.Equals(entry.Value.IP.Wallet, tmpNodeWalletKey))
                    {
                        tmpIpAddress = entry.Value.IP.IpAddress;
                        tmpPortNo = entry.Value.IP.Port;
                        break;
                    }
                }
                if (tmpPortNo == 0)
                {
                    NVG.Settings.WaitForGeneratedBlock = false;
                    NP.Warning("NVG.Settings.WaitForGeneratedBlock = FALSE;");
                    Console.WriteLine("Queue.cs -> tmpPortNo Is Zero");
                    return "fncResult-port-zero";
                }
                Variable.Class.BlockData? tmpBlockData =
                    Notus.Toolbox.Network.GetBlockFromNode(tmpIpAddress, tmpPortNo, tmpBlockNo, NVG.Settings);
                if (tmpBlockData == null)
                {
                    NVG.Settings.WaitForGeneratedBlock = false;
                    NP.Warning("NVG.Settings.WaitForGeneratedBlock = FALSE;");
                    Console.WriteLine("Queue.cs -> Block Is NULL");
                    return "tmpError-true";
                }
                //NP.Info("<block> Downloaded from other validator");
                if (Func_NewBlockIncome != null)
                {
                    if (Func_NewBlockIncome(tmpBlockData) == true)
                    {
                        NVG.Settings.WaitForGeneratedBlock = false;
                        NP.Warning("NVG.Settings.WaitForGeneratedBlock = FALSE;");
                        return "done";
                    }
                }
                NVG.Settings.WaitForGeneratedBlock = false;
                NP.Warning("NVG.Settings.WaitForGeneratedBlock = FALSE;");
                return "fncResult-false";
            }
            if (CheckXmlTag(incomeData, "kill"))
            {
                incomeData = GetPureText(incomeData, "kill");
                string[] tmpHashPart = incomeData.Split(NVC.CommonDelimeterChar);
                ulong incomeUtc = ulong.Parse(tmpHashPart[1]);
                ulong incomeDiff = (ulong)Math.Abs((decimal)NVG.NOW.Int - incomeUtc);

                //100 saniyeden eski ise göz ardı edilecek
                if (incomeDiff > 100000)
                {
                    return "0";
                }
                foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
                {
                    if (string.Equals(tmpHashPart[0], entry.Value.IP.Wallet) == true)
                    {
                        if (
                            Notus.Wallet.ID.Verify(
                                tmpHashPart[1] +
                                    Notus.Variable.Constant.CommonDelimeterChar +
                                tmpHashPart[0],
                                tmpHashPart[2],
                                entry.Value.PublicKey
                            ) == true
                        )
                        {
                            if (NVG.NodeList.ContainsKey(entry.Key))
                            {
                                NVG.NodeList.TryRemove(entry.Key, out _);
                                //NVG.NodeList[entry.Key].Status = NVS.NodeStatus.Offline;
                                //NVG.NodeList[entry.Key].SyncNo = 0;
                                //Thread.Sleep(10);
                                NP.Info("Node Just Left : " + entry.Value.IP.Wallet);
                                NP.NodeCount();
                                return "1";
                            }
                        }
                    }
                }
                return "0";
            }

            if (CheckXmlTag(incomeData, "when"))
            {
                StartingTimeAfterEnoughNode = ND.ToDateTime(GetPureText(incomeData, "when"));
                NVG.NodeQueue.Starting = Notus.Date.ToLong(StartingTimeAfterEnoughNode);
                NVG.CurrentSyncNo = NVG.NodeQueue.Starting;
                NVG.NodeQueue.OrderCount = 1;
                NVG.NodeQueue.Begin = true;
                StartingTimeAfterEnoughNode_Arrived = true;
                return "done";
            }
            if (CheckXmlTag(incomeData, "hash"))
            {
                incomeData = GetPureText(incomeData, "hash");
                string[] tmpHashPart = incomeData.Split(':');
                if (string.Equals(tmpHashPart[0], NGF.ValidatorListHash.Substring(0, 20)) == false)
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
                return (string.Equals(GetPureText(incomeData, "lhash"), NGF.ValidatorListHash) == true ? "1" : "0");
            }
            if (CheckXmlTag(incomeData, "nList"))
            {
                //burada eğer liste içeriği farklı ise yeni listeyi gönder
                incomeData = GetPureText(incomeData, "nList");
                SortedDictionary<string, NVS.IpInfo>? tmpNodeList = JsonSerializer.Deserialize<SortedDictionary<string, NVS.IpInfo>>(incomeData);
                if (tmpNodeList == null)
                {
                    return "0";
                }
                foreach (KeyValuePair<string, NVS.IpInfo> entry in tmpNodeList)
                {
                    NGF.AddToValidatorList(entry.Value.IpAddress, entry.Value.Port);
                }
                return "1";
            }


            if (CheckXmlTag(incomeData, "ready"))
            {
                incomeData = GetPureText(incomeData, "ready");
                Console.WriteLine("Ready Income : " + incomeData);
                foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
                {
                    if (string.Equals(entry.Value.IP.Wallet, incomeData) == true)
                    {
                        NVG.NodeList[entry.Key].Ready = true;
                    }
                }
                return "done";
            }
            if (CheckXmlTag(incomeData, "rNode"))
            {
                return "<node>" + JsonSerializer.Serialize(NVG.NodeList[NVG.Settings.Nodes.My.HexKey]) + "</node>";
            }
            if (CheckXmlTag(incomeData, "syncNo"))
            {
                incomeData = GetPureText(incomeData, "syncNo");
                string[] tmpArr = incomeData.Split(":");
                string selectedEarliestWalletId = tmpArr[0];
                string chooserWalletId = tmpArr[2];
                string chooserSignStr = tmpArr[3];
                string controlText =
                    selectedEarliestWalletId +
                        NVC.CommonDelimeterChar +
                    NVG.CurrentSyncNo.ToString() +
                        NVC.CommonDelimeterChar +
                    chooserWalletId;
                foreach (var iEntry in NVG.NodeList)
                {
                    if (string.Equals(iEntry.Value.IP.Wallet, chooserWalletId) == true)
                    {
                        if (Notus.Wallet.ID.Verify(controlText, chooserSignStr, iEntry.Value.PublicKey) == true)
                        {
                            if (NVG.NetworkSelectorList.ContainsKey(selectedEarliestWalletId) == false)
                            {
                                // sıradaki cüzdan, sıradaki node'a haber verecek node
                                NVG.NetworkSelectorList.Add(selectedEarliestWalletId, chooserWalletId);
                            }
                            Console.WriteLine("Queue.cs -> Line 441");
                            Console.WriteLine(JsonSerializer.Serialize(NVG.NetworkSelectorList));
                            return "1";
                        }
                    }
                }
                return "0";
            }
            if (CheckXmlTag(incomeData, "yourTurn"))
            {
                incomeData = GetPureText(incomeData, "yourTurn");
                string[] tmpArr = incomeData.Split(":");
                ulong selectedSyncNo = ulong.Parse(tmpArr[0]);
                string chooserWalletId = tmpArr[1];
                string chooserSignStr = tmpArr[2];

                foreach (var iEntry in NVG.NodeList)
                {
                    if (string.Equals(iEntry.Value.IP.Wallet, chooserWalletId) == true)
                    {
                        string controlText =
                            NVG.Settings.Nodes.My.IP.Wallet + NVC.CommonDelimeterChar +
                            selectedSyncNo.ToString() + NVC.CommonDelimeterChar + chooserWalletId;

                        if (Notus.Wallet.ID.Verify(controlText, chooserSignStr, iEntry.Value.PublicKey) == true)
                        {
                            Console.WriteLine("My Turn Sign Valid");
                            /*
                            if (NVG.NetworkSelectorList.ContainsKey(selectedEarliestWalletId) == false)
                            {
                                // sıradaki cüzdan, sıradaki node'a haber verecek node
                                NVG.NetworkSelectorList.Add(selectedEarliestWalletId, chooserWalletId);
                            }
                            Console.WriteLine("Queue.cs -> Line 441");
                            Console.WriteLine(JsonSerializer.Serialize(NVG.NetworkSelectorList));
                            */
                            NVG.OtherValidatorSelectedMe = true;
                            return "1";
                        }
                        else
                        {
                            Console.WriteLine("My Turn Sign WRONG");
                            Console.WriteLine("My Turn Sign WRONG");
                        }
                    }
                }
                return "0";
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
                        // NVC.MinimumNodeCount

                        /*
                        control - point

                        burası normal şekilde çalışacak
                        bir kişi ağa katılmak istediğinde mevcut 
                        sync olanlar arasından bir tanesi ona ne zaman dahil olacağını bildirecek.
                        yeni node o zamana kadar bekleyecek ve o zaman geldiğinde ağa dahil olacak
                        eğer ağda yeterli sayıda node yok ise hemen bağlanacak
                        eğer ağda yeterli sayıda node var ise o zaman group numarası verecek o numara gelince başlayacak


                        burada sync numaraları sıfır ile başlıyorsa ilk başlangıç demektir.
                        "Sync No" eğer sıfırdan büyük ise o zaman "JoinTime" geçerli zaman değerini referans alarak
                        içeri eklenecek.
                        seçilen "JoinTime" değeri zaman olarak geldiğinde sıralamaya dahil edilecek
                        o zamana kadar dinlemeye devam edecek
                        */
                        if (NVG.NodeList.ContainsKey(tmpNodeQueueInfo.HexKey))
                        {
                            NVG.NodeList[tmpNodeQueueInfo.HexKey] = tmpNodeQueueInfo;
                        }
                        else
                        {
                            NVG.NodeList.TryAdd(tmpNodeQueueInfo.HexKey, tmpNodeQueueInfo);
                        }

                        // eğer false ise senkronizasyon başlamamış demektir...
                        // NVG.Settings.SyncStarted = false;
                        // Console.WriteLine("*******************************");
                        Console.WriteLine("Queue.cs->Line 511");
                        Console.WriteLine(JsonSerializer.Serialize(NVG.NodeList));
                        NGF.AddToValidatorList(tmpNodeQueueInfo.IP.IpAddress, tmpNodeQueueInfo.IP.Port);
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
                    NGF.AddToValidatorList(entry.Value.IpAddress, entry.Value.Port);
                }
                return "<list>" + JsonSerializer.Serialize(NGF.ValidatorList) + "</list>";
            }
            return "<err>1</err>";
        }
        private void NodeIsOnline(string nodeHexText)
        {
            if (NVG.NodeList.ContainsKey(nodeHexText) == true)
            {
                NVG.NodeList[nodeHexText].Status = NVS.NodeStatus.Online;
            }
        }
        private string SendMessage(NVS.NodeInfo receiverIp, string messageText, string nodeHexStr = "")
        {
            return NGF.SendMessage(receiverIp.IpAddress, receiverIp.Port, messageText, nodeHexStr);
        }
        private string SendMessageED(string nodeHex, string ipAddress, int portNo, string messageText)
        {
            (bool worksCorrent, string incodeResponse) = Notus.Communication.Request.PostSync(
                Notus.Network.Node.MakeHttpListenerPath(ipAddress, portNo) +
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
        private string SendMessageED(string nodeHex, NVS.IpInfo nodeInfo, string messageText)
        {
            return SendMessageED(nodeHex, nodeInfo.IpAddress, nodeInfo.Port, messageText);
        }
        private string Message_Hash_ViaSocket(string _ipAddress, int _portNo)
        {
            return NGF.SendMessage(
                _ipAddress,
                _portNo,
                "<hash>" +
                    NGF.ValidatorListHash.Substring(0, 20) + ":" + NodeListHash.Substring(0, 20) +
                "</hash>"
            );
        }
        private void Message_Node_ViaSocket(string _ipAddress, int _portNo, string _nodeHex = "")
        {
            string responseStr = NGF.SendMessage(_ipAddress, _portNo,
                "<node>" + JsonSerializer.Serialize(NVG.NodeList[NVG.Settings.Nodes.My.HexKey]) + "</node>"
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
            string tmpReturnStr = NGF.SendMessage(_ipAddress, _portNo, "<list>" + JsonSerializer.Serialize(MainAddressList) + "</list>", _nodeHex);
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
                if ((NVG.NOW.Obj - LastPingTime).TotalSeconds > 20 || SyncReady == false)
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
                    LastPingTime = NVG.NOW.Obj;
                }

                // burada durumu bilinmeyen nodeların bilgilerinin sorgulandığı kısım
                foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
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
                foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
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

            Dictionary<int, ulong> tmpTimeList = new Dictionary<int, ulong>();
            Dictionary<int, NVS.NodeInfo> tmpNodeList = new Dictionary<int, NVS.NodeInfo>();
            int tmpOrderNo = 1;
            while (exitFromInnerWhile == false)
            {
                foreach (KeyValuePair<BigInteger, string> outerEntry in nodeWalletList)
                {
                    foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
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
                                    Wallet = entry.Value.IP.Wallet,
                                    GroupNo = NVG.GroupNo,
                                    //Client = new Dictionary<string, Communication.Sync.Socket.Client>()
                                });


                                // her node için sunucu listesi oluşturulacak ve
                                // bunun için geçici liste oluşturuluyor...
                                tmpTimeList.Add(tmpOrderNo, tmpSyncNo);
                                tmpNodeList.Add(tmpOrderNo, new NVS.NodeInfo()
                                {
                                    IpAddress = entry.Value.IP.IpAddress,
                                    Port = entry.Value.IP.Port,
                                    Wallet = entry.Value.IP.Wallet,
                                    GroupNo = NVG.GroupNo
                                });
                                tmpOrderNo++;


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

            /*
            //önce soket server başlatılacak
            foreach (KeyValuePair<int, NVS.NodeInfo> entry in tmpNodeList)
            {
                StartPrivateSockerServer(entry.Value.Wallet);
            }

            //şimdi kuyruktaki her node için istemci başlatılacak...
            foreach (KeyValuePair<int, NVS.NodeInfo> entry in tmpNodeList)
            {
                if (NVG.Settings.Nodes.Queue[tmpTimeList[entry.Key]].Client.ContainsKey(entry.Value.Wallet) == false)
                {
                    NVG.Settings.Nodes.Queue[tmpTimeList[entry.Key]].Client.Add(
                        entry.Value.Wallet,
                        new Notus.Communication.Sync.Socket.Client()
                    );
                }
                //StartPrivateSockerServer(entry.Value.Wallet);
            }
            */

            /*
            burada her node, diğer nodeların client'larını başlatacak ve çalışır hale getirecek...

            veya doğrudan gossip protokolü benzeri bir yapı ekleyelim
            ve bu yapı daha ilk başlangıçta kurulsun ve gerekli durumlarda kullanılsın

            //şimdi burada her node diğer nodeların hepsine bağlanacak...
            foreach (KeyValuePair<int, NVS.NodeInfo> entry in tmpNodeList)
            {
                if (string.Equals(entry.Value.Wallet, NVG.Settings.Nodes.My.IP.Wallet) == false)
                {
                    if (NVG.Settings.Nodes.Queue[tmpTimeList[entry.Key]].Client.ContainsKey(entry.Value.Wallet) == false)
                    {

                    }
                }
            }
            */

            // sonra client nesneleri başlatılacak
            NVG.GroupNo = NVG.GroupNo + 1;

            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
            {
                if (entry.Value.Status == NVS.NodeStatus.Online && entry.Value.SyncNo == biggestSyncNo)
                {
                    NVG.NodeList[entry.Key].SyncNo = syncStaringTime;
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
        public SortedDictionary<BigInteger, string> MakeOrderToNode(ulong biggestSyncNo, string seedForQueue)
        {
            SortedDictionary<BigInteger, string> resultList = new SortedDictionary<BigInteger, string>();
            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
            {
                //node-order-exception
                //burada hangi nodeların devreye gireceğini seçelim
                //burada hangi nodeların devreye gireceğini seçelim
                //burada hangi nodeların devreye gireceğini seçelim
                //burada hangi nodeların devreye gireceğini seçelim
                if (entry.Value.Status == NVS.NodeStatus.Online && entry.Value.SyncNo == biggestSyncNo)
                {
                    bool exitInnerWhileLoop = false;
                    int innerCount = 1;
                    while (exitInnerWhileLoop == false)
                    {
                        BigInteger intWalletNo = BigInteger.Parse(
                            "0" +
                            new NH().CommonHash("sha1",
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

            NVG.NodeList.Clear();
            NVG.NodeList.TryAdd(NVG.Settings.Nodes.My.HexKey, new NVS.NodeQueueInfo()
            {
                Ready = true,
                Status = NVS.NodeStatus.Online,
                HexKey = NVG.Settings.Nodes.My.HexKey,
                Begin = NVG.Settings.Nodes.My.Begin,
                SyncNo = 0,
                Tick = NVG.NOW.Int,
                IP = new NVS.NodeInfo()
                {
                    IpAddress = NVG.Settings.Nodes.My.IP.IpAddress,
                    Port = NVG.Settings.Nodes.My.IP.Port,
                    Wallet = NVG.Settings.NodeWallet.WalletKey
                },
                JoinTime = 0,
                PublicKey = NVG.Settings.Nodes.My.PublicKey,
            });
            NGF.AddToValidatorList(NVG.Settings.Nodes.My.IP.IpAddress, NVG.Settings.Nodes.My.IP.Port);

            foreach (KeyValuePair<string, NVS.IpInfo> entry in NGF.ValidatorList)
            {
                if (string.Equals(NVG.Settings.Nodes.My.HexKey, entry.Key) == false)
                {
                    string tmpHexKeyStr = Notus.Toolbox.Network.IpAndPortToHex(entry.Value.IpAddress, entry.Value.Port);
                    NVG.NodeList.TryAdd(tmpHexKeyStr, new NVS.NodeQueueInfo()
                    {
                        Ready = false,
                        Status = NVS.NodeStatus.Unknown,
                        Begin = 0,
                        Tick = 0,
                        SyncNo = 0,
                        HexKey = tmpHexKeyStr,
                        IP = new NVS.NodeInfo()
                        {
                            IpAddress = entry.Value.IpAddress,
                            Port = entry.Value.Port,
                            Wallet = "#"
                        },
                        JoinTime = 0,
                        PublicKey = ""
                    });
                }
            }

            foreach (KeyValuePair<string, NVS.IpInfo> entry in NGF.ValidatorList)
            {
                NGF.ValidatorList[entry.Key].Status = NVS.NodeStatus.Unknown;
            }
            NGF.ValidatorList[NVG.Settings.Nodes.My.HexKey].Status = NVS.NodeStatus.Online;
            /*
            Console.WriteLine("Queue.cs->Line 1044");
            Console.WriteLine(JsonSerializer.Serialize(NVG.NodeList, NVC.JsonSetting));
            NP.ReadLine();
            */
            NP.Info("Node Sync Starting", false);

            //listedekilere ping atıyor, eğer 1 adet node aktif ise çıkış yapıyor...
            PingOtherNodes();

            // mevcut node ile diğer nodeların listeleri senkron hale getiriliyor
            SyncListWithNode();

            // diğer node'lara bizim kim olduğumuz söyleniyor...
            SendMyInfoToAllNodes();

            //eğer bende bilgisi olmayan node varsa bilgisini istiyor
            AskInfoFromNode();

            //önce node'ların içerisinde senkronizasyon bekleyen olmadığına emin ol
            WaitUntilAvailable();

            //node-order-exception
            //NP.ReadLine();

            /*
            bu değişken true ise sync_no diğer validator tarafından gönderilecek demektir
            node'un önce kendi bloklarını eşitlemesi gerekir
            sonrasıda ise izin verilen zamana kadar beklemesi gerekir.
            */

            ulong biggestSyncNo = FindBiggestSyncNo();

            if (NVG.OtherValidatorSelectedMe == true)
            {
                // NVG.CurrentSyncNo = biggestSyncNo;
                // NVG.NodeQueue.Starting = biggestSyncNo;
                //NVG.NodeQueue.OrderCount = 1;

                // NVG.NodeQueue.Begin = false;
                // NVG.Settings.Nodes.My.JoinTime = ND.GetJoinTime(biggestSyncNo);
                // NVG.NodeQueue.
                // NVG.NodeList[NVG.Settings.Nodes.My.HexKey].
                // eğer false ise senkronizasyon başlamamış demektir...
                //NVG.Settings.SyncStarted = false;

                //Console.WriteLine("Queue.cs -> Line 1035");
                //Console.WriteLine("biggestSyncNo : " + biggestSyncNo.ToString());
                //Console.WriteLine("if (NVG.OtherValidatorSelectedMe == true)");
            }

            if (NVG.OtherValidatorSelectedMe == false)
            {
                //bu fonksyion ile amaç en çok sayıda olan sync no bulunacak
                StartingTimeAfterEnoughNode_Arrived = false;
                if (biggestSyncNo == 0)
                {
                    NP.NodeCount();
                    //cüzdanların hashleri alınıp sıraya koyuluyor.
                    SortedDictionary<BigInteger, string> tmpWalletList = MakeOrderToNode(biggestSyncNo, "beginning");

                    //Console.WriteLine(JsonSerializer.Serialize(NVG.NodeList, NVC.JsonSetting));
                    //birinci sırada ki cüzdan seçiliyor...
                    string tmpFirstWalletId = tmpWalletList.First().Value;
                    if (string.Equals(tmpFirstWalletId, NVG.Settings.Nodes.My.IP.Wallet))
                    {
                        Thread.Sleep(5000);
                        StartingTimeAfterEnoughNode = CalculateStartingTime();
                        ulong syncStaringTime = ND.ToLong(StartingTimeAfterEnoughNode);
                        GenerateNodeQueue(biggestSyncNo, syncStaringTime, tmpWalletList);

                        NP.Info(
                            "I'm Sending Starting (When) Time / Current : " +
                            StartingTimeAfterEnoughNode.ToString("HH:mm:ss.fff") +
                            " / " + NVG.NOW.Obj.ToString("HH:mm:ss.fff")
                        );
                        NVG.CurrentSyncNo = syncStaringTime;
                        NVG.NodeQueue.Starting = syncStaringTime;
                        NVG.NodeQueue.OrderCount = 1;
                        NVG.NodeQueue.Begin = true;
                        // eğer false ise senkronizasyon başlamamış demektir...
                        NVG.Settings.SyncStarted = false;

                        // diğer nodelara belirlediğimiz zaman bilgisini gönderiyoruz
                        foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
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
                                            string tmpResult = SendMessage(entry.Value.IP, "<when>" + syncStaringTime + "</when>", entry.Key);
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
                        NP.Info(
                            "I'm Waiting Starting (When) Time / Current : " +
                            StartingTimeAfterEnoughNode.ToString("HH:mm:ss.fff") +
                            " /  " +
                            NVG.NOW.Obj.ToString("HH:mm:ss.fff")
                        );
                        // eğer false ise senkronizasyon başlamamış demektir...
                        NVG.Settings.SyncStarted = false;
                    }
                }
                else
                {
                    // bu değişken true ise, senkronizasyonun başladığı anlaşılıyor...

                    NVG.Settings.SyncStarted = true;

                    /*
                    büyük değerli bir sayı var ise bu sayının 100 saniye eksiği ile listeye eklenecek
                    ve her turda 1 saniye eklenecek ta ki diğer  en başta belirlene sync numarasına erişene kadar
                    sonrasında kuraya da
                    */
                    NewNodeJoinToGroup(biggestSyncNo);
                    Console.WriteLine("------------------------------------------");
                    Console.WriteLine("Queue.cs -> Line 1194");
                    Console.WriteLine(JsonSerializer.Serialize(NVG.NodeList));
                    Console.WriteLine("------------------------------------------");
                    Console.WriteLine("There Is Biggest Sync No");
                    NP.ReadLine();
                }
            }
        }
        public string NewNodeJoinToGroup(ulong biggestSyncNo)
        {
            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
            {
                if (
                    entry.Value.SyncNo == biggestSyncNo
                        &&
                    entry.Value.Status == NVS.NodeStatus.Online
                )
                {

                }
                /*
                burada yeni katılan node için bir karar verecek node seçilecek
                karar verecek node diğer nodelara bilgi verecek bu node'un katılma zamanını bildirecek.
                */
            }
            return string.Empty;
        }
        public void ReOrderNodeQueue(ulong currentQueueTime, string queueSeedStr = "")
        {
            ulong biggestSyncNo = FindBiggestSyncNo();
            SortedDictionary<BigInteger, string> tmpWalletList = MakeOrderToNode(biggestSyncNo, queueSeedStr);
            GenerateNodeQueue(currentQueueTime, ND.AddMiliseconds(currentQueueTime, 1500), tmpWalletList);
            NVG.NodeQueue.OrderCount++;
        }
        public void TeelTheNodeWhoWaitingRoom(string selectedEarliestWalletId)
        {
            foreach (var iEntry in NVG.NodeList)
            {
                if (string.Equals(iEntry.Value.IP.Wallet, selectedEarliestWalletId) == true)
                {
                    string tmpSyncNoStr = "<yourTurn>" +
                        NVG.CurrentSyncNo.ToString() + NVC.CommonDelimeterChar +
                        NVG.Settings.Nodes.My.IP.Wallet + NVC.CommonDelimeterChar +
                        Notus.Wallet.ID.Sign(
                            selectedEarliestWalletId + NVC.CommonDelimeterChar +
                            NVG.CurrentSyncNo.ToString() + NVC.CommonDelimeterChar +
                            NVG.Settings.Nodes.My.IP.Wallet,
                            NVG.SessionPrivateKey
                        ) +
                        "</yourTurn>";
                    /*
                    NVG.Settings.Nodes.My.IP.Wallet + NVC.CommonDelimeterChar +
                    selectedSyncNo.ToString() + NVC.CommonDelimeterChar + chooserWalletId;
                    */
                    string resultStr = SendMessageED(
                        iEntry.Key,
                        iEntry.Value.IP.IpAddress,
                        iEntry.Value.IP.Port,
                        tmpSyncNoStr
                    );
                    Console.WriteLine("TeelTheNodeWhoWaitingRoom : " + resultStr);
                }
            }
        }
        public void TellSyncNoToEarlistNode(string selectedEarliestWalletId)
        {
            string tmpSyncNoStr = "<syncNo>" +
                selectedEarliestWalletId + NVC.CommonDelimeterChar +
                NVG.CurrentSyncNo.ToString() + NVC.CommonDelimeterChar +
                NVG.Settings.Nodes.My.IP.Wallet + NVC.CommonDelimeterChar +
                Notus.Wallet.ID.Sign(
                    selectedEarliestWalletId +
                        NVC.CommonDelimeterChar +
                    NVG.CurrentSyncNo.ToString() +
                        NVC.CommonDelimeterChar +
                    NVG.Settings.Nodes.My.IP.Wallet,
                    NVG.SessionPrivateKey
                ) +
                "</syncNo>";
            foreach (var iEntry in NVG.NodeList)
            {
                bool youCanSend = false;
                if (string.Equals(iEntry.Value.IP.Wallet, selectedEarliestWalletId) == false)
                {
                    youCanSend = true;
                }
                else
                {
                    if (iEntry.Value.SyncNo == NVG.CurrentSyncNo)
                    {
                        if (string.Equals(iEntry.Value.IP.Wallet, NVG.Settings.Nodes.My.IP.Wallet) == false)
                        {
                            youCanSend = true;
                        }
                    }
                }

                if (youCanSend == true)
                {
                    string resultStr = SendMessageED(
                        iEntry.Key,
                        iEntry.Value.IP.IpAddress,
                        iEntry.Value.IP.Port,
                        tmpSyncNoStr
                    );
                }
            }
        }
        private void WaitUntilAvailable()
        {
            /*
            burada beklerken diğer node'dan syncno zamanı gelecek
            gelen zamana kadar buradan ve diğer işlemleri bypass ederek 
            doğrudan iletişim kısmına geçecek
            */
            // buradaki sayı 2 olana kadar bekle
            Dictionary<ulong, bool> syncNoCount = new();
            bool exitLoop = false;
            while (exitLoop == false)
            {
                syncNoCount.Clear();
                foreach (var iEntry in NVG.NodeList)
                {
                    if (iEntry.Value.Status == NVS.NodeStatus.Online)
                    {
                        Console.WriteLine(iEntry.Key + " -> " + iEntry.Value.SyncNo.ToString());
                        if (syncNoCount.ContainsKey(iEntry.Value.SyncNo) == false)
                        {
                            syncNoCount.Add(iEntry.Value.SyncNo, true);
                        }
                    }
                }

                /*
                bu değişken diğer validator tarafından verilen izni temsil ediyor
                */
                if (NVG.OtherValidatorSelectedMe == true)
                {
                    exitLoop = true;
                }

                /*
                
                sayı 1 adet veya benim SYNC_NO değerim eşit olduğunda çıkış yapılsın
                çıkış yapıldıktan sonra eksik bloklar yüklenecek ve senkronizasyon
                süreci tamamlanana kadar bekleyecek.
                
                */
                if (syncNoCount.Count == 1)
                {
                    exitLoop = true;
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }
        private ulong FindBiggestSyncNo()
        {
            Dictionary<ulong, int> syncNoCount = new Dictionary<ulong, int>();
            foreach (var iEntry in NVG.NodeList)
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
            if (biggestSyncNo > 0)
            {
                //Console.WriteLine(JsonSerializer.Serialize(syncNoCount));
            }
            return biggestSyncNo;
        }
        private void SendMyInfoToAllNodes()
        {
            // her 30 saniyede bir diğer node'ları kim olduğumu söylüyor.
            KeyValuePair<string, NVS.IpInfo>[]? tmpMainList = MainAddressList.ToArray();
            if (tmpMainList != null)
            {
                string myNodeDataText = "<node>" + JsonSerializer.Serialize(NVG.NodeList[NVG.Settings.Nodes.My.HexKey]) + "</node>";
                for (int i = 0; i < tmpMainList.Length; i++)
                {
                    if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                    {
                        if (SendMessageED(tmpMainList[i].Key, tmpMainList[i].Value, myNodeDataText) == "1")
                        {
                            NVG.NodeList[tmpMainList[i].Key].Status = NVS.NodeStatus.Online;
                        }
                    }
                }
            }
        }
        private void AskInfoFromNode()
        {
            KeyValuePair<string, NVS.IpInfo>[]? tmpMainList = MainAddressList.ToArray();
            if (tmpMainList != null)
            {
                for (int i = 0; i < tmpMainList.Length; i++)
                {
                    if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                    {
                        if (NVG.NodeList.ContainsKey(tmpMainList[i].Key))
                        {
                            if (NVG.NodeList[tmpMainList[i].Key].Status == NVS.NodeStatus.Online)
                            {
                                if (NVG.NodeList[tmpMainList[i].Key].Tick == 0)
                                {
                                    ProcessIncomeData(SendMessageED(
                                        tmpMainList[i].Key, tmpMainList[i].Value, "<rNode>1</rNode>"
                                    ));
                                }
                            }
                        }
                    }
                }
            }
        }
        private void SyncListWithNode()
        {
            NP.Info("Node List Sync With Other Nodes");
            /*
            burayı düzeltelim
            sadece liste değiştirilsin,
            liste değiştirilmesi tamamlandıktan sonra sonraki adıma geçilsin
            çünkü katılmak isteyen node'lar sıra ile içeri alınacak
            */

            bool exitSyncLoop = false;
            KeyValuePair<string, NVS.IpInfo>[]? tmpMainList = MainAddressList.ToArray();
            while (exitSyncLoop == false)
            {
                if (tmpMainList != null)
                {
                    //liste değişimi yapılıyor
                    for (int i = 0; i < tmpMainList.Length; i++)
                    {
                        if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                        {
                            string innerResponseStr = SendMessageED(
                                tmpMainList[i].Key, tmpMainList[i].Value,
                                "<nList>" + JsonSerializer.Serialize(MainAddressList) + "</nList>"
                            );
                            if (innerResponseStr == "1")
                            {
                                MainAddressList[tmpMainList[i].Key].Status = NVS.NodeStatus.Online;
                                Console.WriteLine("Queue.cs -> Line 1435 -> PING online -> " + MainAddressList[tmpMainList[i].Key].IpAddress);
                            }
                            else
                            {
                                MainAddressList[tmpMainList[i].Key].Status = NVS.NodeStatus.Offline;
                                Console.WriteLine("Queue.cs -> Line 1435 -> PING offline -> " + MainAddressList[tmpMainList[i].Key].IpAddress);
                            }
                        }
                    }

                    /*
                    bool allListSyncWithNode = true;
                    //return (string.Equals(GetPureText(incomeData, "lhash"), NGF.ValidatorListHash) == true ? "1" : "0");

                    // liste hashleri takas ediliyor
                    for (int i = 0; i < tmpMainList.Length; i++)
                    {
                        if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                        {
                            string innerResponseStr = SendMessageED(
                                tmpMainList[i].Key, tmpMainList[i].Value,
                                "<lhash>" + NGF.ValidatorListHash + "</lhash>"
                            );
                            //Console.WriteLine("<lhash> innerResponseStr: [ " + tmpMainList[i].Value.IpAddress + " ] " + innerResponseStr);
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
                    */
                }
                exitSyncLoop = true;
            }
        }
        public Queue()
        {
            NVG.NodeList.Clear();
            LastPingTime = NVC.DefaultTime;
        }
        ~Queue()
        {
            Dispose();
        }
        public void Dispose()
        {
            ExitFromLoop = true;
        }
    }
}
//bitiş noktası 1400.satır