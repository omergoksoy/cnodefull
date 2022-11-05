﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVClass = Notus.Variable.Class;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Validator
{
    public class Main : IDisposable
    {
        private bool MyReadyMessageSended = false;
        private bool FirstSyncIsDone = false;
        //this variable hold current processing block number
        private long CurrentBlockRowNo = 1;
        private int SelectedPortVal = 0;

        private Notus.Sync.Validator ValidatorCountObj = new Notus.Sync.Validator();
        private Notus.Sync.Time TimeSyncObj = new Notus.Sync.Time();
        private Notus.Sync.Date NtpDateSyncObj = new Notus.Sync.Date();
        private Notus.Reward.Block RewardBlockObj = new Notus.Reward.Block();
        private Notus.Communication.Http HttpObj = new Notus.Communication.Http(true);
        private Notus.Block.Integrity Obj_Integrity;
        private Notus.Validator.Api Obj_Api;

        /*
        private ConcurrentDictionary<string, NVS.BlockStatus> Obj_BlockStatusList = new ConcurrentDictionary<string, NVS.BlockStatus>();
        public ConcurrentDictionary<string, NVS.BlockStatus> BlockStatusList
        {
            get { return BlockStatusList; }
        }
        */

        private bool CryptoTransferTimerIsRunning = false;
        private DateTime CryptoTransferTime = NVG.NOW.Obj;

        private bool FileStorageTimerIsRunning = false;
        private DateTime FileStorageTime = NVG.NOW.Obj;

        //bu liste diğer nodelardan gelen yeni blokları tutan liste
        public ulong FirstQueueGroupTime = 0;
        public Dictionary<ulong, string> TimeBaseBlockUidList = new Dictionary<ulong, string>();
        public SortedDictionary<long, NVClass.BlockData> IncomeBlockList = new SortedDictionary<long, NVClass.BlockData>();
        //private Notus.Block.Queue Obj_BlockQueue = new Notus.Block.Queue();
        private Notus.Validator.Queue ValidatorQueueObj = new Notus.Validator.Queue();

        public void FileStorageTimer()
        {
            NP.Basic(NVG.Settings, "File Storage Timer Has Started");

            Notus.Threads.Timer TimerObj = new Notus.Threads.Timer(2000);
            TimerObj.Start(() =>
            {
                if (FileStorageTimerIsRunning == false)
                {
                    FileStorageTimerIsRunning = true;
                    using (Notus.Mempool ObjMp_FileStatus =
                        new Notus.Mempool(
                            Notus.IO.GetFolderName(
                                NVG.Settings.Network,
                                NVG.Settings.Layer,
                                NVC.StorageFolderName.File) + "upload_list_status"
                        )
                    )
                    {
                        ObjMp_FileStatus.AsyncActive = false;
                        ObjMp_FileStatus.Each((string tmpStorageId, string rawStatusStr) =>
                        {
                            NVE.BlockStatusCode tmpDataStatus = JsonSerializer.Deserialize<NVE.BlockStatusCode>(rawStatusStr);
                            if (tmpDataStatus == NVE.BlockStatusCode.Pending)
                            {
                                using (Notus.Mempool ObjMp_FileList =
                                    new Notus.Mempool(
                                        Notus.IO.GetFolderName(
                                            NVG.Settings.Network,
                                            NVG.Settings.Layer,
                                            NVC.StorageFolderName.File) + "upload_list"
                                    )
                                )
                                {

                                    string tmpStorageStructStr = ObjMp_FileList.Get(tmpStorageId, "");
                                    NVS.FileTransferStruct tmpFileObj = JsonSerializer.Deserialize<NVS.FileTransferStruct>(tmpStorageStructStr);

                                    string tmpCurrentList = ObjMp_FileList.Get(tmpStorageId + "_chunk", "");
                                    //try
                                    //{
                                    string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpFileObj.PublicKey, NVG.Settings.Network);
                                    string tmpOutputFolder = Notus.IO.GetFolderName(
                                        NVG.Settings.Network,
                                        NVG.Settings.Layer,
                                        NVC.StorageFolderName.Storage
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
                                                        NVG.Settings.Network,
                                                        NVG.Settings.Layer,
                                                        NVC.StorageFolderName.File) + "chunk_list_" + tmpStorageNo.ToString()
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

                                    NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
                                    {
                                        uid = NGF.GenerateTxUid(),
                                        type = 250,
                                        data = outputFileName
                                    }); ;

                                    ObjMp_FileStatus.Set(tmpStorageId, JsonSerializer.Serialize(NVE.BlockStatusCode.InProgress));
                                    try
                                    {
                                        File.Delete(outputFileName);
                                    }
                                    catch (Exception err3)
                                    {
                                        NP.Danger(NVG.Settings, "Error Text : [9abc546ac] : " + err3.Message);
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
            NVS.WalletBalanceStruct tmpWalletBalanceObj = NGF.Balance.Get(WalletKey, timeYouCanUse);
            return tmpWalletBalanceObj.Balance;
        }
        public void CryptoTransferTimerFunc()
        {
            NP.Success(NVG.Settings, "Crypto Transfer Timer Has Started");
            Notus.Threads.Timer TimerObj = new Notus.Threads.Timer(1000);
            TimerObj.Start(() =>
            {
                if (CryptoTransferTimerIsRunning == false)
                {
                    CryptoTransferTimerIsRunning = true;
                    int tmpRequestSend_ListCount = Obj_Api.RequestSend_ListCount();
                    if (tmpRequestSend_ListCount > 0)
                    {
                        //Console.WriteLine("tmpRequestSend_ListCount : " + tmpRequestSend_ListCount.ToString());
                        ulong unlockTimeForNodeWallet = NVG.NOW.Int;
                        NVS.WalletBalanceStruct tmpValidatorWalletBalance = NGF.Balance.Get(NVG.Settings.NodeWallet.WalletKey, unlockTimeForNodeWallet);
                        List<string> tmpWalletList = new List<string>() { };
                        tmpWalletList.Clear();

                        List<string> tmpKeyList = new List<string>();
                        tmpKeyList.Clear();
                        BigInteger totalBlockReward = 0;

                        NVClass.BlockStruct_120 tmpBlockCipherData = new NVClass.BlockStruct_120()
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

                        ConcurrentDictionary<string, NVS.MempoolDataList> tmpTransactionList = Obj_Api.RequestSend_DataList();
                        //Console.WriteLine("tmpTransactionList [1] : " + tmpTransactionList.Count.ToString());
                        // wallet balances are assigned
                        Int64 transferFee = Notus.Wallet.Fee.Calculate(
                            NVE.Fee.CryptoTransfer,
                            NVG.Settings.Network,
                            NVG.Settings.Layer
                        );
                        ulong transactionCount = 0;
                        foreach (KeyValuePair<string, NVS.MempoolDataList> entry in tmpTransactionList)
                        {
                            NVS.CryptoTransactionStoreStruct? tmpObjPoolCrypto = JsonSerializer.Deserialize<NVS.CryptoTransactionStoreStruct>(entry.Value.Data);
                            if (tmpObjPoolCrypto != null)
                            {
                                bool thisRecordCanBeAdded = false;
                                bool senderAvailable = NGF.Balance.WalletUsageAvailable(tmpObjPoolCrypto.Sender);
                                if (senderAvailable == true)
                                {
                                    bool receiverAvailable = NGF.Balance.WalletUsageAvailable(tmpObjPoolCrypto.Receiver);
                                    if (receiverAvailable == true)
                                    {
                                        bool senderLocked = NGF.Balance.StartWalletUsage(tmpObjPoolCrypto.Sender);
                                        if (senderLocked == true)
                                        {
                                            bool receiverLocked = NGF.Balance.StartWalletUsage(tmpObjPoolCrypto.Receiver);
                                            if (receiverLocked == true)
                                            {
                                                thisRecordCanBeAdded = true;
                                            }
                                            else
                                            {
                                                //Console.WriteLine("Receiver Locked");
                                            }
                                        }
                                        else
                                        {
                                            //Console.WriteLine("Sender Locked");
                                        }
                                    }
                                    else
                                    {
                                        //Console.WriteLine("Receiver Not Available");
                                    }
                                }
                                else
                                {
                                    //Console.WriteLine("Sender Not Available");
                                }
                                if (thisRecordCanBeAdded == true)
                                {
                                    bool walletHaveEnoughCoinOrToken = true;
                                    NGF.Balance.StartWalletUsage(tmpObjPoolCrypto.Sender);
                                    NGF.Balance.StartWalletUsage(tmpObjPoolCrypto.Receiver);

                                    bool senderExist = tmpWalletList.IndexOf(tmpObjPoolCrypto.Sender) >= 0 ? true : false;
                                    bool receiverExist = tmpWalletList.IndexOf(tmpObjPoolCrypto.Receiver) >= 0 ? true : false;
                                    //Console.WriteLine(senderExist)
                                    if (senderExist == false && receiverExist == false)
                                    {
                                        tmpWalletList.Add(tmpObjPoolCrypto.Sender);
                                        tmpWalletList.Add(tmpObjPoolCrypto.Receiver);

                                        NVS.WalletBalanceStruct tmpSenderBalance = NGF.Balance.Get(tmpObjPoolCrypto.Sender, unlockTimeForNodeWallet);
                                        NVS.WalletBalanceStruct tmpReceiverBalance = NGF.Balance.Get(tmpObjPoolCrypto.Receiver, unlockTimeForNodeWallet);
                                        string tmpTokenTagStr = "";
                                        BigInteger tmpTokenVolume = 0;

                                        if (string.Equals(tmpObjPoolCrypto.Currency, NVG.Settings.Genesis.CoinInfo.Tag))
                                        {
                                            tmpTokenTagStr = NVG.Settings.Genesis.CoinInfo.Tag;
                                            BigInteger WalletBalanceInt = NGF.Balance.GetCoinBalance(tmpSenderBalance, tmpTokenTagStr);
                                            BigInteger RequiredBalanceInt = BigInteger.Parse(tmpObjPoolCrypto.Volume);
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
                                                    BigInteger tokenCurrentBalance = NGF.Balance.GetCoinBalance(tmpSenderBalance, tmpObjPoolCrypto.Currency);
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
                                                new NVS.CryptoTransferStatus()
                                                {
                                                    Code = NVE.BlockStatusCode.Rejected,
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
                                            Console.WriteLine("entry.Key : " + entry.Key);
                                            tmpBlockCipherData.In.Add(entry.Key, new NVClass.BlockStruct_120_In_Struct()
                                            {
                                                Fee = tmpObjPoolCrypto.Fee,
                                                PublicKey = tmpObjPoolCrypto.PublicKey,
                                                Sign = tmpObjPoolCrypto.Sign,
                                                CurrentTime = tmpObjPoolCrypto.CurrentTime,
                                                Volume = tmpObjPoolCrypto.Volume,
                                                Currency = tmpObjPoolCrypto.Currency,
                                                Receiver = new NVClass.WalletBalanceStructForTransaction()
                                                {
                                                    Balance = NGF.Balance.ReAssign(tmpReceiverBalance.Balance),
                                                    Wallet = tmpObjPoolCrypto.Receiver,
                                                    WitnessBlockUid = tmpReceiverBalance.UID,
                                                    WitnessRowNo = tmpReceiverBalance.RowNo
                                                },
                                                Sender = new NVClass.WalletBalanceStructForTransaction()
                                                {
                                                    Balance = NGF.Balance.ReAssign(tmpSenderBalance.Balance),
                                                    Wallet = tmpObjPoolCrypto.Sender,
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
                                            tmpBlockCipherData.Out[tmpObjPoolCrypto.Sender] = tmpNewResultForTransaction.Balance;

                                            //receiver get coin or token
                                            NVS.WalletBalanceStruct tmpNewReceiverBalance = NGF.Balance.AddVolumeWithUnlockTime(
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
                            else
                            {
                                Console.WriteLine("Contents : Null -> " + entry.Value.Data);
                            }
                        }
                        //Console.WriteLine("transactionCount   [2] : " + transactionCount.ToString());
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
                            NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
                            {
                                // type = 120,
                                // uid =
                                uid = NGF.GenerateTxUid(),
                                type = NVE.BlockTypeList.CryptoTransfer,
                                data = JsonSerializer.Serialize(tmpBlockCipherData)
                            });
                        }
                        foreach (KeyValuePair<string, NVClass.BlockStruct_120_In_Struct> entry in tmpBlockCipherData.In)
                        {
                            Obj_Api.RequestSend_Remove(entry.Key);
                        }
                    }  //if (ObjMp_CryptoTransfer.Count() > 0)

                    CryptoTransferTime = NVG.NOW.Obj;
                    CryptoTransferTimerIsRunning = false;

                }  //if (CryptoTransferTimerIsRunning == false)
            }, true);  //TimerObj.Start(() =>
        }
        private void SetTimeStatusForBeginSync(bool status)
        {
            if (NVG.Settings.GenesisCreated == false)
            {
                if (NVG.Settings.Layer == NVE.NetworkLayer.Layer1)
                {
                    CryptoTransferTimerIsRunning = status;
                }
                if (NVG.Settings.Layer == NVE.NetworkLayer.Layer2)
                {
                }
                if (NVG.Settings.Layer == NVE.NetworkLayer.Layer3)
                {
                }
            }
        }
        private void WaitUntilEnoughNode()
        {
            if (NVG.Settings.GenesisCreated == false)
            {
                SetTimeStatusForBeginSync(true);        // stop timer
                while (ValidatorQueueObj.WaitForEnoughNode == true)
                {
                    Thread.Sleep(1);
                }
                SetTimeStatusForBeginSync(false);       // release timer
            }
        }
        public ulong EmptyBlockGenerationTime()
        {
            şu andaki blok çakışmaları empty bloktan dolayı gerçekleşiyor
            bunu önlemenin yolu son bloğu referans alarak empty bloğun oluşturulmasında geçiyor
            ayrıca node sayısının kontrolünü farklı bir yöntem ile gerçekleştirmek gerekiyor
            burayı kontrol et

            /*
            node'un bir tanesi
20:07:20.009 L1-Dev  -> Empty Block Executed
20:07:20.009 L1-Dev  -> Block Came From The Main Loop [ ____________105 :  300 ]
20:07:20.009 L1-Dev  -> Settings -> Last Block -> 105 -> 1348cb0310010015747b
20:07:20.009 L1-Dev  -> Block Added To Chain -> 105 -> 1348cb0310010015747b
20:07:20.009 L1-Dev  -> Block Is Proccessing -> 105 -> 1348cb0310010015747b
20:07:20.026 L1-Dev  -> Distributing [ ____________105 : 300 ] To 3.75.110.186:5002

20:07:35.354 L1-Dev  -> Empty Block Executed
20:07:35.354 L1-Dev  -> Block Came From The Main Loop [ ____________106 :  300 ]
20:07:35.354 L1-Dev  -> Settings -> Last Block -> 106 -> 1348cb0310100026f39d
20:07:35.354 L1-Dev  -> Block Added To Chain -> 106 -> 1348cb0310100026f39d
20:07:35.354 L1-Dev  -> Block Is Proccessing -> 106 -> 1348cb0310100026f39d
20:07:35.354 L1-Dev  -> Distributing [ ____________106 : 300 ] To 3.75.110.186:5002

NVG.Settings.WaitForGeneratedBlock = TRUE;
20:07:35.354 L1-Dev  -> Income Block Row No -> 106, Validator => NODHmAk7wnknZNG7cXhwErudh7KJTNBDF4xmcuK
20:07:36.154 L1-Dev  -> That block generated by wrong validator
20:07:36.154 L1-Dev  -> Block Time : 202211042007350501103
NVG.Settings.WaitForGeneratedBlock = false;

           
            node'un bir tanesi
NVG.Settings.WaitForGeneratedBlock = TRUE;
20:07:20.161 L1-Dev  -> Income Block Row No -> 105, Validator => NODBDuT7PTzAhdgBecamFJa8hSpXR6SV49bvaKW
20:07:20.474 L1-Dev  -> Arrived New Block : 1348cb031010002
20:07:20.474 L1-Dev  -> Block Came From The Validator Queue [ ____________105 :  300 ]
20:07:20.474 L1-Dev  -> Settings -> Last Block -> 105 -> 1348cb0310010015747b
20:07:20.474 L1-Dev  -> Block Added To Chain -> 105 -> 1348cb0310010015747b
20:07:20.474 L1-Dev  -> Block Is Proccessing -> 105 -> 1348cb0310010015747b
NVG.Settings.WaitForGeneratedBlock = false;

20:07:35.501 L1-Dev  -> Empty Block Executed
20:07:35.501 L1-Dev  -> Block Came From The Main Loop [ ____________106 :  300 ]
20:07:35.501 L1-Dev  -> Settings -> Last Block -> 106 -> 1348cb0310100026f39d
20:07:35.501 L1-Dev  -> Block Added To Chain -> 106 -> 1348cb0310100026f39d
20:07:35.501 L1-Dev  -> Block Is Proccessing -> 106 -> 1348cb0310100026f39d
20:07:35.501 L1-Dev  -> Distributing [ ____________106 : 300 ] To 13.229.56.127:5002

NVG.Settings.WaitForGeneratedBlock = TRUE;
20:07:35.516 L1-Dev  -> Income Block Row No -> 106, Validator => NODBDuT7PTzAhdgBecamFJa8hSpXR6SV49bvaKW
20:07:35.828 L1-Dev  -> Arrived New Block : 1348cb03101f056
20:07:35.828 L1-Dev  -> Block Came From The Validator Queue [ ____________106 :  300 ]
20:07:35.828 L1-Dev  -> We Already Processed The Block -> [ 106 ]
NVG.Settings.WaitForGeneratedBlock = false;


            */

            bool executeEmptyBlock = false;
            int howManySeconds = NVG.Settings.Genesis.Empty.Interval.Time;
            if (NVG.Settings.Genesis.Empty.SlowBlock.Count >= NVG.Settings.EmptyBlockCount)
            {
                howManySeconds = (
                    NVG.Settings.Genesis.Empty.Interval.Time
                        *
                    NVG.Settings.Genesis.Empty.SlowBlock.Multiply
                );
            }
            howManySeconds = 15;
            ulong earliestTime = ND.ToLong(ND.ToDateTime(NVG.Settings.LastBlock.info.time).AddSeconds(howManySeconds));
            if (NVG.NOW.Int > ND.ToLong(ND.ToDateTime(NVG.Settings.LastBlock.info.time).AddSeconds(howManySeconds)))
            {
                executeEmptyBlock = true;
            }
            if (NVG.Settings.OtherBlockCount > NVG.Settings.Genesis.Empty.Interval.Block)
            {
                earliestTime = ND.ToLong(ND.ToDateTime(NVG.Settings.LastBlock.info.time).AddSeconds(1));
                executeEmptyBlock = true;
            }
            if (executeEmptyBlock == true)
            {
                NVG.Settings.OtherBlockCount = 0;
                NVG.Settings.EmptyBlockCount++;
                NGF.BlockQueue.AddEmptyBlock();
            }

            if (executeEmptyBlock == true)
            {
                NP.Success(NVG.Settings, "Empty Block Executed");
            }
            return earliestTime;
        }
        public void Start()
        {
            NGF.GetUtcTimeFromNode(20, true);
            TimeBaseBlockUidList.Clear();

            //Console.WriteLine(JsonSerializer.Serialize(NVG.NOW, Notus.Variable.Constant.JsonSetting));

            if (NVG.Settings.GenesisCreated == false)
            {
                TimeSyncObj.Start();
                NtpDateSyncObj.Start();
                ValidatorCountObj.Start();
            }
            Obj_Integrity = new Notus.Block.Integrity();
            Obj_Integrity.ControlGenesisBlock(); // we check and compare genesis with onther node
            Obj_Integrity.GetLastBlock();        // get last block from current node

            if (NVG.Settings.Genesis == null)
            {
                NP.Basic(NVG.Settings, "Notus.Validator.Main -> Genesis Is NULL");
            }

            Obj_Api = new Notus.Validator.Api();

            NGF.BlockQueue.Start();

            Obj_Api.Prepare();

            //Obj_MainCache = new Notus.Cache.Main();
            // Obj_TokenStorage = new Notus.Token.Storage();
            // Obj_TokenStorage.Settings = NVG.Settings;

            if (NVG.Settings.GenesisCreated == false && NVG.Settings.Genesis != null)
            {
                NP.Basic(NVG.Settings, "Last Block Row No : " + NVG.Settings.LastBlock.info.rowNo.ToString());
                using (Notus.Block.Storage Obj_Storage = new Notus.Block.Storage(false))
                {
                    //Console.WriteLine(JsonSerializer.Serialize( NGF.BlockOrder));
                    foreach (KeyValuePair<long, string> entry in NGF.BlockOrder)
                    {
                        //Console.WriteLine(entry.Key.ToString() + " -  "  + entry.Value);
                        //tgz-exception
                        NVClass.BlockData? tmpBlockData = Obj_Storage.ReadBlock(entry.Value);
                        if (tmpBlockData != null)
                        {
                            ProcessBlock(tmpBlockData, 1);
                        }
                        else
                        {
                            NP.Danger(NVG.Settings, "Notus.Block.Integrity -> Block Does Not Exist");
                            NP.Danger(NVG.Settings, "Reset Block");
                            NP.ReadLine(NVG.Settings);
                        }
                    }
                }
                NP.Info(NVG.Settings, "All Blocks Loaded");

                /*
                using (Notus.Mempool ObjMp_BlockOrder =
                    new Notus.Mempool(
                        Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, NVC.StorageFolderName.Common) +
                        "block_order_list"
                    )
                )
                {
                    ObjMp_BlockOrder.Each((string blockOrderNo, string blockUniqueId) =>
                    {
                        using (Notus.Block.Storage Obj_Storage = new Notus.Block.Storage(false))
                        {
                            //tgz-exception
                            NVClass.BlockData? tmpBlockData = Obj_Storage.ReadBlock(blockUniqueId);
                            if (tmpBlockData != null)
                            {
                                ProcessBlock(tmpBlockData, 1);
                            }
                            else
                            {
                                NP.Danger(NVG.Settings, "Notus.Block.Integrity -> Block Does Not Exist");
                                NP.Danger(NVG.Settings, "Reset Block");
                                NP.ReadLine(NVG.Settings);
                            }
                        }
                    }, 0
                    );
                    NP.Info(NVG.Settings, "All Blocks Loaded");
                }
                */
                SelectedPortVal = NVG.Settings.Nodes.My.IP.Port;
            }
            else
            {
                SelectedPortVal = Notus.Toolbox.Network.FindFreeTcpPort();
            }
            //Console.WriteLine(SelectedPortVal);
            //NP.ReadLine();

            HttpObj.DefaultResult_OK = "null";
            HttpObj.DefaultResult_ERR = "null";
            //NP.Basic(Settings.InfoMode,"empty count : " + Obj_Integrity.EmptyBlockCount);
            if (NVG.Settings.GenesisCreated == false)
            {
                NP.Basic(NVG.Settings, "Main Validator Started");
            }
            //BlockStatObj = Obj_BlockQueue.CurrentBlockStatus();
            Start_HttpListener();
            if (NVG.Settings.GenesisCreated == false)
            {
                NVG.Settings.CommEstablished = true;
            }

            if (NVG.Settings.LocalNode == false)
            {
                // her gelen blok bir listeye eklenmeli ve o liste ile sıra ile eklenmeli
                ValidatorQueueObj.Func_NewBlockIncome = tmpNewBlockIncome =>
                {
                    if (tmpNewBlockIncome != null)
                    {

                        /*
                        * bloğu oluşturan validatör bir imza ila kendisinin oluşturduğunu ispatlamalı
                          eğer hatalı işlem yaparsa bu işlem diğer validatörler tarafından validatörün imzası 
                          ile doğrulanacağı için bu çok önemli

                        * gereksiz debug noktalarını kapatabiliriz.
                        
                        * ayrıca bir node devre dışı kalıp yeniden ağa bağlanınca oluşan durumu gözlemle ve düzelt
                        */


                        bool innerSendToMyChain = false;
                        try
                        {
                            innerSendToMyChain = Notus.Validator.Helper.RightBlockValidator(tmpNewBlockIncome);
                            /*
                            ulong queueTimePeriod = (ulong)(NVC.BlockListeningForPoolTime + NVC.BlockGeneratingTime + NVC.BlockDistributingTime);
                            ulong blockTimeVal = ND.ToLong(tmpNewBlockIncome.info.time);
                            ulong blockGenarationTime = blockTimeVal - (blockTimeVal % queueTimePeriod);
                            if (NVG.Settings.Nodes.Queue.ContainsKey(blockGenarationTime) == true)
                            {
                                string blockValidator = tmpNewBlockIncome.validator.count.First().Key;
                                if (string.Equals(blockValidator, NVG.Settings.Nodes.Queue[blockGenarationTime].Wallet))
                                {
                                    innerSendToMyChain = true;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Time Does Not Have From Nodes List");

                                Console.WriteLine("blockTimeVal : " + blockTimeVal.ToString() + " - " + tmpNewBlockIncome.info.time);

                                Console.WriteLine("Main.cs -> blockGenarationTime [ " +
                                    tmpNewBlockIncome.info.rowNo.ToString() +
                                    " ]: " +
                                    blockGenarationTime.ToString()
                                );
                                Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.Nodes.Queue, NVC.JsonSetting));
                            }
                            */
                        }
                        catch (Exception innerErr)
                        {
                            Console.WriteLine("Main.cs -> tmpNewBlockIncome.info.time : " + tmpNewBlockIncome.info.time);
                            Console.WriteLine("Main.cs -> innerErr.Message : " + innerErr.Message);
                        }
                        if (innerSendToMyChain == true)
                        {
                            NP.Info("New Block Arrived : " + tmpNewBlockIncome.info.uID.Substring(0, 15));
                            ProcessBlock(tmpNewBlockIncome, 2);
                        }
                        else
                        {
                            NP.Warning("That block generated by wrong validator");
                            NP.Warning("Block Time : " + tmpNewBlockIncome.info.time);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Main.cs -> Income Block Is NULL !!!!");
                    }
                    return true;
                };
            }

            ValidatorQueueObj.PreStart();

            //şimdilik kapatıldı
            //ValidatorQueueObj.Start();

            if (NVG.Settings.LocalNode == false)
            {
                // kontrol noktası
                // burada dışardan gelen blok datalarının tamamlandığı durumda node hazırım sinyalini diğer
                // nodelara gönderecek
                // node hazır olmadan HAZIR sinyalini gönderdiği için
                // senkronizasyon hatası oluyor ve gelen bloklar hatalı birşekilde kaydediliyor.
                // sonrasında gelen bloklar explorer'da aranırken hata oluşturuyor.
                if (NVG.Settings.GenesisCreated == false)
                {
                    NP.Info(NVG.Settings, "Node Blocks Are Checking For Sync");
                    bool waitForOtherNodes = Notus.Sync.Block.Block2(
                        ValidatorQueueObj.GiveMeNodeList(),
                        tmpNewBlockIncome =>
                        {
                            //sync-control
                            ProcessBlock(tmpNewBlockIncome, 3);
                            //Console.WriteLine(".tmp.");
                            /*
                            bool sendToMyChain = false;
                            try
                            {

                                ulong queueTimePeriod = (ulong)(NVC.BlockListeningForPoolTime + NVC.BlockGeneratingTime + NVC.BlockDistributingTime);
                                ulong blockTimeVal = ulong.Parse(tmpNewBlockIncome.info.time);
                                ulong blockGenarationTime = blockTimeVal - (blockTimeVal % queueTimePeriod);
                                if (NVG.Settings.Nodes.Queue.ContainsKey(blockGenarationTime) == true)
                                {
                                    string blockValidator = tmpNewBlockIncome.validator.count.First().Key;
                                    if(string.Equals(blockValidator, NVG.Settings.Nodes.Queue[blockGenarationTime].Wallet))
                                    {
                                        sendToMyChain = true;
                                    }
                                }
                            }
                            catch { }
                            if (sendToMyChain == true)
                            {
                            }
                            */

                            /*
                            if (sendToMyChain == true)
                            {
                                ProcessBlock(tmpNewBlockIncome, 3);
                            }
                            else
                            {
                                Console.WriteLine("Block Income But - Does Not Belong To Right Validator");
                                Console.WriteLine("Block Income But - Does Not Belong To Right Validator");
                                Console.WriteLine("Block Income But - Does Not Belong To Right Validator");
                            }
                            */
                        }
                    );

                    if (MyReadyMessageSended == false && waitForOtherNodes == false)
                    {
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
                                ValidatorQueueObj.MyNodeIsReady();
                            }
                        }
                    }


                }
            }



            if (NVG.Settings.GenesisCreated == false)
            {
                //Console.WriteLine("Control-Point-4-4564654");
                //private Queue<KeyValuePair<string, string>> BlockRewardList = new Queue<KeyValuePair<string, string>>();
                /*
                RewardBlockObj.Execute(NVG.Settings);
                */

                /*
                RewardBlockObj.Execute(NVG.Settings, tmpPreBlockIncome =>
                {
                    //Console.WriteLine(JsonSerializer.Serialize(BlockRewardList));
                    Console.WriteLine(JsonSerializer.Serialize(tmpPreBlockIncome));
                    //Console.WriteLine(JsonSerializer.Serialize(tmpPreBlockIncome));
                    //Console.ReadLine();
                    //Obj_BlockQueue.Add(new NVS.PoolBlockRecordStruct()
                    //{
                        //type = 255, // empty block ödülleri
                        //data = JsonSerializer.Serialize(tmpPreBlockIncome)
                    //});
                });
                */

                if (NVG.Settings.Layer == NVE.NetworkLayer.Layer1)
                {
                    CryptoTransferTimerFunc();
                }
                if (NVG.Settings.Layer == NVE.NetworkLayer.Layer2)
                {
                }
                if (NVG.Settings.Layer == NVE.NetworkLayer.Layer3)
                {
                    FileStorageTimer();
                }
                NP.Success(NVG.Settings, "First Synchronization Is Done");
            }

            DateTime LastPrintTime = NVG.NOW.Obj;
            bool tmpStartWorkingPrinted = false;
            bool tmpExitMainLoop = false;
            if (NVG.Settings.LocalNode == true)
            {
                ValidatorQueueObj.WaitForEnoughNode = false;
            }

            // her node için ayrılan süre
            ulong queueTimePeriod = (ulong)(NVC.BlockListeningForPoolTime + NVC.BlockGeneratingTime + NVC.BlockDistributingTime);
            ulong currentQueueTime = NVG.NodeQueue.Starting;

            bool prepareNextQueue = false;
            string selectedWalletId = string.Empty;
            byte nodeOrderCount = 0;
            bool waitPrinted = false;

            NVG.Settings.MsgOrch.OnReceive((string IncomeText) =>
            {
                //sync-control
                string innerResultStr = ValidatorQueueObj.ProcessIncomeData(IncomeText);
                //Console.WriteLine("Main.Cs -> IncomeText [ " + IncomeText.Length + " ] : " + IncomeText);
                //Console.WriteLine("Main.Cs -> resultStr [ " + innerResultStr.Length + " ] : " + innerResultStr);
            });
            NVG.Settings.MsgOrch.Start();

            bool start_FirstQueueGroupTime = false;
            //while (tmpExitMainLoop == false)
            
            while (
                tmpExitMainLoop == false && 
                NVG.Settings.NodeClosing == false &&
                NVG.Settings.GenesisCreated == false
            )
            {
                if (prepareNextQueue == false)
                {
                    prepareNextQueue = true;
                    selectedWalletId = NVG.Settings.Nodes.Queue[currentQueueTime].Wallet;
                    if (selectedWalletId.Length == 0)
                    {
                        if (NVG.Settings.NodeClosing == true)
                        {
                            tmpExitMainLoop = true;
                            NVG.Settings.ClosingCompleted = true;
                        }
                    }
                }
                if (NVG.NOW.Int >= currentQueueTime)
                {
                    waitPrinted = false;
                    nodeOrderCount++;


                    if (nodeOrderCount == 1)
                    {
                        FirstQueueGroupTime = currentQueueTime;
                        TimeBaseBlockUidList.Add(currentQueueTime, "");
                        TimeBaseBlockUidList[currentQueueTime] = "id:" + currentQueueTime.ToString();
                    }

                    if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, selectedWalletId))
                    {
                        /*
                        if (waitPrinted == false)
                        {
                            waitPrinted = true;
                            Console.WriteLine("My Turn ->" + NVG.NOW.Int.ToString() + " - " + currentQueueTime.ToString());
                        }
                        */

                        while (NVG.Settings.WaitForGeneratedBlock == true)
                        {
                            Thread.Sleep(1);
                        }

                        bool txExecuted = false;
                        while (ND.AddMiliseconds(currentQueueTime, queueTimePeriod - 10) >= NVG.NOW.Int)
                        {
                            if (txExecuted == false)
                            {
                                bool executeEmptyBlock = EmptyBlockGeneration();

                                NVS.PoolBlockRecordStruct? TmpBlockStruct = NGF.BlockQueue.Get(
                                    ND.AddMiliseconds(currentQueueTime, NVC.BlockListeningForPoolTime)
                                );
                                if (TmpBlockStruct != null)
                                {
                                    txExecuted = true;
                                    NVClass.BlockData? PreBlockData = JsonSerializer.Deserialize<NVClass.BlockData>(TmpBlockStruct.data);
                                    if (PreBlockData != null)
                                    {
                                        PreBlockData = NGF.BlockQueue.OrganizeBlockOrder(PreBlockData);
                                        NVClass.BlockData PreparedBlockData = new Notus.Block.Generate(NVG.Settings.NodeWallet.WalletKey).Make(PreBlockData, 1000);
                                        ProcessBlock(PreparedBlockData, 4);

                                        //socket-exception
                                        ValidatorQueueObj.Distrubute(PreBlockData.info.rowNo, PreBlockData.info.type);
                                        
                                        NGF.WalletUsageList.Clear();
                                    }
                                    else
                                    {
                                        NP.Danger(NVG.Settings, "Pre Block Is NULL");
                                    }
                                }
                                else
                                {
                                    if ((NVG.NOW.Obj - LastPrintTime).TotalSeconds > 20)
                                    {
                                        LastPrintTime = NVG.NOW.Obj;
                                        if (NVG.Settings.GenesisCreated == true)
                                        {
                                            tmpExitMainLoop = true;
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                                            Console.Write("+");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (NGF.BlockQueue.CheckPoolDb == true)
                        {
                            NGF.BlockQueue.LoadFromPoolDb();
                        }
                    }
                    prepareNextQueue = false;
                    if (NVC.RegenerateNodeQueueCount == nodeOrderCount)
                    {
                        // eğer yeterli sayıda node yokse
                        // zamanları hazırlasın ancak node verileri boş oluşturulsun
                        if (NVC.MinimumNodeCount > NVG.OnlineNodeCount)
                        {
                            ValidatorQueueObj.GenerateNotEnoughNodeQueue(ND.AddMiliseconds(currentQueueTime, 1500));
                        }
                        else
                        {
                            string queueSeedStr = "";
                            if (start_FirstQueueGroupTime == true)
                            {
                                queueSeedStr = TimeBaseBlockUidList[FirstQueueGroupTime];
                            }
                            ValidatorQueueObj.ReOrderNodeQueue(currentQueueTime, queueSeedStr);
                        }
                    }
                    currentQueueTime = ND.AddMiliseconds(currentQueueTime, queueTimePeriod);
                    if (nodeOrderCount == 6)
                    {
                        nodeOrderCount = 0;
                        start_FirstQueueGroupTime = true;
                    }
                }
                else
                {
                    /*
                    if (waitPrinted == false)
                    {
                        waitPrinted = true;
                        
                        Console.WriteLine(
                            "Wait For Turn ->"+
                            NVG.NOW.Int.ToString() + " - " + 
                            currentQueueTime.ToString() + " -> " +
                            NVG.Settings.Nodes.My.IP.Wallet.Substring(0, 6) + 
                            " - " + 
                            selectedWalletId.Substring(0, 6)
                        );
                    }
                    */
                }
            }

            if (NVG.Settings.NodeClosing == false)
            {
                if (NVG.Settings.GenesisCreated == true)
                {
                    NP.Warning(NVG.Settings, "Main Class Temporary Ended");
                }
                else
                {
                    NP.Warning(NVG.Settings, "Main Class Ended");
                }
            }
        }

        private string fixedRowNoLength(NVClass.BlockData blockData)
        {
            return blockData.info.rowNo.ToString().PadLeft(15, '_');
        }
        private void ProcessBlock_PrintSection(NVClass.BlockData blockData, int blockSource)
        {
            if (blockSource == 1)
            {
                if (
                    blockData.info.type != NVE.BlockTypeList.EmptyBlock
                    &&
                    blockData.info.type != NVE.BlockTypeList.GenesisBlock
                )
                {
                    if (blockData.info.rowNo % 500 == 0)
                    {
                        NP.Status("Block Came From The Loading DB [ " +
                            fixedRowNoLength(blockData) + " : " + blockData.info.type.ToString().PadLeft(4, ' ') + " ]"
                        );
                    }
                }
            }
            if (blockSource == 2)
            {
                NP.Status("Block Came From The Validator Queue [ " +
                    fixedRowNoLength(blockData) + " : " + blockData.info.type.ToString().PadLeft(4, ' ') + " ]"
                );
            }
            if (blockSource == 3)
            {
                NP.Status("Block Came From The Block Sync [ " +
                    fixedRowNoLength(blockData) + " : " + blockData.info.type.ToString().PadLeft(4, ' ') + " ]"
                );
            }
            if (blockSource == 4)
            {
                NP.Status("Block Came From The Main Loop [ " +
                    fixedRowNoLength(blockData) + " : " + blockData.info.type.ToString().PadLeft(4, ' ') + " ]"
                );
            }
            if (blockSource == 5)
            {
                NP.Status("Block Came From The Dictionary List [ " +
                    fixedRowNoLength(blockData) + " : " + blockData.info.type.ToString().PadLeft(4, ' ') + " ]"
                );
            }
            if (blockData.info.type == NVE.BlockTypeList.GenesisBlock)
            {
                RewardBlockObj.RewardList.Clear();
            }
            if (blockData.info.type == 255)
            {
                RewardBlockObj.RewardList.Clear();
                RewardBlockObj.LastTypeUid = blockData.info.uID;
            }
            if (blockData.info.type == NVE.BlockTypeList.EmptyBlock)
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
        private bool ProcessBlock(NVClass.BlockData blockData, int blockSource)
        {
            //gelen blok datası
            //Console.WriteLine("blockData.info.rowNo              : " + blockData.info.rowNo.ToString());

            //son olarak eklenen blok datası
            //Console.WriteLine("NVG.Settings.LastBlock.info.rowNo : " + NVG.Settings.LastBlock.info.rowNo.ToString());

            //işlenen blok datası
            //Console.WriteLine("CurrentBlockRowNo                 : " + CurrentBlockRowNo.ToString());
            bool addBlockToChain = false;

            // yeni blok geldiğinde burası devreye girecek
            if (blockData.info.rowNo > NVG.Settings.LastBlock.info.rowNo)
            {
                addBlockToChain = true;
                NVG.Settings.LastBlock = JsonSerializer.Deserialize<
                    NVClass.BlockData>(JsonSerializer.Serialize(blockData)
                );
                if (blockData.info.type == 300)
                {
                    NVG.Settings.EmptyBlockCount++;
                    NVG.Settings.OtherBlockCount = 0;
                }
                else
                {
                    NVG.Settings.OtherBlockCount++;
                    NVG.Settings.EmptyBlockCount = 0;
                }
            }

            //eğer gelen yeni blok mevcut bloklardan 2 veya daha blok sonrası ise bekleme listesine alacak
            if (blockData.info.rowNo > CurrentBlockRowNo)
            {
                string tmpBlockDataStr = JsonSerializer.Serialize(blockData);
                NVClass.BlockData? tmpBlockData =
                    JsonSerializer.Deserialize<NVClass.BlockData>(tmpBlockDataStr);
                if (tmpBlockData != null)
                {
                    if (IncomeBlockList.ContainsKey(blockData.info.rowNo) == true)
                    {
                        Console.WriteLine("Block Exist -> Line 937");
                    }
                    else
                    {
                        IncomeBlockList.Add(blockData.info.rowNo, tmpBlockData);
                        ProcessBlock_PrintSection(blockData, blockSource);
                        NP.Status(NVG.Settings, "Insert Block To Temporary Block List");
                    }
                }
                else
                {
                    Console.WriteLine("Block Convert Error -> Main.Cs -> Line 986");
                    //ProcessBlock_PrintSection(blockData, blockSource);
                }
                return true;
            }

            // eğer gelen blok zaten işlenmiş blok ise gözardı edilecek
            if (CurrentBlockRowNo > blockData.info.rowNo)
            {
                innerSendToMyChain = Notus.Validator.Helper.RightBlockValidator(blockData);
                ProcessBlock_PrintSection(blockData, blockSource);
                NP.Warning(NVG.Settings, "We Already Processed The Block -> [ " + blockData.info.rowNo.ToString() + " ]");
                return true;
            }

            // eğer gelen blok yeni blok ise buraya girecek ve blok işlenir
            if (blockData.info.rowNo > NVG.Settings.LastBlock.info.rowNo)
            {
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
                    Console.WriteLine(JsonSerializer.Serialize(blockData, NVC.JsonSetting));

                    NVS.StorageOnChainStruct tmpStorageOnChain = JsonSerializer.Deserialize<NVS.StorageOnChainStruct>(System.Text.Encoding.UTF8.GetString(
                        System.Convert.FromBase64String(
                            blockData.cipher.data
                        )
                    ));
                    Console.WriteLine("----------------------------------------------------------");
                    Console.WriteLine(JsonSerializer.Serialize(tmpStorageOnChain));
                    Console.WriteLine("----------------------------------------------------------");

                    int calculatedChunkCount = (int)Math.Ceiling(System.Convert.ToDouble(tmpStorageOnChain.Size / NVC.DefaultChunkSize));
                    NVS.FileTransferStruct tmpFileData = new NVS.FileTransferStruct()
                    {
                        BlockType = 240,
                        ChunkSize = NVC.DefaultChunkSize,
                        ChunkCount = calculatedChunkCount,
                        FileHash = tmpStorageOnChain.Hash,
                        FileName = tmpStorageOnChain.Name,
                        FileSize = tmpStorageOnChain.Size,
                        Level = NVE.ProtectionLevel.Low,
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
                        NVG.Settings.Network,
                        NVE.NetworkLayer.Layer3,
                        NVG.Settings
                    );
                    Console.WriteLine("Main.Cs -> Line 1076");
                    Console.WriteLine(responseData);
                }

                ProcessBlock_PrintSection(blockData, blockSource);
            }
            else
            {
                ProcessBlock_PrintSection(blockData, blockSource);
            }


            if (NVG.Settings.LastBlock.prev.Length < 20)
            {
                NP.Info("Settings -> Last Block -> " +
                    NVG.Settings.LastBlock.info.rowNo.ToString() + " -> " +
                    "Prev is Empty [ " + NVG.Settings.LastBlock.prev + " ]"
                );
            }
            else
            {
                NP.Info("Settings -> Last Block -> " +
                    NVG.Settings.LastBlock.info.rowNo.ToString() + " -> " +
                    NVG.Settings.LastBlock.prev.Substring(0, 20)
                );
            }

            if (addBlockToChain == true)
            {
                NGF.BlockQueue.AddToChain(blockData);
            }


            //gelen blok burada işleniyor...
            Obj_Api.AddForCache(blockData);

            //eğer blok numarası varsa, işlem bittiği için listeden silinir
            if (IncomeBlockList.ContainsKey(CurrentBlockRowNo))
            {
                IncomeBlockList.Remove(CurrentBlockRowNo);
            }

            CurrentBlockRowNo++;
            // eğer sonraki bloklardan listede olan varsa o da işlenir
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
                        ValidatorQueueObj.MyNodeIsReady();
                    }
                }
            }

            return true;
        }

        private void Start_HttpListener()
        {
            if (NVG.Settings.LocalNode == true)
            {
                NP.Basic(NVG.Settings, "Listining : " +
                Notus.Network.Node.MakeHttpListenerPath(NVG.Settings.IpInfo.Local, SelectedPortVal), false);
            }
            else
            {
                NP.Basic(NVG.Settings, "Listining : " +
                Notus.Network.Node.MakeHttpListenerPath(NVG.Settings.IpInfo.Public, SelectedPortVal), false);
            }
            HttpObj.OnReceive(Fnc_OnReceiveData);
            HttpObj.ResponseType = "application/json";
            IPAddress NodeIpAddress = IPAddress.Parse(
                (
                    NVG.Settings.LocalNode == false ?
                    NVG.Settings.IpInfo.Public :
                    NVG.Settings.IpInfo.Local
                )
            );
            HttpObj.StoreUrl = false;
            HttpObj.Start(NodeIpAddress, SelectedPortVal);
            NP.Success(NVG.Settings, "Http Has Started", false);
        }

        private string Fnc_OnReceiveData(NVS.HttpRequestDetails IncomeData)
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
            /*
            if (Obj_BlockQueue != null)
            {
                try
                {
                    Obj_BlockQueue.Dispose();
                }
                catch (Exception err)
                {
                }
            }
            */

            if (ValidatorQueueObj != null)
            {
                try
                {
                    ValidatorQueueObj.Dispose();
                }
                catch (Exception err)
                {
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
                }
            }

        }
    }
}
