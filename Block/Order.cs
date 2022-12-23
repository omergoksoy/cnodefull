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
        /*
        private ConcurrentDictionary<long, string> blockList = new ConcurrentDictionary<long, string>();
        public ConcurrentDictionary<long, string> List
        { 
            get { 
                return blockList; 
            } 
        }

        private Notus.Mempool? MP_OrderList;
        */

        public void Clear()
        {
            listObj.Clear();
            //MP_OrderList.Clear();
            //blockList.Clear();
        }
        public string Get(long blockRowNo)
        {
            return listObj.Get(blockRowNo.ToString());
        }
        /*
        public void Add(long rowNo)
        {
            return listObj.Get(blockRowNo.ToString());
            return string.Empty;
        }
        public long Add(string blockUid)
        {
            return long.MinValue;
        }
        */
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
            /*
            MP_OrderList = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings, Notus.Variable.Constant.StorageFolderName.Common) +
                Notus.Variable.Constant.MemoryPoolName["BlockOrderList"]
            );
            MP_OrderList.AsyncActive = false;
            MP_OrderList.AsyncActive = true;
            */
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
