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

namespace Notus.Block
{
    public class Order : IDisposable
    {
        private Notus.Data.KeyValue listObj = new Notus.Data.KeyValue();
        public void Clear()
        {
            listObj.Clear();
        }
        public string Get(long blockRowNo)
        {
            return listObj.Get(blockRowNo.ToString());
        }
        public void Each(System.Action<string, string> incomeAction, int UseThisNumberAsCountOrMiliSeconds = 1000, Notus.Variable.Enum.MempoolEachRecordLimitType UseThisNumberType = Notus.Variable.Enum.MempoolEachRecordLimitType.Count)
        {

        }
        public void Add(long blockRowNo, string blockUid)
        {
            listObj.Set(blockRowNo.ToString(), blockUid);
        }
        public void Start()
        {
            listObj.SetSettings(new Notus.Data.KeyValueSettings()
            {
                Path = "block_meta",
                MemoryLimitCount = 0,
                Name = Notus.Variable.Constant.MemoryPoolName["BlockOrderList"]
            });
        }
        public Order()
        {
        }
        ~Order()
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
