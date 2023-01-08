using Notus.Block;
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
        private string TempPath = string.Empty;
        private string DirPath = string.Empty;

        private NVS.KeyValueSettings ObjSettings = new();
        private bool TimerRunning = false;
        private Notus.Threads.Timer TimerObj = new();
        private Notus.Data.Sql SqlObj = new();
        private ConcurrentDictionary<string, NVS.ValueTimeStruct> ValueList = new();

        private ConcurrentQueue<NVS.KeyValueDataList> DataValueList = new();
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
        public void FirstLoad()
        {
            Each((string key, string value) =>
            {
                AddToMemoryList(key, value);
            });
        }
        private int LoadFromDisk()
        {
            SortedDictionary<ulong, string> setFileOrder = new();
            string[] dataList = NI.GetFileList(TempPath, "data");
            DataValueList.Clear();
            foreach (string setFilename in dataList)
            {
                ulong fileTime = ND.ToLong(File.GetCreationTime(setFilename));
                bool innerLoop = false;
                while (innerLoop == false)
                {
                    if (setFileOrder.ContainsKey(fileTime) == true)
                    {
                        fileTime++;
                        innerLoop = true;
                    }
                    else
                    {
                        setFileOrder.Add(fileTime, setFilename);
                    }
                }
            }
            while (setFileOrder.Count > 0)
            {
                var firstItem = setFileOrder.First();
                string setDataText = System.IO.File.ReadAllText(firstItem.Value);
                try
                {
                    NVS.KeyValueDataList? storeObj = JsonSerializer.Deserialize<NVS.KeyValueDataList>(setDataText);
                    if (storeObj != null)
                    {
                        DataValueList.Enqueue(storeObj);
                    }
                    else
                    {
                        NI.DeleteFile(firstItem.Value);
                    }
                }
                catch
                {
                    NI.DeleteFile(firstItem.Value);
                }
                setFileOrder.Remove(firstItem.Key);
            }

            return dataList.Length;
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
            ObjSettings.Path = settings.Path;
            ObjSettings.Name = settings.Name;

            DirPath = NNT.NetworkTypeText(NVG.Settings.Network) +
                System.IO.Path.DirectorySeparatorChar +
            NNT.NetworkLayerText(NVG.Settings.Layer) +
                System.IO.Path.DirectorySeparatorChar +
            ObjSettings.Path +
                System.IO.Path.DirectorySeparatorChar;
            TempPath = DirPath + ObjSettings.Name + "_temp" + System.IO.Path.DirectorySeparatorChar;
            //Console.WriteLine(DirPath);
            //Console.WriteLine(TempPath);

            NI.CreateDirectory(DirPath);
            NI.CreateDirectory(TempPath);

            PoolName = DirPath + ObjSettings.Name + ".db";
            DataValueList.Clear();

            SqlObj.Open(PoolName);
            try
            {
                SqlObj.TableExist(
                    "key_value", "CREATE TABLE key_value ( key TEXT NOT NULL UNIQUE, value TEXT NOT NULL );"
                );
            }
            catch { }

            int timerInterval = 100;
            if (ObjSettings.ResetTable == false)
            {
                if (ObjSettings.LoadFromBeginning == true)
                {
                    FirstLoad();
                }
                timerInterval = (LoadFromDisk() > 10 ? 5 : timerInterval);
            }

            TimerObj.Start(timerInterval, () =>
            {
                SetValueToDbFunction();
            }, true);
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

            DataValueList.Clear();
            Thread.Sleep(100);

            ValueList.Clear();
            try
            {
                SqlObj.Clear("key_value");
            }
            catch { }

            NI.DeleteAllFileInsideDirectory(TempPath, "data");
        }
        public void Each(System.Action<string, string> incomeAction)
        {
            string valueKeyText = string.Empty;
            string valueResultText = string.Empty;
            SqlObj.Select("key_value",
                (Dictionary<string, string> rList) =>
                {
                    foreach (KeyValuePair<string, string> entry in rList)
                    {
                        if (entry.Key == "key")
                        {
                            valueKeyText = entry.Value;
                        }
                        if (entry.Key == "value")
                        {
                            valueResultText = entry.Value;
                        }
                    }
                    incomeAction(valueKeyText, valueResultText);
                }, new List<string>() { "key", "value" }
            );
        }
        private (bool, string) GetFromSqlDb(string key)
        {
            bool founded = false;
            string resultText = string.Empty;
            SqlObj.Select("key_value",
                (Dictionary<string, string> rList) =>
                {
                    foreach (KeyValuePair<string, string> entry in rList)
                    {
                        if (string.Equals(entry.Key, "value"))
                        {
                            resultText = entry.Value;
                            founded = true;
                        }
                    }
                },
                new List<string>() { "key", "value" },
                new Dictionary<string, string>() { { "key", key } }
            );
            return (founded, resultText);
        }
        public string Get(string? key)
        {
            if (key == null)
                return string.Empty;

            if (key.Length==0)
                return string.Empty;

            if (ValueList.ContainsKey(key) == true)
                return ValueList[key].Value;

            if (SqlObj == null)
                return string.Empty;

            (bool founded, string resultText) = GetFromSqlDb(key);

            if (founded == false)
                return string.Empty;

            AddToMemoryList(key, resultText);
            return resultText;
        }
        public void Remove(string key)
        {
            Delete(key);
        }
        public void Delete(string key)
        {
            ValueList.TryRemove(key, out _);
            Task.Run(() =>
            {
                StoreObject(new NVS.KeyValueDataList()
                {
                    Set = false,
                    Key = key,
                    Value = "",
                    Time = DateTime.UtcNow
                });
            });
        }
        public bool ContainsKey(string key)
        {
            if (ValueList.ContainsKey(key) == true)
            {
                return true;
            }
            (bool founded, string resultText) = GetFromSqlDb(key);
            if (founded == true)
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
        private void StoreObject(NVS.KeyValueDataList storeObj)
        {
            bool fileWritten = false;
            while (fileWritten == false)
            {
                try
                {
                    File.WriteAllText(
                        FileName(storeObj.Key, storeObj.Time, true),
                        JsonSerializer.Serialize(storeObj)
                    );
                    fileWritten = true;
                }
                catch { }
                if (fileWritten == false)
                {
                    Thread.Sleep(10);
                }
            }
            DataValueList.Enqueue(storeObj);
        }
        public void Set(string? key, string? value)
        {
            if (key == null)
                return;

            if (key.Length==0)
                return;

            if (value == null)
                value = string.Empty;

            AddToMemoryList(key, value);
            Task.Run(() =>
            {
                StoreObject(new NVS.KeyValueDataList()
                {
                    Set = true,
                    Key = key,
                    Value = value,
                    Time = DateTime.UtcNow
                });
            });
        }
        private void SetValueToDbFunction()
        {
            if (TimerRunning == false)
            {
                TimerRunning = true;
                TimerObj.SetInterval(DataValueList.Count > 10 ? 5 : 100);

                // burada silinecek kayıtlar kontrol ediliyor...
                if (DataValueList.TryDequeue(out NVS.KeyValueDataList? dataResult))
                {
                    if (dataResult != null)
                    {
                        if (SqlObj != null)
                        {
                            if (dataResult.Set == true)
                            {
                                bool insertStatus = false, updateStatus = false, deletFile = true;
                                try
                                {
                                    insertStatus = SqlObj.Insert("key_value", new Dictionary<string, string>(){
                                        { "key", dataResult.Key},
                                        { "value", dataResult.Value}
                                    });
                                }
                                catch { }
                                if (insertStatus == false)
                                {
                                    try
                                    {
                                        updateStatus = SqlObj.Update("key_value",
                                            new Dictionary<string, string>(){
                                        { "value", dataResult.Value }
                                            },
                                            new Dictionary<string, string>(){
                                        { "key",dataResult.Key}
                                            }
                                        );
                                        if (updateStatus == false)
                                        {
                                            deletFile = false;
                                            DataValueList.Enqueue(dataResult);
                                        }
                                    }
                                    catch { }
                                }

                                if (deletFile == true)
                                {
                                    File.Delete(FileName(dataResult.Key, dataResult.Time, true));
                                }
                            }
                            else
                            {
                                bool deleteResultVal = SqlObj.Delete(
                                    "key_value",
                                    new Dictionary<string, string>(){
                                        { "key", dataResult.Key }
                                    }
                                );
                                if (deleteResultVal == true)
                                {
                                    File.Delete(FileName(dataResult.Key, dataResult.Time, false));
                                }
                                else
                                {
                                    DataValueList.Enqueue(dataResult);
                                }
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
            /*
            Console.WriteLine(PoolName);
            Console.WriteLine(TempPath);
            Console.WriteLine(DirPath);
            Console.WriteLine("----------------------------");
            */
            string dataLockFileName =
                TempPath +
                exactTime.ToString(NVC.DefaultDateTimeFormatText + "ff") +
                "_" +
                hexKey +
                ".data";
            return dataLockFileName;
        }
        public KeyValue()
        {
        }
        public KeyValue(NVS.KeyValueSettings settings)
        {
            SetSettings(settings);
        }
        ~KeyValue()
        {
            Dispose();
        }
        public void Dispose()
        {
            DataValueList.Clear();
            ValueList.Clear();
            if (SqlObj != null)
            {
                try { SqlObj.Close(); } catch { }
                try { SqlObj.Dispose(); } catch { }
            }
            if (TimerObj != null)
            {
                try { TimerObj.Close(); } catch { }
                try { TimerObj.Dispose(); } catch { }
            }
        }
    }
}

