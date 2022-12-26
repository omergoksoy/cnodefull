using Notus.Compression.TGZ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Block
{
    public class Sign : IDisposable
    {
        private long BiggestCountNumber = 0;
        private Notus.Data.KeyValue listObj = new Notus.Data.KeyValue();

        public void Clear()
        {
            listObj.Clear();
        }
        public string Get(long blockRowNo)
        {
            return listObj.Get(blockRowNo.ToString());
        }
        public Dictionary<long, string> List()
        {
            KeyValuePair<string, NVS.ValueTimeStruct>[]? tmpObj_DataList = listObj.List.ToArray();
            if (tmpObj_DataList == null)
            {
                return new Dictionary<long, string>();
            }
            Dictionary<long, string> resultList = new();
            for (long count = 0; count < tmpObj_DataList.Count(); count++)
            {
                long nextCount = count + 1;
                resultList.Add(nextCount, listObj.List[nextCount.ToString()].Value);
            }
            return resultList;
        }
        public void Add(long blockRowNo, string blockSign)
        {
            listObj.Set(blockRowNo.ToString(), blockSign);

            if (blockRowNo > BiggestCountNumber)
                BiggestCountNumber = blockRowNo;
        }
        public void Start()
        {
            listObj.SetSettings(new NVS.KeyValueSettings()
            {
                Path = "block_meta",
                MemoryLimitCount = 0,
                Name = Notus.Variable.Constant.MemoryPoolName["BlockSignList"]
            });
        }
        public Sign()
        {
        }
        ~Sign()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                if (listObj != null)
                {
                    listObj.Dispose();
                }
            }
            catch { }
        }
    }
}
