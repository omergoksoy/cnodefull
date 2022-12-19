using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ND = Notus.Date;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVS = Notus.Variable.Struct;
namespace Notus.Data
{
    public class KeyValue : IDisposable
    {
        private bool TimerRunning = false;
        private Notus.Threads.Timer TimerObj;
        private readonly string dirName = "test";
        private Notus.Data.Sql? SqlObj;
        private ConcurrentQueue<NVS.KeyValueDataList> DeleteKeyList = new ConcurrentQueue<NVS.KeyValueDataList>();
        private ConcurrentQueue<NVS.KeyValueDataList> SetValueList = new ConcurrentQueue<NVS.KeyValueDataList>();
        public KeyValue(string PoolName)
        {
            if (Directory.Exists(dirName) == false)
            {
                Directory.CreateDirectory(dirName);
            }
            PoolName = dirName + "/" + PoolName;
            
            DeleteKeyList.Clear();
            SetValueList.Clear();

            SqlObj = new Notus.Data.Sql();
            SqlObj.Open(PoolName + ".db");
            try
            {
                SqlObj.TableExist(
                    "key_value",
                    "CREATE TABLE key_value ( key TEXT NOT NULL UNIQUE, value TEXT NOT NULL );"
                );
            }
            catch { }
            TimerObj = new Notus.Threads.Timer(100);
            TimerObj.Start(() =>
            {
                SetValueToDbFunction();
            }, true);
        }
        private void SetValueToDbFunction()
        {
            if (TimerRunning == false)
            {
                TimerRunning = true;

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
                dirName +
                "/" +
                exactTime.ToString(NVC.DefaultDateTimeFormatText) +
                "_" +
                hexKey +
                "." + (setFile == true ? "set" : "del");
            return dataLockFileName;
        }
        public string Get(string key)
        {
            string resultText = string.Empty;
            if (SqlObj == null)
            {
                return resultText;
            }

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
            return resultText;
        }
        public bool Delete(string key)
        {
            NVS.KeyValueDataList storeObj = new NVS.KeyValueDataList()
            {
                Key = key,
                Value = "",
                Time = DateTime.UtcNow
            };
            File.WriteAllTextAsync(
                FileName(key, storeObj.Time, false),
                JsonSerializer.Serialize(storeObj)
            ).GetAwaiter().GetResult();
            DeleteKeyList.Enqueue(storeObj);
            return true;
        }
        public async Task SetAsync(string key, string value)
        {
            NVS.KeyValueDataList storeObj = new NVS.KeyValueDataList()
            {
                Key = key,
                Value = value,
                Time = DateTime.UtcNow
            };
            await File.WriteAllTextAsync(
                FileName(key, storeObj.Time, true),
                JsonSerializer.Serialize(storeObj)
            );
            SetValueList.Enqueue(storeObj);
            //return true;
        }
        public bool Set(string key, string value)
        {
            NVS.KeyValueDataList storeObj = new NVS.KeyValueDataList()
            {
                Key = key,
                Value = value,
                Time = DateTime.UtcNow
            };
            File.WriteAllTextAsync(
                FileName(key, storeObj.Time, true),
                JsonSerializer.Serialize(storeObj)
            ).GetAwaiter().GetResult();
            SetValueList.Enqueue(storeObj);
            return true;
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
