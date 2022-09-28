﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Threading;

namespace Notus.Validator
{
    public class Main : IDisposable
    {
        private Notus.Variable.Common.ClassSetting Obj_Settings;
        public Notus.Variable.Common.ClassSetting Settings
        {
            get { return Obj_Settings; }
            set { Obj_Settings = value; }
        }

        private bool MyReadyMessageSended = false;
        private bool FirstSyncIsDone = false;
        //this variable hold current processing block number
        private long CurrentBlockRowNo = 1;
        private int SelectedPortVal = 0;

        //bu nesnenin görevi network'e bağlı nodeların listesini senkronize etmek
        //private Notus.Network.Controller ControllerObj = new Notus.Network.Controller();
        private Notus.Reward.Block RewardBlockObj = new Notus.Reward.Block();
        private Notus.Communication.Http HttpObj = new Notus.Communication.Http(true);
        private Notus.Block.Integrity Obj_Integrity;
        private Notus.Validator.Api Obj_Api;
        private Notus.TGZArchiver ArchiverObj;
        //private Notus.Cache.Main Obj_MainCache;
        //private Notus.Token.Storage Obj_TokenStorage;

        //blok durumlarını tutan değişken

        private Dictionary<string, Notus.Variable.Struct.BlockStatus> Obj_BlockStatusList = new Dictionary<string, Notus.Variable.Struct.BlockStatus>();
        public Dictionary<string, Notus.Variable.Struct.BlockStatus> BlockStatusList
        {
            get { return BlockStatusList; }
        }

        private bool CryptoTransferTimerIsRunning = false;
        private DateTime CryptoTransferTime = DateTime.Now;

        private bool EmptyBlockNotMyTurnPrinted = false;
        private bool EmptyBlockTimerIsRunning = false;
        private DateTime EmptyBlockGeneratedTime = new DateTime(2000, 01, 1, 0, 00, 00);

        private bool FileStorageTimerIsRunning = false;
        private DateTime FileStorageTime = DateTime.Now;

        //bu liste diğer nodelardan gelen yeni blokları tutan liste
        public SortedDictionary<long, Notus.Variable.Class.BlockData> IncomeBlockList = new SortedDictionary<long, Notus.Variable.Class.BlockData>();
        //public ConcurrentQueue<Notus.Variable.Class.BlockData> IncomeBlockList = new ConcurrentQueue<Notus.Variable.Class.BlockData>();
        private Notus.Block.Queue Obj_BlockQueue = new Notus.Block.Queue();
        private Notus.Validator.Queue ValidatorQueueObj = new Notus.Validator.Queue();

        //private System.Action<string, Notus.Variable.Class.BlockData> OnReadFromChainFuncObj = null;
        public void EmptyBlockTimerFunc()
        {
            Notus.Print.Basic(Obj_Settings, "Empty Block Timer Has Started");
            Notus.Threads.Timer TimerObj = new Notus.Threads.Timer(1000);
            TimerObj.Start(() =>
            {
                if (ValidatorQueueObj.MyTurn == true && EmptyBlockTimerIsRunning == false)
                {
                    EmptyBlockTimerIsRunning = true;
                    int howManySeconds = Obj_Settings.Genesis.Empty.Interval.Time;

                    if (Obj_Settings.Genesis.Empty.SlowBlock.Count >= Obj_Integrity.EmptyBlockCount)
                    {
                        howManySeconds = (
                            Obj_Settings.Genesis.Empty.Interval.Time
                                *
                            Obj_Settings.Genesis.Empty.SlowBlock.Multiply
                        );
                    }

                    //blok zamanı ve utc zamanı çakışıyor
                    DateTime tmpLastTime = Notus.Date.ToDateTime(
                        Obj_Settings.LastBlock.info.time
                    ).AddSeconds(howManySeconds);

                    // get utc time from validatır Queue
                    DateTime utcTime = ValidatorQueueObj.GetUtcTime();
                    if (utcTime > tmpLastTime)
                    {
                        if (ValidatorQueueObj.MyTurn)
                        {
                            if ((DateTime.Now - EmptyBlockGeneratedTime).TotalSeconds > 30)
                            {
                                //Console.WriteLine((DateTime.Now - EmptyBlockGeneratedTime).TotalSeconds);
                                EmptyBlockGeneratedTime = DateTime.Now;
                                Notus.Print.Success(Obj_Settings, "Empty Block Executed");
                                Obj_BlockQueue.AddEmptyBlock();
                            }
                            EmptyBlockNotMyTurnPrinted = false;
                        }
                        else
                        {
                            if (EmptyBlockNotMyTurnPrinted == false)
                            {
                                //Notus.Print.Warning(Obj_Settings, "Not My Turn For Empty Block");
                                EmptyBlockNotMyTurnPrinted = true;
                            }
                        }
                        EmptyBlockTimerIsRunning = false;
                    }
                }
            }, true);
        }
        public void FileStorageTimer()
        {
            Notus.Print.Basic(Obj_Settings, "File Storage Timer Has Started");

            Notus.Threads.Timer TimerObj = new Notus.Threads.Timer(2000);
            TimerObj.Start(() =>
            {
                if (FileStorageTimerIsRunning == false)
                {
                    FileStorageTimerIsRunning = true;
                    using (Notus.Mempool ObjMp_FileStatus =
                        new Notus.Mempool(
                            Notus.IO.GetFolderName(
                                Obj_Settings.Network,
                                Obj_Settings.Layer,
                                Notus.Variable.Constant.StorageFolderName.File) + "upload_list_status"
                        )
                    )
                    {
                        ObjMp_FileStatus.AsyncActive = false;
                        ObjMp_FileStatus.Each((string tmpStorageId, string rawStatusStr) =>
                        {
                            Notus.Variable.Enum.BlockStatusCode tmpDataStatus = JsonSerializer.Deserialize<Notus.Variable.Enum.BlockStatusCode>(rawStatusStr);
                            if (tmpDataStatus == Notus.Variable.Enum.BlockStatusCode.Pending)
                            {
                                using (Notus.Mempool ObjMp_FileList =
                                    new Notus.Mempool(
                                        Notus.IO.GetFolderName(
                                            Obj_Settings.Network,
                                            Obj_Settings.Layer,
                                            Notus.Variable.Constant.StorageFolderName.File) + "upload_list"
                                    )
                                )
                                {

                                    string tmpStorageStructStr = ObjMp_FileList.Get(tmpStorageId, "");
                                    Notus.Variable.Struct.FileTransferStruct tmpFileObj = JsonSerializer.Deserialize<Notus.Variable.Struct.FileTransferStruct>(tmpStorageStructStr);

                                    string tmpCurrentList = ObjMp_FileList.Get(tmpStorageId + "_chunk", "");
                                    //try
                                    //{
                                    string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpFileObj.PublicKey, Obj_Settings.Network);
                                    string tmpOutputFolder = Notus.IO.GetFolderName(
                                        Obj_Settings.Network,
                                        Obj_Settings.Layer,
                                        Notus.Variable.Constant.StorageFolderName.Storage
                                    ) + tmpWalletKey + System.IO.Path.DirectorySeparatorChar +
                                    tmpStorageId + System.IO.Path.DirectorySeparatorChar;
                                    Notus.IO.CreateDirectory(tmpOutputFolder);
                                    string outputFileName = tmpOutputFolder + tmpFileObj.FileName;
                                    using (FileStream fs = new FileStream(outputFileName, FileMode.Create, FileAccess.ReadWrite))
                                    {
                                        Dictionary<int, string> tmpChunkList = JsonSerializer.Deserialize<Dictionary<int, string>>(tmpCurrentList);
                                        foreach (KeyValuePair<int, string> entry in tmpChunkList)
                                        {
                                            string tmpChunkIdKey = entry.Value;
                                            int tmpStorageNo = Notus.Block.Key.CalculateStorageNumber(
                                                Notus.Convert.Hex2BigInteger(tmpChunkIdKey).ToString()
                                            );
                                            using (Notus.Mempool ObjMp_FileChunkList =
                                                new Notus.Mempool(
                                                    Notus.IO.GetFolderName(
                                                        Obj_Settings.Network,
                                                        Obj_Settings.Layer,
                                                        Notus.Variable.Constant.StorageFolderName.File) + "chunk_list_" + tmpStorageNo.ToString()
                                                )
                                            )
                                            {
                                                ObjMp_FileChunkList.AsyncActive = false;
                                                string tmpRawDataStr = ObjMp_FileChunkList.Get(tmpChunkIdKey);
                                                byte[] tmpByteBuffer = System.Convert.FromBase64String(System.Uri.UnescapeDataString(tmpRawDataStr));
                                                fs.Write(tmpByteBuffer, 0, tmpByteBuffer.Length);
                                            }
                                        }
                                        fs.Close();
                                    }

                                    Obj_BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
                                    {
                                        type = 250,
                                        data = outputFileName
                                    });

                                    ObjMp_FileStatus.Set(tmpStorageId, JsonSerializer.Serialize(Notus.Variable.Enum.BlockStatusCode.InProgress));
                                    try
                                    {
                                        File.Delete(outputFileName);
                                    }
                                    catch (Exception err3)
                                    {
                                        Notus.Print.Log(
                                            Notus.Variable.Enum.LogLevel.Info,
                                            88008800,
                                            err3.Message,
                                            "BlockRowNo",
                                            null,
                                            err3
                                        );
                                        Notus.Print.Danger(Obj_Settings, "Error Text : [9abc546ac] : " + err3.Message);
                                    }
                                    //}
                                    //catch (Exception err)
                                    //{
                                    //Console.WriteLine("Notus.Node.Validator.Main -> Convertion Error - Line 271");
                                    //Console.WriteLine(err.Message);
                                    //Console.WriteLine("Notus.Node.Validator.Main -> Convertion Error - Line 271");
                                    //}
                                }
                            }
                        }, 0);
                    }
                    FileStorageTimerIsRunning = false;
                }
            }, true);
        }
        private Dictionary<string, Dictionary<ulong, string>> GetWalletBalanceDictionary(string WalletKey, ulong timeYouCanUse)
        {
            Notus.Variable.Struct.WalletBalanceStruct tmpWalletBalanceObj = Obj_Api.BalanceObj.Get(WalletKey, timeYouCanUse);
            return tmpWalletBalanceObj.Balance;
        }

        public void CryptoTransferTimerFunc()
        {
            Notus.Print.Success(Obj_Settings, "Crypto Transfer Timer Has Started");
            Notus.Threads.Timer TimerObj = new Notus.Threads.Timer(1000);
            TimerObj.Start(() =>
            {
                if (CryptoTransferTimerIsRunning == false)
                {
                    CryptoTransferTimerIsRunning = true;
                    int tmpRequestSend_ListCount = Obj_Api.RequestSend_ListCount();
                    if (tmpRequestSend_ListCount > 0)
                    {
                        ulong unlockTimeForNodeWallet = Notus.Time.NowToUlong();
                        Notus.Variable.Struct.WalletBalanceStruct tmpValidatorWalletBalance = Obj_Api.BalanceObj.Get(Obj_Settings.NodeWallet.WalletKey, unlockTimeForNodeWallet);
                        List<string> tmpWalletList = new List<string>() { };
                        tmpWalletList.Clear();

                        List<string> tmpKeyList = new List<string>();
                        tmpKeyList.Clear();
                        BigInteger totalBlockReward = 0;

                        Notus.Variable.Class.BlockStruct_120 tmpBlockCipherData = new Notus.Variable.Class.BlockStruct_120()
                        {
                            In = new Dictionary<string, Notus.Variable.Class.BlockStruct_120_In_Struct>(),
                            //                  who                 coin               time   volume
                            Out = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>(),
                            Validator = new Notus.Variable.Struct.ValidatorStruct()
                            {
                                NodeWallet = Obj_Settings.NodeWallet.WalletKey,
                                Reward = totalBlockReward.ToString()
                            }
                        };

                        Dictionary<string, Notus.Variable.Struct.MempoolDataList> tmpTransactionList = Obj_Api.RequestSend_DataList();

                        // wallet balances are assigned
                        Int64 transferFee = Notus.Wallet.Fee.Calculate(
                            Notus.Variable.Enum.Fee.CryptoTransfer,
                            Obj_Settings.Network,
                            Obj_Settings.Layer
                        );
                        ulong transactionCount = 0;
                        foreach (KeyValuePair<string, Notus.Variable.Struct.MempoolDataList> entry in tmpTransactionList)
                        {
                            Notus.Variable.Struct.CryptoTransactionStoreStruct? tmpObjPoolCrypto = JsonSerializer.Deserialize<Notus.Variable.Struct.CryptoTransactionStoreStruct>(entry.Value.Data);
                            if (tmpObjPoolCrypto != null)
                            {
                                bool thisRecordCanBeAdded = false;
                                bool senderAvailable = Obj_Api.BalanceObj.WalletUsageAvailable(tmpObjPoolCrypto.Sender);
                                if (senderAvailable == true)
                                {
                                    bool receiverAvailable = Obj_Api.BalanceObj.WalletUsageAvailable(tmpObjPoolCrypto.Receiver);
                                    if (receiverAvailable == true)
                                    {
                                        bool senderLocked = Obj_Api.BalanceObj.StartWalletUsage(tmpObjPoolCrypto.Sender);
                                        if (senderLocked == true)
                                        {
                                            bool receiverLocked = Obj_Api.BalanceObj.StartWalletUsage(tmpObjPoolCrypto.Receiver);
                                            if (receiverLocked == true)
                                            {
                                                thisRecordCanBeAdded = true;
                                            }
                                        }
                                    }
                                }

                                if (thisRecordCanBeAdded == true)
                                {
                                    bool walletHaveEnoughCoinOrToken = true;
                                    Obj_Api.BalanceObj.StartWalletUsage(tmpObjPoolCrypto.Sender);
                                    Obj_Api.BalanceObj.StartWalletUsage(tmpObjPoolCrypto.Receiver);

                                    bool senderExist = tmpWalletList.IndexOf(tmpObjPoolCrypto.Sender) >= 0 ? true : false;
                                    bool receiverExist = tmpWalletList.IndexOf(tmpObjPoolCrypto.Receiver) >= 0 ? true : false;
                                    if (senderExist == false && receiverExist == false)
                                    {
                                        tmpWalletList.Add(tmpObjPoolCrypto.Sender);
                                        tmpWalletList.Add(tmpObjPoolCrypto.Receiver);

                                        Notus.Variable.Struct.WalletBalanceStruct tmpSenderBalance = Obj_Api.BalanceObj.Get(tmpObjPoolCrypto.Sender, unlockTimeForNodeWallet);
                                        Notus.Variable.Struct.WalletBalanceStruct tmpReceiverBalance = Obj_Api.BalanceObj.Get(tmpObjPoolCrypto.Receiver, unlockTimeForNodeWallet);
                                        string tmpTokenTagStr = "";
                                        BigInteger tmpTokenVolume = 0;

                                        if (string.Equals(tmpObjPoolCrypto.Currency, Obj_Settings.Genesis.CoinInfo.Tag))
                                        {
                                            tmpTokenTagStr = Obj_Settings.Genesis.CoinInfo.Tag;
                                            BigInteger WalletBalanceInt = Obj_Api.BalanceObj.GetCoinBalance(tmpSenderBalance, tmpTokenTagStr);
                                            BigInteger RequiredBalanceInt = BigInteger.Parse(tmpObjPoolCrypto.Volume);
                                            tmpTokenVolume = RequiredBalanceInt;
                                            if ((RequiredBalanceInt + transferFee) > WalletBalanceInt)
                                            {
                                                walletHaveEnoughCoinOrToken = false;
                                            }
                                        }
                                        else
                                        {
                                            if (tmpSenderBalance.Balance.ContainsKey(Obj_Settings.Genesis.CoinInfo.Tag) == false)
                                            {
                                                walletHaveEnoughCoinOrToken = false;
                                            }
                                            else
                                            {
                                                BigInteger coinFeeBalance = Obj_Api.BalanceObj.GetCoinBalance(tmpSenderBalance, Obj_Settings.Genesis.CoinInfo.Tag);
                                                if (transferFee > coinFeeBalance)
                                                {
                                                    walletHaveEnoughCoinOrToken = false;
                                                }
                                                else
                                                {
                                                    BigInteger tokenCurrentBalance = Obj_Api.BalanceObj.GetCoinBalance(tmpSenderBalance, tmpObjPoolCrypto.Currency);
                                                    BigInteger RequiredBalanceInt = BigInteger.Parse(tmpObjPoolCrypto.Volume);
                                                    if (RequiredBalanceInt > tokenCurrentBalance)
                                                    {
                                                        walletHaveEnoughCoinOrToken = false;
                                                    }
                                                    else
                                                    {
                                                        tmpTokenTagStr = tmpObjPoolCrypto.Currency;
                                                        tmpTokenVolume = RequiredBalanceInt;
                                                    }
                                                }
                                            }
                                        }


                                        if (walletHaveEnoughCoinOrToken == false)
                                        {
                                            Obj_Api.RequestSend_Remove(entry.Key);
                                            Obj_Api.CryptoTranStatus.Set(entry.Key, JsonSerializer.Serialize(
                                                new Notus.Variable.Struct.CryptoTransferStatus()
                                                {
                                                    Code = Notus.Variable.Enum.BlockStatusCode.Rejected,
                                                    RowNo = 0,
                                                    UID = "",
                                                    Text = "Rejected"
                                                }
                                            ));
                                        }
                                        else
                                        {
                                            totalBlockReward = totalBlockReward + transferFee;
                                            transactionCount++;
                                            if (tmpBlockCipherData.Out.ContainsKey(tmpObjPoolCrypto.Sender) == false)
                                            {
                                                tmpBlockCipherData.Out.Add(tmpObjPoolCrypto.Sender, GetWalletBalanceDictionary(tmpObjPoolCrypto.Sender, unlockTimeForNodeWallet));
                                            }
                                            if (tmpBlockCipherData.Out.ContainsKey(tmpObjPoolCrypto.Receiver) == false)
                                            {
                                                tmpBlockCipherData.Out.Add(tmpObjPoolCrypto.Receiver, GetWalletBalanceDictionary(tmpObjPoolCrypto.Receiver, unlockTimeForNodeWallet));
                                            }

                                            tmpBlockCipherData.In.Add(entry.Key, new Notus.Variable.Class.BlockStruct_120_In_Struct()
                                            {
                                                Fee = tmpObjPoolCrypto.Fee,
                                                PublicKey = tmpObjPoolCrypto.PublicKey,
                                                Sign = tmpObjPoolCrypto.Sign,
                                                CurrentTime = tmpObjPoolCrypto.CurrentTime,
                                                Volume = tmpObjPoolCrypto.Volume,
                                                Currency = tmpObjPoolCrypto.Currency,
                                                Receiver = new Notus.Variable.Class.WalletBalanceStructForTransaction()
                                                {
                                                    Balance = Obj_Api.BalanceObj.ReAssign(tmpReceiverBalance.Balance),
                                                    Wallet = tmpObjPoolCrypto.Receiver,
                                                    WitnessBlockUid = tmpReceiverBalance.UID,
                                                    WitnessRowNo = tmpReceiverBalance.RowNo
                                                },
                                                Sender = new Notus.Variable.Class.WalletBalanceStructForTransaction()
                                                {
                                                    Balance = Obj_Api.BalanceObj.ReAssign(tmpSenderBalance.Balance),
                                                    Wallet = tmpObjPoolCrypto.Sender,
                                                    WitnessBlockUid = tmpSenderBalance.UID,
                                                    WitnessRowNo = tmpSenderBalance.RowNo
                                                }
                                            });

                                            // transfer fee added to validator wallet

                                            tmpValidatorWalletBalance = Obj_Api.BalanceObj.AddVolumeWithUnlockTime(
                                                tmpValidatorWalletBalance,
                                                transferFee.ToString(),
                                                Obj_Settings.Genesis.CoinInfo.Tag,
                                                unlockTimeForNodeWallet
                                            );
                                            //tmpBlockCipherData.Out[Obj_Settings.NodeWallet.WalletKey] = tmpValidatorWalletBalance.Balance;

                                            // sender pays transfer fee
                                            (bool tmpErrorStatusForFee, Notus.Variable.Struct.WalletBalanceStruct tmpNewResultForFee) =
                                            Obj_Api.BalanceObj.SubtractVolumeWithUnlockTime(
                                                tmpSenderBalance,
                                                transferFee.ToString(),
                                                Obj_Settings.Genesis.CoinInfo.Tag,
                                                unlockTimeForNodeWallet
                                            );
                                            if (tmpErrorStatusForFee == true)
                                            {
                                                Console.WriteLine("Coin Needed - Main.Cs -> Line 498");
                                                Console.WriteLine("Coin Needed - Main.Cs -> Line 498");
                                                Console.ReadLine();
                                            }

                                            // sender give coin or token
                                            (bool tmpErrorStatusForTransaction, Notus.Variable.Struct.WalletBalanceStruct tmpNewResultForTransaction) =
                                            Obj_Api.BalanceObj.SubtractVolumeWithUnlockTime(
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
                                            tmpBlockCipherData.Out[tmpObjPoolCrypto.Sender] = tmpNewResultForTransaction.Balance;

                                            //receiver get coin or token
                                            Notus.Variable.Struct.WalletBalanceStruct tmpNewReceiverBalance = Obj_Api.BalanceObj.AddVolumeWithUnlockTime(
                                                tmpReceiverBalance,
                                                tmpObjPoolCrypto.Volume,
                                                tmpObjPoolCrypto.Currency,
                                                tmpObjPoolCrypto.UnlockTime
                                            );
                                            tmpBlockCipherData.Out[tmpObjPoolCrypto.Receiver] = tmpNewReceiverBalance.Balance;
                                        }
                                    }
                                }
                            }
                        }


                        if (transactionCount > 0)
                        {
                            foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> walletEntry in tmpBlockCipherData.Out)
                            {
                                foreach (KeyValuePair<string, Dictionary<ulong, string>> currencyEntry in walletEntry.Value)
                                {
                                    List<ulong> tmpRemoveList = new List<ulong>();
                                    foreach (KeyValuePair<ulong, string> balanceEntry in currencyEntry.Value)
                                    {
                                        if (balanceEntry.Value == "0")
                                        {
                                            tmpRemoveList.Add(balanceEntry.Key);
                                        }
                                    }
                                    for (int innerForCount = 0; innerForCount < tmpRemoveList.Count; innerForCount++)
                                    {
                                        tmpBlockCipherData.Out[walletEntry.Key][currencyEntry.Key].Remove(tmpRemoveList[innerForCount]);
                                    }
                                }
                            }
                            tmpBlockCipherData.Validator.Reward = totalBlockReward.ToString();
                            
                            // crypto / token transfer
                            Obj_BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
                            {
                                // type = 120,
                                // uid =
                                type = Notus.Variable.Enum.BlockTypeList.CryptoTransfer,
                                data = JsonSerializer.Serialize(tmpBlockCipherData)
                            });
                        }
                        foreach (KeyValuePair<string, Notus.Variable.Class.BlockStruct_120_In_Struct> entry in tmpBlockCipherData.In)
                        {
                            Obj_Api.RequestSend_Remove(entry.Key);
                        }
                    }  //if (ObjMp_CryptoTransfer.Count() > 0)

                    CryptoTransferTime = DateTime.Now;
                    CryptoTransferTimerIsRunning = false;

                }  //if (CryptoTransferTimerIsRunning == false)
            }, true);  //TimerObj.Start(() =>
        }
        private void SetTimeStatusForBeginSync(bool status)
        {
            if (Obj_Settings.GenesisCreated == false)
            {
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                {
                    EmptyBlockTimerIsRunning = status;
                    CryptoTransferTimerIsRunning = status;
                }
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer2)
                {
                }
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer3)
                {
                }
            }
        }
        private void WaitUntilEnoughNode()
        {
            if (Obj_Settings.GenesisCreated == false)
            {
                SetTimeStatusForBeginSync(true);        // stop timer
                while (ValidatorQueueObj.WaitForEnoughNode == true)
                {
                    Thread.Sleep(1);
                }
                SetTimeStatusForBeginSync(false);       // release timer
            }
        }
        public void Start()
        {
            Obj_Integrity = new Notus.Block.Integrity();
            Obj_Integrity.Settings = Obj_Settings;
            Obj_Integrity.ControlGenesisBlock(); // we check and compare genesis with onther node
            Obj_Integrity.GetLastBlock();        // get last block from current node

            Obj_Settings.GenesisCreated = Obj_Integrity.Settings.GenesisCreated;
            Obj_Settings.LastBlock = Obj_Integrity.Settings.LastBlock;
            Obj_Settings.Genesis = Obj_Integrity.Settings.Genesis;

            if (Obj_Settings.Genesis == null)
            {
                Notus.Print.Basic(Obj_Settings, "Notus.Validator.Main -> Genesis Is NULL");
            }
            ArchiverObj = new TGZArchiver(Obj_Settings);

            Obj_Api = new Notus.Validator.Api();
            Obj_Api.Settings = Obj_Settings;

            Obj_BlockQueue.Settings = Obj_Settings;
            Obj_BlockQueue.Start();

            Obj_Api.Func_GetPoolList = blockTypeNo =>
            {
                return Obj_BlockQueue.GetPoolList(blockTypeNo);
            };
            Obj_Api.Func_GetPoolCount = () =>
            {
                return Obj_BlockQueue.GetPoolCount();
            };

            Obj_Api.Func_OnReadFromChain = blockKeyIdStr =>
            {
                Notus.Variable.Class.BlockData? tmpBlockResult = Obj_BlockQueue.ReadFromChain(blockKeyIdStr);
                if (tmpBlockResult != null)
                {
                    return tmpBlockResult;
                }
                return null;
            };
            Obj_Api.Func_AddToChainPool = blockStructForQueue =>
            {
                return Obj_BlockQueue.Add(blockStructForQueue);
            };
            Obj_Api.Prepare();

            //Obj_MainCache = new Notus.Cache.Main();
            //Obj_MainCache.Settings = Obj_Settings;
            //Obj_MainCache.Start();
            // Obj_TokenStorage = new Notus.Token.Storage();
            // Obj_TokenStorage.Settings = Obj_Settings;

            if (Obj_Settings.GenesisCreated == false && Obj_Settings.Genesis != null)
            {
                Notus.Print.Basic(Obj_Settings, "Last Block Row No : " + Obj_Settings.LastBlock.info.rowNo.ToString());
                using (Notus.Mempool ObjMp_BlockOrder =
                    new Notus.Mempool(
                        Notus.IO.GetFolderName(Obj_Settings.Network, Obj_Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) +
                        "block_order_list"
                    )
                )
                {
                    ObjMp_BlockOrder.Each((string blockUniqueId, string BlockText) =>
                    {
                        KeyValuePair<string, string> BlockOrder = JsonSerializer.Deserialize<KeyValuePair<string, string>>(BlockText);
                        if (string.Equals(new Notus.Hash().CommonSign("sha1", BlockOrder.Key + Obj_Settings.HashSalt), BlockOrder.Value))
                        {
                            using (Notus.Block.Storage Obj_Storage = new Notus.Block.Storage(false))
                            {
                                Obj_Storage.Network = Obj_Settings.Network;
                                Obj_Storage.Layer = Obj_Settings.Layer;
                                Notus.Variable.Class.BlockData? tmpBlockData = Obj_Storage.ReadBlock(BlockOrder.Key);
                                if (tmpBlockData != null)
                                {
                                    ProcessBlock(tmpBlockData, 1);
                                }
                                else
                                {
                                    Notus.Print.Danger(Obj_Settings, "Notus.Block.Integrity -> Block Does Not Exist");
                                    Notus.Print.Danger(Obj_Settings, "Reset Block");
                                    Notus.Print.ReadLine(Obj_Settings);
                                }
                            }
                        }
                        else
                        {
                            Notus.Print.Danger(Obj_Settings, "Hash calculation error");
                        }
                    }, 0
                    );
                    Notus.Print.Info(Obj_Settings, "All Blocks Loaded");
                }
                SelectedPortVal = Notus.Toolbox.Network.GetNetworkPort(Obj_Settings);
            }
            else
            {
                SelectedPortVal = Notus.Toolbox.Network.FindFreeTcpPort();
            }

            HttpObj.DefaultResult_OK = "null";
            HttpObj.DefaultResult_ERR = "null";
            //Notus.Print.Basic(Settings.InfoMode,"empty count : " + Obj_Integrity.EmptyBlockCount);
            if (Obj_Settings.GenesisCreated == false)
            {
                Notus.Print.Basic(Obj_Settings, "Main Validator Started");
            }
            Obj_BlockQueue.Settings = Obj_Settings;
            //BlockStatObj = Obj_BlockQueue.CurrentBlockStatus();
            Start_HttpListener();
            ValidatorQueueObj.Settings = Obj_Settings;

            // her gelen blok bir listeye eklenmeli ve o liste ile sıra ile eklenmeli
            ValidatorQueueObj.Func_NewBlockIncome = tmpNewBlockIncome =>
            {
                ProcessBlock(tmpNewBlockIncome, 2);
                //Notus.Print.Info(Obj_Settings, "Arrived New Block : " + tmpNewBlockIncome.info.uID);
                return true;
            };

            ValidatorQueueObj.GetUtcTimeFromServer();
            if (Obj_Settings.GenesisCreated == false)
            {
                //Console.ReadLine();

                ValidatorQueueObj.PreStart(
                    Obj_Settings.LastBlock.info.rowNo,
                    Obj_Settings.LastBlock.info.uID,
                    Obj_Settings.LastBlock.sign,
                    Obj_Settings.LastBlock.prev
                );

                ValidatorQueueObj.PingOtherNodes();
                //burada ping ve pong yaparak bekleyecek
            }

            ValidatorQueueObj.Start();


            // kontrol noktası
            // burada dışardan gelen blok datalarının tamamlandığı durumda node hazırım sinyalini diğer
            // nodelara gönderecek
            // node hazır olmadan HAZIR sinyalini gönderdiği için
            // senkronizasyon hatası oluyor ve gelen bloklar hatalı birşekilde kaydediliyor.
            // sonrasında gelen bloklar explorer'da aranırken hata oluşturuyor.
            //Console.WriteLine("Control-Point-4-GHJJ");
            if (Obj_Settings.GenesisCreated == false)
            {
                //Console.WriteLine("Control-Point-4-WRET");
                Notus.Print.Info(Obj_Settings, "Node Blocks Are Checking For Sync");
                bool waitForOtherNodes = Notus.Sync.Block(
                    Obj_Settings, ValidatorQueueObj.GiveMeNodeList(),
                    tmpNewBlockIncome =>
                    {
                        ProcessBlock(tmpNewBlockIncome, 3);
                        //Notus.Print.Info(Obj_Settings, "Temprorary Arrived New Block : " + tmpNewBlockIncome.info.uID);
                    }
                );

                //Console.WriteLine(waitForOtherNodes);
                //Console.WriteLine(FirstSyncIsDone);
                //Console.WriteLine(MyReadyMessageSended);
                //Console.WriteLine(IncomeBlockList.Count);
                if (MyReadyMessageSended == false && waitForOtherNodes == false)
                {
                    //Console.WriteLine("Control-Point-1");
                    FirstSyncIsDone = true;
                    MyReadyMessageSended = true;
                    ValidatorQueueObj.MyNodeIsReady();
                }
                else
                {
                    if (FirstSyncIsDone == false && MyReadyMessageSended == false)
                    {
                        if (IncomeBlockList.Count == 0)
                        {
                            FirstSyncIsDone = true;
                            MyReadyMessageSended = true;
                            //Console.WriteLine("Control-Point-1-DDDD");
                            ValidatorQueueObj.MyNodeIsReady();
                        }
                    }
                }
            }


            if (Obj_Settings.GenesisCreated == false)
            {
                //Console.WriteLine("Control-Point-4-4564654");
                //private Queue<KeyValuePair<string, string>> BlockRewardList = new Queue<KeyValuePair<string, string>>();
                /*
                RewardBlockObj.Execute(Obj_Settings);
                */

                /*
                RewardBlockObj.Execute(Obj_Settings, tmpPreBlockIncome =>
                {
                    //Console.WriteLine(JsonSerializer.Serialize(BlockRewardList));
                    Console.WriteLine(JsonSerializer.Serialize(tmpPreBlockIncome));
                    //Console.WriteLine(JsonSerializer.Serialize(tmpPreBlockIncome));
                    //Console.ReadLine();
                    //Obj_BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
                    //{
                        //type = 255, // empty block ödülleri
                        //data = JsonSerializer.Serialize(tmpPreBlockIncome)
                    //});
                });
                */
                //Console.WriteLine("Control-Point-4-99665588");

                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                {
                    EmptyBlockTimerFunc();
                    CryptoTransferTimerFunc();
                }
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer2)
                {
                }
                if (Obj_Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer3)
                {
                    FileStorageTimer();
                }
                Notus.Print.Success(Obj_Settings, "First Synchronization Is Done");
            }

            DateTime LastPrintTime = DateTime.Now;
            bool tmpStartWorkingPrinted = false;
            bool tmpExitMainLoop = false;
            while (tmpExitMainLoop == false)
            {
                //Console.WriteLine(EmptyBlockTimerIsRunning);
                WaitUntilEnoughNode();
                if (tmpStartWorkingPrinted == false)
                {
                    tmpStartWorkingPrinted = true;
                    Notus.Print.Success(Obj_Settings, "Node Starts");
                }
                if (ValidatorQueueObj.MyTurn == true || Obj_Settings.GenesisCreated == true)
                {
                    // geçerli utc zaman bilgisini alıp block oluşturma işlemi için parametre olarak gönder böylece
                    // her blok utc zamanı ile oluşturulmuş olsun
                    DateTime currentUtcTime = ValidatorQueueObj.GetUtcTime();

                    Notus.Variable.Struct.PoolBlockRecordStruct? TmpBlockStruct = Obj_BlockQueue.Get(
                        currentUtcTime,
                        Obj_Api.BalanceObj
                    );
                    if (TmpBlockStruct != null)
                    {
                        //Console.WriteLine(JsonSerializer.Serialize(TmpBlockStruct));
                        Notus.Variable.Class.BlockData? PreBlockData = JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(TmpBlockStruct.data);

                        // omergoksoy
                        //Notus.Variable.Enum.ValfwidatorOrder NodeOrder = ValidatorQueueObj.Distrubute(PreBlockData);

                        /*
                        if (NodeOrder == Notus.Variable.Enum.ValidatorOrder.Primary)
                        {

                        }
                        */

                        //blok sıra ve önceki değerleri düzenleniyor...

                        /*
                        */
                        if (PreBlockData != null)
                        {
                            PreBlockData = Obj_BlockQueue.OrganizeBlockOrder(PreBlockData);
                            Notus.Variable.Class.BlockData PreparedBlockData = new Notus.Block.Generate(Obj_Settings.NodeWallet.WalletKey).Make(PreBlockData, 1000);
                            ProcessBlock(PreparedBlockData, 4);
                            ValidatorQueueObj.Distrubute(PreBlockData.info.rowNo, PreBlockData.info.type);
                            Thread.Sleep(1);
                        }
                        else
                        {
                            Notus.Print.Danger(Obj_Settings, "Pre Block Is NULL");
                        }
                    }
                    else
                    {
                        if ((DateTime.Now - LastPrintTime).TotalSeconds > 20)
                        {
                            LastPrintTime = DateTime.Now;
                            if (Obj_Settings.GenesisCreated == true)
                            {
                                tmpExitMainLoop = true;
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                Console.Write("+");
                                Thread.Sleep(1);
                            }
                        }
                    }
                }
                else
                {
                    if (Obj_Settings.GenesisCreated == false)
                    {
                        if ((DateTime.Now - LastPrintTime).TotalSeconds > 20)
                        {
                            LastPrintTime = DateTime.Now;
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.Write("-");
                            Thread.Sleep(1);
                        }
                    }
                }
            }
            if (Obj_Settings.GenesisCreated == true)
            {
                Notus.Print.Warning(Obj_Settings, "Main Class Temporary Ended");
            }
            else
            {
                Notus.Print.Warning(Obj_Settings, "Main Class Ended");
            }
        }

        private string fixedRowNoLength(Notus.Variable.Class.BlockData blockData)
        {
            string tmpStr = blockData.info.rowNo.ToString();
            return tmpStr.PadLeft(15, '_');
        }
        private void ProcessBlock_PrintSection(Notus.Variable.Class.BlockData blockData, int blockSource)
        {
            if (blockSource == 1)
            {
                if (
                    blockData.info.type != 300
                    &&
                    blockData.info.type != 360
                )
                {
                    Notus.Print.Status(Obj_Settings, "Block Came From The Loading DB [ " + fixedRowNoLength(blockData) + " ]");
                }
            }
            if (blockSource == 2)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    100000002,
                    JsonSerializer.Serialize(blockData),
                    blockData.info.rowNo.ToString(),
                    Obj_Settings,
                    null
                );

                Notus.Print.Status(Obj_Settings, "Block Came From The Validator Queue [ " + fixedRowNoLength(blockData) + " ]");
            }
            if (blockSource == 3)
            {
                Notus.Print.Status(Obj_Settings, "Block Came From The Block Sync [ " + fixedRowNoLength(blockData) + " ]");
            }
            if (blockSource == 4)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    100000004,
                    JsonSerializer.Serialize(blockData),
                    blockData.info.rowNo.ToString(),
                    Obj_Settings,
                    null
                );
                Notus.Print.Status(Obj_Settings, "Block Came From The Main Loop [ " + fixedRowNoLength(blockData) + " ]");
            }
            if (blockSource == 5)
            {
                Notus.Print.Status(Obj_Settings, "Block Came From The Dictionary List [ " + fixedRowNoLength(blockData) + " ]");
            }
            if (blockData.info.type == 360)
            {
                RewardBlockObj.RewardList.Clear();
            }
            if (blockData.info.type == 255)
            {
                RewardBlockObj.RewardList.Clear();
                RewardBlockObj.LastTypeUid = blockData.info.uID;
            }
            if (blockData.info.type == 300)
            {
                RewardBlockObj.RewardList.Enqueue(
                    new KeyValuePair<string, string>(
                        blockData.info.uID,
                        blockData.validator.count.First().Key
                    )
                );

                if (RewardBlockObj.LastTypeUid.Length == 0)
                {
                    RewardBlockObj.LastTypeUid = blockData.info.uID;
                }
                RewardBlockObj.LastBlockUid = blockData.info.uID;
            }
        }
        private bool ProcessBlock(Notus.Variable.Class.BlockData blockData, int blockSource)
        {
            if (blockData.info.rowNo > CurrentBlockRowNo)
            {
                string tmpBlockDataStr = JsonSerializer.Serialize(blockData);
                Notus.Variable.Class.BlockData? tmpBlockData =
                    JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(tmpBlockDataStr);
                if (tmpBlockData != null)
                {
                    IncomeBlockList.Add(blockData.info.rowNo, tmpBlockData);
                    ProcessBlock_PrintSection(blockData, blockSource);
                    Notus.Print.Status(Obj_Settings, "Insert Block To Temporary Block List");
                }
                else
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Error,
                        300000099,
                        tmpBlockDataStr,
                        "BlockRowNo",
                        Obj_Settings,
                        null
                    );
                    ProcessBlock_PrintSection(blockData, blockSource);
                }
                return true;
            }
            if (CurrentBlockRowNo > blockData.info.rowNo)
            {
                ProcessBlock_PrintSection(blockData, blockSource);
                Notus.Print.Warning(Obj_Settings, "We Already Processed The Block");
                return true;
            }

            if (blockData.info.rowNo > Obj_Settings.LastBlock.info.rowNo)
            {
                if (blockData.info.type == 300)
                {
                    EmptyBlockGeneratedTime = Notus.Date.ToDateTime(blockData.info.time);
                }

                Obj_Settings.LastBlock = blockData.Clone();

                Obj_BlockQueue.Settings = Obj_Settings;
                Obj_Api.Settings = Obj_Settings;

                Obj_BlockQueue.AddToChain(blockData);

                if (blockData.info.type == 250)
                {
                    Obj_Api.Layer3_StorageFileDone(blockData.info.uID);
                }
                if (blockData.info.type == 40)
                {
                    Console.WriteLine("Lock Account");
                    Console.WriteLine("Notus.Main.OrganizeEachBlock -> Line 964");
                    Console.WriteLine("Lock Account");
                }
                if (blockData.info.type == 240)
                {
                    Console.WriteLine("Notus.Main.OrganizeEachBlock -> Line 705");
                    Console.WriteLine("Notus.Main.OrganizeEachBlock -> Line 705");
                    Console.WriteLine("Make request and add file to layer 3");
                    Console.WriteLine(JsonSerializer.Serialize(blockData, Notus.Variable.Constant.JsonSetting));

                    Notus.Variable.Struct.StorageOnChainStruct tmpStorageOnChain = JsonSerializer.Deserialize<Notus.Variable.Struct.StorageOnChainStruct>(System.Text.Encoding.UTF8.GetString(
                        System.Convert.FromBase64String(
                            blockData.cipher.data
                        )
                    ));
                    Console.WriteLine("----------------------------------------------------------");
                    Console.WriteLine(JsonSerializer.Serialize(tmpStorageOnChain));
                    Console.WriteLine("----------------------------------------------------------");

                    int calculatedChunkCount = (int)Math.Ceiling(System.Convert.ToDouble(tmpStorageOnChain.Size / Notus.Variable.Constant.DefaultChunkSize));
                    Notus.Variable.Struct.FileTransferStruct tmpFileData = new Notus.Variable.Struct.FileTransferStruct()
                    {
                        BlockType = 240,
                        ChunkSize = Notus.Variable.Constant.DefaultChunkSize,
                        ChunkCount = calculatedChunkCount,
                        FileHash = tmpStorageOnChain.Hash,
                        FileName = tmpStorageOnChain.Name,
                        FileSize = tmpStorageOnChain.Size,
                        Level = Notus.Variable.Enum.ProtectionLevel.Low,
                        PublicKey = tmpStorageOnChain.PublicKey,
                        Sign = tmpStorageOnChain.Sign,
                        StoreEncrypted = tmpStorageOnChain.Encrypted,
                        WaterMarkIsLight = true
                    };

                    string responseData = Notus.Network.Node.FindAvailableSync(
                        "storage/file/new/" + blockData.info.uID,
                        new Dictionary<string, string>()
                        {
                    {
                        "data",
                        JsonSerializer.Serialize(tmpFileData)
                    }
                        },
                        Obj_Settings.Network,
                        Notus.Variable.Enum.NetworkLayer.Layer3,
                        Obj_Settings
                    );
                    Console.WriteLine(responseData);
                }

                ProcessBlock_PrintSection(blockData, blockSource);
                /*
                Notus.Print.Success(Obj_Settings,
                    "Generated Last Block UID  [" +
                    blockData.info.type.ToString() +
                    "] : " +
                    Obj_Settings.LastBlock.info.uID.Substring(0, 10) +
                    "...." +
                    Obj_Settings.LastBlock.info.uID.Substring(80) +
                    " -> " + Obj_Settings.LastBlock.info.rowNo.ToString()
                );
                */
            }
            else
            {
                ProcessBlock_PrintSection(blockData, blockSource);
            }

            Obj_Api.AddForCache(blockData);

            if (IncomeBlockList.ContainsKey(CurrentBlockRowNo))
            {
                IncomeBlockList.Remove(CurrentBlockRowNo);
            }

            CurrentBlockRowNo++;

            if (IncomeBlockList.ContainsKey(CurrentBlockRowNo))
            {
                ProcessBlock(IncomeBlockList[CurrentBlockRowNo], 5);
            }

            if (FirstSyncIsDone == false && MyReadyMessageSended == false)
            {
                if (blockSource == 2)
                {
                    if (IncomeBlockList.Count == 0)
                    {
                        Console.WriteLine("Control-Point-1-AAA");
                        ValidatorQueueObj.MyNodeIsReady();
                    }
                }
            }
            return true;
        }

        private void Start_HttpListener()
        {
            if (Obj_Settings.LocalNode == true)
            {
                Notus.Print.Basic(Obj_Settings, "Listining : " +
                Notus.Network.Node.MakeHttpListenerPath(Obj_Settings.IpInfo.Local, SelectedPortVal), false);
            }
            else
            {
                Notus.Print.Basic(Obj_Settings, "Listining : " +
                Notus.Network.Node.MakeHttpListenerPath(Obj_Settings.IpInfo.Public, SelectedPortVal), false);
            }
            HttpObj.OnReceive(Fnc_OnReceiveData);
            HttpObj.ResponseType = "application/json";
            IPAddress NodeIpAddress = IPAddress.Parse(
                (
                    Obj_Settings.LocalNode == false ?
                    Obj_Settings.IpInfo.Public :
                    Obj_Settings.IpInfo.Local
                )
            );
            HttpObj.Settings = Obj_Settings;
            HttpObj.StoreUrl = false;
            HttpObj.Start(NodeIpAddress, SelectedPortVal);
            Notus.Print.Success(Obj_Settings, "Http Has Started", false);
        }

        private string Fnc_OnReceiveData(Notus.Variable.Struct.HttpRequestDetails IncomeData)
        {
            string resultData = Obj_Api.Interpret(IncomeData);
            if (string.Equals(resultData, "queue-data"))
            {
                resultData = ValidatorQueueObj.Process(IncomeData);
            }
            return resultData;
        }

        public Main()
        {
        }
        ~Main()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (Obj_BlockQueue != null)
            {
                try
                {
                    Obj_BlockQueue.Dispose();
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
                        66000505,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );
                }
            }

            if (ValidatorQueueObj != null)
            {
                try
                {
                    ValidatorQueueObj.Dispose();
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
                        900000845,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );
                }
            }

            if (Obj_Api != null)
            {
                try
                {
                    Obj_Api.Dispose();
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
                        7000777,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );
                }
            }

            if (HttpObj != null)
            {
                try
                {
                    HttpObj.Dispose();
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
                        9000805,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );
                }
            }

            if (Obj_Integrity != null)
            {
                try
                {
                    Obj_Integrity.Dispose();
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
                        10012540,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );
                }
            }

        }
    }
}
