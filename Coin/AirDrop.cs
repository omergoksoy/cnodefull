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
        Notus.Data.KeyValue LimitDb = new Notus.Data.KeyValue();
        public string Request(NVS.HttpRequestDetails IncomeData)
        {
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

            if (ReceiverWalletKey.Length != NVC.SingleWalletTextLength)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 91983,
                    ErrorText = "WrongWallet",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WrongWallet
                });
            }
            List<string> innerRequestList = LoadFromDb(ReceiverWalletKey);
            for (int count = 0; count < innerRequestList.Count; count++)
            {
                TimeSpan diff = (NVG.NOW.Obj - NBK.BlockIdToTime(innerRequestList[count])).Duration();
                if (NVC.AirDropTimeLimit > diff.TotalMinutes)
                {
                    RequestList[ReceiverWalletKey].Add(innerRequestList[count]);
                }
            }

            if (RequestList[ReceiverWalletKey].Count >= NVC.AirDropVolumeCount)
            {
                LimitDb.Set(ReceiverWalletKey,JsonSerializer.Serialize(RequestList[ReceiverWalletKey]));
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 371854,
                    ErrorText = "TooManyRequest",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.TooManyRequest
                });
            }

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

            // eğer cüzdan başka bir işlem tarafından kilitli ise hata gönderecek
            if (NGF.Balance.WalletUsageAvailable(ReceiverWalletKey) == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 36789,
                    ErrorText = "WalletUsing",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WalletUsing
                });
            }

            // eğer cüzdanı kilitleyemezse hata gönderecek
            if (NGF.Balance.StartWalletUsage(ReceiverWalletKey) == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 27468,
                    ErrorText = "AnErrorOccurred",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            string tmpCoinCurrency = NVG.Settings.Genesis.CoinInfo.Tag;

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
            airDrop.In.Add(tmpChunkIdKey, tmpBalanceBefore);
            airDrop.Out.Add(ReceiverWalletKey, tmpBalanceAfter.Balance);

            bool tmpAddResult = NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
            {
                uid = tmpChunkIdKey,
                type = NVE.BlockTypeList.AirDrop,
                data = JsonSerializer.Serialize(airDrop)
            });
            if (tmpAddResult == true)
            {
                RequestList[ReceiverWalletKey].Add(tmpChunkIdKey);
                LimitDb.Set(ReceiverWalletKey,
                    JsonSerializer.Serialize(RequestList[ReceiverWalletKey])
                );
                Console.WriteLine("Set Wallet - >" + ReceiverWalletKey);
                Console.WriteLine(JsonSerializer.Serialize(RequestList[ReceiverWalletKey]));
                // burada transactionları belleğe alıyor böyle hızlı ulaşım sağlanıyor...
                NVG.Cache.Transaction.Add(tmpChunkIdKey, NVE.BlockStatusCode.AddedToQueue);

                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = "AddedToQueue",
                    ID = tmpChunkIdKey,
                    Result = NVE.BlockStatusCode.AddedToQueue
                });
            }
            NVG.Cache.Transaction.Add(tmpChunkIdKey, NVE.BlockStatusCode.Unknown);
            return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
            {
                ErrorNo = 55632,
                ErrorText = "Unknown",
                ID = string.Empty,
                Result = NVE.BlockStatusCode.Unknown
            });
        }

        public void Process(NVClass.BlockData blockData)
        {
            if (blockData.info.type != NVE.BlockTypeList.AirDrop)
                return;

            NVClass.BlockStruct_125? tmpLockBalance = NBD.Convert_125(blockData.cipher.data, true);
            if (tmpLockBalance != null)
            {
                //Console.WriteLine("Process AirDrop Block");
                foreach (var entry in tmpLockBalance.In)
                {
                    if (RequestList.ContainsKey(entry.Value.Wallet) == false)
                    {
                        RequestList.TryAdd(entry.Value.Wallet, new List<string>());
                    }
                    RequestList[entry.Value.Wallet].Add(entry.Key);
                    LimitDb.Set(
                        entry.Value.Wallet,
                        JsonSerializer.Serialize(RequestList[entry.Value.Wallet])
                    );
                }
            }
        }
        public AirDrop()
        {
            LimitDb.SetSettings(new NVS.KeyValueSettings()
            {
                ResetTable = false,
                Path = "wallet",
                MemoryLimitCount = 1000,
                Name = "airdrop"
            });

            // veri tabanındaki versiyon mevcut versiyon ile farklı ise
            // tabloda bulunan kayıtları temizle
            if (string.Equals(LimitDb.Get("CurrentVersion"), CurrentVersion) == false)
            {
                LimitDb.Clear();
                LimitDb.Set("CurrentVersion", CurrentVersion);
            }
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
        private List<string> LoadFromDb(string walletId)
        {
            List<string>? innerRequestList = new();
            string controlStr = LimitDb.Get(walletId);
            //Console.WriteLine("Get Wallet - >" + walletId);
            //Console.WriteLine("controlStr : " + controlStr);
            if (controlStr.Length > 0)
            {
                try
                {
                    innerRequestList = JsonSerializer.Deserialize<List<string>>(controlStr);
                }
                catch { }
            }

            if (innerRequestList == null)
            {
                innerRequestList = new List<string>();
            }
            if (RequestList.ContainsKey(walletId) == false)
            {
                RequestList.TryAdd(walletId, new List<string>());
            }
            else
            {
                RequestList[walletId].Clear();
            }
            return innerRequestList;
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
