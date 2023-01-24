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
        private Notus.Data.KeyValue statusDb = new();

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
                MemoryLimitCount = 10000,
                Name = "tx_status"
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
