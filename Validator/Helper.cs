﻿using System.Globalization;
using System.Text.Json;
using NCH = Notus.Communication.Helper;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVD = Notus.Validator.Date;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVS = Notus.Variable.Struct;
namespace Notus.Validator
{
    public static class Helper
    {
        public static void DefineMyNodeInfo()
        {
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
                ChainId = NVG.Settings.Nodes.My.ChainId,
                PrivateKey = "",
                State = new NVS.NodeStateStruct()
                {
                    blockUid = "",
                    rowNo = 0,
                    sign = ""
                }
            }, true);
        }
        public static void GenerateNodeInfoListViaValidatorList()
        {
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
                        PublicKey = "",
                        ChainId = "",
                        PrivateKey = "",
                        State = new NVS.NodeStateStruct()
                        {
                            sign = "",
                            rowNo = 0,
                            blockUid = ""
                        }
                    }, false);
                }
            }
        }
        public static void PrepareValidatorList()
        {
            NVG.NodeList.Clear();
            NGF.ValidatorList.Clear();
            bool generateBaseValidatorList = false;
            using (Notus.Mempool objMpNodeList = new Notus.Mempool(NVC.MemoryPoolName["ValidatorList"]))
            {
                if (objMpNodeList.Get("address_list", string.Empty).Length == 0)
                {
                    generateBaseValidatorList = true;
                }
                objMpNodeList.Dispose();
            }
            if (generateBaseValidatorList == true)
            {
                foreach (Variable.Struct.IpInfo item in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
                {
                    NVH.AddToValidatorList(item.IpAddress, item.Port, true);
                }
            }

            string tmpOfflineNodeListStr = string.Empty;
            string tmpNodeListStr = string.Empty;
            using (Notus.Mempool objMpNodeList = new Notus.Mempool(NVC.MemoryPoolName["ValidatorList"]))
            {
                tmpOfflineNodeListStr = objMpNodeList.Get("offline_list", "");
                tmpNodeListStr = objMpNodeList.Get("address_list", "");
            }

            if (tmpOfflineNodeListStr.Length == 0 && tmpNodeListStr.Length == 0)
            {
                foreach (NVS.IpInfo defaultNodeInfo in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
                {
                    AddToValidatorList(defaultNodeInfo.IpAddress, defaultNodeInfo.Port);
                }
            }
            else
            {
                if (tmpOfflineNodeListStr.Length > 0)
                {
                    SortedDictionary<string, NVS.IpInfo>? tmpDbNodeList = JsonSerializer.Deserialize<SortedDictionary<string, NVS.IpInfo>>(tmpOfflineNodeListStr);
                    if (tmpDbNodeList != null)
                    {
                        foreach (var iE in tmpDbNodeList)
                        {
                            AddToValidatorList(iE.Value.IpAddress, iE.Value.Port);
                        }
                    }
                }
                if (tmpNodeListStr.Length > 0)
                {
                    SortedDictionary<string, NVS.IpInfo>? tmpDbNodeList = JsonSerializer.Deserialize<SortedDictionary<string, NVS.IpInfo>>(tmpNodeListStr);
                    if (tmpDbNodeList != null)
                    {
                        foreach (var iE in tmpDbNodeList)
                        {
                            AddToValidatorList(iE.Value.IpAddress, iE.Value.Port);
                        }
                    }
                }
            }

            DefineMyNodeInfo();
            NVH.AddToValidatorList(NVG.Settings.Nodes.My.IP.IpAddress, NVG.Settings.Nodes.My.IP.Port);
            GenerateNodeInfoListViaValidatorList();
            foreach (KeyValuePair<string, NVS.IpInfo> entry in NGF.ValidatorList)
            {
                NGF.ValidatorList[entry.Key].Status = NVS.NodeStatus.Unknown;

                if (string.Equals(entry.Key, NVG.Settings.Nodes.My.HexKey) == true)
                {
                    NGF.SetNodeOnline(entry.Key);
                }
            }
        }

        public static List<NVS.IpInfo> GiveMeNodeList()
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

        public static void CheckBlockAndEmptyCounter(int blockTypeNo)
        {
            if (blockTypeNo == NVE.BlockTypeList.EmptyBlock)
            {
                NVG.Settings.EmptyBlockCount++;
                NVG.Settings.OtherBlockCount = 0;
            }
            else
            {
                NVG.Settings.OtherBlockCount++;
                NVG.Settings.EmptyBlockCount = 0;
            }
        }
        public static string BlockValidator(Notus.Variable.Class.BlockData incomeBlock)
        {
            return incomeBlock.validator.count.First().Key;
        }
        public static bool RightBlockValidator(Notus.Variable.Class.BlockData incomeBlock, string sendingLocation)
        {
            ulong queueTimePeriod = NVD.Calculate();
            ulong blockTimeVal = ND.ToLong(incomeBlock.info.time);
            ulong blockGenarationTime = blockTimeVal - (blockTimeVal % queueTimePeriod);

            if (NVG.Settings.Nodes.Queue.ContainsKey(blockGenarationTime) == true)
            {
                string blockValidatorWalletId = BlockValidator(incomeBlock);
                if (string.Equals(blockValidatorWalletId, NVG.Settings.Nodes.Queue[blockGenarationTime].Wallet))
                {
                    return true;
                }
                else
                {
                    /*
                    Console.WriteLine("blockValidatorWalletId : " + blockValidatorWalletId);
                    Console.WriteLine("NVG.Settings.Nodes.Queue[blockGenarationTime].Wallet : " + NVG.Settings.Nodes.Queue[blockGenarationTime].Wallet);
                    */
                }
            }
            else
            {
                /*
                Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.Nodes.Queue));
                Console.WriteLine("blockGenarationTime : " + blockGenarationTime.ToString());
                Console.WriteLine("blockTimeVal : " + blockTimeVal.ToString());
                Console.WriteLine("if (NVG.Settings.Nodes.Queue.ContainsKey(blockGenarationTime) == FALSE -> FALSE)");
                */
                //ekleme noktası burası buradan kontrol edilecek
                if (Notus.Sync.Block.downloadDone == true)
                {

                }
            }
            return false;
        }
        public static void RemoveFromValidatorList(string nodeHexKey)
        {
            if (NVG.NodeList.ContainsKey(nodeHexKey) == true)
            {
                NVG.NodeList.TryRemove(nodeHexKey, out _);
            }
            if (NGF.ValidatorList.ContainsKey(nodeHexKey) == true)
            {
                using (Notus.Mempool objMpNodeList = new Notus.Mempool(NVC.MemoryPoolName["ValidatorList"]))
                {
                    objMpNodeList.AsyncActive = false;
                    SortedDictionary<string, NVS.IpInfo>? tmpDbNodeList = new SortedDictionary<string, NVS.IpInfo>();
                    string tmpOfflineNodeListStr = objMpNodeList.Get("offline_list", "");
                    if (tmpOfflineNodeListStr.Length > 0)
                    {
                        tmpDbNodeList = JsonSerializer.Deserialize<SortedDictionary<string, NVS.IpInfo>>(tmpOfflineNodeListStr);
                    }
                    else
                    {
                        tmpDbNodeList.Clear();
                    }

                    if (tmpDbNodeList != null)
                    {
                        if (tmpDbNodeList.ContainsKey(nodeHexKey) == false)
                        {
                            tmpDbNodeList.Add(nodeHexKey, new NVS.IpInfo()
                            {
                                IpAddress = "",
                                Port = 0,
                                Status = NVS.NodeStatus.Unknown
                            });
                        }
                        tmpDbNodeList[nodeHexKey].IpAddress = NGF.ValidatorList[nodeHexKey].IpAddress;
                        tmpDbNodeList[nodeHexKey].Port = NGF.ValidatorList[nodeHexKey].Port;
                        tmpDbNodeList[nodeHexKey].Status = NVS.NodeStatus.Unknown;
                        objMpNodeList.Set("offline_list", JsonSerializer.Serialize(tmpDbNodeList), true);
                    }
                    else
                    {
                        objMpNodeList.Set("offline_list", string.Empty, true);
                    }
                    objMpNodeList.Set("address_list", JsonSerializer.Serialize(NGF.ValidatorList), true);
                }
                NGF.ValidatorList.Remove(nodeHexKey);
            }
            NVG.OnlineNodeCount = NVG.NodeList.Count;
            NGF.ControlForShutDownNode();
        }
        public static void AddValidatorInfo(NVS.NodeQueueInfo nodeQueueInfo, bool structCameFromOwner)
        {
            if (NVG.NodeList.ContainsKey(nodeQueueInfo.HexKey))
            {
                NVG.NodeList[nodeQueueInfo.HexKey] = nodeQueueInfo;
            }
            else
            {
                NVG.NodeList.TryAdd(nodeQueueInfo.HexKey, nodeQueueInfo);
            }
            NVG.OnlineNodeCount = NVG.NodeList.Count;
        }
        public static void AddToValidatorList(string ipAddress, int portNo, bool storeToTable = true)
        {
            string tmpHexKeyStr = Notus.Toolbox.Network.IpAndPortToHex(ipAddress, portNo);
            if (NGF.ValidatorList.ContainsKey(tmpHexKeyStr) == false)
            {
                NGF.ValidatorList.Add(tmpHexKeyStr, new NVS.IpInfo()
                {
                    IpAddress = ipAddress,
                    Port = portNo,
                });
                if (storeToTable == true)
                {
                    using (Notus.Mempool objMpNodeList = new Notus.Mempool(NVC.MemoryPoolName["ValidatorList"]))
                    {
                        objMpNodeList.AsyncActive = false;
                        objMpNodeList.Set("address_list", JsonSerializer.Serialize(NGF.ValidatorList), true);
                    }
                }
                SortedDictionary<ulong, string> tmpNodeList = new SortedDictionary<ulong, string>();
                foreach (KeyValuePair<string, NVS.IpInfo> entry in NGF.ValidatorList)
                {
                    tmpNodeList.Add(
                        UInt64.Parse(entry.Key, NumberStyles.AllowHexSpecifier),
                        entry.Value.Status.ToString()
                    );
                }
                NGF.ValidatorListHash = new Notus.Hash().CommonHash("sha1", JsonSerializer.Serialize(tmpNodeList));
            }
            NVG.OnlineNodeCount = NVG.NodeList.Count;
        }
        public static void TellTheNodeWhoWaitingRoom(string selectedEarliestWalletId)
        {
            NP.Info("We Told The Node Whose In The Waiting Room");
            bool exitLoop = false;
            while (exitLoop == false)
            {
                foreach (var iEntry in NVG.NodeList)
                {
                    if (string.Equals(iEntry.Value.IP.Wallet, selectedEarliestWalletId) == true)
                    {
                        string tmpSyncNoStr = "<yourTurn>" +
                            NVG.CurrentSyncNo.ToString() + NVC.Delimeter +
                            NVG.Settings.Nodes.My.IP.Wallet + NVC.Delimeter +
                            Notus.Wallet.ID.Sign(
                                selectedEarliestWalletId + NVC.Delimeter +
                                NVG.CurrentSyncNo.ToString() + NVC.Delimeter +
                                NVG.Settings.Nodes.My.IP.Wallet,
                                NVG.Settings.Nodes.My.PrivateKey
                            ) +
                            "</yourTurn>";
                        string resultStr = NCH.SendMessageED(
                            iEntry.Key,
                            iEntry.Value.IP.IpAddress,
                            iEntry.Value.IP.Port,
                            tmpSyncNoStr
                        );
                        if (resultStr == "1")
                        {
                            exitLoop = true;
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                }
            }
        }
        public static void SetJoinTimeToNode(string nodeKey, ulong startingTime)
        {
            NVG.NodeList[nodeKey].JoinTime = ND.ToLong(
                ND.ToDateTime(startingTime).Subtract(
                    new TimeSpan(0, 1, 0)
                )
            );
        }
        public static void TellToNetworkNewNodeJoinTime(string selectedEarliestWalletId, ulong joinTime)
        {
            string tmpSyncNoStr = "<joinTime>" +
                selectedEarliestWalletId + NVC.Delimeter +
                joinTime.ToString() + NVC.Delimeter +
                NVG.Settings.Nodes.My.IP.Wallet + NVC.Delimeter +
                Notus.Wallet.ID.Sign(
                    selectedEarliestWalletId +
                        NVC.Delimeter +
                    joinTime.ToString() +
                        NVC.Delimeter +
                    NVG.Settings.Nodes.My.IP.Wallet,
                    NVG.Settings.Nodes.My.PrivateKey
                ) +
                "</joinTime>";
            List<string> tmpNodeList = new List<string>();
            string earlistNodeHexKeyStr = string.Empty;
            foreach (var iEntry in NVG.NodeList)
            {
                if (string.Equals(iEntry.Value.IP.Wallet, selectedEarliestWalletId) == true)
                {
                    earlistNodeHexKeyStr = iEntry.Key;
                    tmpNodeList.Add(iEntry.Key);
                }
                else
                {
                    if (string.Equals(iEntry.Value.IP.Wallet, NVG.Settings.Nodes.My.IP.Wallet) == false)
                    {
                        if (iEntry.Value.SyncNo == NVG.CurrentSyncNo)
                        {
                            tmpNodeList.Add(iEntry.Key);
                        }
                    }
                }
            }

            foreach (var nodeKey in tmpNodeList)
            {
                string resultStr = NCH.SendMessageED(
                    nodeKey,
                    NVG.NodeList[nodeKey].IP.IpAddress,
                    NVG.NodeList[nodeKey].IP.Port,
                    tmpSyncNoStr
                );
                if (resultStr == "1")
                {
                    NP.Basic("We Sended JoinTime -> " +
                        NVG.NodeList[nodeKey].IP.Wallet.Substring(0, 7) +
                        "..." +
                        NVG.NodeList[nodeKey].IP.Wallet.Substring(NVG.NodeList[nodeKey].IP.Wallet.Length - 7) +
                        " -> " +
                        joinTime.ToString()
                    );
                }
                else
                {
                    NP.Basic("JoinTime Sending Error -> " +
                        NVG.NodeList[nodeKey].IP.Wallet.Substring(0, 7) +
                        "..." +
                        NVG.NodeList[nodeKey].IP.Wallet.Substring(NVG.NodeList[nodeKey].IP.Wallet.Length - 7) +
                        " -> " +
                        joinTime.ToString()
                    );
                }
            }
            NVG.NodeList[earlistNodeHexKeyStr].JoinTime = ND.ToLong(
                ND.ToDateTime(joinTime).Subtract(new TimeSpan(0, 0, 0, 0, 50))
            );
            //Console.WriteLine("Node Join Time : " + NVG.NodeList[earlistNodeHexKeyStr].JoinTime.ToString());
            NVG.NodeList[earlistNodeHexKeyStr].SyncNo = NVG.CurrentSyncNo;
            NVG.ShowWhoseTurnOrNot = false;
        }
        public static void TellSyncNoToEarlistNode(string selectedEarliestWalletId)
        {
            string tmpSyncNoStr = "<syncNo>" +
                selectedEarliestWalletId + NVC.Delimeter +
                NVG.CurrentSyncNo.ToString() + NVC.Delimeter +
                NVG.Settings.Nodes.My.IP.Wallet + NVC.Delimeter +
                Notus.Wallet.ID.Sign(
                    selectedEarliestWalletId +
                        NVC.Delimeter +
                    NVG.CurrentSyncNo.ToString() +
                        NVC.Delimeter +
                    NVG.Settings.Nodes.My.IP.Wallet,
                    NVG.Settings.Nodes.My.PrivateKey
                ) +
                "</syncNo>";
            List<string> tmpNodeList = new List<string>();
            foreach (var iEntry in NVG.NodeList)
            {
                if (string.Equals(iEntry.Value.IP.Wallet, selectedEarliestWalletId) == true)
                {
                    tmpNodeList.Add(iEntry.Key);
                }
                else
                {
                    if (string.Equals(iEntry.Value.IP.Wallet, NVG.Settings.Nodes.My.IP.Wallet) == false)
                    {
                        if (iEntry.Value.SyncNo == NVG.CurrentSyncNo)
                        {
                            tmpNodeList.Add(iEntry.Key);
                        }
                    }
                }
            }

            foreach (var nodeKey in tmpNodeList)
            {
                string resultStr = NCH.SendMessageED(
                    nodeKey,
                    NVG.NodeList[nodeKey].IP.IpAddress,
                    NVG.NodeList[nodeKey].IP.Port,
                    tmpSyncNoStr
                );
                if (resultStr == "1")
                {
                    NP.Basic("We Sended SyncNo To -> " +
                        NVG.NodeList[nodeKey].IP.Wallet.Substring(0, 7) +
                        "..." +
                        NVG.NodeList[nodeKey].IP.Wallet.Substring(NVG.NodeList[nodeKey].IP.Wallet.Length - 7)
                    );
                }
                else
                {
                    NP.Basic("SyncNo Sending Error -> " +
                        NVG.NodeList[nodeKey].IP.Wallet.Substring(0, 7) +
                        "..." +
                        NVG.NodeList[nodeKey].IP.Wallet.Substring(NVG.NodeList[nodeKey].IP.Wallet.Length - 7)
                    );
                }
            }
        }
    }
}
