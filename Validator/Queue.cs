﻿using System.Globalization;
using System.Numerics;
using System.Text.Json;
using NCH = Notus.Communication.Helper;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NH = Notus.Hash;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVS = Notus.Variable.Struct;
namespace Notus.Validator
{
    public class Queue : IDisposable
    {
        private Dictionary<string,bool> ReadyMessageIncomeList = new Dictionary<string, bool>();
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
            ulong totalQueuePeriod = NVC.BlockListeningForPoolTime + NVC.BlockGeneratingTime + NVC.BlockDistributingTime;
            ulong nextValidatorNodeTime = ND.AddMiliseconds(currentNodeStartingTime, totalQueuePeriod);


            // sonraki node'a doğrudan gönder,
            // 2 sonraki node task ile gönderebilirsin.

            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
            {
                if (
                    string.Equals(NVG.Settings.Nodes.My.HexKey, entry.Key) == false &&
                    entry.Value.Status == NVS.NodeStatus.Online
                )
                {
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
                            Console.WriteLine("Queue.cs -> Line 72");
                            Console.WriteLine(JsonSerializer.Serialize(wList));
                        }
                    }
                    string incomeResult = NVG.Settings.MsgOrch.SendMsg(
                        entry.Value.IP.Wallet,
                        "<block>" +
                            blockRowNo.ToString() + ":" + NVG.Settings.NodeWallet.WalletKey +
                        "</block>"
                    );
                    if (string.Equals(incomeResult.ToLower(), "done"))
                    {
                        NP.Info(
                        "Distributed [ " + fixedRowNoLength(blockRowNo) + " : " + blockType.ToString() + " ] To " +
                            entry.Value.IP.IpAddress + ":" + entry.Value.IP.Port.ToString()
                        );
                    }
                    else
                    {
                        NP.Warning(
                        "Distribution Error [ " + fixedRowNoLength(blockRowNo) + " : " + blockType.ToString() + " ] " +
                            entry.Value.IP.IpAddress + ":" + entry.Value.IP.Port.ToString() +
                            " -> " + incomeResult
                        );
                    }
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
        private void FindOnlineNode()
        {
            // listesinde eğer 1 adet çevrim içi node varsa döngüden çıkış yapacak
            bool exitInnerWhile = false;
            NP.Info("Finding Online Nodes");

            while (exitInnerWhile == false)
            {
                foreach (var iE in NGF.ValidatorList)
                {
                    if (string.Equals(iE.Key, NVG.Settings.Nodes.My.HexKey) == false)
                    {
                        if (Notus.Toolbox.Network.PingToNode(iE.Value) == NVS.NodeStatus.Online)
                        {
                            NGF.ValidatorList[iE.Key].Status = NVS.NodeStatus.Online;
                            exitInnerWhile = true;
                            break;
                        }
                    }
                }
                if (exitInnerWhile == false)
                    Thread.Sleep(100);
            }
        }

        private void RemoveOfflineNodes()
        {
            // çevrim dışı node'lar devre dışı bırakılıyor...
            NP.Info("Removing Offline Nodes From List");
            List<string> tmpRemoveKeyList = new List<string>();
            foreach (var iE in NGF.ValidatorList)
            {
                if (string.Equals(iE.Key, NVG.Settings.Nodes.My.HexKey) == false)
                {
                    if (Notus.Toolbox.Network.PingToNode(iE.Value) == NVS.NodeStatus.Online)
                    {
                        NGF.ValidatorList[iE.Key].Status = NVS.NodeStatus.Online;
                    }
                    else
                    {
                        tmpRemoveKeyList.Add(iE.Key);
                    }
                }
            }
            for (int i = 0; i < tmpRemoveKeyList.Count; i++)
            {
                NVH.RemoveFromValidatorList(tmpRemoveKeyList[i]);
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
                                NP.Info("Node Just Left : " + entry.Value.IP.Wallet);
                                NVH.RemoveFromValidatorList(entry.Key);
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
                    NVH.AddToValidatorList(entry.Value.IpAddress, entry.Value.Port);
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
                        //NVG.NodeList[entry.Key].Ready = true;
                    }
                }
                return "done";
            }
            if (CheckXmlTag(incomeData, "rNode"))
            {
                return "<node>" + JsonSerializer.Serialize(NVG.NodeList[NVG.Settings.Nodes.My.HexKey]) + "</node>";
            }

            if (CheckXmlTag(incomeData, "fReady"))
            {
                incomeData = GetPureText(incomeData, "fReady");
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
                                Console.WriteLine("my wallet : " + NVG.Settings.Nodes.My.IP.Wallet);
                                Console.WriteLine("income wallet : " + entry.Value.IP.Wallet);
                                if (ReadyMessageIncomeList.ContainsKey(entry.Value.IP.Wallet)==false)
                                {
                                    ReadyMessageIncomeList.Add(entry.Value.IP.Wallet, true);
                                }
                                return "1";
                            }
                        }
                    }
                }
                return "0";
            }
            if (CheckXmlTag(incomeData, "syncNo"))
            {
                /*
                burada syncNo diğer nodelar tarafından kabul edilecek
                burada syncNo diğer nodelar tarafından kabul edilecek
                burada syncNo diğer nodelar tarafından kabul edilecek
                burada syncNo diğer nodelar tarafından kabul edilecek
                */

                Console.WriteLine("incomeData : " + incomeData);
                incomeData = GetPureText(incomeData, "syncNo");
                string[] tmpArr = incomeData.Split(":");
                if (tmpArr.Length > 3)
                {
                    /*
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
                    */
                }
                return "0";
            }

            if (CheckXmlTag(incomeData, "yourTurn"))
            {
                incomeData = GetPureText(incomeData, "yourTurn");
                if (incomeData.IndexOf(':') >= 0)
                {
                    string[] tmpArr = incomeData.Split(":");
                    if (tmpArr.Length > 2)
                    {
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
                                    NVG.OtherValidatorSelectedMe = true;
                                    return "1";
                                }
                                return "0";
                            }
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
                        NVH.AddValidatorInfo(tmpNodeQueueInfo, true);

                        // eğer false ise senkronizasyon başlamamış demektir...
                        // NVG.Settings.SyncStarted = false;
                        //NP.Info("Validator Info Just Came Up -> " + tmpNodeQueueInfo.IP.Wallet);
                        NVH.AddToValidatorList(tmpNodeQueueInfo.IP.IpAddress, tmpNodeQueueInfo.IP.Port);
                        return "1";
                    }
                }
                catch { }
                return "0";
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
                    NVH.AddToValidatorList(entry.Value.IpAddress, entry.Value.Port);
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
            Console.WriteLine("Sending Node Info");
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
            string tmpReturnStr = NGF.SendMessage(_ipAddress, _portNo, "<list>" + JsonSerializer.Serialize(NGF.ValidatorList) + "</list>", _nodeHex);
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
                            tmpData = JsonSerializer.Serialize(NGF.ValidatorList);
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
            ulong queueTimePeriod = NVC.BlockListeningForPoolTime + NVC.BlockGeneratingTime + NVC.BlockDistributingTime;

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
            NVH.AddValidatorInfo(new NVS.NodeQueueInfo()
            {
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
            }, true);
            NVH.AddToValidatorList(NVG.Settings.Nodes.My.IP.IpAddress, NVG.Settings.Nodes.My.IP.Port);

            foreach (KeyValuePair<string, NVS.IpInfo> entry in NGF.ValidatorList)
            {
                if (string.Equals(NVG.Settings.Nodes.My.HexKey, entry.Key) == false)
                {
                    NVH.AddValidatorInfo(new NVS.NodeQueueInfo()
                    {
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
                        JoinTime = 0,
                        PublicKey = ""
                    }, false);
                }
            }

            foreach (KeyValuePair<string, NVS.IpInfo> entry in NGF.ValidatorList)
            {
                NGF.ValidatorList[entry.Key].Status = NVS.NodeStatus.Unknown;
            }
            NGF.ValidatorList[NVG.Settings.Nodes.My.HexKey].Status = NVS.NodeStatus.Online;

            NP.Info("Node Sync Starting", false);

            // eğer sadece 2 adet node var ise, node selector timer devreye girmeyecek
            // ilk 2 node'un devreye girişinden sonra selector timer çalışmaya başlayacak ve
            // diğer node'ların başlangıç zamanlarını baz alarak içeriye alacak
            // listedekilere ping atıyor, eğer 1 adet node aktif ise çıkış yapıyor...
            FindOnlineNode();

            // offline olan node'ları listeden çıkartılıyor
            RemoveOfflineNodes();

            // mevcut node ile diğer nodeların listeleri senkron hale getiriliyor
            SyncListWithNode();

            // diğer node'lara bizim kim olduğumuz söyleniyor...
            SendMyInfoToAllNodes();

            // eğer bende bilgisi olmayan node varsa bilgisini istiyor
            AskInfoFromNode();

            // önce node'ların içerisinde senkronizasyon bekleyen olmadığına emin ol
            WaitUntilAvailable();

            // node-order-exception

            /*
            bu değişken true ise sync_no diğer validator tarafından gönderilecek demektir
            node'un önce kendi bloklarını eşitlemesi gerekir
            sonrasıda ise izin verilen zamana kadar beklemesi gerekir.
            */

            ulong biggestSyncNo = FindBiggestSyncNo();
            NP.Info("Biggest Sync No : " + biggestSyncNo.ToString());
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
                    /*
                    foreach(var iE in NVG.NodeList)
                    {

                    }
                    //private string NCH.SendMessageED(string nodeHex, NVS.IpInfo nodeInfo, string messageText)
                    string resultStr = SendMessageED(
                        iEntry.Key,
                        iEntry.Value.IP.IpAddress,
                        iEntry.Value.IP.Port,
                        tmpSyncNoStr
                    );

                    

                    burada iki tarafında ready olması beklenecek
                    çünkü node'lardan biri diğerinden önce hareket ederse
                    diğer node önde hareket ediyor
                    */
                    // node'lar diğerini beklemeden başlangıç işlemine başlıyor bu yüzden 
                    // karşılıklı ready işlemi yapılmalı

                    //Console.WriteLine(JsonSerializer.Serialize(NVG.NodeList, NVC.JsonSetting));
                    //cüzdanların hashleri alınıp sıraya koyuluyor.
                    SortedDictionary<BigInteger, string> tmpWalletList = MakeOrderToNode(biggestSyncNo, "beginning");

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
                                            string tmpResult = NCH.SendMessage(entry.Value.IP, "<when>" + syncStaringTime + "</when>", entry.Key);
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
        private void WaitUntilAvailable()
        {
            NP.Info("Wait Until Nodes Available");
            // burada beklerken diğer node'dan syncno zamanı gelecek
            // gelen zamana kadar buradan ve diğer işlemleri bypass ederek 
            // doğrudan iletişim kısmına geçecek

            // buradaki sayı 2 olana kadar bekle
            Dictionary<ulong, int> syncNoCount = new Dictionary<ulong, int>();
            bool firstHandShake = false;
            bool exitLoop = false;
            while (exitLoop == false)
            {
                // bu değişken diğer validator tarafından verilen izni temsil ediyor
                if (NVG.OtherValidatorSelectedMe == true)
                {
                    exitLoop = true;
                }
                else
                {
                    KeyValuePair<string, NVS.NodeQueueInfo>[]? tmpArr = NVG.NodeList.ToArray();
                    if (tmpArr != null)
                    {
                        syncNoCount.Clear();
                        foreach (KeyValuePair<string, NVS.NodeQueueInfo> iE in tmpArr)
                        {
                            if (syncNoCount.ContainsKey(iE.Value.SyncNo) == false)
                            {
                                syncNoCount.Add(iE.Value.SyncNo, 0);
                            }
                            syncNoCount[iE.Value.SyncNo] = syncNoCount[iE.Value.SyncNo] + 1;
                        }

                        if (syncNoCount.ContainsKey(0))
                        {
                            if (syncNoCount[0] == 2)
                            {
                                //Console.WriteLine("Ilk-Baslangic-Durumu");
                                //Console.WriteLine(JsonSerializer.Serialize(NVG.NodeList));
                                firstHandShake = true;
                                // burada diğer node'un hazır olması durumunu bekleyecek
                                // kendisinin de buraya geldiğini belirtecek
                            }
                        }
                        else
                        {
                            Console.WriteLine(JsonSerializer.Serialize(syncNoCount));
                            NP.ReadLine();
                        }
                        // sayı 1 adet veya benim SYNC_NO değerim eşit olduğunda çıkış yapılsın
                        // çıkış yapıldıktan sonra eksik bloklar yüklenecek ve senkronizasyon
                        // süreci tamamlanana kadar bekleyecek.
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
            }
            Console.WriteLine("Queue.cs -> Line 1241");
            Console.WriteLine(JsonSerializer.Serialize(syncNoCount, NVC.JsonSetting));

            if (firstHandShake == true)
            {
                ulong nowUtcValue = NVG.NOW.Int;
                string controlSignForReadyMsg = Notus.Wallet.ID.Sign(
                    nowUtcValue.ToString() +
                        NVC.CommonDelimeterChar +
                    NVG.Settings.Nodes.My.IP.Wallet,
                    NVG.SessionPrivateKey
                );
                Dictionary<string, string> response = new Dictionary<string, string>();
                ReadyMessageIncomeList.Add(NVG.Settings.Nodes.My.IP.Wallet,true);
                foreach (var iE in NVG.NodeList)
                {
                    if (string.Equals(iE.Key, NVG.Settings.Nodes.My.HexKey) == false)
                    {
                        string resultStr=NCH.SendMessageED(iE.Key, iE.Value.IP.IpAddress, iE.Value.IP.Port, 
                            "<fReady>" +
                                NVG.Settings.Nodes.My.IP.Wallet +
                                NVC.CommonDelimeterChar +
                                nowUtcValue.ToString() +
                                NVC.CommonDelimeterChar +
                                controlSignForReadyMsg +
                            "</fReady>"
                        );
                        Console.WriteLine("resultStr : " + resultStr);
                    }
                }
                Console.WriteLine(JsonSerializer.Serialize(ReadyMessageIncomeList));
                Console.WriteLine(ReadyMessageIncomeList.Count);
                while (ReadyMessageIncomeList.Count == 1)
                {
                    Thread.Sleep(5);
                }
                Console.WriteLine(ReadyMessageIncomeList.Count);
            }
        }
        private ulong FindBiggestSyncNo()
        {
            ulong biggestSyncNo = 0;
            Dictionary<ulong, int> syncNoCount = new Dictionary<ulong, int>();
            foreach (var iE in NVG.NodeList.ToArray())
            {
                if (syncNoCount.ContainsKey(iE.Value.SyncNo) == false)
                {
                    syncNoCount.Add(iE.Value.SyncNo, 0);
                }
                syncNoCount[iE.Value.SyncNo]++;
            }
            foreach (var iE in syncNoCount)
            {
                if (iE.Key > biggestSyncNo)
                {
                    biggestSyncNo = iE.Key;
                }
            }
            return biggestSyncNo;
        }
        private void SendMyInfoToAllNodes()
        {
            NP.Info("Send My Node Full Info");
            // her 30 saniyede bir diğer node'ları kim olduğumu söylüyor.
            KeyValuePair<string, NVS.IpInfo>[]? tmpMainList = NGF.ValidatorList.ToArray();
            if (tmpMainList != null)
            {
                string myNodeDataText = "<node>" + JsonSerializer.Serialize(NVG.NodeList[NVG.Settings.Nodes.My.HexKey]) + "</node>";
                for (int i = 0; i < tmpMainList.Length; i++)
                {
                    if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                    {
                        NCH.SendMessageED(tmpMainList[i].Key, tmpMainList[i].Value, myNodeDataText);
                    }
                }
            }
        }
        private void AskInfoFromNode()
        {
            NP.Info("Ask Other Nodes Full Info");
            KeyValuePair<string, NVS.IpInfo>[]? tmpMainList = NGF.ValidatorList.ToArray();
            if (tmpMainList != null)
            {
                for (int i = 0; i < tmpMainList.Length; i++)
                {
                    bool weHaveNodeInfo = false;
                    if (string.Equals(tmpMainList[i].Key, NVG.Settings.Nodes.My.HexKey) == false)
                    {
                        if (NVG.NodeList.ContainsKey(tmpMainList[i].Key))
                        {
                            string tmpNodeHex = Notus.Toolbox.Network.IpAndPortToHex(tmpMainList[i].Value);
                            if (NVG.NodeList.ContainsKey(tmpNodeHex))
                            {
                                if (NVG.NodeList[tmpNodeHex].Begin > 0)
                                {
                                    weHaveNodeInfo = true;
                                }
                            }
                        }
                    }
                    if (weHaveNodeInfo == false)
                    {
                        ProcessIncomeData(NCH.SendMessageED(
                            tmpMainList[i].Key, tmpMainList[i].Value, "<rNode>1</rNode>"
                        ));
                    }
                }
            }
        }
        private void SyncListWithNode()
        {
            NP.Info("Node List Sync With Other Nodes");
            List<string> tmpRemoveKeyList = new List<string>();
            foreach (var iE in NGF.ValidatorList)
            {
                if (string.Equals(iE.Key, NVG.Settings.Nodes.My.HexKey) == false)
                {
                    string innerResponseStr = NCH.SendMessageED(
                        iE.Key, iE.Value,
                        "<nList>" + JsonSerializer.Serialize(NGF.ValidatorList) + "</nList>"
                    );
                    if (string.Equals(innerResponseStr.Trim(), "1"))
                    {
                        NGF.ValidatorList[iE.Key].Status = NVS.NodeStatus.Online;
                    }
                    else
                    {
                        tmpRemoveKeyList.Add(iE.Key);
                    }
                }
            }
            for (int i = 0; i < tmpRemoveKeyList.Count; i++)
            {
                NVH.RemoveFromValidatorList(tmpRemoveKeyList[i]);
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