using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Block
{
    public class TxStatus : IDisposable
    {
        private Notus.Data.KeyValue keyValue = new();
        public NVS.CryptoTransferStatus Status(string blockUid)
        {
            string rawDataStr = keyValue.Get(blockUid);
            try
            {
                NVS.CryptoTransferStatus tmpStatus = JsonSerializer.Deserialize<NVS.CryptoTransferStatus>(rawDataStr);
                return tmpStatus;
            }
            catch { }
            return new NVS.CryptoTransferStatus()
            {
                Code = NVE.BlockStatusCode.Unknown,
                RowNo = 0,
                UID = "",
                Text = "Unknown"
            };
        }
        public void Set(string? blockUid, NVS.CryptoTransferStatus statusCode)
        {
            keyValue.Set(blockUid, JsonSerializer.Serialize(statusCode));
        }
        public void Start()
        {
            keyValue.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 10000,
                Name = "tx_status"
            });
        }
        public TxStatus()
        {
        }
        ~TxStatus()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                keyValue.Dispose();
            }
            catch { }
        }
    }
}
