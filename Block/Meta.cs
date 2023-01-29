using ND = Notus.Date;
using NVD = Notus.Validator.Date;
using System;
using System.Collections.Generic;
using System.Text.Json;
using NP = Notus.Print;
using NT = Notus.Threads;
using NTN = Notus.Toolbox.Network;
using NVC = Notus.Variable.Constant;
using NVH = Notus.Validator.Helper;
using NVClass = Notus.Variable.Class;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Block
{
    public class Meta : IDisposable
    {
        private bool SubTimerIsRunning = false;
        private NT.Timer SubTimer = new NT.Timer();
        private Notus.Data.KeyValue validatorDb = new();
        
        private Notus.Data.KeyValue blockDb = new();

        private Notus.Data.KeyValue typeDb = new();

        private Notus.Data.KeyValue orderDb = new();
        private long BiggestCountNumber_ForOrder = 0;

        private Notus.Data.KeyValue statusDb = new();

        private Notus.Data.KeyValue signDb = new();
        private long BiggestCountNumber_ForSign = 0;

        private Notus.Data.KeyValue prevDb = new();
        private long BiggestCountNumber_ForPrev = 0;

        public void Remove(string dbKey, NVE.MetaDataDbTypeList tableType)
        {
            if (tableType == NVE.MetaDataDbTypeList.ValidatorOrderList || tableType == NVE.MetaDataDbTypeList.All)
            {
                validatorDb.Remove(dbKey);
            }
            if (tableType == NVE.MetaDataDbTypeList.PreviouseList || tableType == NVE.MetaDataDbTypeList.All)
            {
                prevDb.Remove(dbKey);
            }
            if (tableType == NVE.MetaDataDbTypeList.SignList || tableType == NVE.MetaDataDbTypeList.All)
            {
                signDb.Remove(dbKey);
            }
            if (tableType == NVE.MetaDataDbTypeList.StatusList || tableType == NVE.MetaDataDbTypeList.All)
            {
                statusDb.Remove(dbKey);
            }
            if (tableType == NVE.MetaDataDbTypeList.OrderList || tableType == NVE.MetaDataDbTypeList.All)
            {
                orderDb.Remove(dbKey);
            }
            if (tableType == NVE.MetaDataDbTypeList.TypeList || tableType == NVE.MetaDataDbTypeList.All)
            {
                typeDb.Remove(dbKey);
            }
            if (tableType == NVE.MetaDataDbTypeList.BlockDataList || tableType == NVE.MetaDataDbTypeList.All)
            {
                blockDb.Remove(dbKey);
            }
        }
        public void ClearTable(NVE.MetaDataDbTypeList tableType)
        {
            if (tableType == NVE.MetaDataDbTypeList.PreviouseList || tableType == NVE.MetaDataDbTypeList.All)
            {
                prevDb.Clear();
            }
            if (tableType == NVE.MetaDataDbTypeList.ValidatorOrderList || tableType == NVE.MetaDataDbTypeList.All)
            {
                validatorDb.Clear();
            }
            if (tableType == NVE.MetaDataDbTypeList.SignList || tableType == NVE.MetaDataDbTypeList.All)
            {
                signDb.Clear();
            }
            if (tableType == NVE.MetaDataDbTypeList.StatusList || tableType == NVE.MetaDataDbTypeList.All)
            {
                statusDb.Clear();
            }
            if (tableType == NVE.MetaDataDbTypeList.OrderList || tableType == NVE.MetaDataDbTypeList.All)
            {
                orderDb.Clear();
            }
            if (tableType == NVE.MetaDataDbTypeList.TypeList || tableType == NVE.MetaDataDbTypeList.All)
            {
                typeDb.Clear();
            }
            if (tableType == NVE.MetaDataDbTypeList.BlockDataList || tableType == NVE.MetaDataDbTypeList.All)
            {
                blockDb.Clear();
            }
        }

        private void CheckBlockValidator(NVClass.BlockData blockData)
        {
            ulong queueTimePeriod = NVD.Calculate();
            ulong blockTimeVal = ND.ToLong(blockData.info.time);
            ulong blockGenarationTime = blockTimeVal - (blockTimeVal % queueTimePeriod);
            string validatorWalletId=Validator(blockGenarationTime);
            if (validatorWalletId.Length == 0)
            {
                Console.WriteLine("Validasyon Yapilamadi");
            }
            else
            {
                string blockValidaotrWalletId=blockData.validator.count.First().Key;
                if (string.Equals(blockValidaotrWalletId, validatorWalletId))
                {
                    Console.WriteLine("Dogru kisi tarafindan uretilen blok");
                }
                else
                {
                    Console.WriteLine("HATALI kisi tarafindan uretilen blok - DEGISTIR");
                }
            }
        }
        public void WriteBlock(NVClass.BlockData blockData, string senderLocation)
        {
            CheckBlockValidator(blockData);
            //NVH.RightBlockValidator(blockData, "Block Meta - WriteBlock");
            //NVG.BlockMeta.Validator(peerStaringTime, NVG.Settings.Nodes.Queue[peerStaringTime].Wallet);
            Console.WriteLine("Saving Block Data -> " + blockData.info.rowNo.ToString() + " - [ " + senderLocation + " ]");
            blockDb.Set(blockData.info.uID, JsonSerializer.Serialize(blockData));

            signDb.Set(blockData.info.rowNo.ToString(), blockData.sign);
            if (blockData.info.rowNo > BiggestCountNumber_ForSign)
                BiggestCountNumber_ForSign = blockData.info.rowNo;

            prevDb.Set(blockData.info.rowNo.ToString(), blockData.prev);
            if (blockData.info.rowNo > BiggestCountNumber_ForPrev)
                BiggestCountNumber_ForPrev = blockData.info.rowNo;

            orderDb.Set(blockData.info.rowNo.ToString(), blockData.info.uID);
            if (blockData.info.rowNo > BiggestCountNumber_ForOrder)
                BiggestCountNumber_ForOrder = blockData.info.rowNo;
        }
        public NVClass.BlockData? ReadBlock(string blockUid)
        {
            string? blockDataText = blockDb.Get(blockUid);
            if (blockDataText == null)
                return null;

            if (blockDataText.Length == 0)
                return null;
            try
            {
                NVClass.BlockData? tmpBlockData = JsonSerializer.Deserialize<NVClass.BlockData>(blockDataText);
                return tmpBlockData;
            }
            catch { }

            return null;
        }
        public NVClass.BlockData? ReadBlock(long blockRowNo)
        {
            string blockUid = Order(blockRowNo);
            return ReadBlock(blockUid);
        }

        public NVE.UidTypeList Type(string Uid)
        {
            string tmpResult = typeDb.Get(Uid.ToString());
            if (tmpResult == null)
                return NVE.UidTypeList.Unknown;

            if (tmpResult.Length == 0)
                return NVE.UidTypeList.Unknown;

            NVE.UidTypeList tmpStatus = NVE.UidTypeList.Unknown;
            try
            {
                tmpStatus = JsonSerializer.Deserialize<NVE.UidTypeList>(tmpResult);
            }
            catch { }
            return tmpStatus;
        }
        public void Type(string Uid, NVE.UidTypeList typeData)
        {
            orderDb.Set(Uid, typeData.ToString());
        }

        public Dictionary<long, string> Order()
        {
            KeyValuePair<string, NVS.ValueTimeStruct>[]? tmpObj_DataList = orderDb.List.ToArray();
            if (tmpObj_DataList == null)
                return new Dictionary<long, string>();

            Dictionary<long, string> resultList = new();
            for (long count = 0; count < tmpObj_DataList.Count(); count++)
            {
                long nextCount = count + 1;
                string orderListKey = nextCount.ToString();
                if (orderDb.List.ContainsKey(orderListKey) == false)
                {
                    NP.Warning(NVG.Settings, "Block Missing: " + orderListKey);
                    bool exitFromInnerWhileLoop = false;
                    while (exitFromInnerWhileLoop == false)
                    {
                        foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
                        {
                            bool getFromNode = (entry.Value.Status == NVS.NodeStatus.Online ? true : false);
                            getFromNode = true;

                            if (string.Equals(entry.Value.IP.Wallet, NVG.Settings.NodeWallet.WalletKey) == true)
                                getFromNode = false;

                            if (getFromNode == true)
                            {
                                NVClass.BlockData? tmpInnerBlockData = NTN.GetBlockFromNode(
                                    entry.Value.IP.IpAddress,
                                    entry.Value.IP.Port,
                                    nextCount,
                                    NVG.Settings
                                );
                                if (tmpInnerBlockData != null)
                                {
                                    NP.Basic(NVG.Settings, "Added Block : " + tmpInnerBlockData.info.uID);
                                    NVG.BlockMeta.WriteBlock(tmpInnerBlockData, "Meta -> Line -> 183");
                                    exitFromInnerWhileLoop = true;
                                }
                                else
                                {
                                    Console.WriteLine("Block Does Not Get From Node");
                                }
                            }
                        }
                    }

                    Console.WriteLine("get block from other nodes");
                    Console.ReadLine();
                }
                resultList.Add(nextCount, orderDb.List[orderListKey].Value);
            }
            //Console.WriteLine(JsonSerializer.Serialize(resultList));
            //Console.ReadLine();
            return resultList;
        }
        public string Order(long blockRowNo)
        {
            string tmpResult = orderDb.Get(blockRowNo.ToString());
            if (tmpResult == null)
                return string.Empty;

            if (tmpResult.Length == 0)
                return string.Empty;

            return tmpResult;

        }

        public string Sign(long blockRowNo)
        {
            string tmpResult = signDb.Get(blockRowNo.ToString());
            if (tmpResult == null)
                return string.Empty;

            if (tmpResult.Length == 0)
                return string.Empty;

            return tmpResult;
        }
        public string Validator(ulong blockTime)
        {
            string tmpResult = validatorDb.Get(blockTime.ToString());
            if (tmpResult == null)
                return string.Empty;

            if (tmpResult.Length == 0)
                return string.Empty;

            return tmpResult;
        }
        public void Validator(ulong blockTime,string validatorWalletId)
        {
            Console.WriteLine("Validator -> " + blockTime.ToString() + " : " + validatorWalletId);
            validatorDb.Set(blockTime.ToString(), validatorWalletId);
        }

        public string Prev(long blockRowNo)
        {
            string tmpResult = prevDb.Get(blockRowNo.ToString());

            if (tmpResult == null)
                return string.Empty;

            if (tmpResult.Length == 0)
                return string.Empty;

            return tmpResult;
        }

        public NVS.CryptoTransferStatus Status(string blockUid)
        {
            string rawDataStr = statusDb.Get(blockUid);
            if (rawDataStr == null)
            {
                if (rawDataStr.Length > 0)
                {
                    try
                    {
                        NVS.CryptoTransferStatus tmpStatus = JsonSerializer.Deserialize<NVS.CryptoTransferStatus>(rawDataStr);
                        return tmpStatus;
                    }
                    catch { }
                }
            }
            return new NVS.CryptoTransferStatus()
            {
                Code = NVE.BlockStatusCode.Unknown,
                RowNo = 0,
                UID = "",
                Text = "Unknown"
            };
        }
        public void Status(string? blockUid, NVS.CryptoTransferStatus statusCode)
        {
            statusDb.Set(blockUid, JsonSerializer.Serialize(statusCode));
        }

        public void StartBlockSyncTimer()
        {
            SubTimer.Start(3000,() =>
            {
                if (SubTimerIsRunning == false)
                {
                    SubTimerIsRunning = true;

                    SubTimerIsRunning = false;
                }
            });
        }
        public void Start()
        {
            statusDb.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 0,
                Name = Notus.Variable.Constant.MemoryPoolName["TransactionStatus"]
            });

            prevDb.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 0,
                Name = Notus.Variable.Constant.MemoryPoolName["BlockPrevList"]
            });

            signDb.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 0,
                Name = Notus.Variable.Constant.MemoryPoolName["BlockSignList"]
            });

            orderDb.SetSettings(new NVS.KeyValueSettings()
            {
                LoadFromBeginning = true,
                MemoryLimitCount = 0,
                Name = Notus.Variable.Constant.MemoryPoolName["BlockOrderList"]
            });

            typeDb.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 0,
                Name = Notus.Variable.Constant.MemoryPoolName["UidTypeList"]
            });

            blockDb.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 0,
                Name = Notus.Variable.Constant.MemoryPoolName["BlockData"]
            });

            validatorDb.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 0,
                Name = Notus.Variable.Constant.MemoryPoolName["ValidatorOrderList"]
            });
        }
        public Meta()
        {
        }
        ~Meta()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                SubTimer.Dispose();
            }
            catch { }

            try
            {
                orderDb.Dispose();
            }
            catch { }

            try
            {
                typeDb.Dispose();
            }
            catch { }

            try
            {
                blockDb.Dispose();
            }
            catch { }


            try
            {
                prevDb.Dispose();
            }
            catch { }

            try
            {
                signDb.Dispose();
            }
            catch { }

            try
            {
                statusDb.Dispose();
            }
            catch { }
        }
    }
}
