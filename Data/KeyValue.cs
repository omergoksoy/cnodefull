using System.Collections.Concurrent;
using System.Text.Json;
using ND = Notus.Date;
using NNT = Notus.Network.Text;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Data
{
    public class ValueTimeStruct
    {
        public string Value { get; set; }
        public ulong Time { get; set; }
    }
    public class KeyValueSettings
    {
        public ulong MemoryLimitCount { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
    }

    public class KeyValue : IDisposable
    {
        private string DirPath = string.Empty;
        private KeyValueSettings ObjSettings = new KeyValueSettings();
        private bool TimerRunning = false;
        private Notus.Threads.Timer TimerObj = new();
        private Notus.Data.Sql SqlObj = new();
        private ConcurrentDictionary<string, ValueTimeStruct> ValueList = new();
        private ConcurrentQueue<NVS.KeyValueDataList> DeleteKeyList = new();
        private ConcurrentQueue<NVS.KeyValueDataList> SetValueList = new();
        public void Print()
        {
            Console.WriteLine(JsonSerializer.Serialize(ValueList));
        }
        private void AddToMemoryList(string key, string value)
        {
            //ulong exactTime = NVG.NOW.Int;
            ulong exactTime = ND.ToLong(DateTime.UtcNow);
            if (ValueList.ContainsKey(key) == false)
            {
                ValueList.TryAdd(key, new ValueTimeStruct()
                {
                    Value = value,
                    Time = exactTime
                });
            }
            else
            {
                ValueList[key].Time = exactTime;
                ValueList[key].Value = value;
            }
        }

        public KeyValue(KeyValueSettings settings)
        {
            ObjSettings.MemoryLimitCount = settings.MemoryLimitCount;
            if (ObjSettings.MemoryLimitCount > 10000)
            {
                ObjSettings.MemoryLimitCount = 10000;
            }

            ObjSettings.Path = settings.Path;
            ObjSettings.Name = settings.Name;

            DirPath = NNT.NetworkTypeText(NVG.Settings.Network) +
                System.IO.Path.DirectorySeparatorChar +
            NNT.NetworkLayerText(NVG.Settings.Layer) +
                System.IO.Path.DirectorySeparatorChar +
            ObjSettings.Path +
                System.IO.Path.DirectorySeparatorChar;
            Notus.IO.CreateDirectory(DirPath);

            string PoolName = DirPath + ObjSettings.Name + ".db";
            Console.WriteLine("PoolName : " + PoolName);
            DeleteKeyList.Clear();
            SetValueList.Clear();

            SqlObj = new Notus.Data.Sql();
            SqlObj.Open(PoolName);
            try
            {
                SqlObj.TableExist(
                    "key_value",
                    "CREATE TABLE key_value ( key TEXT NOT NULL UNIQUE, value TEXT NOT NULL );"
                );
            }
            catch { }
            TimerObj.Interval = 100;
            TimerObj.Start(() =>
            {
                SetValueToDbFunction();
            }, true);
        }
        public string Get(string key)
        {
            if (ValueList.ContainsKey(key) == true)
                return ValueList[key].Value;

            if (SqlObj == null)
                return string.Empty;

            string resultText = string.Empty;
            SqlObj.Select("key_value",
                (Dictionary<string, string> rList) =>
                {
                    foreach (KeyValuePair<string, string> entry in rList)
                    {
                        if (entry.Key == "value")
                        {
                            resultText = entry.Value;
                        }
                    }
                },
                new List<string>() { "key", "value" },
                new Dictionary<string, string>() { { "key", key } }
            );

            AddToMemoryList(key, resultText);
            return resultText;
        }
        public void Delete(string key)
        {
            if (ValueList.ContainsKey(key) == true)
                ValueList.TryRemove(key, out _);
            Task.Run(() =>
            {
                NVS.KeyValueDataList storeObj = new NVS.KeyValueDataList()
                {
                    Key = key,
                    Value = "",
                    Time = DateTime.UtcNow
                };

                File.WriteAllText(
                    FileName(key, storeObj.Time, false),
                    JsonSerializer.Serialize(storeObj)
                );

                DeleteKeyList.Enqueue(storeObj);
            });
        }
        public void Set(string key, string value)
        {
            AddToMemoryList(key, value);
            Task.Run(() =>
            {
                NVS.KeyValueDataList storeObj = new NVS.KeyValueDataList()
                {
                    Key = key,
                    Value = value,
                    Time = DateTime.UtcNow
                };
                File.WriteAllText(
                    FileName(key, storeObj.Time, true),
                    JsonSerializer.Serialize(storeObj)
                );
                SetValueList.Enqueue(storeObj);
            });
        }
        private void SetValueToDbFunction()
        {
            if (TimerRunning == false)
            {
                TimerRunning = true;
                TimerObj.SetInterval(DeleteKeyList.Count > 10 || SetValueList.Count > 10 ? 5 : 100);

                // burada silinecek kayıtlar kontrol ediliyor...
                if (DeleteKeyList.TryDequeue(out NVS.KeyValueDataList? deleteResult))
                {
                    if (deleteResult != null)
                    {
                        if (SqlObj != null)
                        {
                            bool deleteResultVal = SqlObj.Delete("key_value", new Dictionary<string, string>(){
                                { "key", deleteResult.Key }
                            });
                            if (deleteResultVal == true)
                            {
                                File.Delete(FileName(deleteResult.Key, deleteResult.Time, false));
                            }
                            else
                            {
                                DeleteKeyList.Enqueue(deleteResult);
                            }
                        }
                    }
                }

                //burada eklenecek veya güncellenecek kayıtlar kontrol ediliyor...
                if (SetValueList.TryDequeue(out NVS.KeyValueDataList? setResult))
                {
                    if (setResult != null)
                    {
                        if (SqlObj != null)
                        {
                            bool insertStatus = false, updateStatus = false, deletFile = true;
                            try
                            {
                                insertStatus = SqlObj.Insert("key_value", new Dictionary<string, string>(){
                                { "key", setResult.Key},
                                { "value", setResult.Value}
                            });
                            }
                            catch { }
                            if (insertStatus == false)
                            {
                                try
                                {
                                    updateStatus = SqlObj.Update("key_value",
                                        new Dictionary<string, string>(){
                                        { "value", setResult.Value }
                                        },
                                        new Dictionary<string, string>(){
                                        { "key",setResult.Key}
                                        }
                                    );
                                    if (updateStatus == false)
                                    {
                                        deletFile = false;
                                        SetValueList.Enqueue(setResult);
                                    }
                                }
                                catch { }
                            }

                            if (deletFile == true)
                            {
                                File.Delete(FileName(setResult.Key, setResult.Time, true));
                            }
                        }
                    }
                }
                TimerRunning = false;
            }
        }
        private string FileName(string key, DateTime exactTime, bool setFile)
        {
            string hexKey = System.Convert.ToHexString(System.Text.Encoding.ASCII.GetBytes(key));
            if (hexKey.Length > 30)
            {
                hexKey = hexKey.Substring(0, 30);
            }
            string dataLockFileName =
                DirPath +
                exactTime.ToString(NVC.DefaultDateTimeFormatText) +
                "_" +
                hexKey +
                "." + (setFile == true ? "set" : "del");
            return dataLockFileName;
        }
        ~KeyValue()
        {
            Dispose();
        }
        public void Dispose()
        {
            DeleteKeyList.Clear();
            SetValueList.Clear();

            if (SqlObj != null)
            {
                try
                {
                    SqlObj.Close();
                }
                catch { }

                try
                {
                    SqlObj.Dispose();
                }
                catch { }
            }
        }
    }
}

