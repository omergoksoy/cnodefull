using Notus.Block;
using RocksDbSharp;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ND = Notus.Date;
using NI = Notus.IO;
using NNT = Notus.Network.Text;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Data
{
    public class KeyValue : IDisposable
    {
        private bool SettingsDefined = false;
        private string PoolName = string.Empty;
        private string DirPath = string.Empty;

        private NVS.KeyValueSettings ObjSettings = new();
        private RocksDb SqlObj;
        private ConcurrentDictionary<string, NVS.ValueTimeStruct> ValueList = new();

        public ConcurrentDictionary<string, NVS.ValueTimeStruct> List
        {
            get { return ValueList; }
        }

        public Dictionary<string, string> GetList()
        {
            Dictionary<string, string> resultList = new();
            foreach (var item in ValueList)
            {
                resultList.Add(item.Key, item.Value.Value);
            }
            return resultList;
        }
        public int Count()
        {
            return ValueList.Count;
        }
        private void AddToMemoryList(string key, string value)
        {
            if (ValueList.ContainsKey(key) == false)
            {
                bool errorCheck = ValueList.TryAdd(key, new NVS.ValueTimeStruct()
                {
                    Value = value,
                    Time = NVG.NOW.Int
                });
                if (errorCheck == false)
                {
                    Console.WriteLine(key + " - " + value);
                }
            }
            else
            {
                ValueList[key].Time = NVG.NOW.Int;
                ValueList[key].Value = value;
            }
        }
        public void SetSettings(NVS.KeyValueSettings settings)
        {
            ObjSettings.MemoryLimitCount = settings.MemoryLimitCount;
            if (settings.MemoryLimitCount > 0)
            {
                if (ObjSettings.MemoryLimitCount > 10000)
                {
                    ObjSettings.MemoryLimitCount = 10000;
                }
            }

            ObjSettings.LoadFromBeginning = settings.LoadFromBeginning;
            ObjSettings.ResetTable = settings.ResetTable;
            ObjSettings.Name = settings.Name;


            DirPath = NNT.NetworkTypeText(NVG.Settings.Network) +
                System.IO.Path.DirectorySeparatorChar +
            NNT.NetworkLayerText(NVG.Settings.Layer) +
                System.IO.Path.DirectorySeparatorChar +
            "db" +
                System.IO.Path.DirectorySeparatorChar;
            NI.CreateDirectory(DirPath);

            PoolName = DirPath + ObjSettings.Name;
            DbOptions options = new DbOptions().SetCreateIfMissing(true);
            SqlObj = RocksDb.Open(options, PoolName);

            if (ObjSettings.ResetTable == false)
            {
                if (ObjSettings.LoadFromBeginning == true)
                {
                    Each((string key, string value) =>
                    {
                        AddToMemoryList(key, value);
                    });
                }
            }
            SettingsDefined = true;
            if (ObjSettings.ResetTable == true)
            {
                Clear();
            }
        }
        public void Clear()
        {
            if (SettingsDefined == false)
                return;
            ValueList.Clear();
            return;
            NI.DeleteAllFileInsideDirectory(DirPath, "*");
        }
        public void Each(System.Action<string, string> incomeAction)
        {
            Iterator iterator = SqlObj.NewIterator().SeekToFirst();
            while (iterator.Valid())
            {
                incomeAction(iterator.StringKey(), iterator.StringValue());
                iterator.Next();
            }
        }
        public string Get(string? key)
        {
            if (ValueList.ContainsKey(key) == true)
            {
                string value = ValueList[key].Value;
                string? value2 = SqlObj.Get(key);
                Console.WriteLine("MemoryList -> " + value);
                Console.WriteLine("RockDB -> " + value2);
                Console.WriteLine("Check if Equal -> " + value == value2);
            }

            if (ValueList.ContainsKey(key) == true)
                return ValueList[key].Value;

            string? resultText = SqlObj.Get(key);
            return (resultText == null ? string.Empty : resultText);
        }
        public void Remove(string key)
        {
            Delete(key);
        }
        public void Delete(string key)
        {
            ValueList.TryRemove(key, out _);
            SqlObj.Remove(key);
        }
        public bool ContainsKey(string key)
        {
            if (ValueList.ContainsKey(key) == true)
            {
                return true;
            }
            string resultText = SqlObj.Get(key);
            if (resultText.Length > 0)
            {
                return true;
            }
            return false;
        }
        public void Set(string key, string value, bool onlyMemory)
        {
            if (onlyMemory == true)
            {
                AddToMemoryList(key, value);
            }
            else
            {
                Set(key, value);
            }
        }
        public void Set(string? key, string? value)
        {
            if (key == null)
                return;

            if (key.Length == 0)
                return;

            if (value == null)
                value = string.Empty;

            AddToMemoryList(key, value);
            SqlObj.Put(key, value);
        }
        public KeyValue()
        {
        }
        ~KeyValue()
        {
            Dispose();
        }
        public void Dispose()
        {
            ValueList.Clear();
            if (SqlObj != null)
            {
                try { SqlObj.Dispose(); } catch { }
            }
        }
    }
}

