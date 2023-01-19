using Notus.Sync;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NBD = Notus.Block.Decrypt;
using NBK = Notus.Block.Key;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVClass = Notus.Variable.Class;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Coin
{
    //aşrdrop üzerinden işlemin pool'a atılması durumunu kontrol et ve key value DB'ye bağla
    public class AirDrop : IDisposable
    {
        private readonly string CurrentVersion = "1.0.0.0";
        private ConcurrentDictionary<string, List<string>> RequestList = new ConcurrentDictionary<string, List<string>>();
        private Notus.Data.KeyValue LimitDb = new Notus.Data.KeyValue();
        public bool LimitExceeded(string ReceiverWalletKey)
        {
            lock (RequestList)
            {
                List<string>? innerRequestList = new();
                string controlStr = LimitDb.Get(ReceiverWalletKey);
                if (controlStr.Length > 0)
                {
                    try
                    {
                        innerRequestList = JsonSerializer.Deserialize<List<string>>(controlStr);
                    }
                    catch { }
                    if (innerRequestList == null)
                    {
                        innerRequestList = new List<string>();
                    }
                }

                if (RequestList.ContainsKey(ReceiverWalletKey) == false)
                {
                    RequestList.TryAdd(ReceiverWalletKey, new List<string>());
                }

                RequestList[ReceiverWalletKey].Clear();
                for (int count = 0; count < innerRequestList.Count; count++)
                {
                    TimeSpan diff = (NVG.NOW.Obj - NBK.BlockIdToTime(innerRequestList[count])).Duration();
                    if (NVC.AirDropTimeLimit > diff.TotalHours)
                    {
                        RequestList[ReceiverWalletKey].Add(innerRequestList[count]);
                    }
                }

                if (RequestList[ReceiverWalletKey].Count >= NVC.AirDropVolumeCount)
                {
                    LimitDb.Set(ReceiverWalletKey, JsonSerializer.Serialize(RequestList[ReceiverWalletKey]));
                    return true;
                }
            }
            return false;
        }
        public string Request(NVS.HttpRequestDetails IncomeData)
        {
            if (NVG.Settings.Genesis == null)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 553268,
                    ErrorText = "AnErrorOccurred",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            // mainnet ise hata gönderecek
            if (NVG.Settings.Network == Variable.Enum.NetworkType.MainNet)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 35496,
                    ErrorText = "NotSupported",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.NotSupported
                });
            }

            string ReceiverWalletKey = IncomeData.UrlList[1];

            if (ReceiverWalletKey.Length != NVC.WalletFullTextLength)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 91983,
                    ErrorText = "WrongWallet",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WrongWallet
                });
            }

            if (LimitExceeded(ReceiverWalletKey) == true)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 371854,
                    ErrorText = "TooManyRequest",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.TooManyRequest
                });
            }

            string airdropUid = NGF.GenerateTxUid();

            // eğer cüzdan kilitli ise hata gönderecek
            if (NGF.Balance.AccountIsLock(ReceiverWalletKey) == true)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 3827,
                    ErrorText = "WalletNotAllowed",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WalletNotAllowed
                });
            }

            /*
"rowNo":3,
{"In":{"134afde3707f":{"Wallet":"NSXhhh","Balance":{"NOTUS":{"20230110225407707":"0"}},"RowNo":0,"UID":""}},"Out":{"NSXhhhhhh888888888888888488888888822222":{"NOTUS":{"20230110225407708":"2000000000"}}},"Validator":"NSX6woSKz9hc4fUtd4K8iJpK99XsK7Y96rArN63"}

"rowNo":4
{"In":{"134afde37081":{"Wallet":"NSXhhh","Balance":{"NOTUS":{"20230110225407708":"2000000000"}},"RowNo":3,"UID":"134afde37080000787fff8b6746f19981d5aba47fb78e1143e3f0c99ea5fd7ca6fe0026000ee675560a36bce08"}},"Out":{"NSXhhhhhh888888888888888488888888822222":{"NOTUS":{"20230110225407708":"2000000000","20230110225409591":"2000000000"}}},"Validator":"NSX6woSKz9hc4fUtd4K8iJpK99XsK7Y96rArN63"}

"rowNo":5
{"In":{"134afde37080":{"Wallet":"NSXhhh","Balance":{"NOTUS":{"20230110225407708":"2000000000"}},"RowNo":3,"UID":"134afde37080000787fff8b6746f19981d5aba47fb78e1143e3f0c99ea5fd7ca6fe0026000ee675560a36bce08"}},"Out":{"NSXhhhhhh888888888888888488888888822222":{"NOTUS":{"20230110225407708":"2000000000","20230110225408273":"2000000000"}}},"Validator":"NSX6woSKz9hc4fUtd4K8iJpK99XsK7Y96rArN63"}

"rowNo":6
{"In":{"134afde3708c":{"Wallet":"NSXhhh","Balance":{"NOTUS":{"20230110225407708":"2000000000","20230110225408273":"2000000000"}},"RowNo":5,"UID":"134afde3708800044df265139bfffe5bec532b792bdee4b3d28ba4b8b8261e78a9dda97eef7de880a47ea971f3"}},"Out":{"NSXhhhhhh888888888888888488888888822222":{"NOTUS":{"20230110225407708":"2000000000","20230110225408273":"2000000000","20230110225420398":"2000000000"}}},"Validator":"NSX6woSKz9hc4fUtd4K8iJpK99XsK7Y96rArN63"}


            */
            lock (NGF.WalletUsageList)
            {
                bool returnWalletUsing = false;
                if (NGF.WalletUsageList.ContainsKey(ReceiverWalletKey) == true)
                {
                    if (string.Equals(NGF.WalletUsageList[ReceiverWalletKey], airdropUid) == false)
                    {
                        returnWalletUsing = true;
                    }
                }
                else
                {
                    if (NGF.WalletUsageList.TryAdd(ReceiverWalletKey, airdropUid) == false)
                    {
                        returnWalletUsing = true;
                    }
                    else
                    {
                        if (string.Equals(NGF.WalletUsageList[ReceiverWalletKey], airdropUid) == false)
                        {
                            returnWalletUsing = true;
                        }
                    }
                }

                if (returnWalletUsing == true)
                {
                    return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                    {
                        ErrorNo = 36789,
                        ErrorText = "WalletUsing",
                        ID = string.Empty,
                        Result = NVE.BlockStatusCode.WalletUsing
                    });
                }
            }

            /*
            // eğer cüzdan başka bir işlem tarafından kilitli ise hata gönderecek
            http://18.156.37.61:5002/airdrop/NSXhhhhhh888888888888888488888888822222
            http://18.156.37.61:5002/airdrop/NSXhhhhhh888888888888888488888888844444
            http://18.156.37.61:5002/balance/NSXhhhhhh888888888888888488888888822222
            */

            //burada değişken geri dönecek
            NVClass.BlockStruct_125 airDrop = Calculate(ReceiverWalletKey, airdropUid);
            // Console.WriteLine("---------------------------------------");
            // Console.WriteLine(JsonSerializer.Serialize(airDrop, NVC.JsonSetting));
            // Console.WriteLine("---------------------------------------");

            bool tmpAddResult = NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
            {
                uid = airdropUid,
                type = NVE.BlockTypeList.AirDrop,
                data = JsonSerializer.Serialize(airDrop)
            });

            /*
            
            control-point-123456
            // burada listeye eklensin
            // burada listeye eklensin
            // burada listeye eklensin
            control-noktası
            */
            NVG.TxPool.Add(new Notus.Compiler.TxQueueStruct()
            {
                Uid = airdropUid,
                Type = Compiler.TxQueueType.Contract,
                ContractId = NVC.AirdropBlockUid,
                Fee = "0",
                PublicKey = "",
                FunctionList = new List<Compiler.FunctionList>()
                {

                },
                Sign = ""
            });
            if (tmpAddResult == true)
            {
                RequestList[ReceiverWalletKey].Add(airdropUid);
                LimitDb.Set(ReceiverWalletKey,
                    JsonSerializer.Serialize(RequestList[ReceiverWalletKey])
                );

                // burada transactionları belleğe alıyor böyle hızlı ulaşım sağlanıyor...
                NVG.Settings.TxStatus.Set(airdropUid, new NVS.CryptoTransferStatus()
                {
                    Code = NVE.BlockStatusCode.AddedToQueue,
                    RowNo = 0,
                    UID = "",
                    Text = "AddedToQueue"
                });


                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = "AddedToQueue",
                    ID = airdropUid,
                    Result = NVE.BlockStatusCode.AddedToQueue
                });
            }
            NVG.Settings.TxStatus.Set(airdropUid, new NVS.CryptoTransferStatus()
            {
                Code = NVE.BlockStatusCode.Unknown,
                RowNo = 0,
                UID = "",
                Text = "Unknown"
            });

            return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
            {
                ErrorNo = 55632,
                ErrorText = "Unknown",
                ID = string.Empty,
                Result = NVE.BlockStatusCode.Unknown
            });
        }

        public NVClass.BlockStruct_125 Calculate(string ReceiverWalletKey, string airdropUid)
        {

            string tmpCoinCurrency = NVG.Settings.Genesis.CoinInfo.Tag;

            /*
            burada cüzdandan bakiye çekilirken kilitleme işlemi yapılmalı
            airdropta hala anlık bakiye çekme esnasında hata oluşturuyor
            */

            NVS.WalletBalanceStruct tmpBalanceBefore = NGF.Balance.Get(ReceiverWalletKey, 0);
            NVS.WalletBalanceStruct tmpBalanceAfter = NGF.Balance.Get(ReceiverWalletKey, 0);

            string airdropStr = GetAirDropVolume();
            ulong tmpCoinKeyVal = NVG.NOW.Int;
            if (tmpBalanceAfter.Balance[tmpCoinCurrency].ContainsKey(tmpCoinKeyVal) == false)
            {
                tmpBalanceAfter.Balance[tmpCoinCurrency].Add(tmpCoinKeyVal, airdropStr);
            }
            else
            {
                BigInteger tmpResult =
                    BigInteger.Parse(tmpBalanceAfter.Balance[tmpCoinCurrency][tmpCoinKeyVal]) +
                    BigInteger.Parse(airdropStr);
                tmpBalanceAfter.Balance[tmpCoinCurrency][tmpCoinKeyVal] = tmpResult.ToString();
            }


            NVClass.BlockStruct_125 airDrop = new NVClass.BlockStruct_125()
            {
                In = new Dictionary<string, NVS.WalletBalanceStruct>(),
                Out = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>(),
                Validator = NVG.Settings.NodeWallet.WalletKey
            };
            airDrop.In.Add(airdropUid, tmpBalanceBefore);
            airDrop.Out.Add(ReceiverWalletKey, tmpBalanceAfter.Balance);
            return airDrop;
        }
        public void Process(NVClass.BlockData blockData)
        {
            if (blockData.info.type != NVE.BlockTypeList.AirDrop)
                return;

            NVClass.BlockStruct_125? tmpLockBalance = NBD.Convert_125(blockData.cipher.data, true);
            if (tmpLockBalance != null)
            {
                lock (RequestList)
                {
                    //Console.WriteLine("Process AirDrop Block");
                    foreach (var entry in tmpLockBalance.In)
                    {
                        if (RequestList.ContainsKey(entry.Value.Wallet) == false)
                        {
                            RequestList.TryAdd(entry.Value.Wallet, new List<string>());
                        }
                        else
                        {
                            if (RequestList[entry.Value.Wallet].IndexOf(entry.Key) == -1)
                            {
                                RequestList[entry.Value.Wallet].Add(entry.Key);
                            }
                        }
                        LimitDb.Set(
                            entry.Value.Wallet,
                            JsonSerializer.Serialize(RequestList[entry.Value.Wallet])
                        );
                    }
                }
            }
        }
        public void Start()
        {
            LimitDb.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 1000,
                Name = "airdrop"
            });

            // veri tabanındaki versiyon mevcut versiyon ile farklı ise
            // tabloda bulunan kayıtları temizle
            if (string.Equals(LimitDb.Get("CurrentVersion"), CurrentVersion) == false)
            {
                //LimitDb.Clear();
                LimitDb.Set("CurrentVersion", CurrentVersion);
            }
        }
        public AirDrop()
        {
        }

        ~AirDrop()
        {
            Dispose();
        }
        private string GetAirDropVolume()
        {
            string airdropStr = NVC.AirDropVolume_Default;
            if (NVC.AirDropVolume.ContainsKey(NVG.Settings.Layer))
            {
                if (NVC.AirDropVolume[NVG.Settings.Layer].ContainsKey(NVG.Settings.Network))
                {
                    airdropStr = NVC.AirDropVolume[NVG.Settings.Layer][NVG.Settings.Network];
                }
            }
            return airdropStr;
        }
        public void Dispose()
        {
            try
            {
                LimitDb.Dispose();
            }
            catch { }
        }
    }
}
