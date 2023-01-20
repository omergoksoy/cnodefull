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
            // 1- data gelip, gelmediği kontrol ediliyor
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

            // 2- data içerikleri kontrol ediliyor
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

            // 3- cüzdan adresinin uzunluğu kontrol ediliyor
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

            // 4- gönderen, kendisine gönderemez
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

            // 5- hesap kilitli ise, gönderim yapamaz
            if (NGF.Balance.AccountIsLock(tmpTransfer.Sender) == true)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 3827,
                    ErrorText = "WalletNotAllowed",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WalletNotAllowed
                });
            }

            // 6- multi signature wallet, bu işlem ile gönderim yapamaz
            if (Notus.Wallet.MultiID.IsMultiId(tmpTransfer.Sender) == true)
            {
                return Request_MultiSignatureSend(IncomeData, tmpTransfer);
            }

            // 7- iki günden eski işlem ise geçerli sayılmaz
            const int transferTimeOut = (2 * 86400);
            if (Math.Abs((ND.NowObj() - Notus.Date.ToDateTime(tmpTransfer.CurrentTime)).TotalSeconds) > transferTimeOut)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 5245,
                    ErrorText = "OldTransaction",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.OldTransaction
                });
            }

            // 8- gönderilmiş ve işlenmiş olan TX hatalı olarak işaretlenir
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
                    }
                }
            }
            TxSignListObj.Set(controlKey, tmpTransfer.Sign);

            // 9- public key ve gönderilen cüzdan adresi eşleştiriliyor
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

            // 10- gönderilecek bakiye kontrol ediliyor
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


            // 11- imza kontrol ediliyor
            if (Notus.Wallet.ID.Verify(
                Notus.Core.MergeRawData.Transaction(tmpTransfer), 
                tmpTransfer.Sign, 
                tmpTransfer.PublicKey
            ) == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 7314,
                    ErrorText = "WrongSignature",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WrongSignature
                });
            }


            // 12- işlem için Notus Coin olup olmadığı kontrol ediliyor
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

            // 13- transafer ücreti ve işlem için yeterli coin / token olup olmadığı  kontrol ediliyor
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

            // 14- işlem durumu kayıt altına alınıyor...
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

            Console.WriteLine(JsonSerializer.Serialize(recordStruct));

            NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
            {
                uid = tmpTransferIdKey,
                type = NVE.BlockTypeList.CryptoTransfer,
                data = JsonSerializer.Serialize(recordStruct)
            });

            /*
            //omergoksoy
            // transfer data saved for next step
            lock (CryptoTransferPool)
            {
                CryptoTransferPool.Set(tmpTransferIdKey, JsonSerializer.Serialize(recordStruct));
                CryptoTransferPool_List.TryAdd(tmpTransferIdKey, recordStruct);
            }
            */
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
