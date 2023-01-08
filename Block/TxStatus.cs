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
        public NVE.BlockStatusCode Status(string blockUid)
        {
            string rawDataStr = keyValue.Get(blockUid);
            try
            {
                NVE.BlockStatusCode tmpstatus = JsonSerializer.Deserialize<NVE.BlockStatusCode>(rawDataStr);
                return tmpstatus;
            }
            catch { }
            return NVE.BlockStatusCode.Unknown;
        }
        public void Set(string blockUid, NVE.BlockStatusCode statusCode)
        {
            keyValue.Set(blockUid, JsonSerializer.Serialize(statusCode));
        }
        public void Start() {
            keyValue.SetSettings(new NVS.KeyValueSettings()
            {
                LoadFromBeginning = true,
                ResetTable = false,
                Path = "block_meta",
                MemoryLimitCount = 1000,
                Name = "block_status"
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
