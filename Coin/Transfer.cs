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
using ND = Notus.Date;
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
    public class Transfer : IDisposable
    {
        private readonly string CurrentVersion = "1.0.0.0";

        //crypto transfer pool
        private Notus.Data.KeyValue CryptoTransferPool = new();
        private ConcurrentDictionary<string, NVS.CryptoTransactionStoreStruct> CryptoTransferPool_List = new();

        private Notus.Mempool ObjMp_MultiSignPool;

        //bu değişken aynı transaction'ın 2 kere işleme alınmasını engelliyor
        private Notus.Data.KeyValue TxSignListObj = new();

        private ConcurrentDictionary<string, List<string>> RequestList = new ConcurrentDictionary<string, List<string>>();
        
        public ConcurrentDictionary<string, NVS.CryptoTransactionStoreStruct> GetList()
        {
            return CryptoTransferPool_List;
        }
        public void Remove(string txUid)
        {
            CryptoTransferPool_List.TryRemove(txUid, out _);
            CryptoTransferPool.Remove(txUid);
        }
        public string Request(NVS.HttpRequestDetails IncomeData)
        {
            NVS.CryptoTransactionStruct? tmpTransfer = null;
            try
            {
                tmpTransfer = JsonSerializer.Deserialize<NVS.CryptoTransactionStruct>(IncomeData.PostParams["data"]);
            }
            catch { tmpTransfer = null; }

            if (tmpTransfer == null)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 78945,
                    ErrorText = "AnErrorOccurred",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            if (
                tmpTransfer.Volume == null ||
                tmpTransfer.Sign == null ||
                tmpTransfer.PublicKey == null ||
                tmpTransfer.Sender == null ||
                tmpTransfer.CurrentTime == 0 ||
                tmpTransfer.UnlockTime == 0 ||
                tmpTransfer.Currency == null ||
                tmpTransfer.Receiver == null
            )
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 4928,
                    ErrorText = "WrongParameter",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WrongParameter
                });
            }

            if (tmpTransfer.Sender.Length != Notus.Variable.Constant.WalletFullTextLength)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 7546,
                    ErrorText = "WrongWallet_Sender",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WrongWallet_Sender
                });
            }
            if (tmpTransfer.Receiver.Length != Notus.Variable.Constant.WalletFullTextLength)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 5245,
                    ErrorText = "WrongWallet_Receiver",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WrongWallet_Receiver
                });
            }
            if (string.Equals(tmpTransfer.Receiver, tmpTransfer.Sender))
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 5245,
                    ErrorText = "SenderCantBeReceiver",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.SenderCantBeReceiver
                });
            }

            bool accountLocked = NGF.Balance.AccountIsLock(tmpTransfer.Sender);
            if (accountLocked == true)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 3827,
                    ErrorText = "WalletNotAllowed",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WalletNotAllowed
                });
            }

            if (Notus.Wallet.MultiID.IsMultiId(tmpTransfer.Sender) == true)
            {
                return Request_MultiSignatureSend(IncomeData, tmpTransfer);
            }

            const int transferTimeOut = 0;
            DateTime rightNow = ND.NowObj();
            DateTime currentTime = Notus.Date.ToDateTime(tmpTransfer.CurrentTime);
            double totaSeconds = Math.Abs((rightNow - currentTime).TotalSeconds);
            // iki günden eski ise  zaman aşımı olarak işaretle
            if (totaSeconds > (2 * 86400))
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 5245,
                    ErrorText = "OldTransaction",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.OldTransaction
                });
            }


            lock (TxSignListObj)
            {
                string controlKey = tmpTransfer.CurrentTime.ToString().PadRight(24, '0') + "_" + tmpTransfer.Sender;
                string? prevSignStr = TxSignListObj.Get(controlKey);
                if (prevSignStr != null)
                {
                    if (prevSignStr.Length > 0)
                    {
                        if (string.Equals(prevSignStr, tmpTransfer.Sign))
                        {
                            return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                            {
                                ErrorNo = 5245,
                                ErrorText = "OldTransaction",
                                ID = string.Empty,
                                Result = NVE.BlockStatusCode.OldTransaction
                            });
                            /*


        blok bakiyelerini çekme işlemi yapılırken bakiye güncellenmesi ile işlemin çekilmesi
        arasında bir blokluk boşluk gerekiyor.

        bu boşluk olmadığı için hesabın bakiyesi çekilirken hatalı bir şekilde çekiliyor.

        bu hatayı düzeltmenin en kolay yolu her cüzdanın kilidi 2 blok sonra kaldırılsın
        bu hatayı düzeltmenin en kolay yolu her cüzdanın kilidi 2 blok sonra kaldırılsın
        bu hatayı düzeltmenin en kolay yolu her cüzdanın kilidi 2 blok sonra kaldırılsın
        bu hatayı düzeltmenin en kolay yolu her cüzdanın kilidi 2 blok sonra kaldırılsın


        922. blok
        {"In":{"134afdd31db0080062d3a85fcca69b3ca52cc05d1d36b2c01aea3f2d2e598c25b0417c5fd18b40ac6559b8b29a":{"Wallet":"NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT","Balance":{"NOTUS":{"20230109204208538":"0"}},"RowNo":0,"UID":""}},"Out":{"NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT":{"NOTUS":{"20230109204208538":"2000000000"}}},"Validator":"NSX4jmTMPuq5JZnGrXb2DsGxt85B5svJL8PnFKb"}

        923. blok
        {"In":{"134afdd31daf0d8858e787ebe8af05b723cefd67db610489b4d10998f03d90c9b371c81597a20abb2a0be14685":{"Wallet":"NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT","Balance":{"NOTUS":{"20230109204207903":"0"}},"RowNo":0,"UID":""}},"Out":{"NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT":{"NOTUS":{"20230109204207903":"2000000000"}}},"Validator":"NSX4jmTMPuq5JZnGrXb2DsGxt85B5svJL8PnFKb"}

        924. blok
        {"In":{"134afdd31db80beb8d8c43cdb1585bff3397fc5e6442fbd24092c845451f4324eb455a36c60b0eee87ed92f004":{"Wallet":"NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT","Balance":{"NOTUS":{"20230109204207903":"2000000000"}},"RowNo":923,"UID":"134afdd31db5000426085872b0016dc2bbe73fb92e78c5c049647be5d7a4c06c21271f0ff0d66589d286fa744c"}},"Out":{"NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT":{"NOTUS":{"20230109204207903":"2000000000","20230109204216803":"2000000000"}}},"Validator":"NSX4jmTMPuq5JZnGrXb2DsGxt85B5svJL8PnFKb"}

        925. blok
        {"In":{"134afdd31dbc0b5ddca0b7adb40bce15ad6b823dac3dba30449bf907fa57f9ec3af497f8568c5c2c9cdab44423":{"Wallet":"NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT","Balance":{"NOTUS":{"20230109204207903":"2000000000","20230109204216803":"2000000000"}},"RowNo":924,"UID":"134afdd31db9000489d5ba83ca5592b77e621c7d6b8ba97b531fc6c4fe85fbadc21d2467a7c9547323e3a5e4ad"}},"Out":{"NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT":{"NOTUS":{"20230109204207903":"2000000000","20230109204216803":"2000000000","20230109204220764":"2000000000"}}},"Validator":"NSX4jmTMPuq5JZnGrXb2DsGxt85B5svJL8PnFKb"}

        926. blok
        {"In":{"134afdd31dbd07e07694a936956e9cc6e7be7eb8e4bcfbe4bdd78d280ce7a34a67b19385f6aa98ed5be482a399":{"Wallet":"NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT","Balance":{"NOTUS":{"20230109204207903":"2000000000","20230109204216803":"2000000000"}},"RowNo":924,"UID":"134afdd31db9000489d5ba83ca5592b77e621c7d6b8ba97b531fc6c4fe85fbadc21d2467a7c9547323e3a5e4ad"}},"Out":{"NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT":{"NOTUS":{"20230109204207903":"2000000000","20230109204216803":"2000000000","20230109204221539":"2000000000"}}},"Validator":"NSX4jmTMPuq5JZnGrXb2DsGxt85B5svJL8PnFKb"}


                            */
                        }
                    }
                }
                TxSignListObj.Set(controlKey, tmpTransfer.Sign);
            }

            string calculatedWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpTransfer.PublicKey);
            if (string.Equals(calculatedWalletKey, tmpTransfer.Sender) == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 5245,
                    ErrorText = "WrongWallet_Sender",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WrongWallet_Sender
                });
            }

            if (Int64.TryParse(tmpTransfer.Volume, out _) == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 3652,
                    ErrorText = "WrongVolume",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WrongVolume
                });
            }


            string rawDataStr = Notus.Core.MergeRawData.Transaction(tmpTransfer);
            //transaction sign
            if (Notus.Wallet.ID.Verify(rawDataStr, tmpTransfer.Sign, tmpTransfer.PublicKey) == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 7314,
                    ErrorText = "WrongSignature",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WrongSignature
                });
            }


            // burada gelen bakiyeyi zaman kiliti ile kontrol edecek.

            NVS.WalletBalanceStruct tmpSenderBalanceObj = NGF.Balance.Get(tmpTransfer.Sender, 0);

            if (tmpSenderBalanceObj.Balance.ContainsKey(tmpTransfer.Currency) == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 7854,
                    ErrorText = "InsufficientBalance",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.InsufficientBalance
                });
            }

            // if wallet wants to send coin then control only coin balance
            Int64 transferFee = Notus.Wallet.Fee.Calculate(
                NVE.Fee.CryptoTransfer,
                NVG.Settings.Network,
                NVG.Settings.Layer
            );
            if (string.Equals(tmpTransfer.Currency, NVG.Settings.Genesis.CoinInfo.Tag))
            {
                BigInteger RequiredBalanceInt = BigInteger.Parse(tmpTransfer.Volume) + transferFee;
                BigInteger CoinBalanceInt = NGF.Balance.GetCoinBalance(tmpSenderBalanceObj, NVG.Settings.Genesis.CoinInfo.Tag);

                if (RequiredBalanceInt > CoinBalanceInt)
                {
                    return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                    {
                        ErrorNo = 2536,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = NVE.BlockStatusCode.InsufficientBalance
                    });
                }
            }
            else
            {
                if (tmpSenderBalanceObj.Balance.ContainsKey(NVG.Settings.Genesis.CoinInfo.Tag) == false)
                {
                    return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                    {
                        ErrorNo = 7854,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = NVE.BlockStatusCode.InsufficientBalance
                    });
                }
                BigInteger coinFeeBalance = NGF.Balance.GetCoinBalance(tmpSenderBalanceObj, NVG.Settings.Genesis.CoinInfo.Tag);
                if (transferFee > coinFeeBalance)
                {
                    return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                    {
                        ErrorNo = 7523,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = NVE.BlockStatusCode.InsufficientBalance
                    });
                }
                BigInteger tokenCurrentBalance = NGF.Balance.GetCoinBalance(tmpSenderBalanceObj, tmpTransfer.Currency);
                BigInteger RequiredBalanceInt = BigInteger.Parse(tmpTransfer.Volume);
                if (RequiredBalanceInt > tokenCurrentBalance)
                {
                    return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                    {
                        ErrorNo = 2365,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = NVE.BlockStatusCode.InsufficientBalance
                    });
                }
            }

            // transfer process status is saved
            string tmpTransferIdKey = NGF.GenerateTxUid();
            NVG.Settings.TxStatus.Set(tmpTransferIdKey, new NVS.CryptoTransferStatus()
            {
                Code = NVE.BlockStatusCode.InQueue,
                RowNo = 0,
                UID = "",
                Text = "InQueue"
            });

            NVS.CryptoTransactionStoreStruct recordStruct = new NVS.CryptoTransactionStoreStruct()
            {
                Version = 1000,
                TransferId = tmpTransferIdKey,
                CurrentTime = tmpTransfer.CurrentTime,
                UnlockTime = tmpTransfer.UnlockTime,
                Currency = tmpTransfer.Currency,
                Sender = tmpTransfer.Sender,
                Receiver = tmpTransfer.Receiver,
                Volume = tmpTransfer.Volume,
                Fee = transferFee.ToString(),
                PublicKey = tmpTransfer.PublicKey,
                Sign = tmpTransfer.Sign,
            };

            // transfer data saved for next step
            lock (CryptoTransferPool)
            {
                CryptoTransferPool.Set(tmpTransferIdKey, JsonSerializer.Serialize(recordStruct));
                CryptoTransferPool_List.TryAdd(tmpTransferIdKey, recordStruct);
            }

            if (NVG.Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    new NVS.CryptoTransactionResult()
                    {
                        ErrorNo = 0,
                        ErrorText = "AddedToQueue",
                        ID = tmpTransferIdKey,
                        Result = NVE.BlockStatusCode.AddedToQueue,
                    }, Notus.Variable.Constant.JsonSetting
                );
            }
            return JsonSerializer.Serialize(
                new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = "AddedToQueue",
                    ID = tmpTransferIdKey,
                    Result = NVE.BlockStatusCode.AddedToQueue,
                }
            );
        }
        public int Count()
        {
            return CryptoTransferPool_List.Count;
        }
        private string Request_MultiSignatureSend(
            NVS.HttpRequestDetails IncomeData,
            NVS.CryptoTransactionStruct tmpTransfer
        )
        {
            Dictionary<ulong, NVS.MultiWalletTransactionVoteStruct>? uidList = null;
            string dbKeyStr = Notus.Toolbox.Text.ToHex(tmpTransfer.Sender, 90);
            string dbText = ObjMp_MultiSignPool.Get(dbKeyStr, "");
            if (dbText.Length > 0)
            {
                uidList = JsonSerializer.Deserialize<Dictionary<
                    ulong,
                    NVS.MultiWalletTransactionVoteStruct>
                >(dbText);
            }
            if (uidList == null)
            {
                uidList = new Dictionary<ulong, NVS.MultiWalletTransactionVoteStruct>();
            }

            if (uidList.ContainsKey(tmpTransfer.CurrentTime) == true)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = uidList[tmpTransfer.CurrentTime].Status.ToString(),
                    ID = uidList[tmpTransfer.CurrentTime].TransactionId,
                    Result = uidList[tmpTransfer.CurrentTime].Status
                });
            }

            string tmpBlockUid = Notus.Block.Key.Generate(Notus.Date.ToDateTime(tmpTransfer.CurrentTime), NVG.Settings.NodeWallet.WalletKey);
            List<string>? participant = NGF.Balance.GetParticipant(tmpTransfer.Sender);
            uidList.Add(tmpTransfer.CurrentTime, new NVS.MultiWalletTransactionVoteStruct()
            {
                TransactionId = tmpBlockUid,
                Sender = tmpTransfer,
                VoteType = NGF.Balance.GetMultiWalletType(tmpTransfer.Sender),
                Status = Variable.Enum.BlockStatusCode.Pending,
                Approve = new Dictionary<string, NVS.MultiWalletTransactionApproveStruct>()
                {

                }
            });
            string calculatedWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpTransfer.PublicKey);
            for (int i = 0; i < participant.Count; i++)
            {
                if (string.Equals(participant[i], calculatedWalletKey) == false)
                {
                    uidList[tmpTransfer.CurrentTime].Approve.Add(
                        participant[i], new NVS.MultiWalletTransactionApproveStruct()
                        {
                            Approve = false,
                            TransactionId = "",
                            CurrentTime = 0,
                            PublicKey = "",
                            Sign = ""
                        }
                    );
                }
            }

            bool addingResult = ObjMp_MultiSignPool.Add(
                dbKeyStr,
                JsonSerializer.Serialize(uidList),
                Notus.Variable.Constant.MultiWalletTransactionTimeout
            );
            if (addingResult == true)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = "AddedToQueue",
                    ID = tmpBlockUid,
                    Result = NVE.BlockStatusCode.AddedToQueue
                });
            }
            return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
            {
                ErrorNo = 7546,
                ErrorText = "AnErrorOccurred",
                ID = string.Empty,
                Result = NVE.BlockStatusCode.AnErrorOccurred
            });
        }

        public void Start()
        {
            //Console.WriteLine("deneme");
            TxSignListObj.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 1000,
                Name = "tx_sign_list"
            });

            CryptoTransferPool.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 1000,
                Name = "tx_pool"
            });
            CryptoTransferPool.Each((string txKey, string txData) =>
            {
                if (CryptoTransferPool_List.ContainsKey(txKey) == false)
                {
                    CryptoTransferPool_List.TryAdd(
                        txKey,
                        JsonSerializer.Deserialize<NVS.CryptoTransactionStoreStruct>(txData)
                    );
                }
            });


            // veri tabanındaki versiyon mevcut versiyon ile farklı ise
            // tabloda bulunan kayıtları temizle
            if (string.Equals(TxSignListObj.Get("CurrentVersion"), CurrentVersion) == false)
            {
                TxSignListObj.Clear();
                TxSignListObj.Set("CurrentVersion", CurrentVersion);
            }
        }
        public Transfer()
        {
        }

        public NVS.CryptoTransferStatus Status(NVS.HttpRequestDetails IncomeData)
        {
            return NVG.Settings.TxStatus.Status(IncomeData.UrlList[2].ToLower());
        }
        ~Transfer()
        {
            Dispose();
        }
        public void Dispose()
        {
            //Console.WriteLine("");
            try
            {
                //LimitDb.Dispose();
            }
            catch { }
        }
    }
}
