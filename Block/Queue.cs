using Notus.Coin;
using Notus.Compression.TGZ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using NBD = Notus.Block.Decrypt;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NTT = Notus.Toolbox.Text;
using NVC = Notus.Variable.Constant;
using NVClass = Notus.Variable.Class;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Block
{

    //bu kitaplık üzerinde çalışmaya devam et
    //bu kitaplık üzerinde çalışmaya devam et
    //bu kitaplık üzerinde çalışmaya devam et
    ////bu kitaplık üzerinde çalışmaya devam et
    public class Queue : IDisposable
    {
        private Notus.Block.Storage BS_Storage;

        // tüm işlemlerin kayıt altına alındığı Key-Value DB
        private Notus.Data.KeyValue kvPoolDb = new();
        private Notus.Data.KeyValue kvPoolTxErrorList = new();
        private List<string> tempRemovePoolList = new();

        //buradaki queue ve dictionary değişkenlerini kontrol ederek gereksiz olarak sil veya düzelt
        private ConcurrentQueue<string> txQueue = new();
        private ConcurrentDictionary<string, byte> txQueueList = new();




        //private ConcurrentDictionary<int, List<NVS.List_PoolBlockRecordStruct>> Obj_PoolTransactionList = new();

        //bu foknsiyonun görevi blok sırası ve önceki değerlerini blok içeriğine eklemek
        public List<NVS.List_PoolBlockRecordStruct>? GetPoolList(int BlockType)
        {
            /*
            if (Obj_PoolTransactionList.ContainsKey(BlockType))
            {
                return Obj_PoolTransactionList[BlockType];
            }
            */
            return null;
        }
        public Dictionary<int, int>? GetPoolCount()
        {
            Dictionary<int, int> resultList = new Dictionary<int, int>();
            /*
            foreach (KeyValuePair<int, List<NVS.List_PoolBlockRecordStruct>> entry in Obj_PoolTransactionList)
            {
                resultList.Add(entry.Key, entry.Value.Count);
            }
            */
            return resultList;
        }
        public NVClass.BlockData OrganizeBlockOrder(NVClass.BlockData CurrentBlock)
        {
            //NP.Basic("NVG.Settings.LastBlock.info.rowNo [ USED ] : " + NVG.Settings.LastBlock.info.rowNo.ToString());
            CurrentBlock.info.rowNo = NVG.Settings.LastBlock.info.rowNo + 1;
            CurrentBlock.prev = NVG.Settings.LastBlock.info.uID + NVG.Settings.LastBlock.sign;
            CurrentBlock.info.prevList.Clear();
            foreach (KeyValuePair<int, string> entry in NVG.Settings.LastBlock.info.prevList)
            {
                if (entry.Value != "")
                {
                    CurrentBlock.info.prevList.Add(entry.Key, entry.Value);
                }
            }
            if (CurrentBlock.info.prevList.ContainsKey(CurrentBlock.info.type))
            {
                CurrentBlock.info.prevList[CurrentBlock.info.type] = CurrentBlock.prev;
            }
            else
            {
                CurrentBlock.info.prevList.Add(CurrentBlock.info.type, CurrentBlock.prev);
            }
            return CurrentBlock;
        }


        //bu fonksiyon ile işlem yapılacak aynı türden bloklar sırası ile listeden çekilip geri gönderilecek
        private void WrongTx(string? txUid, string? rawTxText)
        {
            // hatalı işlem kayıt altına alınıyor...
            kvPoolTxErrorList.Set(txUid, rawTxText);

            // tx durumu hatalı olarak işaretleniyor...
            //NVG.Settings.TxStatus.Set(txUid, NVE.BlockStatusCode.WrongTxFormat);
            NVG.Settings.TxStatus.Set(txUid, new NVS.CryptoTransferStatus()
            {
                Code = NVE.BlockStatusCode.WrongTxFormat,
                RowNo = 0,
                UID = txUid,
                Text = "WrongTxFormat"
            });

            // işlem hatalı olduğu için kuyruktan çıkartılıyor
            txQueue.TryDequeue(out _);

            // işlem hatalı olduğu için kuyruk listesinden çıkartılıyor
            if (txUid != null)
                txQueueList.TryRemove(txUid, out _);

        }
        public NVClass.BlockData? Get(ulong WaitingForPool)
        {
            if (txQueue.Count == 0)
                return null;

            int CurrentBlockType = -1;

            // aynı cüzdan adresini sadece 1 kez kullanmak için bu değişken kullanılıyor
            ConcurrentDictionary<string, byte> TempWalletList = new();
            TempWalletList.TryAdd(NVG.Settings.NodeWallet.WalletKey, 1);

            // data elamanı içersine eklenecek olan veri bu dizi içinde tutuluyor
            List<string> TempBlockList = new List<string>();

            bool exitLoop = false;
            string transactionId = string.Empty;

            tempRemovePoolList.Clear();
            while (exitLoop == false)
            {
                exitLoop = (NVG.NOW.Int >= WaitingForPool ? true : exitLoop);
                exitLoop = (txQueue.Count == 0 ? true : exitLoop);
                string? tmpTxUid = string.Empty;
                if (exitLoop == false)
                {
                    if (txQueue.TryPeek(out tmpTxUid))
                    {
                        exitLoop = (tmpTxUid == null ? true : (tmpTxUid.Length == 0 ? true : exitLoop));
                    }
                }

                if (exitLoop == false)
                {
                    string kvDataStr = kvPoolDb.Get(tmpTxUid);
                    NVS.PoolBlockRecordStruct? TmpPoolRecord = null;
                    if (kvDataStr.Length > 0)
                    {
                        try
                        {
                            TmpPoolRecord = JsonSerializer.Deserialize<NVS.PoolBlockRecordStruct>(kvDataStr);
                        }
                        catch
                        {
                            TmpPoolRecord = null;
                        }
                    }

                    if (TmpPoolRecord == null)
                    {
                        WrongTx(tmpTxUid, kvDataStr);
                        tmpTxUid = "";
                    }

                    if (TmpPoolRecord != null)
                    {
                        CurrentBlockType = (CurrentBlockType == -1 ? TmpPoolRecord.type : CurrentBlockType);

                        if (CurrentBlockType == TmpPoolRecord.type)
                        {
                            bool addToList = true;
                            if (CurrentBlockType == NVE.BlockTypeList.MultiWalletCryptoTransfer)
                            {
                                Dictionary<string, NVS.MultiWalletTransactionStruct>? multiTx =
                                    JsonSerializer.Deserialize<
                                        Dictionary<string, NVS.MultiWalletTransactionStruct>
                                    >(TmpPoolRecord.data);
                                if (multiTx == null)
                                {
                                    addToList = false;
                                    WrongTx(tmpTxUid, TmpPoolRecord.data);
                                    tmpTxUid = "";
                                }

                                if (multiTx != null)
                                {
                                    foreach (var iEntry in multiTx)
                                    {
                                        if (transactionId.Length == 0)
                                        {
                                            transactionId = iEntry.Key;
                                        }
                                    }
                                }
                            }

                            if (CurrentBlockType == NVE.BlockTypeList.AirDrop)
                            {
                                NVClass.BlockStruct_125? tmpBlockCipherData = JsonSerializer.Deserialize<NVClass.BlockStruct_125>(TmpPoolRecord.data);
                                if (tmpBlockCipherData == null)
                                {
                                    addToList = false;
                                    WrongTx(tmpTxUid, TmpPoolRecord.data);
                                    tmpTxUid = "";
                                }

                                if (tmpBlockCipherData != null)
                                {
                                    // out işlemindeki cüzdanları kontrol ediyor...
                                    foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> tmpEntry in tmpBlockCipherData.Out)
                                    {
                                        if (TempWalletList.ContainsKey(tmpEntry.Key) == false)
                                        {
                                            string airdropReceiver = tmpEntry.Key;
                                            if (NVG.Settings.Airdrop.LimitExceeded(airdropReceiver) == true)
                                            {
                                                addToList = false;
                                                NVG.Settings.TxStatus.Set(tmpTxUid, new NVS.CryptoTransferStatus()
                                                {
                                                    Code = NVE.BlockStatusCode.TooManyRequest,
                                                    RowNo = 0,
                                                    UID = tmpTxUid,
                                                    Text = "TooManyRequest"
                                                });

                                                // airdrop işlem çok fazla olduğu için kuyruktan çıkartılıyor
                                                txQueue.TryDequeue(out _);

                                                // airdrop işlemi çok fazla olduğu için kuyruk listesinden çıkartılıyor
                                                txQueueList.TryRemove(tmpTxUid, out _);

                                                //işlem tekrar gelmemesi için veri tabanından siliniyor
                                                kvPoolDb.Remove(tmpTxUid);

                                                tmpTxUid = "";
                                            }
                                            else
                                            {
                                                string airdropTxUid = string.Empty;
                                                foreach (var tmpInnerEntry in tmpBlockCipherData.In)
                                                {
                                                    airdropTxUid = tmpInnerEntry.Key;
                                                }
                                                var newAirdropObj = NVG.Settings.Airdrop.Calculate(airdropReceiver, airdropTxUid);
                                                TmpPoolRecord.data = JsonSerializer.Serialize(newAirdropObj);
                                                bool innerAddedToList = TempWalletList.TryAdd(tmpEntry.Key, 1);
                                                if (innerAddedToList == false)
                                                {
                                                    Console.WriteLine("Not Added To List -> Line 284");
                                                    addToList = false;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            addToList = false;
                                        }
                                    }
                                }
                            }

                            if (CurrentBlockType == NVE.BlockTypeList.CryptoTransfer)
                            {
                                NVS.CryptoTransactionStoreStruct? incomeConvertData = JsonSerializer.Deserialize<NVS.CryptoTransactionStoreStruct>(TmpPoolRecord.data);

                                //NVClass.BlockStruct_120? tmpBlockCipherData = JsonSerializer.Deserialize<NVClass.BlockStruct_120>(TmpPoolRecord.data);
                                if (incomeConvertData == null)
                                {
                                    addToList = false;
                                    WrongTx(tmpTxUid, TmpPoolRecord.data);
                                    tmpTxUid = "";
                                }


                                Console.WriteLine(JsonSerializer.Serialize(incomeConvertData, NVC.JsonSetting));
                                //Environment.Exit(0);
                                if (incomeConvertData != null)
                                {
                                    if (
                                        TempWalletList.ContainsKey(incomeConvertData.Sender) == true
                                         &&
                                        TempWalletList.ContainsKey(incomeConvertData.Receiver) == true
                                    )
                                    {
                                        addToList = false;
                                    }

                                    if (addToList == true)
                                    {
                                        addToList = NGF.Balance.CheckTransactionAvailability(incomeConvertData.Sender,incomeConvertData.Receiver);
                                    }

                                    BigInteger totalBlockReward = 0;
                                    ulong unlockTimeForNodeWallet = NVG.NOW.Int;
                                    NVS.WalletBalanceStruct? tmpValidatorWalletBalance = new();
                                    NVClass.BlockStruct_120? tmpBlockCipherData=new();
                                    Int64 transferFee = 0;
                                    NVS.WalletBalanceStruct? tmpSenderBalance=new();
                                    NVS.WalletBalanceStruct? tmpReceiverBalance = new();
                                    string tmpTokenTagStr = "";
                                    BigInteger tmpTokenVolume = 0;

                                    if (addToList == true)
                                    {
                                        tmpValidatorWalletBalance = NGF.Balance.Get(NVG.Settings.NodeWallet.WalletKey, unlockTimeForNodeWallet);
                                        tmpBlockCipherData = new NVClass.BlockStruct_120()
                                        {
                                            In = new Dictionary<string, NVClass.BlockStruct_120_In_Struct>(),
                                            //                  who                 coin               time   volume
                                            Out = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>(),
                                            Validator = new NVS.ValidatorStruct()
                                            {
                                                NodeWallet = NVG.Settings.NodeWallet.WalletKey,
                                                Reward = totalBlockReward.ToString()
                                            }
                                        };

                                        // wallet balances are assigned
                                        transferFee = Notus.Wallet.Fee.Calculate(
                                            NVE.Fee.CryptoTransfer,
                                            NVG.Settings.Network,
                                            NVG.Settings.Layer
                                        );
                                        //ulong transactionCount = 0;

                                        bool walletHaveEnoughCoinOrToken = true;

                                        tmpSenderBalance = NGF.Balance.Get(incomeConvertData.Sender, unlockTimeForNodeWallet);
                                        tmpReceiverBalance = NGF.Balance.Get(incomeConvertData.Receiver, unlockTimeForNodeWallet);
                                        //NP.Info("sewnder      : " + JsonSerializer.Serialize(tmpSenderBalance));
                                        //NP.Info("receiver     : " + JsonSerializer.Serialize(tmpReceiverBalance));

                                        if (string.Equals(incomeConvertData.Currency, NVG.Settings.Genesis.CoinInfo.Tag))
                                        {
                                            tmpTokenTagStr = NVG.Settings.Genesis.CoinInfo.Tag;
                                            BigInteger WalletBalanceInt = NGF.Balance.GetCoinBalance(tmpSenderBalance, tmpTokenTagStr);
                                            BigInteger RequiredBalanceInt = BigInteger.Parse(incomeConvertData.Volume);
                                            tmpTokenVolume = RequiredBalanceInt;
                                            if ((RequiredBalanceInt + transferFee) > WalletBalanceInt)
                                            {
                                                walletHaveEnoughCoinOrToken = false;
                                            }
                                        }
                                        else
                                        {
                                            if (tmpSenderBalance.Balance.ContainsKey(NVG.Settings.Genesis.CoinInfo.Tag) == false)
                                            {
                                                walletHaveEnoughCoinOrToken = false;
                                            }
                                            else
                                            {
                                                BigInteger coinFeeBalance = NGF.Balance.GetCoinBalance(tmpSenderBalance, NVG.Settings.Genesis.CoinInfo.Tag);
                                                if (transferFee > coinFeeBalance)
                                                {
                                                    walletHaveEnoughCoinOrToken = false;
                                                }
                                                else
                                                {
                                                    BigInteger tokenCurrentBalance = NGF.Balance.GetCoinBalance(tmpSenderBalance, incomeConvertData.Currency);
                                                    BigInteger RequiredBalanceInt = BigInteger.Parse(incomeConvertData.Volume);
                                                    if (RequiredBalanceInt > tokenCurrentBalance)
                                                    {
                                                        walletHaveEnoughCoinOrToken = false;
                                                    }
                                                    else
                                                    {
                                                        tmpTokenTagStr = incomeConvertData.Currency;
                                                        tmpTokenVolume = RequiredBalanceInt;
                                                    }
                                                }
                                            }
                                        }


                                        if(walletHaveEnoughCoinOrToken == false)
                                        {
                                            NVG.Settings.TxStatus.Set(incomeConvertData.TransferId, new NVS.CryptoTransferStatus()
                                            {
                                                Code = NVE.BlockStatusCode.Rejected,
                                                RowNo = 0,
                                                UID = "",
                                                Text = "Rejected"
                                            });
                                            // airdrop işlem çok fazla olduğu için kuyruktan çıkartılıyor
                                            txQueue.TryDequeue(out _);
                                            // airdrop işlemi çok fazla olduğu için kuyruk listesinden çıkartılıyor
                                            txQueueList.TryRemove(incomeConvertData.TransferId, out _);
                                            //işlem tekrar gelmemesi için veri tabanından siliniyor
                                            kvPoolDb.Remove(incomeConvertData.TransferId);
                                            tmpTxUid = "";
                                            addToList = false;
                                        }
                                    }

                                    if(addToList == true)
                                    {
                                        totalBlockReward = totalBlockReward + transferFee;
                                        //(transactionCount++;
                                        if (tmpBlockCipherData.Out.ContainsKey(incomeConvertData.Sender) == false)
                                        {
                                            tmpBlockCipherData.Out.Add(incomeConvertData.Sender, GetWalletBalanceDictionary(incomeConvertData.Sender, unlockTimeForNodeWallet));
                                        }
                                        if (tmpBlockCipherData.Out.ContainsKey(incomeConvertData.Receiver) == false)
                                        {
                                            tmpBlockCipherData.Out.Add(incomeConvertData.Receiver, GetWalletBalanceDictionary(incomeConvertData.Receiver, unlockTimeForNodeWallet));
                                        }
                                        //NP.Basic("entry.Key : " + entry.Key);
                                        tmpBlockCipherData.In.Add(incomeConvertData.TransferId, new NVClass.BlockStruct_120_In_Struct()
                                        {
                                            Fee = incomeConvertData.Fee,
                                            PublicKey = incomeConvertData.PublicKey,
                                            Sign = incomeConvertData.Sign,
                                            CurrentTime = incomeConvertData.CurrentTime,
                                            Volume = incomeConvertData.Volume,
                                            Currency = incomeConvertData.Currency,
                                            Receiver = new NVClass.WalletBalanceStructForTransaction()
                                            {
                                                Balance = NGF.Balance.ReAssign(tmpReceiverBalance.Balance),
                                                Wallet = incomeConvertData.Receiver,
                                                WitnessBlockUid = tmpReceiverBalance.UID,
                                                WitnessRowNo = tmpReceiverBalance.RowNo
                                            },
                                            Sender = new NVClass.WalletBalanceStructForTransaction()
                                            {
                                                Balance = NGF.Balance.ReAssign(tmpSenderBalance.Balance),
                                                Wallet = incomeConvertData.Sender,
                                                WitnessBlockUid = tmpSenderBalance.UID,
                                                WitnessRowNo = tmpSenderBalance.RowNo
                                            }
                                        });

                                        // transfer fee added to validator wallet

                                        tmpValidatorWalletBalance = NGF.Balance.AddVolumeWithUnlockTime(
                                            tmpValidatorWalletBalance,
                                            transferFee.ToString(),
                                            NVG.Settings.Genesis.CoinInfo.Tag,
                                            unlockTimeForNodeWallet
                                        );
                                        //tmpBlockCipherData.Out[NVG.Settings.NodeWallet.WalletKey] = tmpValidatorWalletBalance.Balance;

                                        // sender pays transfer fee
                                        (bool tmpErrorStatusForFee, NVS.WalletBalanceStruct tmpNewResultForFee) =
                                        NGF.Balance.SubtractVolumeWithUnlockTime(
                                            tmpSenderBalance,
                                            transferFee.ToString(),
                                            NVG.Settings.Genesis.CoinInfo.Tag,
                                            unlockTimeForNodeWallet
                                        );
                                        if (tmpErrorStatusForFee == true)
                                        {
                                            Console.WriteLine("Coin Needed - Main.Cs -> Line 498");
                                            Console.WriteLine("Coin Needed - Main.Cs -> Line 498");
                                            Console.ReadLine();
                                        }

                                        // sender give coin or token
                                        (bool tmpErrorStatusForTransaction, NVS.WalletBalanceStruct tmpNewResultForTransaction) =
                                        NGF.Balance.SubtractVolumeWithUnlockTime(
                                            tmpNewResultForFee,
                                            tmpTokenVolume.ToString(),
                                            tmpTokenTagStr,
                                            unlockTimeForNodeWallet
                                        );
                                        if (tmpErrorStatusForTransaction == true)
                                        {
                                            Console.WriteLine("Coin Needed - Main.Cs -> Line 498");
                                            Console.WriteLine("Coin Needed - Main.Cs -> Line 498");
                                            Console.ReadLine();
                                        }
                                        tmpBlockCipherData.Out[incomeConvertData.Sender] = tmpNewResultForTransaction.Balance;

                                        //receiver get coin or token
                                        NVS.WalletBalanceStruct tmpNewReceiverBalance = NGF.Balance.AddVolumeWithUnlockTime(
                                            tmpReceiverBalance,
                                            incomeConvertData.Volume,
                                            incomeConvertData.Currency,
                                            incomeConvertData.UnlockTime
                                        );
                                        tmpBlockCipherData.Out[incomeConvertData.Receiver] = tmpNewReceiverBalance.Balance;
                                        Console.WriteLine(JsonSerializer.Serialize(incomeConvertData,NVC.JsonSetting));
                                        Environment.Exit(0);
                                        TmpPoolRecord.data = JsonSerializer.Serialize(incomeConvertData);

                                    }

                                    /*
                                    // out işlemindeki cüzdanları kontrol ediyor...
                                    foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> tmpEntry in tmpBlockCipherData.Out)
                                    {
                                        if (TempWalletList.ContainsKey(tmpEntry.Key) == false)
                                        {
                                            bool innerAddedToList = TempWalletList.TryAdd(tmpEntry.Key, 1);
                                            if (innerAddedToList == false)
                                            {
                                                Console.WriteLine("Not Added To -> TempWalletList.TryAdd(tmpEntry.Key, 1);");
                                                addToList = false;
                                            }
                                        }
                                        else
                                        {
                                            addToList = false;
                                        }
                                    }
                                    */
                                }
                            }

                            if (addToList == true)
                            {
                                txQueue.TryDequeue(out _);
                                tempRemovePoolList.Add(tmpTxUid);
                                TempBlockList.Add(TmpPoolRecord.data);
                            }
                            else
                            {
                                /*
                                Console.WriteLine("----------------------------------------------------------");
                                Console.WriteLine(JsonSerializer.Serialize(new List<string>(txQueue)));
                                Console.WriteLine("Eklenmeyen Uid : " + tmpTxUid);
                                Console.WriteLine(JsonSerializer.Serialize(new List<string>(txQueue)));
                                */
                            }
                            //Obj_PoolTransactionList[CurrentBlockType].RemoveAt(0);
                            exitLoop = (TempBlockList.Count == NVC.BlockTransactionLimit ? true : exitLoop);
                            exitLoop = (CurrentBlockType == 240 ? true : exitLoop); // layer1 - > dosya ekleme isteği
                            exitLoop = (CurrentBlockType == 250 ? true : exitLoop); // layer3 - > dosya içeriği
                            exitLoop = (CurrentBlockType == NVE.BlockTypeList.EmptyBlock ? true : exitLoop);
                            exitLoop = (CurrentBlockType == NVE.BlockTypeList.MultiWalletCryptoTransfer ? true : exitLoop);
                        }

                        if (CurrentBlockType != TmpPoolRecord.type)
                        {
                            exitLoop = true;
                        }
                    }
                }
            }

            if (TempBlockList.Count == 0)
                return null;

            NVClass.BlockData BlockStruct = NVClass.Block.GetOrganizedEmpty(CurrentBlockType);

            string LongNonceText = string.Empty;

            BlockStruct.cipher.ver = "NE";
            BlockStruct.info.uID = (transactionId.Length == 0 ? NGF.GenerateTxUid() : transactionId);
            if (CurrentBlockType == NVE.BlockTypeList.GenesisBlock)
            {
                LongNonceText = TempBlockList[0];
                BlockStruct.info.rowNo = 1;
                BlockStruct.info.multi = false;
                BlockStruct.info.uID = NVC.GenesisBlockUid;
            }
            else
            {
                //BLOCK UNIQUE ID'Sİ BURADA EKLENİYOR....
                //BLOCK UNIQUE ID'Sİ BURADA EKLENİYOR....
                //BLOCK UNIQUE ID'Sİ BURADA EKLENİYOR....
                //BLOCK UNIQUE ID'Sİ BURADA EKLENİYOR....
                //BLOCK UNIQUE ID'Sİ BURADA EKLENİYOR....
                // buraya UTC time verisi parametre olarak gönderilecek
                // böylece blok için alınan zaman bilgisi ortak bir zaman olacak
                //NVE.BlockTypeList.CryptoTransfer

                if (CurrentBlockType == 240)
                {
                    List<string> tmpUploadStatus = JsonSerializer.Deserialize<List<string>>(TempBlockList[0]);
                    BlockStruct.info.uID = tmpUploadStatus[0];
                    TempBlockList.Clear();
                    TempBlockList.Add(tmpUploadStatus[1]);
                }

                if (CurrentBlockType == 250)
                {
                    string tmpFileName = TempBlockList[0];
                    TempBlockList.Clear();
                    TempBlockList.Add(
                        System.Convert.ToBase64String(File.ReadAllBytes(tmpFileName))
                    );
                }

                if (CurrentBlockType == NVE.BlockTypeList.LockAccount)
                {
                    string tmpLockWalletKey = TempBlockList[0];

                    NVS.LockWalletBeforeStruct? tmpLockWalletStruct = JsonSerializer.Deserialize<NVS.LockWalletBeforeStruct>(tmpLockWalletKey);
                    TempBlockList.Clear();
                    if (tmpLockWalletStruct == null)
                    {
                        TempBlockList.Add(
                            JsonSerializer.Serialize(
                                new NVS.LockWalletStruct()
                                {
                                    WalletKey = "",
                                    Balance = null,
                                    Out = null,
                                    UnlockTime = 0,
                                    PublicKey = "",
                                    Sign = "",
                                }
                            )
                        );
                    }
                    else
                    {
                        Console.WriteLine("Queue.Cs -> Line 354");
                        Console.WriteLine(tmpLockWalletStruct.WalletKey);
                        string lockAccountFee = NVG.Settings.Genesis.Fee.BlockAccount.ToString();
                        NVS.WalletBalanceStruct currentBalance =
                            NGF.Balance.Get(tmpLockWalletStruct.WalletKey, 0);
                        (bool tmpBalanceResult, NVS.WalletBalanceStruct tmpNewGeneratorBalance) =
                            NGF.Balance.SubtractVolumeWithUnlockTime(
                                NGF.Balance.Get(tmpLockWalletStruct.WalletKey, 0),
                                lockAccountFee,
                                NVG.Settings.Genesis.CoinInfo.Tag
                            );
                        if (tmpBalanceResult == false)
                        {
                            /*
                            foreach (KeyValuePair<string, Dictionary<ulong, string>> curEntry in tmpNewGeneratorBalance.Balance)
                            {
                                foreach (KeyValuePair<ulong, string> balanceEntry in curEntry.Value)
                                {
                                    if (tmpLockWalletStruct.UnlockTime > balanceEntry.Key){

                                        tmpNewGeneratorBalance.Balance[curEntry.Key]
                                    }
                                }
                            }
                            */

                            TempBlockList.Add(
                                JsonSerializer.Serialize(
                                    new NVS.LockWalletStruct()
                                    {
                                        WalletKey = tmpLockWalletStruct.WalletKey,
                                        Balance = new NVClass.WalletBalanceStructForTransaction()
                                        {
                                            Balance = NGF.Balance.ReAssign(currentBalance.Balance),
                                            Wallet = tmpLockWalletStruct.WalletKey,
                                            WitnessBlockUid = currentBalance.UID,
                                            WitnessRowNo = currentBalance.RowNo
                                        },
                                        Out = tmpNewGeneratorBalance.Balance,
                                        Fee = lockAccountFee,
                                        UnlockTime = tmpLockWalletStruct.UnlockTime,
                                        PublicKey = tmpLockWalletStruct.PublicKey,
                                        Sign = tmpLockWalletStruct.Sign
                                    }
                                )
                            );
                        }
                        else
                        {
                            Console.WriteLine("Balance result true");
                            Console.WriteLine("burada true dönüş yapınca, JSON convert işlemi hata veriyor");
                            Console.WriteLine("buraya true dönmesi durumunda bloğu oluşturmamak için kontrol eklensin");
                            Console.WriteLine("Balance result true");
                        }
                        BlockStruct.info.uID = tmpLockWalletStruct.UID;
                    }
                }

                if (CurrentBlockType == NVE.BlockTypeList.AirDrop)
                {
                    if (TempBlockList.Count > 0)
                    {
                        NVClass.BlockStruct_125 tmpBlockCipherData = new Variable.Class.BlockStruct_125()
                        {
                            //Sender=NVC.NetworkProgramWallet
                            In = new Dictionary<string, NVS.WalletBalanceStruct>(),
                            Out = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>(),
                            Validator = string.Empty
                        };

                        for (int i = 0; i < TempBlockList.Count; i++)
                        {
                            NVClass.BlockStruct_125? tmpInnerData = JsonSerializer.Deserialize<NVClass.BlockStruct_125>(TempBlockList[i]);
                            if (tmpInnerData != null)
                            {
                                foreach (var iEntry in tmpInnerData.In)
                                {
                                    tmpBlockCipherData.In.Add(iEntry.Key, iEntry.Value);
                                }
                                foreach (var iEntry in tmpInnerData.Out)
                                {
                                    tmpBlockCipherData.Out.Add(iEntry.Key, iEntry.Value);
                                }
                                tmpBlockCipherData.Validator = tmpInnerData.Validator;
                            }
                            else
                            {
                                Console.WriteLine("TempBlockList[i] IS NULL");
                                Console.WriteLine(TempBlockList[i]);
                                Console.WriteLine("TempBlockList[i] IS NULL");
                            }
                        }
                        TempBlockList.Clear();
                        TempBlockList.Add(JsonSerializer.Serialize(tmpBlockCipherData));
                    }
                }

                if (CurrentBlockType == NVE.BlockTypeList.CryptoTransfer)
                {
                    if (TempBlockList.Count > 1)
                    {
                        NVClass.BlockStruct_120 tmpBlockCipherData = new Variable.Class.BlockStruct_120()
                        {
                            In = new Dictionary<string, Variable.Class.BlockStruct_120_In_Struct>(),
                            Out = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>(),
                            Validator = new Variable.Struct.ValidatorStruct()
                        };

                        bool validatorAssigned = false;
                        for (int i = 0; i < TempBlockList.Count; i++)
                        {
                            NVClass.BlockStruct_120? tmpInnerData = JsonSerializer.Deserialize<NVClass.BlockStruct_120>(TempBlockList[i]);
                            if (tmpInnerData != null)
                            {
                                if (validatorAssigned == false)
                                {
                                    tmpBlockCipherData.Validator = tmpInnerData.Validator;
                                    validatorAssigned = true;
                                }
                                else
                                {
                                    BigInteger tmpFee =
                                        BigInteger.Parse(tmpBlockCipherData.Validator.Reward)
                                        +
                                        BigInteger.Parse(tmpInnerData.Validator.Reward);
                                    tmpBlockCipherData.Validator.Reward = tmpFee.ToString();
                                }

                                foreach (KeyValuePair<string, Variable.Class.BlockStruct_120_In_Struct> iEntry in tmpInnerData.In)
                                {
                                    tmpBlockCipherData.In.Add(iEntry.Key, iEntry.Value);
                                }
                                foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> iEntry in tmpInnerData.Out)
                                {
                                    tmpBlockCipherData.Out.Add(iEntry.Key, iEntry.Value);
                                }
                            }
                            else
                            {
                                Console.WriteLine("TempBlockList[i] IS NULL");
                                Console.WriteLine(TempBlockList[i]);
                                Console.WriteLine("TempBlockList[i] IS NULL");
                            }
                        }
                        TempBlockList.Clear();
                        TempBlockList.Add(JsonSerializer.Serialize(tmpBlockCipherData));
                    }
                }
                LongNonceText = string.Join(NVC.Delimeter, TempBlockList.ToArray());
            }

            BlockStruct.prev = "";
            BlockStruct.info.prevList.Clear();

            BlockStruct.info.time = Notus.Block.Key.GetTimeFromKey(BlockStruct.info.uID, true);

            BlockStruct.info.type = CurrentBlockType;
            BlockStruct.cipher.data = System.Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes(
                    LongNonceText
                )
            );

            //burası pooldaki kayıtların fazla birikmesi ve para transferi işlemlerinin key'lerinin örtüşmemesinden
            //dolayı eklendi
            return BlockStruct;
        }

        public void RemoveTempPoolList()
        {
            for (int i = 0; i < tempRemovePoolList.Count; i++)
            {
                txQueueList.TryRemove(tempRemovePoolList[i], out _);
                kvPoolDb.Remove(tempRemovePoolList[i]);
            }
            tempRemovePoolList.Clear();
        }
        public void ReloadPoolList()
        {
            Console.WriteLine(JsonSerializer.Serialize(tempRemovePoolList));
            for (int i = 0; i < tempRemovePoolList.Count; i++)
            {
                txQueue.Enqueue(tempRemovePoolList[i]);
            }
            tempRemovePoolList.Clear();
            //LoadFromPoolDb();
        }
        public NVClass.BlockData? ReadFromChain(string BlockId)
        {
            //tgz-exception
            return BS_Storage.ReadBlock(BlockId);
        }
        //yeni blok hesaplanması tamamlandığı zaman buraya gelecek ve geçerli blok ise eklenecek.
        public void AddToChain(NVClass.BlockData NewBlock)
        {
            /*
            if (NewBlock.prev.Length < 20)
            {
                NP.Info("Block Added To Chain -> " +
                    NewBlock.info.rowNo.ToString() + " -> " +
                    "Prev is Empty [ " + NewBlock.prev + " ]"
                );
            }
            else
            {
                NP.Info("Block Added To Chain -> " + 
                    NewBlock.info.rowNo.ToString() + " -> " + 
                    NewBlock.prev.Substring(0,20)
                );
            }
            */
            BS_Storage.AddSync(NewBlock);

            string rawDataStr = NTT.RawCipherData2String(
                NewBlock.cipher.data
            );

            string RemoveKeyStr = string.Empty;
            if (NewBlock.info.type == NVE.BlockTypeList.AirDrop)
            {
                NVClass.BlockStruct_125? tmpLockBalance = NBD.Convert_125(rawDataStr, false);
                if (tmpLockBalance != null)
                {
                    foreach (var entry in tmpLockBalance.In)
                    {
                        NVG.Settings.TxStatus.Set(entry.Key, new NVS.CryptoTransferStatus()
                        {
                            Code = NVE.BlockStatusCode.Completed,
                            RowNo = NewBlock.info.rowNo,
                            UID = NewBlock.info.uID,
                            Text = "Completed"
                        });
                    }
                }
            }

            if (NewBlock.info.type == 40)
            {
                NVS.LockWalletStruct? tmpTransferResult =
                    JsonSerializer.Deserialize<NVS.LockWalletStruct>(rawDataStr);
                if (tmpTransferResult != null)
                {
                    RemoveKeyStr = Notus.Toolbox.Text.ToHex("lock-" + tmpTransferResult.WalletKey);
                }
            }
            else
            {
            }
        }
        private Dictionary<string, Dictionary<ulong, string>> GetWalletBalanceDictionary(string WalletKey, ulong timeYouCanUse)
        {
            NVS.WalletBalanceStruct tmpWalletBalanceObj = NGF.Balance.Get(WalletKey, timeYouCanUse);
            return tmpWalletBalanceObj.Balance;
        }

        public bool Add(NVS.PoolBlockRecordStruct PreBlockData)
        {
            PreBlockData.uid = (PreBlockData.uid == null ? NGF.GenerateTxUid() : PreBlockData.uid);
            PreBlockData.uid = (PreBlockData.uid.Length == 0 ? NGF.GenerateTxUid() : PreBlockData.uid);

            kvPoolDb.Set(PreBlockData.uid, JsonSerializer.Serialize(PreBlockData));
            Add2Queue(PreBlockData);
            return true;

            /*
            // eğer bu blok tipi veya id'si daha önceden eklendiyse uid kabul edilmesin
            if (NVG.Settings.UidTypeList.Type(PreBlockData.uid) != NVE.UidTypeCode.Unknown)
            {
                return false;
            }
            string keyStr = PreBlockData.uid;
            if (PreBlockData.type == 40)
            {
                NVS.LockWalletBeforeStruct? tmpLockWalletData = JsonSerializer.Deserialize<NVS.LockWalletBeforeStruct>(PreBlockData.data);
                if (tmpLockWalletData != null)
                {
                    keyStr = Notus.Toolbox.Text.ToHex("lock-" + tmpLockWalletData.WalletKey);
                }
                else
                {
                    keyStr = "";
                }
            }
            */
        }

        private void Add2Queue(NVS.PoolBlockRecordStruct PreBlockData)
        {
            if (txQueueList.TryAdd(PreBlockData.uid, 1) == true)
            {
                txQueue.Enqueue(PreBlockData.uid);
            }
            else
            {
                NP.Danger("txQueueList.TryAdd(PreBlockData.uid, 1) == FALSE");
            }
        }
        public void LoadFromPoolDb()
        {
            kvPoolDb.Each((string blockTransactionKey, string TextBlockDataString) =>
            {
                if (txQueueList.ContainsKey(blockTransactionKey) == false)
                {
                    NVS.PoolBlockRecordStruct? PreBlockData =
                        JsonSerializer.Deserialize<NVS.PoolBlockRecordStruct>(TextBlockDataString);
                    if (PreBlockData != null)
                    {
                        Add2Queue(PreBlockData);
                    }
                }
            });
        }
        public void Start()
        {
            NP.Info("Pool Loaded From Local DB");
            BS_Storage = new Notus.Block.Storage(false);
            BS_Storage.Start();
            txQueueList.Clear();

            kvPoolDb.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 1000,
                Name = "new_block"
            });

            kvPoolTxErrorList.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 1000,
                Name = "wrong_tx"
            });
            LoadFromPoolDb();
        }
        public void Reset()
        {
            Notus.Archive.ClearBlocks(NVG.Settings);
            tempRemovePoolList.Clear();
            kvPoolDb.Clear();
            kvPoolTxErrorList.Clear();
            txQueue.Clear();
            //Obj_PoolTransactionList.Clear();
        }
        public Queue()
        {
            //Obj_PoolTransactionList.Clear();
        }
        ~Queue()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                kvPoolDb.Dispose();
            }
            catch { }
            try
            {
                kvPoolTxErrorList.Dispose();
            }
            catch { }
        }
    }
}
