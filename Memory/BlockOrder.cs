/*
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NP = Notus.Print;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
namespace Notus.Memory
{
    public class BlockOrder : IDisposable
    {
        // buradaki sayı 1 milyonu geçince gruplandırma işlemi yapalım
        // block row no ve block uid
        private ConcurrentDictionary<long, string> blockList = new ConcurrentDictionary<long, string>();
        public ConcurrentDictionary<long, string> List
        {
            get { return blockList; }
        }
        private Notus.Threads.Timer TimerObj = new Notus.Threads.Timer();
        public void Clear()
        {
            blockList.Clear();
        }
        public long Count()
        {
            return blockList.Count;
        }
        public string Get(long blockRowNo)
        {
            if (blockList.ContainsKey(blockRowNo) == true)
            {
                return blockList[blockRowNo];
            }
            return string.Empty;
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
        }
        public BlockOrder()
        {
            TimerObj.Start(100, () =>
            {
                if (blockList.Count > 0)
                {

                }
            }, true);
        }
        ~BlockOrder()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                if (TimerObj != null)
                {
                    TimerObj.Dispose();
                }
            }
            catch (Exception err)
            {
            }
        }
    }
}
*/