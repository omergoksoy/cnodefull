using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text.Json;
using NCH = Notus.Communication.Helper;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NH = Notus.Hash;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVClass = Notus.Variable.Class;
using NVD = Notus.Validator.Date;
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVR = Notus.Validator.Register;
using NVS = Notus.Variable.Struct;
namespace Notus.Validator
{
    public class Queue : IDisposable
    {
        private Notus.Threads.Timer DistributeTimerObj = new Notus.Threads.Timer();
        private bool DistributeTimerIsRunning = false;
        private ConcurrentQueue<NVS.BlockDistributeListStruct> DistributeErrorList = new();

        private Dictionary<string, bool> ReadyMessageIncomeList = new Dictionary<string, bool>();

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

        private string NodeListHash = "#";

        public System.Func<Notus.Variable.Class.BlockData, bool>? Func_NewBlockIncome = null;

        private string fixedRowNoLength(long blockRowNo)
        {
            return blockRowNo.ToString().PadLeft(15, '_');
        }
        public void Distrubute(long blockRowNo, int blockType, ulong currentNodeStartingTime)
        {
            ulong totalQueuePeriod = NVD.Calculate();
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

                    //omergoksoy
                    string messageText = "<block>" +
                            blockRowNo.ToString() + ":" + NVG.Settings.NodeWallet.WalletKey +
                        "</block>";
                    bool messageSended = NVG.Settings.PeerManager.Send(entry.Value.IP.Wallet, messageText);
                    if (messageSended == true)
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
                            entry.Value.IP.IpAddress + ":" + entry.Value.IP.Port.ToString()
                        );
                        DistributeErrorList.Enqueue(new NVS.BlockDistributeListStruct()
                        {
                            rowNo = blockRowNo,
                            peerId = entry.Value.IP.Wallet,
                            ipAddress = entry.Value.IP.IpAddress,
                            tryCount = 1,
                            message = messageText,
                            sended = DateTime.UtcNow
                        });
                    }
                }
            }
        }

        private DateTime CalculateStartingTime(ulong addExtraSeconds)
        {
            DateTime tmpNtpTime = NVG.NOW.Obj;
            const ulong secondPointConst = 1000;

            DateTime afterMiliSecondTime = tmpNtpTime.AddMilliseconds(
                secondPointConst + (secondPointConst - (ND.ToLong(tmpNtpTime) % secondPointConst))
            );
            double secondVal = addExtraSeconds + NVC.NodeStartingSync +
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
            if (CheckXmlTag(incomeData, "pQueue"))
            {
                NP.PrintQueue("if (CheckXmlTag(incomeData,pQueue))");
                return "ok";
            }

            if (CheckXmlTag(incomeData, "block"))
            {
                if (Notus.Toolbox.Text.CountChar(incomeData, '/') > 1)
                {
                    NP.Basic("IncomeText : " + incomeData);
                }
                //sync-control
                /*
                bu değişken true olunca, öncelikle diğer node'dan 
                blok alınması işlemini tamamla,
                blok alma işi bitince yeni blok oluşturulsun
                */
                NVG.Settings.WaitForGeneratedBlock = true;
                NP.Warning("Wait To Get New Block");

                string incomeDataStr = GetPureText(incomeData, "block");
                if (incomeDataStr.IndexOf(":") < 0)
                {
                    NVG.Settings.WaitForGeneratedBlock = false;
                    NP.Danger("Error Occurred While Getting New Block");
                    return "error-msg";
                }

                string[] tmpArr = incomeDataStr.Split(":");
                NP.Info("Income Block Row No -> " + tmpArr[0] + ", Validator => " + tmpArr[1]);
                /*
                if (string.Equals(tmpArr[1], "NODD5JuN455ApvunCh3HrLpxEEYWRC6eDHuFcFa"))
                {
                    Console.WriteLine("Other Node- Check From Here");
                    Console.WriteLine("Other Node- Check From Here");
                }
                */
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
                    NP.Danger("Error Occurred While Getting New Block");
                    return "fncResult-port-zero";
                }
                Variable.Class.BlockData? tmpBlockData =
                    Notus.Toolbox.Network.GetBlockFromNode(tmpIpAddress, tmpPortNo, tmpBlockNo, NVG.Settings);
                if (tmpBlockData == null)
                {
                    NVG.Settings.WaitForGeneratedBlock = false;
                    NP.Danger("Error Occurred While Getting New Block");
                    return "tmpError-true";
                }
                //NP.Info("<block> Downloaded from other validator");
                if (Func_NewBlockIncome != null)
                {
                    if (Func_NewBlockIncome(tmpBlockData) == true)
                    {
                        NVG.Settings.WaitForGeneratedBlock = false;
                        NP.Success("New Block Downloaded From Node");
                        return "done";
                    }
                }
                NVG.Settings.WaitForGeneratedBlock = false;
                NP.Danger("Error Occurred While Getting New Block");
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
                /*
                foreach (var iE in NVG.NodeList)
                {
                    if (string.Equals(iE.Value.IP.Wallet, NVG.Settings.Nodes.My.IP.Wallet) == true)
                    {
                        //NVG.NodeList[iE.Key].SyncNo= NVG.CurrentSyncNo;
                        NVH.SetJoinTimeToNode(iE.Key, NVG.CurrentSyncNo);
                    }
                    if (iE.Value.SyncNo == NVG.CurrentSyncNo)
                    {
                        NVH.SetJoinTimeToNode(iE.Key, NVG.CurrentSyncNo);
                    }
                }
                */
                NVG.NodeQueue.OrderCount = 1;
                NVG.NodeQueue.Begin = true;
                //NVH.SetJoinTimeToNode(NVG.Settings.Nodes.My.HexKey, NVG.CurrentSyncNo);

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
                                if (ReadyMessageIncomeList.ContainsKey(entry.Value.IP.Wallet) == false)
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

            if (CheckXmlTag(incomeData, "waitingRoomNodeReady"))
            {
                //NP.Basic("IncomeText : " + incomeData);
                incomeData = GetPureText(incomeData, "waitingRoomNodeReady");
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
                                if (NVR.ReadyMessageFromNode.ContainsKey(NVG.NodeList[entry.Key].IP.Wallet) == false)
                                {
                                    NVR.ReadyMessageFromNode.Add(NVG.NodeList[entry.Key].IP.Wallet, NVG.NOW.Int);
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
                //NP.Basic("IncomeText : " + incomeData);

                incomeData = GetPureText(incomeData, "syncNo");
                string[] tmpArr = incomeData.Split(":");
                if (tmpArr.Length > 3)
                {
                    string selectedEarliestWalletId = tmpArr[0];
                    ulong incomeSyncNo = ulong.Parse(tmpArr[1]);
                    string chooserWalletId = tmpArr[2];
                    string chooserSignStr = tmpArr[3];

                    string controlText =
                        selectedEarliestWalletId +
                            NVC.CommonDelimeterChar +
                        incomeSyncNo.ToString() +
                            NVC.CommonDelimeterChar +
                        chooserWalletId;
                    string chooserPublicKeyStr = string.Empty;
                    string earlistNodeKeyStr = string.Empty;
                    foreach (var iEntry in NVG.NodeList)
                    {
                        if (string.Equals(iEntry.Value.IP.Wallet, chooserWalletId) == true)
                        {
                            chooserPublicKeyStr = iEntry.Value.PublicKey;
                        }
                        if (string.Equals(iEntry.Value.IP.Wallet, selectedEarliestWalletId) == true)
                        {
                            earlistNodeKeyStr = iEntry.Key;
                        }

                    }

                    if (chooserPublicKeyStr.Length > 0 && earlistNodeKeyStr.Length > 0)
                    {
                        if (Notus.Wallet.ID.Verify(controlText, chooserSignStr, chooserPublicKeyStr) == true)
                        {
                            // sıradaki cüzdan, sıradaki node'a haber verecek node
                            if (NVR.NetworkSelectorList.ContainsKey(selectedEarliestWalletId) == false)
                            {
                                NVR.NetworkSelectorList.Add(selectedEarliestWalletId, chooserWalletId);
                            }

                            // kontrol-noktasi
                            NVG.NodeList[earlistNodeKeyStr].JoinTime = ulong.MaxValue;
                            // NVG.NodeList[earlistNodeKeyStr].SyncNo = incomeSyncNo;

                            NVG.CurrentSyncNo = incomeSyncNo;
                            NVG.NodeQueue.Starting = incomeSyncNo;
                            // NVG.NodeQueue.OrderCount = 1;
                            // NVG.NodeQueue.Begin = true;
                            return "1";
                        }
                    }
                }
                return "0";
            }

            if (CheckXmlTag(incomeData, "joinTime"))
            {
                incomeData = GetPureText(incomeData, "joinTime");
                string[] tmpArr = incomeData.Split(":");
                if (tmpArr.Length > 3)
                {
                    string selectedEarliestWalletId = tmpArr[0];
                    ulong joinTime = ulong.Parse(tmpArr[1]);
                    string chooserWalletId = tmpArr[2];
                    string chooserSignStr = tmpArr[3];

                    string controlText =
                        selectedEarliestWalletId +
                            NVC.CommonDelimeterChar +
                        joinTime.ToString() +
                            NVC.CommonDelimeterChar +
                        chooserWalletId;
                    string chooserPublicKeyStr = string.Empty;
                    string earlistNodeKeyStr = string.Empty;
                    foreach (var iEntry in NVG.NodeList)
                    {
                        if (string.Equals(iEntry.Value.IP.Wallet, chooserWalletId) == true)
                        {
                            chooserPublicKeyStr = iEntry.Value.PublicKey;
                        }
                        if (string.Equals(iEntry.Value.IP.Wallet, selectedEarliestWalletId) == true)
                        {
                            earlistNodeKeyStr = iEntry.Key;
                        }

                    }

                    if (chooserPublicKeyStr.Length > 0 && earlistNodeKeyStr.Length > 0)
                    {
                        if (Notus.Wallet.ID.Verify(controlText, chooserSignStr, chooserPublicKeyStr) == true)
                        {
                            //Console.WriteLine("Node JoinTime : " + joinTime.ToString());
                            NVR.ReadyMessageFromNode.Remove(selectedEarliestWalletId);
                            NVG.NodeList[earlistNodeKeyStr].SyncNo = NVG.CurrentSyncNo;
                            NVG.NodeList[earlistNodeKeyStr].JoinTime = ND.ToLong(
                                ND.ToDateTime(joinTime).Subtract(new TimeSpan(0, 0, 0, 0, 50))
                            );
                            if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, selectedEarliestWalletId))
                            {
                                if (NVG.OtherValidatorSelectedMe == true)
                                {
                                    NVG.OtherValidatorSelectedMe = false;
                                }
                            }
                            NP.Basic("Node Received Join Time : " + NVG.NodeList[earlistNodeKeyStr].JoinTime.ToString());
                            NVG.ShowWhoseTurnOrNot = false;
                            return "1";
                        }
                    }
                }
                return "0";
            }

            //bu komut ile bekleme odasındaki node ağa dahil ediliyor
            if (CheckXmlTag(incomeData, "yourTurn"))
            {
                //NP.Basic("IncomeText : " + incomeData);

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
                                if (Notus.Wallet.ID.Verify(
                                    controlText,
                                    chooserSignStr,
                                    iEntry.Value.PublicKey
                                ) == true)
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

            NP.Basic("Unknown IncomeText : " + incomeData);
            return "<err>1</err>";
        }
        private void NodeIsOnline(string nodeHexText)
        {
            if (NVG.NodeList.ContainsKey(nodeHexText) == true)
            {
                NVG.NodeList[nodeHexText].Status = NVS.NodeStatus.Online;
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
            ulong queueTimePeriod = NVD.Calculate();

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
                                bool addingResult = NVG.Settings.Nodes.Queue.TryAdd(tmpSyncNo, new NVS.NodeInfo()
                                {
                                    IpAddress = entry.Value.IP.IpAddress,
                                    Port = entry.Value.IP.Port,
                                    Wallet = entry.Value.IP.Wallet,
                                    GroupNo = NVG.GroupNo,
                                    //Client = new Dictionary<string, Communication.Sync.Socket.Client>()
                                });
                                if (addingResult == false)
                                {
                                    Console.WriteLine("addingResult : " + tmpSyncNo.ToString());
                                }

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
                //Console.WriteLine(entry.Value.JoinTime.ToString() + " - " + NVG.NOW.Int.ToString());
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
            //Console.WriteLine("Node-Siralama-Fonksiyon-111");
            //Console.WriteLine("---------------------------------------");
        }
        public SortedDictionary<BigInteger, string> MakeOrderToNode(ulong biggestSyncNo, string seedForQueue)
        {
            bool atTheBeginnig = false;
            if (seedForQueue.Length == 0)
            {
                seedForQueue = "beginning";
                atTheBeginnig = true;
            }
            SortedDictionary<BigInteger, string> resultList = new SortedDictionary<BigInteger, string>();
            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
            {
                bool nodeIsAvailable = false;
                if (atTheBeginnig == true)
                {
                    if (
                        entry.Value.Status == NVS.NodeStatus.Online
                            &&
                        entry.Value.SyncNo == biggestSyncNo
                    )
                    {
                        nodeIsAvailable = true;
                    }
                }
                else
                {
                    if (
                        entry.Value.Status == NVS.NodeStatus.Online
                            &&
                        entry.Value.SyncNo == biggestSyncNo
                            &&
                        NVG.NOW.Int > entry.Value.JoinTime
                    )
                    {
                        nodeIsAvailable = true;
                    }
                }
                if (nodeIsAvailable == true)
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

            /*
            if (resultList.Count > 2)
            {
                NP.Info("Node-Queue-Order");
                foreach (var iE in resultList)
                {
                    string numberText = iE.Key.ToString();
                    numberText = numberText.Substring(0, 5) + "..." + numberText.Substring(numberText.Length - 6);

                    string walletText = iE.Value.Substring(0, 6) + "..." + iE.Value.Substring(iE.Value.Length - 7);
                    if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, iE.Value))
                    {
                        NP.Success("My Turn     -> " + walletText + " -> " + numberText);
                    }
                    else
                    {
                        NP.Basic("Others Turn -> " + walletText + " -> " + numberText);
                    }
                }
                NP.Danger("---------------------------------------");
            }
            */
            return resultList;
        }
        public void PreStart()
        {
            if (NVG.Settings.LocalNode == true)
                return;
            if (NVG.Settings.GenesisCreated == true)
                return;

            NP.Info("Node Sync Starting");

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

            //burada hatalı blokların tekar gönderilmesi için oluşturulan timer aktive ediliyor
            DistributionErrorChecker();

            // önce node'ların içerisinde senkronizasyon bekleyen olmadığına emin ol
            bool firstHandShake = WaitUntilAvailable();

            ulong biggestSyncNo = FindBiggestSyncNo();
            if (biggestSyncNo > 0)
            {
                NP.Info("Sync No : " + biggestSyncNo.ToString());
            }
            else
            {
                NP.Info("First Synchronisation");
            }

            if (NVG.OtherValidatorSelectedMe == false)
            {
                //bu fonksyion ile amaç en çok sayıda olan sync no bulunacak
                StartingTimeAfterEnoughNode_Arrived = false;
                if (biggestSyncNo == 0)
                {
                    ulong extraSeconds = 0;
                    NP.NodeCount("if (biggestSyncNo == 0)");

                    // diğer node'un blok sayısını al
                    // alınan blok sayısına göre bekleme süresi ayarlanıyor
                    if (NVG.Settings.LocalNode == false)
                    {
                        if (NVG.Settings.GenesisCreated == false)
                        {
                            long minValue = long.MaxValue;
                            long maxValue = 0;

                            // yerel node'un max ve min değerleri kontrol ediliyor
                            if (minValue > NVG.Settings.LastBlock.info.rowNo)
                            {
                                minValue = NVG.Settings.LastBlock.info.rowNo;
                            }
                            if (NVG.Settings.LastBlock.info.rowNo > maxValue)
                            {
                                maxValue = NVG.Settings.LastBlock.info.rowNo;
                            }

                            foreach (KeyValuePair<string, NVS.NodeQueueInfo> iE in NVG.NodeList)
                            {
                                if (string.Equals(iE.Value.IP.Wallet, NVG.Settings.Nodes.My.IP.Wallet) == false)
                                {
                                    NVClass.BlockData? tmpBlockData = Notus.Toolbox.Network.GetLastBlock(
                                        Notus.Network.Node.MakeHttpListenerPath(
                                            iE.Value.IP.IpAddress,
                                            iE.Value.IP.Port
                                        ),
                                        NVG.Settings
                                    );
                                    if (tmpBlockData != null)
                                    {
                                        if (minValue > tmpBlockData.info.rowNo)
                                        {
                                            minValue = tmpBlockData.info.rowNo;
                                        }
                                        if (tmpBlockData.info.rowNo > maxValue)
                                        {
                                            maxValue = tmpBlockData.info.rowNo;
                                        }
                                    }
                                }
                            }
                            extraSeconds = (ulong)((maxValue - minValue) * 10);
                        }
                    }

                    //cüzdanların hashleri alınıp sıraya koyuluyor.
                    DateTime calculatedStartingTime = CalculateStartingTime(extraSeconds);
                    foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
                    {
                        if (entry.Value.Status == NVS.NodeStatus.Online && entry.Value.SyncNo == biggestSyncNo)
                        {
                            NVH.SetJoinTimeToNode(entry.Key, ND.ToLong(calculatedStartingTime));
                        }
                    }
                    SortedDictionary<BigInteger, string> tmpWalletList = MakeOrderToNode(biggestSyncNo, "");

                    //birinci sırada ki cüzdan seçiliyor...
                    string tmpFirstWalletId = tmpWalletList.First().Value;
                    if (string.Equals(tmpFirstWalletId, NVG.Settings.Nodes.My.IP.Wallet))
                    {
                        Thread.Sleep(5000);
                        StartingTimeAfterEnoughNode = calculatedStartingTime;
                        ulong syncStaringTime = ND.ToLong(StartingTimeAfterEnoughNode);
                        GenerateNodeQueue(biggestSyncNo, syncStaringTime, tmpWalletList);
                        //NVG.Settings.PeerManager.t
                        NP.Info("I'm Sending Starting (When) Time / Current : " +
                            ND.ShortTime(StartingTimeAfterEnoughNode) + " / " + ND.ShortTime(NVG.NOW.Obj)
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
                        NP.Info("I'm Waiting Starting (When) Time / Current : " +
                            ND.ShortTime(StartingTimeAfterEnoughNode) + " /  " + ND.ShortTime(NVG.NOW.Obj)
                        );
                        // eğer false ise senkronizasyon başlamamış demektir...
                        NVG.Settings.SyncStarted = false;
                    }

                    //Console.WriteLine("NVG.GroupNo : " + NVG.GroupNo.ToString());
                    NVG.Settings.PeerManager.RemoveAll();


                    // control-point-1453
                    // burada ilk yükleme işlemi yapılacak
                    ulong peerStaringTime = ND.ToLong(StartingTimeAfterEnoughNode);
                    for (int i = 0; i < 6; i++)
                    {
                        NVG.Settings.PeerManager.Now.TryAdd(
                            peerStaringTime,
                            new NVS.PeerDetailStruct()
                            {
                                IpAddress = NVG.Settings.Nodes.Queue[peerStaringTime].IpAddress,
                                WalletId = NVG.Settings.Nodes.Queue[peerStaringTime].Wallet
                            }
                        );
                        peerStaringTime = ND.AddMiliseconds(
                            peerStaringTime, NVD.Calculate()
                        );
                    }
                    NVG.Settings.PeerManager.StartAllPeers();
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
        public void ReOrderNodeQueue(ulong currentQueueTime, string queueSeedStr)
        {
            ulong biggestSyncNo = FindBiggestSyncNo();
            SortedDictionary<BigInteger, string> tmpWalletList = MakeOrderToNode(biggestSyncNo, queueSeedStr);
            GenerateNodeQueue(currentQueueTime, ND.AddMiliseconds(currentQueueTime, 1500), tmpWalletList);
            NVG.NodeQueue.OrderCount++;

            // control-point-1453
            // yeni listeyi next'e eşitle
            // next içindeki açıklacak soketleri aç
            // old içindeki eski bağlantıları kapat

            ulong peerStartingTime = ND.AddMiliseconds(currentQueueTime, 1500);
            for (int i = 0; i < 6; i++)
            {
                NVG.Settings.PeerManager.Next.TryAdd(peerStartingTime, new NVS.PeerDetailStruct()
                {
                    IpAddress = NVG.Settings.Nodes.Queue[peerStartingTime].IpAddress,
                    WalletId = NVG.Settings.Nodes.Queue[peerStartingTime].Wallet
                });
                peerStartingTime = ND.AddMiliseconds(peerStartingTime, NVD.Calculate());
            }

            NVG.Settings.PeerManager.StartAllPeers();
            NVG.Settings.PeerManager.StopOldPeers();
        }
        private bool WaitUntilAvailable()
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
            if (firstHandShake == false)
            {
                //Console.WriteLine("Queue.cs -> Line 1241");
                //Console.WriteLine(JsonSerializer.Serialize(syncNoCount, NVC.JsonSetting));
            }
            /*
            if (NVG.OtherValidatorSelectedMe == true)
            {
                Console.WriteLine("I'm Waiting In The Waiting Room -> Queue.cs");
            }
            */

            if (firstHandShake == true)
            {
                ulong nowUtcValue = NVG.NOW.Int;
                string controlSignForReadyMsg = Notus.Wallet.ID.Sign(
                    nowUtcValue.ToString() +
                        NVC.CommonDelimeterChar +
                    NVG.Settings.Nodes.My.IP.Wallet,
                    NVG.SessionPrivateKey
                );
                ReadyMessageIncomeList.Add(NVG.Settings.Nodes.My.IP.Wallet, true);
                foreach (var iE in NVG.NodeList)
                {
                    if (string.Equals(iE.Key, NVG.Settings.Nodes.My.HexKey) == false)
                    {
                        NCH.SendMessageED(iE.Key, iE.Value.IP.IpAddress, iE.Value.IP.Port,
                            "<fReady>" +
                                NVG.Settings.Nodes.My.IP.Wallet +
                                NVC.CommonDelimeterChar +
                                nowUtcValue.ToString() +
                                NVC.CommonDelimeterChar +
                                controlSignForReadyMsg +
                            "</fReady>"
                        );
                    }
                }
                while (ReadyMessageIncomeList.Count == 1)
                {
                    Thread.Sleep(5);
                }
            }
            return firstHandShake;
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
        public void DistributionErrorChecker()
        {
            //if(NVG.NOW.Int
            //NVG.NodeList[NVG.Settings.Nodes.My.IP.Wallet].JoinTime
            NP.Basic("Distribution Control Timer Has started");
            DistributeTimerObj.Start(100, () =>
            {
                if (DistributeTimerIsRunning == false)
                {
                    DistributeTimerIsRunning = true;
                    if (DistributeErrorList.TryDequeue(out NVS.BlockDistributeListStruct? testResult))
                    {
                        DateTime localTime = DateTime.UtcNow;
                        TimeSpan timeDiff = localTime - testResult.sended;
                        if (timeDiff.TotalSeconds > 1)
                        {
                            if (NVG.Settings.PeerManager.IsStarted(testResult.peerId) == false)
                            {
                                NVG.Settings.PeerManager.AddPeer(testResult.peerId, testResult.ipAddress);
                            }

                            if (NVG.Settings.PeerManager.Send(testResult.peerId, testResult.message) == true)
                            {
                                NP.Info(
                                "Distributed [ " + fixedRowNoLength(testResult.rowNo) + " ] To " +
                                    testResult.peerId
                                );
                            }
                            else
                            {
                                testResult.sended = localTime;
                                testResult.tryCount = testResult.tryCount + 1;
                                NP.Info(
                                "Distribution Error [ " + fixedRowNoLength(testResult.rowNo) + " ] To " +
                                    testResult.peerId + " -> " + testResult.tryCount.ToString()

                                );
                                DistributeErrorList.Enqueue(testResult);
                            }
                        }
                        else
                        {
                            DistributeErrorList.Enqueue(testResult);
                        }
                    }
                    DistributeTimerIsRunning = false;

                }  //if (DistributeTimerIsRunning == false)
            }, true);  //TimerObj.Start(() =>
        }

        public Queue()
        {
        }
        ~Queue()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (DistributeTimerObj != null)
            {
                try
                {
                    DistributeTimerObj.Dispose();
                }
                catch { }
            }
        }
    }
}
//bitiş noktası 1400.satır