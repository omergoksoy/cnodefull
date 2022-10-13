using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using NGF = Notus.Variable.Globals.Functions;
using NVG = Notus.Variable.Globals;

namespace Notus.Validator
{
    public class Main : IDisposable
    {
        private bool MyReadyMessageSended = false;
        private bool FirstSyncIsDone = false;
        //this variable hold current processing block number
        private long CurrentBlockRowNo = 1;
        private int SelectedPortVal = 0;

        private Notus.Reward.Block RewardBlockObj = new Notus.Reward.Block();
        private Notus.Communication.Http HttpObj = new Notus.Communication.Http(true);
        private Notus.Block.Integrity Obj_Integrity;
        private Notus.Validator.Api Obj_Api;

        /*
        private ConcurrentDictionary<string, Notus.Variable.Struct.BlockStatus> Obj_BlockStatusList = new ConcurrentDictionary<string, Notus.Variable.Struct.BlockStatus>();
        public ConcurrentDictionary<string, Notus.Variable.Struct.BlockStatus> BlockStatusList
        {
            get { return BlockStatusList; }
        }
        */

        private bool CryptoTransferTimerIsRunning = false;
        private DateTime CryptoTransferTime = DateTime.Now;

        //private bool EmptyBlockNotMyTurnPrinted = false;
        private bool EmptyBlockTimerIsRunning = false;
        private DateTime EmptyBlockGeneratedTime = new DateTime(2000, 01, 1, 0, 00, 00);

        private bool FileStorageTimerIsRunning = false;
        private DateTime FileStorageTime = DateTime.Now;

        //bu liste diğer nodelardan gelen yeni blokları tutan liste
        public SortedDictionary<long, Notus.Variable.Class.BlockData> IncomeBlockList = new SortedDictionary<long, Notus.Variable.Class.BlockData>();
        //private Notus.Block.Queue Obj_BlockQueue = new Notus.Block.Queue();
        private Notus.Validator.Queue ValidatorQueueObj = new Notus.Validator.Queue();
        public void EmptyBlockTimerFunc()
        {
            Notus.Print.Basic(NVG.Settings, "Empty Block Timer Has Started");
            Notus.Threads.Timer TimerObj = new Notus.Threads.Timer(1000);
            TimerObj.Start(() =>
            {
                if (ValidatorQueueObj.MyTurn == true && EmptyBlockTimerIsRunning == false)
                {
                    EmptyBlockTimerIsRunning = true;
                    int howManySeconds = NVG.Settings.Genesis.Empty.Interval.Time;

                    if (NVG.Settings.Genesis.Empty.SlowBlock.Count >= Obj_Integrity.EmptyBlockCount)
                    {
                        howManySeconds = (
                            NVG.Settings.Genesis.Empty.Interval.Time
                                *
                            NVG.Settings.Genesis.Empty.SlowBlock.Multiply
                        );
                    }

                    //blok zamanı ve utc zamanı çakışıyor
                    DateTime tmpLastTime = Notus.Date.ToDateTime(
                        NVG.Settings.LastBlock.info.time
                    ).AddSeconds(howManySeconds);

                    // get utc time from validatır Queue
                    DateTime utcTime = ValidatorQueueObj.GetUtcTime();
                    /*
                    Console.WriteLine(
                        utcTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText)
                        + " < - > " +
                        tmpLastTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText)
                    );
                    */
                    if (utcTime > tmpLastTime)
                    {
                        if (ValidatorQueueObj.MyTurn)
                        {
                            if ((DateTime.Now - EmptyBlockGeneratedTime).TotalSeconds > 30)
                            {
                                EmptyBlockGeneratedTime = DateTime.Now;
                                //EmptyBlockGeneratedTime = utcTime;
                                //Console.WriteLine("EmptyBlockGeneratedTime [1]: " + EmptyBlockGeneratedTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText));
                                Notus.Print.Success(NVG.Settings, "Empty Block Executed");
                                NGF.BlockQueue.AddEmptyBlock();

                            }
                        }
                    }
                    EmptyBlockTimerIsRunning = false;
                }
            }, true);
        }
        public void FileStorageTimer()
        {
            Notus.Print.Basic(NVG.Settings, "File Storage Timer Has Started");

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
                                            NVG.Settings.Network,
                                            NVG.Settings.Layer,
                                            Notus.Variable.Constant.StorageFolderName.File) + "upload_list"
                                    )
                                )
                                {

                                    string tmpStorageStructStr = ObjMp_FileList.Get(tmpStorageId, "");
                                    Notus.Variable.Struct.FileTransferStruct tmpFileObj = JsonSerializer.Deserialize<Notus.Variable.Struct.FileTransferStruct>(tmpStorageStructStr);

                                    string tmpCurrentList = ObjMp_FileList.Get(tmpStorageId + "_chunk", "");
                                    //try
                                    //{
                                    string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpFileObj.PublicKey, NVG.Settings.Network);
                                    string tmpOutputFolder = Notus.IO.GetFolderName(
                                        NVG.Settings.Network,
                                        NVG.Settings.Layer,
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
                                                        NVG.Settings.Network,
                                                        NVG.Settings.Layer,
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

                                    NGF.BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
                                    {
                                        uid = NGF.GenerateTxUid(),
                                        type = 250,
                                        data = outputFileName
                                    }); ;

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
                                        Notus.Print.Danger(NVG.Settings, "Error Text : [9abc546ac] : " + err3.Message);
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
            Notus.Variable.Struct.WalletBalanceStruct tmpWalletBalanceObj = NGF.Balance.Get(WalletKey, timeYouCanUse);
            return tmpWalletBalanceObj.Balance;
        }

        public void CryptoTransferTimerFunc()
        {
            Notus.Print.Success(NVG.Settings, "Crypto Transfer Timer Has Started");
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
                        ulong unlockTimeForNodeWallet = Notus.Time.NowToUlong();
                        Notus.Variable.Struct.WalletBalanceStruct tmpValidatorWalletBalance = NGF.Balance.Get(NVG.Settings.NodeWallet.WalletKey, unlockTimeForNodeWallet);
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
                                NodeWallet = NVG.Settings.NodeWallet.WalletKey,
                                Reward = totalBlockReward.ToString()
                            }
                        };

                        ConcurrentDictionary<string, Notus.Variable.Struct.MempoolDataList> tmpTransactionList = Obj_Api.RequestSend_DataList();
                        //Console.WriteLine("tmpTransactionList [1] : " + tmpTransactionList.Count.ToString());
                        // wallet balances are assigned
                        Int64 transferFee = Notus.Wallet.Fee.Calculate(
                            Notus.Variable.Enum.Fee.CryptoTransfer,
                            NVG.Settings.Network,
                            NVG.Settings.Layer
                        );
                        ulong transactionCount = 0;
                        foreach (KeyValuePair<string, Notus.Variable.Struct.MempoolDataList> entry in tmpTransactionList)
                        {
                            Notus.Variable.Struct.CryptoTransactionStoreStruct? tmpObjPoolCrypto = JsonSerializer.Deserialize<Notus.Variable.Struct.CryptoTransactionStoreStruct>(entry.Value.Data);
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

                                        Notus.Variable.Struct.WalletBalanceStruct tmpSenderBalance = NGF.Balance.Get(tmpObjPoolCrypto.Sender, unlockTimeForNodeWallet);
                                        Notus.Variable.Struct.WalletBalanceStruct tmpReceiverBalance = NGF.Balance.Get(tmpObjPoolCrypto.Receiver, unlockTimeForNodeWallet);
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
                                            Console.WriteLine("entry.Key : " + entry.Key);
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
                                                    Balance = NGF.Balance.ReAssign(tmpReceiverBalance.Balance),
                                                    Wallet = tmpObjPoolCrypto.Receiver,
                                                    WitnessBlockUid = tmpReceiverBalance.UID,
                                                    WitnessRowNo = tmpReceiverBalance.RowNo
                                                },
                                                Sender = new Notus.Variable.Class.WalletBalanceStructForTransaction()
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
                                            (bool tmpErrorStatusForFee, Notus.Variable.Struct.WalletBalanceStruct tmpNewResultForFee) =
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
                                            (bool tmpErrorStatusForTransaction, Notus.Variable.Struct.WalletBalanceStruct tmpNewResultForTransaction) =
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
                                            Notus.Variable.Struct.WalletBalanceStruct tmpNewReceiverBalance = NGF.Balance.AddVolumeWithUnlockTime(
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
                            NGF.BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
                            {
                                // type = 120,
                                // uid =
                                uid = NGF.GenerateTxUid(),
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
            if (NVG.Settings.GenesisCreated == false)
            {
                if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                {
                    EmptyBlockTimerIsRunning = status;
                    CryptoTransferTimerIsRunning = status;
                }
                if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer2)
                {
                }
                if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer3)
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
        public void Start()
        {
            Obj_Integrity = new Notus.Block.Integrity();
            Obj_Integrity.ControlGenesisBlock(); // we check and compare genesis with onther node
            Obj_Integrity.GetLastBlock();        // get last block from current node

            if (NVG.Settings.Genesis == null)
            {
                Notus.Print.Basic(NVG.Settings, "Notus.Validator.Main -> Genesis Is NULL");
            }

            Obj_Api = new Notus.Validator.Api();

            NGF.BlockQueue.Start();

            Obj_Api.Prepare();

            //Obj_MainCache = new Notus.Cache.Main();
            // Obj_TokenStorage = new Notus.Token.Storage();
            // Obj_TokenStorage.Settings = NVG.Settings;


            //dekjdhekjdhedhke
            if (NVG.Settings.GenesisCreated == false && NVG.Settings.Genesis != null)
            {
                Notus.Print.Basic(NVG.Settings, "Last Block Row No : " + NVG.Settings.LastBlock.info.rowNo.ToString());
                using (Notus.Block.Storage Obj_Storage = new Notus.Block.Storage(false))
                {
                    //Console.WriteLine(JsonSerializer.Serialize( NGF.BlockOrder));
                    foreach (KeyValuePair<long, string> entry in NGF.BlockOrder)
                    {
                        //Console.WriteLine(entry.Key.ToString() + " -  "  + entry.Value);
                        //tgz-exception
                        Notus.Variable.Class.BlockData? tmpBlockData = Obj_Storage.ReadBlock(entry.Value);
                        if (tmpBlockData != null)
                        {
                            ProcessBlock(tmpBlockData, 1);
                        }
                        else
                        {
                            Notus.Print.Danger(NVG.Settings, "Notus.Block.Integrity -> Block Does Not Exist");
                            Notus.Print.Danger(NVG.Settings, "Reset Block");
                            Notus.Print.ReadLine(NVG.Settings);
                        }
                    }
                }
                Notus.Print.Info(NVG.Settings, "All Blocks Loaded");

                /*
                using (Notus.Mempool ObjMp_BlockOrder =
                    new Notus.Mempool(
                        Notus.IO.GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, Notus.Variable.Constant.StorageFolderName.Common) +
                        "block_order_list"
                    )
                )
                {
                    ObjMp_BlockOrder.Each((string blockOrderNo, string blockUniqueId) =>
                    {
                        using (Notus.Block.Storage Obj_Storage = new Notus.Block.Storage(false))
                        {
                            //tgz-exception
                            Notus.Variable.Class.BlockData? tmpBlockData = Obj_Storage.ReadBlock(blockUniqueId);
                            if (tmpBlockData != null)
                            {
                                ProcessBlock(tmpBlockData, 1);
                            }
                            else
                            {
                                Notus.Print.Danger(NVG.Settings, "Notus.Block.Integrity -> Block Does Not Exist");
                                Notus.Print.Danger(NVG.Settings, "Reset Block");
                                Notus.Print.ReadLine(NVG.Settings);
                            }
                        }
                    }, 0
                    );
                    Notus.Print.Info(NVG.Settings, "All Blocks Loaded");
                }
                */
                SelectedPortVal = Notus.Toolbox.Network.GetNetworkPort(NVG.Settings);
            }
            else
            {
                SelectedPortVal = Notus.Toolbox.Network.FindFreeTcpPort();
            }

            HttpObj.DefaultResult_OK = "null";
            HttpObj.DefaultResult_ERR = "null";
            //Notus.Print.Basic(Settings.InfoMode,"empty count : " + Obj_Integrity.EmptyBlockCount);
            if (NVG.Settings.GenesisCreated == false)
            {
                Notus.Print.Basic(NVG.Settings, "Main Validator Started");
            }
            //BlockStatObj = Obj_BlockQueue.CurrentBlockStatus();
            Start_HttpListener();

            if (NVG.Settings.LocalNode == false)
            {
                // her gelen blok bir listeye eklenmeli ve o liste ile sıra ile eklenmeli
                ValidatorQueueObj.Func_NewBlockIncome = tmpNewBlockIncome =>
                {
                    ProcessBlock(tmpNewBlockIncome, 2);
                    //Notus.Print.Info(NVG.Settings, "Arrived New Block : " + tmpNewBlockIncome.info.uID);
                    return true;
                };
            }

            ValidatorQueueObj.GetUtcTimeFromServer();
            if (NVG.Settings.LocalNode == false)
            {
                if (NVG.Settings.GenesisCreated == false)
                {
                    ValidatorQueueObj.PreStart(
                        NVG.Settings.LastBlock.info.rowNo,
                        NVG.Settings.LastBlock.info.uID,
                        NVG.Settings.LastBlock.sign,
                        NVG.Settings.LastBlock.prev
                    );

                    //burada ping ve pong yaparak bekleyecek
                    ValidatorQueueObj.PingOtherNodes();
                }
                ValidatorQueueObj.Start();
            }


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
                    Notus.Print.Info(NVG.Settings, "Node Blocks Are Checking For Sync");
                    bool waitForOtherNodes = Notus.Sync.Block(
                        NVG.Settings, ValidatorQueueObj.GiveMeNodeList(),
                        tmpNewBlockIncome =>
                        {
                            ProcessBlock(tmpNewBlockIncome, 3);
                        }
                    );

                    if (MyReadyMessageSended == false && waitForOtherNodes == false)
                    {
                        FirstSyncIsDone = true;
                        MyReadyMessageSended = true;
                        //Console.WriteLine("Control-Point-a465as4");
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
                                Console.WriteLine("Control-Point-a465as4");
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
                    //Obj_BlockQueue.Add(new Notus.Variable.Struct.PoolBlockRecordStruct()
                    //{
                        //type = 255, // empty block ödülleri
                        //data = JsonSerializer.Serialize(tmpPreBlockIncome)
                    //});
                });
                */
                //Console.WriteLine("Control-Point-4-99665588");

                if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer1)
                {
                    //EmptyBlockTimerFunc();
                    CryptoTransferTimerFunc();
                }
                if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer2)
                {
                }
                if (NVG.Settings.Layer == Notus.Variable.Enum.NetworkLayer.Layer3)
                {
                    FileStorageTimer();
                }
                Notus.Print.Success(NVG.Settings, "First Synchronization Is Done");
            }

            DateTime LastPrintTime = DateTime.Now;
            bool tmpStartWorkingPrinted = false;
            bool tmpExitMainLoop = false;
            if (NVG.Settings.LocalNode == true)
            {
                ValidatorQueueObj.WaitForEnoughNode = false;
                ValidatorQueueObj.MyTurn = true;
            }

            while (tmpExitMainLoop == false)
            {
                WaitUntilEnoughNode();
                if (tmpStartWorkingPrinted == false)
                {
                    tmpStartWorkingPrinted = true;
                    Notus.Print.Success(NVG.Settings, "Node Starts");
                }
                if (ValidatorQueueObj.MyTurn == true || NVG.Settings.GenesisCreated == true)
                {
                    /*
                    
                    Notus.Print.Success(NVG.Settings, "Empty Block Executed");
                    NGF.BlockQueue.AddEmptyBlock();
                    */

                    /*
                    3 saniye ile 3.000 milisaniye


                    2 node için -> 1.500 milisaniye / node  -> ilk 2 sıra
                    3 node için -> 1.000 milisaniye / node  -> ilk 3 sıra
                    4 node için ->   750 milisaniye / node  -> ilk 3 sıra
                    5 node için ->   600 milisaniye / node  -> ilk 3 sıra
                    6 node için ->   500 milisaniye / node  -> ilk 6 sıra
                    7 ve sonası ->   500 milisaniye / node  -> ilk 6 sıra


                    */


                    int islemSuresi = 200;
                    int olusturmaSuresi = 100;
                    int dagitmaSuresi = 200;
                    DateTime islemBitis = NVG.StartingTime.AddMilliseconds(islemSuresi);
                    DateTime olusturmaBitis = islemBitis.AddMilliseconds(islemSuresi);
                    //Notus.Print.Info(NVG.Settings, "Islem Bitis          : " + islemBitis.ToString("HH:mm:ss.fff"));
                    //Notus.Print.Info(NVG.Settings, "Blok Olusturma Bitis : " + olusturmaBitis.ToString("HH:mm:ss.fff"));

                    NVG.StartingTime = olusturmaBitis;
                    Notus.Variable.Struct.PoolBlockRecordStruct? TmpBlockStruct = NGF.BlockQueue.Get(islemBitis, olusturmaBitis);
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
                            PreBlockData = NGF.BlockQueue.OrganizeBlockOrder(PreBlockData);
                            Notus.Variable.Class.BlockData PreparedBlockData = new Notus.Block.Generate(NVG.Settings.NodeWallet.WalletKey).Make(PreBlockData, 1000);
                            ProcessBlock(PreparedBlockData, 4);
                            NGF.WalletUsageList.Clear();
                            ValidatorQueueObj.Distrubute(PreBlockData.info.rowNo, PreBlockData.info.type);
                            Thread.Sleep(1);
                        }
                        else
                        {
                            Notus.Print.Danger(NVG.Settings, "Pre Block Is NULL");
                        }
                    }
                    else
                    {
                        if ((DateTime.Now - LastPrintTime).TotalSeconds > 20)
                        {
                            LastPrintTime = DateTime.Now;
                            if (NVG.Settings.GenesisCreated == true)
                            {
                                tmpExitMainLoop = true;
                            }
                            else
                            {
                                //Console.WriteLine(JsonSerializer.Serialize(NGF.WalletUsageList));
                                Console.ForegroundColor = ConsoleColor.DarkGreen;
                                Console.Write("+");
                                Thread.Sleep(1);
                            }
                        }
                    }
                }
                else
                {
                    if (NVG.Settings.GenesisCreated == false)
                    {
                        if ((DateTime.Now - LastPrintTime).TotalSeconds > 20)
                        {
                            LastPrintTime = DateTime.Now;
                            Console.ForegroundColor = ConsoleColor.DarkMagenta;
                            Console.Write("-");
                            Thread.Sleep(1);
                        }
                        if (NGF.BlockQueue.CheckPoolDb == true)
                        {
                            //Console.WriteLine("NGF.BlockQueue.LoadFromPoolDb();");
                            NGF.BlockQueue.LoadFromPoolDb();
                        }
                    }
                }
            }
            if (NVG.Settings.GenesisCreated == true)
            {
                Notus.Print.Warning(NVG.Settings, "Main Class Temporary Ended");
            }
            else
            {
                Notus.Print.Warning(NVG.Settings, "Main Class Ended");
            }
        }

        private string fixedRowNoLength(Notus.Variable.Class.BlockData blockData)
        {
            return blockData.info.rowNo.ToString().PadLeft(15, '_');
        }
        private void ProcessBlock_PrintSection(Notus.Variable.Class.BlockData blockData, int blockSource)
        {
            if (blockSource == 1)
            {
                if (
                    blockData.info.type != Notus.Variable.Enum.BlockTypeList.EmptyBlock
                    &&
                    blockData.info.type != Notus.Variable.Enum.BlockTypeList.GenesisBlock
                )
                {
                    if (blockData.info.rowNo % 500 == 0)
                    {
                        Notus.Print.Status(NVG.Settings,
                            "Block Came From The Loading DB [ " + fixedRowNoLength(blockData) + " ] ->" +
                            blockData.info.type.ToString().PadLeft(4, ' ')
                        );
                    }
                }
            }
            if (blockSource == 2)
            {
                Notus.Print.Status(NVG.Settings,
                    "Block Came From The Validator Queue [ " + fixedRowNoLength(blockData) + " ] ->" +
                    blockData.info.type.ToString().PadLeft(4, ' ')
                );
            }
            if (blockSource == 3)
            {
                Notus.Print.Status(NVG.Settings,
                    "Block Came From The Block Sync [ " + fixedRowNoLength(blockData) + " ] ->" +
                    blockData.info.type.ToString().PadLeft(4, ' ')
                    );
            }
            if (blockSource == 4)
            {
                Notus.Print.Status(NVG.Settings,
                    "Block Came From The Main Loop [ " + fixedRowNoLength(blockData) + " ] ->" +
                    blockData.info.type.ToString().PadLeft(4, ' ')
                );
            }
            if (blockSource == 5)
            {
                Notus.Print.Status(NVG.Settings,
                    "Block Came From The Dictionary List [ " + fixedRowNoLength(blockData) + " ] ->" +
                    blockData.info.type.ToString().PadLeft(4, ' ')
                );
            }
            if (blockData.info.type == Notus.Variable.Enum.BlockTypeList.GenesisBlock)
            {
                RewardBlockObj.RewardList.Clear();
            }
            if (blockData.info.type == 255)
            {
                RewardBlockObj.RewardList.Clear();
                RewardBlockObj.LastTypeUid = blockData.info.uID;
            }
            if (blockData.info.type == Notus.Variable.Enum.BlockTypeList.EmptyBlock)
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
            /*
            Console.WriteLine(
                CurrentBlockRowNo.ToString()+
                " - " +
                blockData.info.rowNo.ToString() +
                    " - " +
                NVG.Settings.LastBlock.info.rowNo.ToString()
            );
            */
            if (blockData.info.rowNo > CurrentBlockRowNo)
            {
                string tmpBlockDataStr = JsonSerializer.Serialize(blockData);
                Notus.Variable.Class.BlockData? tmpBlockData =
                    JsonSerializer.Deserialize<Notus.Variable.Class.BlockData>(tmpBlockDataStr);
                if (tmpBlockData != null)
                {
                    if (IncomeBlockList.ContainsKey(blockData.info.rowNo) == true)
                    {
                        Console.WriteLine("Block Exist -> Line 937");
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("Add -> CurrentBlockRowNo    -> " + CurrentBlockRowNo.ToString());
                        Console.WriteLine("Add -> blockData.info.rowNo -> " + blockData.info.rowNo.ToString());
                        Console.WriteLine();
                        IncomeBlockList.Add(blockData.info.rowNo, tmpBlockData);
                        ProcessBlock_PrintSection(blockData, blockSource);
                        Notus.Print.Status(NVG.Settings, "Insert Block To Temporary Block List");
                    }
                }
                else
                {
                    ProcessBlock_PrintSection(blockData, blockSource);
                }
                return true;
            }
            if (CurrentBlockRowNo > blockData.info.rowNo)
            {
                ProcessBlock_PrintSection(blockData, blockSource);
                Notus.Print.Warning(NVG.Settings, "We Already Processed The Block");
                return true;
            }

            if (blockData.info.rowNo > NVG.Settings.LastBlock.info.rowNo)
            {
                NVG.Settings.LastBlock = blockData.Clone();

                if (blockData.info.type == 300)
                {
                    EmptyBlockGeneratedTime = Notus.Date.ToDateTime(blockData.info.time);
                }

                NGF.BlockQueue.AddToChain(blockData);
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
                        NVG.Settings.Network,
                        Notus.Variable.Enum.NetworkLayer.Layer3,
                        NVG.Settings
                    );
                    Console.WriteLine(responseData);
                }

                ProcessBlock_PrintSection(blockData, blockSource);
            }
            else
            {
                ProcessBlock_PrintSection(blockData, blockSource);
            }

            //DateTime start = DateTime.Now;
            //gelen blok burada işleniyor...
            Obj_Api.AddForCache(blockData);
            //Console.WriteLine(DateTime.Now - start);
            //Console.ReadLine();

            if (IncomeBlockList.ContainsKey(CurrentBlockRowNo))
            {
                Console.WriteLine("Remove -> " + CurrentBlockRowNo.ToString());
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
                Notus.Print.Basic(NVG.Settings, "Listining : " +
                Notus.Network.Node.MakeHttpListenerPath(NVG.Settings.IpInfo.Local, SelectedPortVal), false);
            }
            else
            {
                Notus.Print.Basic(NVG.Settings, "Listining : " +
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
            Notus.Print.Success(NVG.Settings, "Http Has Started", false);
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
            /*
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
            */

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
