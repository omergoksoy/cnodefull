using NVClass = Notus.Variable.Class;
using System;
using System.Collections.Generic;
using System.Text.Json;
using NVC = Notus.Variable.Constant;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;

namespace Notus.Block
{
    public class Meta : IDisposable
    {
        private Notus.Data.KeyValue blockDb = new();

        private Notus.Data.KeyValue typeDb = new();

        private Notus.Data.KeyValue orderDb = new();
        private long BiggestCountNumber_ForOrder = 0;

        private Notus.Data.KeyValue statusDb = new();

        private Notus.Data.KeyValue signDb = new();
        private long BiggestCountNumber_ForSign = 0;

        private Notus.Data.KeyValue prevDb = new();
        private long BiggestCountNumber_ForPrev = 0;

        public void ClearTable(NVE.MetaDataDbTypeList tableType)
        {
            if (tableType == NVE.MetaDataDbTypeList.PreviouseList || tableType == NVE.MetaDataDbTypeList.All)
            {
                prevDb.Clear();
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
        public void Store(NVClass.BlockData blockData)
        {
            Sign(blockData.info.rowNo, blockData.sign);
            Prev(blockData.info.rowNo, blockData.prev);
            Order(blockData.info.rowNo, blockData.info.uID);
        }

        public void WriteBlock(NVClass.BlockData blockData)
        {
            blockDb.Set(blockData.info.uID, JsonSerializer.Serialize(blockData));
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
            {
                return new Dictionary<long, string>();
            }
            Dictionary<long, string> resultList = new();
            for (long count = 0; count < tmpObj_DataList.Count(); count++)
            {
                long nextCount = count + 1;
                resultList.Add(nextCount, orderDb.List[nextCount.ToString()].Value);
            }
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
        public void Order(long blockRowNo, string blockUid)
        {
            orderDb.Set(blockRowNo.ToString(), blockUid);

            if (blockRowNo > BiggestCountNumber_ForOrder)
                BiggestCountNumber_ForOrder = blockRowNo;
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
        public void Sign(long blockRowNo, string blockSign)
        {
            signDb.Set(blockRowNo.ToString(), blockSign);

            if (blockRowNo > BiggestCountNumber_ForSign)
                BiggestCountNumber_ForSign = blockRowNo;
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
        public void Prev(long blockRowNo, string blockPrev)
        {
            prevDb.Set(blockRowNo.ToString(), blockPrev);

            if (blockRowNo > BiggestCountNumber_ForPrev)
                BiggestCountNumber_ForPrev = blockRowNo;
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
        }
    }
}
