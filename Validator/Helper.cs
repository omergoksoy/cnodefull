using System.Globalization;
using System.Text.Json;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;

namespace Notus.Validator
{
    public static class Helper
    {
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
        }
    }
}
