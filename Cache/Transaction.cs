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
namespace Notus.Cache
{
    public class Transaction : IDisposable
    {
        //transaction'ın listeye eklenme zamanı
        private SortedDictionary<ulong, string> txCheck = new SortedDictionary<ulong, string>();
        private Dictionary<string, ulong> txTime = new Dictionary<string, ulong>();

        // transaction'ın durumunu tutan değişken
        private Dictionary<string, NVE.BlockStatusCode> txStatus = new Dictionary<string, NVE.BlockStatusCode>();

        // transaction'ın hangi blokta olduğunu tutan liste
        //private Dictionary<string, ulong> txList = new Dictionary<string, ulong>();

        private Notus.Threads.Timer TimerObj;
        private Notus.Mempool MP_BlockPoolList;
        public NVE.BlockStatusCode Status(string blockUid)
        {
            return NVE.BlockStatusCode.Unknown;
        }
        public void Add(string blockUid, NVE.BlockStatusCode statusCode)
        {
            if (txTime.ContainsKey(blockUid))
            {
                // eski zamana ait kayıt siliniyor...
                txCheck.Remove(txTime[blockUid]);

                // yeni kontrol kaydı ekleniyo
                ulong tmpRightNow = NVG.NOW.Int;
                txCheck.Add(tmpRightNow, blockUid);

                // diğerleri güncelleniyor
                txTime[blockUid] = tmpRightNow;
                txStatus[blockUid] = statusCode;
            }
            else
            {
                ulong tmpRightNow = NVG.NOW.Int;

                txCheck.Add(tmpRightNow, blockUid);

                txTime.Add(blockUid, tmpRightNow);
                txStatus.Add(blockUid, statusCode);
            }
        }
        public void Set2(string blockUid, NVE.BlockStatusCode statusCode)
        {
            string bgInt = BigInteger.Parse("0" + blockUid, NumberStyles.AllowHexSpecifier).ToString();
            string lastHexStr = bgInt.Substring(bgInt.Length - 2);

            string dbName =
                Notus.IO.GetFolderName(NVG.Settings, Notus.Variable.Constant.StorageFolderName.TxList) +
                Notus.Variable.Constant.MemoryPoolName["TransactionList"] + lastHexStr + ".db";
            using (MP_BlockPoolList = new Notus.Mempool(dbName))
            {
                MP_BlockPoolList.AsyncActive = false;
            }
        }
        public void Set(string blockUid, NVE.BlockStatusCode statusCode)
        {
            txStatus.Add(blockUid, statusCode);
        }
        public Transaction()
        {
            TimerObj = new Notus.Threads.Timer(100);
            TimerObj.Start(() =>
            {
                if (txStatus.Count > 0)
                {

                }
            }, true);
        }
        ~Transaction()
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
