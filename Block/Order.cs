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
        private ConcurrentDictionary<long, string> blockList = new ConcurrentDictionary<long, string>();
        public ConcurrentDictionary<long, string> List
        { 
            get { 
                return blockList; 
            } 
        }

        private Notus.Mempool? MP_OrderList;
        public void Clear()
        {
            MP_OrderList.Clear();
            blockList.Clear();
        }
        public string Add(long rowNo)
        {
            return string.Empty;
        }
        public long Add(string blockUid)
        {
            return long.MinValue;
        }
        public void Add(long blockRowNo, string blockUid)
        {
            if (blockList.ContainsKey(blockRowNo) == false)
            {
                blockList.TryAdd(blockRowNo, blockUid);
            }
            else
            {
                blockList[blockRowNo] = blockUid;
            }

            MP_OrderList.Set(blockRowNo.ToString(), blockUid, true);
        }
        public Order()
        {
            MP_OrderList = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings, Notus.Variable.Constant.StorageFolderName.Common) +
                Notus.Variable.Constant.MemoryPoolName["BlockOrderList"]
            );
            MP_OrderList.AsyncActive = false;
        }
        ~Order()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                MP_OrderList.Dispose();
            }
            catch
            {
            }
        }
    }
}
