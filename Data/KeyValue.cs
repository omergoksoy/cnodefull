using System.Collections.Concurrent;
using System.Text.Json;
using ND = Notus.Date;
using NNT = Notus.Network.Text;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Data
{
    public class KeyValue : IDisposable
    {
        private bool SettingsDefined = false;
        private string PoolName = string.Empty;
        private string TempPath = string.Empty;
        private string DirPath = string.Empty;
        private NVS.KeyValueSettings ObjSettings = new();
        private bool TimerRunning = false;
        private Notus.Threads.Timer TimerObj = new();
        private Notus.Data.Sql SqlObj = new();
        private ConcurrentDictionary<string, NVS.ValueTimeStruct> ValueList = new();
        private ConcurrentQueue<NVS.KeyValueDataList> DeleteKeyList = new();
        private ConcurrentQueue<NVS.KeyValueDataList> SetValueList = new();
        public ConcurrentDictionary<string, NVS.ValueTimeStruct> List
        {
            get { return ValueList; }
        }

        public void Print()
        {
            //Console.WriteLine(JsonSerializer.Serialize(ValueList));
            File.WriteAllText("deneme.cache",JsonSerializer.Serialize(ValueList));
        }
        private void AddToMemoryList(string key, string value)
        {
            //ulong exactTime = NVG.NOW.Int;
            ulong exactTime = ND.ToLong(DateTime.UtcNow);
            if (ValueList.ContainsKey(key) == false)
            {
                ValueList.TryAdd(key, new NVS.ValueTimeStruct()
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

            ObjSettings.Path = settings.Path;
            ObjSettings.Name = settings.Name;

            DirPath = NNT.NetworkTypeText(NVG.Settings.Network) +
                System.IO.Path.DirectorySeparatorChar +
            NNT.NetworkLayerText(NVG.Settings.Layer) +
                System.IO.Path.DirectorySeparatorChar +
            ObjSettings.Path +
                System.IO.Path.DirectorySeparatorChar;

            TempPath = DirPath + ObjSettings.Name + " _temp" + System.IO.Path.DirectorySeparatorChar;

            Notus.IO.CreateDirectory(DirPath);
            Notus.IO.CreateDirectory(TempPath);

            PoolName = DirPath + ObjSettings.Name + ".db";
            DeleteKeyList.Clear();
            SetValueList.Clear();

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
            SettingsDefined = true;
        }
        public KeyValue()
        {
        }
        public KeyValue(NVS.KeyValueSettings settings)
        {
            SetSettings(settings);
        }
        public void Clear()
        {
            if (SettingsDefined == false)
                return;

            DeleteKeyList.Clear();
            SetValueList.Clear();
            Thread.Sleep(100);

            ValueList.Clear();
            try
            {
                SqlObj.Clear(PoolName);
            }
            catch { }

            Notus.IO.DeleteAllFileInsideDirectory(TempPath, "set");
            Notus.IO.DeleteAllFileInsideDirectory(TempPath, "del");
        }
        public void Each(System.Action<string, string> incomeAction, 
            int UseThisNumberAsCountOrMiliSeconds = 1000, 
            Notus.Variable.Enum.MempoolEachRecordLimitType UseThisNumberType = Notus.Variable.Enum.MempoolEachRecordLimitType.Count
        )
        {
            if (ValueList.Count == 0)
            {
                return;
            }
            KeyValuePair<string, NVS.ValueTimeStruct>[]? tmpObj_DataList = ValueList.ToArray();
            DateTime startTime = DateTime.Now;
            int recordCount = 0;
            for (int i = 0; i < tmpObj_DataList.Count(); i++)
            {
                if (UseThisNumberAsCountOrMiliSeconds > 0)
                {
                    if (UseThisNumberType == Notus.Variable.Enum.MempoolEachRecordLimitType.Count)
                    {
                        recordCount++;
                        if (recordCount > UseThisNumberAsCountOrMiliSeconds)
                        {
                            break;
                        }
                    }
                    else
                    {
                        if ((DateTime.Now - startTime).TotalMilliseconds > UseThisNumberAsCountOrMiliSeconds)
                        {
                            break;
                        }
                    }
                }
                incomeAction(tmpObj_DataList[i].Key, tmpObj_DataList[i].Value.Value);
            }
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
        
        /*
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
        */

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
                TempPath +
                exactTime.ToString(NVC.DefaultDateTimeFormatText + "ff") +
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

