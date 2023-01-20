using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NT = Notus.Threads;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;

namespace Notus.Wallet
{
    public class Balance : IDisposable
    {
        private NT.Timer SubTimer = new NT.Timer();

        //this store balance to Dictionary list
        private readonly object SummaryDb_LockObject = new object();
        private Notus.Data.KeyValue SummaryDb = new Notus.Data.KeyValue();
        //private Notus.Mempool ObjMp_Balance;

        //private Notus.Mempool ObjMp_WalletUsage;
        //private Notus.Mempool ObjMp_LockWallet;
        private Notus.Data.KeyValue MultiWalletParticipantDb = new Notus.Data.KeyValue();
        private Notus.Data.KeyValue WalletsICanApproveDb = new Notus.Data.KeyValue();
        private ConcurrentDictionary<string, Notus.Variable.Enum.MultiWalletType> MultiWalletTypeList = new ConcurrentDictionary<string, Variable.Enum.MultiWalletType>();

        public List<string> WalletsICanApprove(string WalletId)
        {
            string multiParticipantStr = WalletsICanApproveDb.Get(WalletId);
            if (multiParticipantStr == "")
            {
                return new List<string>();
            }
            List<string>? participantList = new List<string>();
            try
            {
                participantList = JsonSerializer.Deserialize<List<string>>(multiParticipantStr);
            }
            catch
            {
            }
            if (participantList == null)
            {
                return new List<string>();
            }
            return participantList;
        }

        public ConcurrentQueue<KeyValuePair<DateTime, string>> WalletReleaseTime = new();
        public Notus.Variable.Enum.MultiWalletType GetMultiWalletType(string MultiSignatureWalletId)
        {
            if (MultiWalletTypeList.ContainsKey(MultiSignatureWalletId))
            {
                return MultiWalletTypeList[MultiSignatureWalletId];
            }
            return Variable.Enum.MultiWalletType.Unknown;
        }
        public List<string> GetParticipant(string MultiSignatureWalletId)
        {
            string multiParticipantStr = MultiWalletParticipantDb.Get(MultiSignatureWalletId);
            if (multiParticipantStr == "")
            {
                return new List<string>();
            }
            List<string>? participantList = new List<string>();
            try
            {
                participantList = JsonSerializer.Deserialize<List<string>>(multiParticipantStr);
            }
            catch
            {
            }
            if (participantList == null)
            {
                return new List<string>();
            }
            return participantList;
        }
        // bu fonksiyonlar ile cüzdanın kilitlenmesi durumuna bakalım
        public bool CheckTransactionAvailability(string senderKey, string receiverKey)
        {
            lock (NGF.WalletUsageList)
            {
                //Console.WriteLine("Wallet Usage Available : " + walletKey);
                //Console.WriteLine(JsonSerializer.Serialize(NGF.WalletUsageList));
                if (
                    NGF.WalletUsageList.ContainsKey(senderKey) == true
                    ||
                    NGF.WalletUsageList.ContainsKey(receiverKey) == true
                ) { return false; }

                bool resultSender = NGF.WalletUsageList.TryAdd(senderKey, "456465");
                bool resultReceiver = NGF.WalletUsageList.TryAdd(receiverKey, "456465");
                if (
                    resultSender == false
                    ||
                    resultReceiver == false
                )
                {
                    if (resultSender == true)
                    {
                        NGF.WalletUsageList.TryRemove(senderKey, out _);
                    }
                    if (resultReceiver == true)
                    {
                        NGF.WalletUsageList.TryRemove(receiverKey, out _);
                    }
                    return false;
                }
            }
            return true;
        }
        public bool WalletUsageAvailable(string walletKey)
        {
            lock (NGF.WalletUsageList)
            {
                //Console.WriteLine("Wallet Usage Available : " + walletKey);
                //Console.WriteLine(JsonSerializer.Serialize(NGF.WalletUsageList));
                return (NGF.WalletUsageList.ContainsKey(walletKey) == false ? true : false);
            }
        }
        public bool StartWalletUsage(string walletKey)
        {
            lock (NGF.WalletUsageList)
            {
                //Console.WriteLine("Start Wallet Usage : " + walletKey);
                //Console.WriteLine(JsonSerializer.Serialize(NGF.WalletUsageList));
                if (NGF.WalletUsageList.ContainsKey(walletKey) == false)
                {
                    bool result = NGF.WalletUsageList.TryAdd(walletKey, "456465");
                    //Console.WriteLine(result);
                    //Console.WriteLine(JsonSerializer.Serialize(NGF.WalletUsageList));
                    return result;
                }
                else
                {
                    //Console.WriteLine(JsonSerializer.Serialize(NGF.WalletUsageList));
                }
                return false;
            }
        }
        public void StopWalletUsage(string walletKey)
        {
            WalletReleaseTime.Enqueue(new KeyValuePair<DateTime, string>(NVG.NOW.Obj.AddSeconds(1), walletKey));
            /*
            lock (NGF.WalletUsageList)
            {
                NGF.WalletUsageList.TryRemove(walletKey, out _);
            }
            */
        }

        private void StoreToDb(NVS.WalletBalanceStruct BalanceObj)
        {
            NP.Basic("StoreToDb : -> " + BalanceObj.Wallet);
            /*
            lock (SummaryDb_LockObject)
            {
            }
            */
            SummaryDb.SetDirectly(BalanceObj.Wallet, JsonSerializer.Serialize(BalanceObj));
            NP.Basic("New Balance -> " + BalanceObj.Wallet + " -> " + JsonSerializer.Serialize(BalanceObj.Balance));
            //burada cüzdan kilidi açılacak...
            StopWalletUsage(BalanceObj.Wallet);
        }
        public NVS.WalletBalanceResponseStruct ReadFromNode(string WalletKey)
        {
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                List<string> ListMainNodeIp = Notus.Validator.List.Get(NVG.Settings.Layer, NVG.Settings.Network);

                for (int a = 0; a < ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    string nodeIpAddress = ListMainNodeIp[a];
                    try
                    {
                        //bool RealNetwork = PreTransfer.Network == Notus.Variable.Enum.NetworkType.Const_MainNetwork;
                        string fullUrlAddress =
                            Notus.Network.Node.MakeHttpListenerPath(
                                nodeIpAddress,
                                Notus.Network.Node.GetNetworkPort(NVG.Settings.Network, Notus.Variable.Enum.NetworkLayer.Layer1)
                            ) + "balance/" + WalletKey + "/";

                        string MainResultStr = Notus.Communication.Request.Get(fullUrlAddress, 10, true).GetAwaiter().GetResult();
                        NVS.WalletBalanceResponseStruct tmpTransferResult = JsonSerializer.Deserialize<NVS.WalletBalanceResponseStruct>(MainResultStr);
                        return tmpTransferResult;
                    }
                    catch (Exception err)
                    {
                        Notus.Print.Basic(true, "Error Text [8ahgd6s4d]: " + err.Message);
                    }
                }
            }
            return null;
        }
        public NVS.WalletBalanceStruct Get(string WalletKey, ulong timeYouCanUse)
        {
            Console.WriteLine("Get Wallet Balance : " + WalletKey);
            string returnText = SummaryDb.GetDirectly(WalletKey);
            if (returnText.Length > 0)
            {
                Console.WriteLine(returnText);
                try
                {
                    Console.WriteLine("Get From RocksDb");
                    return JsonSerializer.Deserialize<NVS.WalletBalanceStruct>(returnText);
                }
                catch { }
            }
            else
            {
                Console.WriteLine("Balance Text Is Empty");
            }

            string defaultCoinTag = Notus.Variable.Constant.MainCoinTagName;
            if (NVG.Settings != null)
            {
                if (NVG.Settings.Genesis != null)
                {
                    if (NVG.Settings.Genesis.CoinInfo != null)
                    {
                        if (NVG.Settings.Genesis.CoinInfo.Tag.Length > 0)
                        {
                            defaultCoinTag = NVG.Settings.Genesis.CoinInfo.Tag;
                        }
                    }

                }
            }
            timeYouCanUse = (timeYouCanUse == 0 ? NVG.NOW.Int : timeYouCanUse);
            return new NVS.WalletBalanceStruct()
            {
                Balance = new Dictionary<string, Dictionary<ulong, string>>()
                {
                    {
                        defaultCoinTag,
                        new Dictionary<ulong, string>(){
                            { timeYouCanUse ,"0" }
                        }
                    },
                },
                RowNo = 0,
                UID = "",
                Wallet = WalletKey
            };
        }
        /*
        public BigInteger GetCoinBalance(string WalletKey)
        {
            return GetCoinBalance(Get(WalletKey));
        }
        */

        //bu fonksiyon hesaptan çıkartma işlemi yapıyor
        public Dictionary<string, Dictionary<ulong, string>> ReAssign(Dictionary<string, Dictionary<ulong, string>> balanceObj)
        {
            Dictionary<string, Dictionary<ulong, string>> tmpResult = new Dictionary<string, Dictionary<ulong, string>>();
            foreach (KeyValuePair<string, Dictionary<ulong, string>> currencyEntry in balanceObj)
            {
                tmpResult.Add(currencyEntry.Key, new Dictionary<ulong, string>());
                foreach (KeyValuePair<ulong, string> balanceEntry in currencyEntry.Value)
                {
                    tmpResult[currencyEntry.Key].Add(balanceEntry.Key, balanceEntry.Value);
                }
            }
            return tmpResult;
        }

        /*

        controlpoint
        controlpoint
        controlpoint
        controlpoint


        BURAYA CÜZDAN KİLİTLEME İŞLEMİ EKLE
        KİLİTLEME OLAYI ŞU : AYNI ANDA BİRDEN FAZLA BAKİYE GİRİŞ VE ÇIKIŞI İŞLEMİNE İZİN VERMEMESİ
        İÇİN GEÇİCİ OLARAK İŞLEMİN KİLİTLENMESİ DURUMU

        AYRICA ÇÖZMEK İÇİNDE BİR FONKSİYON VEYA İŞLEM EKLE

        */
        public Dictionary<ulong, string> RemoveZeroUnlockTime(Dictionary<ulong, string> currentBalance)
        {
            ulong removeKey = 0;
            foreach (var entry in currentBalance)
            {
                if (entry.Value == "0")
                {
                    removeKey = entry.Key;
                }
            }
            if (removeKey > 0)
            {
                currentBalance.Remove(removeKey);
                currentBalance = RemoveZeroUnlockTime(currentBalance);
            }
            return currentBalance;
        }
        public (bool, NVS.WalletBalanceStruct) SubtractVolumeWithUnlockTime(
            NVS.WalletBalanceStruct balanceObj,
            string volume,
            string coinTagName,
            ulong unlockTime = 0
        )
        {
            if (unlockTime == 0)
            {
                unlockTime = NVG.NOW.Int;
            }
            bool volumeError = true;
            // first parametre hata oluşması durumunda
            if (balanceObj.Balance.ContainsKey(coinTagName) == false)
            {
                return (volumeError, balanceObj);
            }

            BigInteger volumeNeeded = BigInteger.Parse(volume);
            foreach (KeyValuePair<ulong, string> entry in balanceObj.Balance[coinTagName])
            {
                if (unlockTime > entry.Key)
                {
                    if (volumeNeeded > 0)
                    {
                        BigInteger currentTimeVolume = BigInteger.Parse(entry.Value);
                        if (currentTimeVolume > volumeNeeded)
                        {
                            BigInteger resultVolume = currentTimeVolume - volumeNeeded;
                            balanceObj.Balance[coinTagName][entry.Key] = resultVolume.ToString();
                            volumeNeeded = 0;
                        }
                        else
                        {
                            volumeNeeded = volumeNeeded - currentTimeVolume;
                            balanceObj.Balance[coinTagName][entry.Key] = "0";
                        }
                    }
                }
            }

            if (volumeNeeded == 0)
            {
                //balanceObj.Balance[coinTagName] = RemoveZeroUnlockTime(balanceObj.Balance[coinTagName]);
                return (false, balanceObj);
            }
            return (true, balanceObj);
        }
        public NVS.WalletBalanceStruct AddVolumeWithUnlockTime(
            NVS.WalletBalanceStruct balanceObj,
            string volume,
            string coinTagName,
            ulong unlockTime
        )
        {
            if (balanceObj.Balance.ContainsKey(coinTagName) == false)
            {
                balanceObj.Balance.Add(coinTagName, new Dictionary<ulong, string>()
                {
                    { unlockTime,volume }
                }
                );
                return balanceObj;
            }
            if (balanceObj.Balance[coinTagName].ContainsKey(unlockTime) == false)
            {
                balanceObj.Balance[coinTagName].Add(unlockTime, volume);
                return balanceObj;
            }
            BigInteger totalVolume = BigInteger.Parse(balanceObj.Balance[coinTagName][unlockTime]) + BigInteger.Parse(volume);
            balanceObj.Balance[coinTagName][unlockTime] = totalVolume.ToString();
            //balanceObj.Balance[coinTagName] = RemoveZeroUnlockTime(balanceObj.Balance[coinTagName]);
            return balanceObj;
        }
        public bool HasEnoughCoin(string walletKey, BigInteger howMuchCoinNeed, string CoinTagName = "")
        {
            if (NVG.Settings == null)
                return false;

            if (NVG.Settings.Genesis == null)
                return false;

            if (CoinTagName.Length == 0)
                CoinTagName = NVG.Settings.Genesis.CoinInfo.Tag;

            NVS.WalletBalanceStruct tmpGeneratorBalanceObj = Get(walletKey, 0);
            BigInteger currentVolume = GetCoinBalance(tmpGeneratorBalanceObj, CoinTagName);
            return (howMuchCoinNeed > currentVolume ? false : true);
        }
        public BigInteger GetCoinBalance(NVS.WalletBalanceStruct tmpBalanceObj, string CoinTagName)
        {
            if (tmpBalanceObj.Balance.ContainsKey(CoinTagName) == false)
            {
                return 0;
            }

            BigInteger resultVal = 0;
            ulong exactTimeLong = NVG.NOW.Int;
            foreach (KeyValuePair<ulong, string> entry in tmpBalanceObj.Balance[CoinTagName])
            {
                if (exactTimeLong > entry.Key)
                {
                    resultVal += BigInteger.Parse(entry.Value);
                }
            }
            return resultVal;
        }
        public bool AccountIsLock(string WalletKey)
        {
            string unlockTimeStr = "";
            lock (NGF.LockWalletList)
            {
                if (NGF.LockWalletList.ContainsKey(WalletKey) == true)
                {
                    unlockTimeStr = NGF.LockWalletList[WalletKey];
                }
            }

            if (unlockTimeStr.Length > 0)
            {
                if (ulong.TryParse(unlockTimeStr, out ulong unlockTimeLong))
                {
                    return (NVG.NOW.Obj > Notus.Date.ToDateTime(unlockTimeLong) ? true : false);
                }
            }
            return false;
        }

        /*
        //control-local-block
        private void StoreToTemp(Notus.Variable.Class.BlockData? tmpBlockData)
        {
            if (tmpBlockData != null)
            {
                string fileName = tmpBlockData.info.uID + ".tmp";
                string folderName = Notus.IO.GetFolderName(NVG.Settings, Notus.Variable.Constant.StorageFolderName.TempBlock);
                string fullPath = folderName + fileName;
                string blockStr = JsonSerializer.Serialize(tmpBlockData);
                using (StreamWriter writer = new StreamWriter(fullPath))
                {
                    writer.WriteLine(blockStr);
                }
            }
            else
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Error,
                    1111199999,
                    "Block Is NULL",
                    "BlockRowNo",
                    NVG.Settings,
                    null
                );
            }
        }
        */
        public void Control(Notus.Variable.Class.BlockData tmpBlockForBalance)
        {
            //StoreToTemp(tmpBlockForBalance);
            // genesis block
            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.GenesisBlock)
            {
                ulong coinStartingTime = Notus.Block.Key.BlockIdToUlong(tmpBlockForBalance.info.uID);


                Notus.Wallet.Block.ClearList(NVG.Settings.Network, NVG.Settings.Layer);

                Notus.Wallet.Block.Add2List(NVG.Settings.Network, NVG.Settings.Layer, new NVS.CurrencyListStorageStruct()
                {
                    Detail = new NVS.CurrencyList()
                    {
                        Logo = new NVS.FileStorageStruct()
                        {
                            Base64 = NVG.Settings.Genesis.CoinInfo.Logo.Base64,
                            Source = NVG.Settings.Genesis.CoinInfo.Logo.Source,
                            Url = NVG.Settings.Genesis.CoinInfo.Logo.Url,
                            Used = NVG.Settings.Genesis.CoinInfo.Logo.Used
                        },
                        Name = NVG.Settings.Genesis.CoinInfo.Name,
                        ReserveCurrency = true,
                        Tag = NVG.Settings.Genesis.CoinInfo.Tag,
                    },
                    Uid = tmpBlockForBalance.info.uID
                });
                //Notus.Wallet.Currency.Add2List(NVG.Settings.Network, NVG.Settings.Genesis.CoinInfo.Tag, tmpBlockForBalance.info.uID);

                //ObjMp_Balance.Clear();
                //ObjMp_LockWallet.Clear();
                ClearAllData();
                string tmpBalanceStr = NVG.Settings.Genesis.Premining.PreSeed.Volume.ToString();
                if (NVG.Settings.Genesis.Premining.PreSeed.DecimalContains == false)
                {
                    tmpBalanceStr = tmpBalanceStr + Notus.Toolbox.Text.RepeatString(NVG.Settings.Genesis.Reserve.Decimal, "0");
                }
                StoreToDb(new NVS.WalletBalanceStruct()
                {
                    UID = tmpBlockForBalance.info.uID,
                    RowNo = tmpBlockForBalance.info.rowNo,
                    Wallet = NVG.Settings.Genesis.Premining.PreSeed.Wallet,
                    Balance = new Dictionary<string, Dictionary<ulong, string>>()
                    {
                        {
                            NVG.Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>()
                            {
                                {
                                    coinStartingTime, tmpBalanceStr
                                }
                            }
                        }
                    }
                });


                tmpBalanceStr = NVG.Settings.Genesis.Premining.Private.Volume.ToString();
                if (NVG.Settings.Genesis.Premining.Private.DecimalContains == false)
                {
                    tmpBalanceStr = tmpBalanceStr + Notus.Toolbox.Text.RepeatString(NVG.Settings.Genesis.Reserve.Decimal, "0");
                }

                StoreToDb(new NVS.WalletBalanceStruct()
                {
                    UID = tmpBlockForBalance.info.uID,
                    RowNo = tmpBlockForBalance.info.rowNo,
                    Wallet = NVG.Settings.Genesis.Premining.Private.Wallet,
                    Balance = new Dictionary<string, Dictionary<ulong, string>>()
                    {
                        {
                            NVG.Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>()
                            {
                                {
                                    coinStartingTime, tmpBalanceStr
                                }
                            }
                        }
                    }
                });


                tmpBalanceStr = NVG.Settings.Genesis.Premining.Public.Volume.ToString();
                if (NVG.Settings.Genesis.Premining.Public.DecimalContains == false)
                {
                    tmpBalanceStr = tmpBalanceStr + Notus.Toolbox.Text.RepeatString(NVG.Settings.Genesis.Reserve.Decimal, "0");
                }
                StoreToDb(new NVS.WalletBalanceStruct()
                {
                    UID = tmpBlockForBalance.info.uID,
                    RowNo = tmpBlockForBalance.info.rowNo,
                    Wallet = NVG.Settings.Genesis.Premining.Public.Wallet,
                    Balance = new Dictionary<string, Dictionary<ulong, string>>()
                    {
                        {
                            NVG.Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>()
                            {
                                {
                                    coinStartingTime, tmpBalanceStr
                                }
                            }
                        }
                    }
                });
            }
            else
            {
                //Console.WriteLine("Balance -> Line 205");
                //Console.WriteLine("Buradaki kontroller hata düzeltmelerinden sonra tekrar aktive edilece.");
                //Console.WriteLine("Buradaki kontroller hata düzeltmelerinden sonra tekrar aktive edilece.");
                //Console.ReadLine();
                //Console.ReadLine();
            }

            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.MultiWalletCryptoTransfer)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                //Console.WriteLine(tmpRawDataStr);
                Dictionary<string, NVS.MultiWalletTransactionStruct>? tmpBalanceVal =
                    JsonSerializer.Deserialize<Dictionary<string, NVS.MultiWalletTransactionStruct>>(
                        tmpRawDataStr
                    );
                if (tmpBalanceVal != null)
                {
                    foreach (KeyValuePair<string, Variable.Struct.MultiWalletTransactionStruct> outerEntry in tmpBalanceVal)
                    {
                        foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> innerEntry in outerEntry.Value.After)
                        {
                            StoreToDb(new NVS.WalletBalanceStruct()
                            {
                                UID = tmpBlockForBalance.info.uID,
                                RowNo = tmpBlockForBalance.info.rowNo,
                                Wallet = innerEntry.Key,
                                Balance = innerEntry.Value
                            });
                        }
                    }
                }
            }

            //LockAccount
            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.LockAccount)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                NVS.LockWalletStruct? tmpLockBalance =
                    JsonSerializer.Deserialize<NVS.LockWalletStruct>(
                        tmpRawDataStr
                    );
                if (tmpLockBalance != null)
                {
                    if (tmpLockBalance.Out != null)
                    {
                        StoreToDb(new NVS.WalletBalanceStruct()
                        {
                            UID = tmpBlockForBalance.info.uID,
                            RowNo = tmpBlockForBalance.info.rowNo,
                            Wallet = tmpLockBalance.WalletKey,
                            Balance = tmpLockBalance.Out
                        });
                    }
                    string pureStr = System.Text.Encoding.UTF8.GetString(
                        System.Convert.FromBase64String(
                            tmpBlockForBalance.cipher.data
                        )
                    );

                    if (NGF.LockWalletList.ContainsKey(tmpLockBalance.WalletKey) == false)
                    {
                        NGF.LockWalletList.TryAdd(
                            tmpLockBalance.WalletKey,
                            tmpLockBalance.UnlockTime.ToString()
                        );
                    }
                    /*
                    ObjMp_LockWallet.Set(
                        Notus.Toolbox.Text.ToHex(tmpLockBalance.WalletKey),
                        tmpLockBalance.UnlockTime.ToString(),
                        true
                    );
                    */
                }
            }

            //Airdrop
            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.AirDrop)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                Notus.Variable.Class.BlockStruct_125? tmpLockBalance =
                    JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_125>(
                        tmpRawDataStr
                    );
                if (tmpLockBalance != null)
                {
                    foreach (var entry in tmpLockBalance.Out)
                    {
                        StoreToDb(new NVS.WalletBalanceStruct()
                        {
                            UID = tmpBlockForBalance.info.uID,
                            RowNo = tmpBlockForBalance.info.rowNo,
                            Wallet = entry.Key,
                            Balance = entry.Value
                        });
                        NGF.Balance.StopWalletUsage(entry.Key);
                    }
                }
            }

            //CryptoTransfer
            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.CryptoTransfer)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                //NP.Basic("Balance -> Control Function -> tmpRawDataStr -> " + tmpRawDataStr);
                Notus.Variable.Class.BlockStruct_120? tmpBalanceVal =
                    JsonSerializer.Deserialize<Notus.Variable.Class.BlockStruct_120>(
                        tmpRawDataStr
                    );
                foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> entry in tmpBalanceVal.Out)
                {
                    //NP.Basic(JsonSerializer.Serialize(tmpBalanceVal.Out));
                    StoreToDb(new NVS.WalletBalanceStruct()
                    {
                        UID = tmpBlockForBalance.info.uID,
                        RowNo = tmpBlockForBalance.info.rowNo,
                        Wallet = entry.Key,
                        Balance = entry.Value
                    });
                }
            }

            //MultiWalletContract 
            if (tmpBlockForBalance.info.type == Notus.Variable.Enum.BlockTypeList.MultiWalletContract)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                NVS.MultiWalletStoreStruct? tmpBalanceVal = JsonSerializer.Deserialize<NVS.MultiWalletStoreStruct>(tmpRawDataStr);
                if (tmpBalanceVal == null)
                {
                    NP.Basic("tmpRawDataStr -> Balance.Cs -> 493. Line");
                    NP.Basic(tmpRawDataStr);
                }
                else
                {
                    //string multiParticipantStr = MultiWalletParticipantDb.Get(tmpBalanceVal.MultiWalletKey, "");

                    //multi wallet cüzdanın katılımcılarını tutan mempool listesi
                    List<string> participantList = GetParticipant(tmpBalanceVal.MultiWalletKey);
                    for (int i = 0; i < tmpBalanceVal.WalletList.Count; i++)
                    {
                        if (participantList.IndexOf(tmpBalanceVal.WalletList[i]) == -1)
                        {
                            participantList.Add(tmpBalanceVal.WalletList[i]);
                        }

                        List<string> walletIcanApprove = WalletsICanApprove(tmpBalanceVal.WalletList[i]);
                        if (walletIcanApprove.IndexOf(tmpBalanceVal.MultiWalletKey) == -1)
                        {
                            walletIcanApprove.Add(tmpBalanceVal.MultiWalletKey);
                            WalletsICanApproveDb.Set(
                                tmpBalanceVal.WalletList[i],
                                JsonSerializer.Serialize(walletIcanApprove)
                            );
                        }

                        //WalletsICanApprove()
                        //
                    }

                    if (MultiWalletTypeList.ContainsKey(tmpBalanceVal.MultiWalletKey) == false)
                    {
                        MultiWalletTypeList.TryAdd(tmpBalanceVal.MultiWalletKey, tmpBalanceVal.VoteType);
                    }
                    else
                    {
                        MultiWalletTypeList[tmpBalanceVal.MultiWalletKey] = tmpBalanceVal.VoteType;
                    }
                    MultiWalletParticipantDb.Set(
                        tmpBalanceVal.MultiWalletKey,
                        JsonSerializer.Serialize(participantList)
                    );


                    //Console.WriteLine(JsonSerializer.Serialize(participantList, Notus.Variable.Constant.JsonSetting));
                    //multi wallet cüzdanın katılımcılarını tutan mempool listesi
                    //List<string> participantList = GetParticipant(tmpBalanceVal.MultiWalletKey);

                    StoreToDb(new NVS.WalletBalanceStruct()
                    {
                        UID = tmpBalanceVal.Balance.UID,
                        RowNo = tmpBalanceVal.Balance.RowNo,
                        Wallet = tmpBalanceVal.Founder.WalletKey,
                        Balance = tmpBalanceVal.Balance.Balance
                    });

                    StoreToDb(new NVS.WalletBalanceStruct()
                    {
                        UID = tmpBlockForBalance.info.uID,
                        RowNo = tmpBlockForBalance.info.rowNo,
                        Wallet = tmpBalanceVal.MultiWalletKey,
                        Balance = new Dictionary<string, Dictionary<ulong, string>>(){
                        {
                            NVG.Settings.Genesis.CoinInfo.Tag,
                            new Dictionary<ulong, string>(){
                                {
                                    Notus.Block.Key.BlockIdToUlong(tmpBalanceVal.Balance.UID) , "0"
                                }
                            }
                        }
                    }
                    });

                    //Console.WriteLine("Multi Signature Wallet -> Balance.Cs -> 498. Line");
                    //Console.WriteLine(JsonSerializer.Serialize(tmpBalanceVal, Notus.Variable.Constant.JsonSetting));
                }
            }
            /*
            if (tmpBlockForBalance.info.type == 240)
            {
                string tmpRawDataStr = System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        tmpBlockForBalance.cipher.data
                    )
                );
                NVS.StorageOnChainStruct tmpBalanceVal = JsonSerializer.Deserialize<NVS.StorageOnChainStruct>(tmpRawDataStr);
                StoreToDb(new NVS.WalletBalanceStruct()
                {
                    UID = tmpBalanceVal.Balance.UID,
                    RowNo = tmpBalanceVal.Balance.RowNo,
                    Wallet = tmpBalanceVal.Balance.Wallet,
                    Balance = tmpBalanceVal.Balance.Balance
                });
            }
            */

            // TokenGeneration
            if (tmpBlockForBalance.info.type == 160)
            {
                NVS.BlockStruct_160? tmpBalanceVal = JsonSerializer.Deserialize<NVS.BlockStruct_160>(
                    System.Text.Encoding.UTF8.GetString(
                        System.Convert.FromBase64String(
                            tmpBlockForBalance.cipher.data
                        )
                    )
                );
                if (tmpBalanceVal != null)
                {
                    Int64 BlockFee = Notus.Wallet.Fee.Calculate(tmpBalanceVal, NVG.Settings.Network);
                    string WalletKeyStr = Notus.Wallet.ID.GetAddressWithPublicKey(tmpBalanceVal.Creation.PublicKey);
                    NVS.WalletBalanceStruct CurrentBalance = Get(WalletKeyStr, 0);
                    string TokenBalanceStr = tmpBalanceVal.Reserve.Supply.ToString();


                    ulong tmpBlockTime = ulong.Parse(tmpBlockForBalance.info.time.Substring(0, 17));
                    CurrentBalance.Balance.Add(tmpBalanceVal.Info.Tag, new Dictionary<ulong, string>()
                    {
                        { tmpBlockTime, TokenBalanceStr }
                    }
                    );

                    (bool tmpErrorStatus, var newBalanceVal) =
                        SubtractVolumeWithUnlockTime(
                            CurrentBalance,
                            BlockFee.ToString(),
                            NVG.Settings.Genesis.CoinInfo.Tag,
                            tmpBlockTime
                        );
                    /*
                    CurrentBalance.Balance[NVG.Settings.Genesis.CoinInfo.Tag] =
                        (BigInteger.Parse(CurrentBalance.Balance[NVG.Settings.Genesis.CoinInfo.Tag]) -
                        BlockFee).ToString();
                    */

                    StoreToDb(new NVS.WalletBalanceStruct()
                    {
                        Balance = newBalanceVal.Balance,
                        RowNo = tmpBlockForBalance.info.rowNo,
                        UID = tmpBlockForBalance.info.uID,
                        Wallet = Notus.Wallet.ID.GetAddressWithPublicKey(tmpBalanceVal.Creation.PublicKey)
                    }
                    );

                    Notus.Wallet.Block.Add2List(NVG.Settings.Network, NVG.Settings.Layer, new NVS.CurrencyListStorageStruct()
                    {
                        Uid = tmpBlockForBalance.info.uID,
                        Detail = new NVS.CurrencyList()
                        {
                            ReserveCurrency = false,
                            Name = tmpBalanceVal.Info.Name,
                            Tag = tmpBalanceVal.Info.Tag,
                            Logo = new NVS.FileStorageStruct()
                            {
                                Base64 = tmpBalanceVal.Info.Logo.Base64,
                                Source = tmpBalanceVal.Info.Logo.Source,
                                Url = tmpBalanceVal.Info.Logo.Url,
                                Used = tmpBalanceVal.Info.Logo.Used
                            }
                        }
                    }
                    );
                }
            }

        }
        public void Start()
        {
            /*
            ObjMp_Balance = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Balance) +
                "account_balance"
            );
            ObjMp_Balance.AsyncActive = true;
            ObjMp_Balance.Clear();
            */
            /*
            ObjMp_LockWallet = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Balance) +
                "account_lock"
            );
            ObjMp_LockWallet.AsyncActive = false;
            ObjMp_LockWallet.Clear();
            */
            /*
            ObjMp_WalletUsage = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Balance) +
                "wallet_usage"
            );
            ObjMp_WalletUsage.AsyncActive = false;
            ObjMp_WalletUsage.Clear();
            */
            MultiWalletTypeList.Clear();
        }
        public Balance()
        {
            SummaryDb.SetSettings(new NVS.KeyValueSettings()
            {
                //ResetTable = true,
                MemoryLimitCount = 1000,
                Name = "balance"
            });

            MultiWalletParticipantDb.SetSettings(new NVS.KeyValueSettings()
            {
                //ResetTable = true,
                MemoryLimitCount = 1000,
                Name = "multi_wallet_participant"
            });

            WalletsICanApproveDb.SetSettings(new NVS.KeyValueSettings()
            {
                //ResetTable = true,
                MemoryLimitCount = 1000,
                Name = "wallet_i_can_approve"
            });

            ExecuteTimer();
        }
        ~Balance()
        {
            Dispose();
        }
        private void ExecuteTimer()
        {
            SubTimer.Start(500, () =>
            {
                if (WalletReleaseTime.TryPeek(out KeyValuePair<DateTime, string> walletObj))
                {
                    if (NVG.NOW.Obj > walletObj.Key)
                    {
                        if (WalletReleaseTime.TryDequeue(out KeyValuePair<DateTime, string> innerWalletObj))
                        {
                            lock (NGF.WalletUsageList)
                            {
                                NGF.WalletUsageList.TryRemove(innerWalletObj.Value, out _);
                            }
                        }
                    }
                }

            });
        }
        private void ClearAllData()
        {
            SummaryDb.Clear();
            NGF.LockWalletList.Clear();

            NP.Basic("NGF.WalletUsageList.Clear(); -> CLEARED -> Balance.Cs");
            NP.Basic(JsonSerializer.Serialize(NGF.WalletUsageList));

            NGF.WalletUsageList.Clear();
            //ObjMp_WalletUsage.Clear();
            MultiWalletParticipantDb.Clear();
            WalletsICanApproveDb.Clear();
            MultiWalletTypeList.Clear();
        }
        public void Dispose()
        {
            try
            {
                if (SubTimer != null)
                {
                    SubTimer.Dispose();
                }
            }
            catch { }
            /*
            try
            {
                if (ObjMp_LockWallet != null)
                {
                    ObjMp_LockWallet.Dispose();
                }
            }
            catch
            {
            }
            */
            /*
            try
            {
                if (ObjMp_WalletUsage != null)
                {
                    ObjMp_WalletUsage.Dispose();
                }
            }
            catch
            {
            }
            */
            try
            {
                SummaryDb.Dispose();
            }
            catch { }
            try
            {
                MultiWalletParticipantDb.Dispose();
            }
            catch
            {
            }
            MultiWalletTypeList.Clear();

            try
            {
                WalletsICanApproveDb.Dispose();
            }
            catch
            {
            }

        }
    }
}
