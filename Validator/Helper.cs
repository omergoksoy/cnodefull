﻿using System.Globalization;
using System.Text.Json;
using ND = Notus.Date;
using NP = Notus.Print;
using NGF = Notus.Variable.Globals.Functions;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
using NCH = Notus.Communication.Helper;

namespace Notus.Validator
{
    public static class Helper
    {
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
            if (blockTypeNo == 300)
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
        public static bool RightBlockValidator(Notus.Variable.Class.BlockData incomeBlock)
        {
            ulong queueTimePeriod = NVC.BlockListeningForPoolTime + NVC.BlockGeneratingTime + NVC.BlockDistributingTime;
            ulong blockTimeVal = ND.ToLong(incomeBlock.info.time);
            ulong blockGenarationTime = blockTimeVal - (blockTimeVal % queueTimePeriod);

            if (NVG.Settings.Nodes.Queue.ContainsKey(blockGenarationTime) == true)
            {
                string blockValidator = incomeBlock.validator.count.First().Key;
                if (string.Equals(blockValidator, NVG.Settings.Nodes.Queue[blockGenarationTime].Wallet))
                {
                    return true;
                }
            }
            return false;
        }
        public static void PreStartValidatorList()
        {
            NGF.ValidatorList.Clear();
            string tmpOfflineNodeListStr = string.Empty;
            string tmpNodeListStr = string.Empty;
            using (Notus.Mempool objMpNodeList = new Notus.Mempool("validator_list"))
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
        }
        public static void SetNodeOnline(string nodeHexKey)
        {
            NVG.NodeList[nodeHexKey].Status = NVS.NodeStatus.Online;
            NGF.ValidatorList[nodeHexKey].Status = NVS.NodeStatus.Online;
        }
        public static void SetNodeOffline(string nodeHexKey)
        {
            NVG.NodeList[nodeHexKey].Status = NVS.NodeStatus.Offline;
            NGF.ValidatorList[nodeHexKey].Status = NVS.NodeStatus.Offline;
        }
        public static void RemoveFromValidatorList(string nodeHexKey)
        {
            if (NVG.NodeList.ContainsKey(nodeHexKey) == true)
            {
                NVG.NodeList.TryRemove(nodeHexKey, out _);
            }
            if (NGF.ValidatorList.ContainsKey(nodeHexKey) == true)
            {
                using (Notus.Mempool objMpNodeList = new Notus.Mempool("validator_list"))
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
            NP.NodeCount();
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
                    using (Notus.Mempool objMpNodeList = new Notus.Mempool("validator_list"))
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
                            NVG.CurrentSyncNo.ToString() + NVC.CommonDelimeterChar +
                            NVG.Settings.Nodes.My.IP.Wallet + NVC.CommonDelimeterChar +
                            Notus.Wallet.ID.Sign(
                                selectedEarliestWalletId + NVC.CommonDelimeterChar +
                                NVG.CurrentSyncNo.ToString() + NVC.CommonDelimeterChar +
                                NVG.Settings.Nodes.My.IP.Wallet,
                                NVG.SessionPrivateKey
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
        public static void SetJoinTimeToNode(string nodeKey,ulong startingTime)
        {
            NVG.NodeList[nodeKey].JoinTime = ND.ToLong(
                ND.ToDateTime(startingTime).Subtract(
                    new TimeSpan(0, 1, 0)
                )
            );
        }
        public static void TellSyncNoToEarlistNode(string selectedEarliestWalletId)
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
                    Console.WriteLine("We Sended SyncNo To -> " + NVG.NodeList[nodeKey].IP.Wallet);
                }
                else
                {
                    Console.WriteLine("SyncNo Sending Error -> " + NVG.NodeList[nodeKey].IP.Wallet);
                }
            }
        }
    }
}
