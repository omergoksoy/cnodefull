/*
 

bu kitaplık ile amaç uid tiplerini bir DB'de saklamak idi
şimdilik beklemeye alındı


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
    public class UidTypeList : IDisposable
    {
        private Notus.Data.KeyValue keyValue = new();
        public NVE.UidTypeCode Type(string blockUid)
        {
            string rawDataStr = keyValue.Get(blockUid);
            try
            {
                NVE.UidTypeCode tmpstatus = JsonSerializer.Deserialize<NVE.UidTypeCode>(rawDataStr);
                return tmpstatus;
            }
            catch { }
            return NVE.UidTypeCode.Unknown;
        }
        public void Add(string blockUid, NVE.UidTypeCode typeCode)
        {
            keyValue.Set(blockUid, JsonSerializer.Serialize(typeCode));
        }
        public void Start() {
            keyValue.SetSettings(new NVS.KeyValueSettings()
            {
                LoadFromBeginning = true,
                ResetTable = false,
                Path = "block_meta",
                MemoryLimitCount = 1000,
                Name = "uid_type_list"
            });
        }
        public UidTypeList()
        {
        }
        ~UidTypeList()
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
*/