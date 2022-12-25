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
    public class Order : IDisposable
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
                //Console.WriteLine(count.ToString());
                //Console.WriteLine(listObj.List[count.ToString()].Value);
                //Console.ReadLine();
                long nextCount = count + 1;
                resultList.Add(nextCount, listObj.List[nextCount.ToString()].Value);
            }
            return resultList;
            /*
            foreach (var item in tmpObj_DataList)
            {

            }
            var tmpObj_DataList = listObj.List.ToArray();

            listObj.List
            return listObj.List;
            */
        }
        /*
        public void Each(System.Action<string, string> incomeAction, int UseThisNumberAsCountOrMiliSeconds = 1000, Notus.Variable.Enum.MempoolEachRecordLimitType UseThisNumberType = Notus.Variable.Enum.MempoolEachRecordLimitType.Count)
        {
            //Console.WriteLine("*******************************");
            //listObj.Print();
            //Console.WriteLine("*******************************");
            NP.ReadLine();

            listObj.Each((string BlockOrderNo, string blockUid) =>
            {
                incomeAction(BlockOrderNo, blockUid);
                //Console.WriteLine(BlockOrderNo.ToString() + " - " + blockUid);
            },int.MaxValue);
            //Console.WriteLine("Burada Each fonksiyonu çalıştırılmalı");
            //NP.ReadLine();
        }
        */
        public void Add(long blockRowNo, string blockUid)
        {
            listObj.Set(blockRowNo.ToString(), blockUid);

            if (blockRowNo > BiggestCountNumber)
                BiggestCountNumber = blockRowNo;
        }
        public void Start()
        {
            listObj.SetSettings(new NVS.KeyValueSettings()
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
