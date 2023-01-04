using System.Collections.Concurrent;
using System.Net;
using System.Numerics;
using System.Text.Json;
using NC = Notus.Ceremony;
using NCH = Notus.Communication.Helper;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NP2P = Notus.P2P;
using NTN = Notus.Toolbox.Network;
using NTT = Notus.Toolbox.Text;
using NVC = Notus.Variable.Constant;
using NVClass = Notus.Variable.Class;
using NVD = Notus.Validator.Date;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVJ = Notus.Validator.Join;
using NVR = Notus.Validator.Register;
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
        private int WrongBlockCount = 0;

        private Notus.Validator.Register ValidatorRegisterObj = new Notus.Validator.Register();
        private Notus.Sync.Validator ValidatorCountObj = new Notus.Sync.Validator();
        private Notus.Sync.Time TimeSyncObj = new Notus.Sync.Time();
        private Notus.Sync.Date NtpDateSyncObj = new Notus.Sync.Date();
        private Notus.Reward.Block RewardBlockObj = new Notus.Reward.Block();
        private Notus.Communication.Http HttpObj = new Notus.Communication.Http(true);
        private Notus.Block.Integrity Obj_Integrity;
        private Notus.Validator.Api Obj_Api;

        private bool CryptoTransferTimerIsRunning = false;
        private DateTime CryptoTransferTime = NVG.NOW.Obj;

        private bool FileStorageTimerIsRunning = false;
        private DateTime FileStorageTime = NVG.NOW.Obj;

        public SortedDictionary<ulong, string> TimeBaseBlockUidList = new SortedDictionary<ulong, string>();

        //bu liste diğer nodelardan gelen yeni blokları tutan liste
        public ulong FirstQueueGroupTime = 0;

        public SortedDictionary<long, NVClass.BlockData> IncomeBlockList = new SortedDictionary<long, NVClass.BlockData>();
        //private Notus.Block.Queue Obj_BlockQueue = new Notus.Block.Queue();
        private Notus.Validator.Queue ValidatorQueueObj = new Notus.Validator.Queue();

        public void GarbageCollector()
        {
            //buradan kaldırılacak ve timer kitaplığı oluşturulup onun içie eklenecek
        }
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
                                    string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpFileObj.PublicKey);
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
        private void ControlEmptyBlockGenerationTime()
        {
            int howManySeconds = NVG.Settings.Genesis.Empty.Interval.Time;
            if (NVG.Settings.Genesis.Empty.SlowBlock.Count >= NVG.Settings.EmptyBlockCount)
            {
                howManySeconds = (
                    NVG.Settings.Genesis.Empty.Interval.Time
                        *
                    NVG.Settings.Genesis.Empty.SlowBlock.Multiply
                );
            }
            //fast-empty-block-generation
            //howManySeconds = 15;
            ulong earliestTime = ND.ToLong(ND.ToDateTime(NVG.Settings.LastBlock.info.time).AddSeconds(howManySeconds));
            bool executeEmptyBlock = false;
            if (NVG.NOW.Int > earliestTime)
            {
                executeEmptyBlock = true;
            }
            if (NVG.Settings.OtherBlockCount > NVG.Settings.Genesis.Empty.Interval.Block)
            {
                executeEmptyBlock = true;
            }
            if (executeEmptyBlock == true)
            {
                NP.Success("Empty Block Executed");
                Notus.Validator.Helper.CheckBlockAndEmptyCounter(300);
                NGF.BlockQueue.AddEmptyBlock();
            }
        }
        public void Start()
        {
            NP.ExecuteTime();

            NVR.NetworkSelectorList.Clear();

            NVH.PrepareValidatorList();

            NGF.GetUtcTimeFromNode(20, true);

            TimeBaseBlockUidList.Clear();

            if (NVG.Settings.GenesisCreated == false)
            {
                TimeSyncObj.Start();
                NtpDateSyncObj.Start();
            }

            Obj_Integrity = new Notus.Block.Integrity();
            Obj_Integrity.IsGenesisNeed();

            int p2pPortNo = Notus.Network.Node.GetP2PPort();
            NP.Info("Node P2P Port No : " + p2pPortNo.ToString());
            NVG.Settings.PeerManager = new NP2P.Manager(
                new IPEndPoint(IPAddress.Any, p2pPortNo),
                p2pPortNo,
                (string incomeMessage) =>
                {
                    Console.WriteLine("incomeMessage [NVG.Settings.PeerManager] : " + incomeMessage);
                    string innerResultStr = ValidatorQueueObj.ProcessIncomeData(incomeMessage);
                    if (string.Equals(innerResultStr, "done") == false)
                    {
                        NP.Basic("Function Response : " + innerResultStr);
                    }
                    /*
                    if (string.Equals(incomeMessage, "<ping>1</ping>") == false)
                    {
                        Console.WriteLine("incomeMessage : " + incomeMessage);
                    }

                    if (
                        string.Equals(innerResultStr, "done") == false
                        &&
                        string.Equals(innerResultStr, "pong") == false
                    )
                    {
                        NP.Basic("Function Response : " + innerResultStr);
                    }
                    */
                }
            , false);

            Obj_Api = new Notus.Validator.Api();

            Start_HttpListener();

            bool controlStatus = Obj_Integrity.ControlGenesisBlock(); // we check and compare genesis with another node
            /*
            if (controlStatus == true)
            {
                // eğer diğer node'lardan Genesis alındı ise TRUE
                Console.WriteLine("Obj_Integrity.ControlGenesisBlock : TRUE");
            }
            else
            {
                // eğer diğer node'lardan Genesis alınmadıysa FALSE
                Console.WriteLine("Obj_Integrity.ControlGenesisBlock : FALSE");
            }
            */

            Obj_Integrity.GetLastBlock();        // get last block from current node

            if (NVG.Settings.Genesis == null)
            {
                NP.Basic(NVG.Settings, "Notus.Validator.Main -> Genesis Is NULL");
            }

            NGF.BlockQueue.Start();

            Obj_Api.Prepare();

            // Obj_MainCache = new Notus.Cache.Main();
            // Obj_TokenStorage = new Notus.Token.Storage();
            // Obj_TokenStorage.Settings = NVG.Settings;

            if (NVG.Settings.GenesisCreated == false && NVG.Settings.Genesis != null)
            {
                NP.Basic(NVG.Settings, "Last Block Row No : " + NVG.Settings.LastBlock.info.rowNo.ToString());
                using (Notus.Block.Storage Obj_Storage = new Notus.Block.Storage(false))
                {
                    Dictionary<long, string> orderListResult = NVG.Settings.BlockOrder.List();
                    foreach (KeyValuePair<long, string> item in orderListResult)
                    {
                        NVClass.BlockData? tmpBlockData = Obj_Storage.ReadBlock(item.Value);
                        if (tmpBlockData != null)
                        {
                            ProcessBlock(tmpBlockData, 1);
                        }
                        else
                        {
                            NP.Danger("Notus.Block.Integrity -> Block Does Not Exist");
                            NP.Danger("Reset Block");
                            NP.ReadLine();
                        }
                    }
                }
                NP.Info("All Blocks Loaded");
            }

            if (NVG.Settings.GenesisCreated == false)
            {
                NP.Basic("Main Validator Started");
            }
            //BlockStatObj = Obj_BlockQueue.CurrentBlockStatus();
            if (NVG.Settings.LocalNode == false)
            {
                // her gelen blok bir listeye eklenmeli ve o liste ile sıra ile eklenmeli
                ValidatorQueueObj.Func_NewBlockIncome = tmpNewBlockIncome =>
                {
                    if (tmpNewBlockIncome == null)
                    {
                        Console.WriteLine("Main.cs -> Income Block Is NULL !!!!");
                        return true;
                    }
                    ProcessBlock(tmpNewBlockIncome, 2);
                    return true;
                };
            }

            if (NVG.Settings.GenesisCreated == false)
            {
                NVG.Settings.CommEstablished = true;
            }

            ValidatorQueueObj.PreStart();

            ValidatorRegisterObj.Start();

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
                    bool waitForOtherNodes = Notus.Sync.Block.Data(
                        NVH.GiveMeNodeList(),
                        tmpNewBlockIncome =>
                        {
                            //sync-control
                            ProcessBlock(tmpNewBlockIncome, 3);
                        }
                    );

                    while (Notus.Sync.Block.downloadDone == false)
                    {
                        Thread.Sleep(10);
                    }

                    if (MyReadyMessageSended == false && waitForOtherNodes == false)
                    {
                        FirstSyncIsDone = true;
                        MyReadyMessageSended = true;
                    }
                    else
                    {
                        if (FirstSyncIsDone == false && MyReadyMessageSended == false)
                        {
                            if (IncomeBlockList.Count == 0)
                            {
                                FirstSyncIsDone = true;
                                MyReadyMessageSended = true;
                            }
                        }
                    }
                }
            }

            if (NVG.Settings.GenesisCreated == false)
            {
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

                NVG.Settings.SyncBlockIsDone = true;
                NP.Success("First Synchronization Is Done");
                GarbageCollector();
            }
            DateTime LastPrintTime = NVG.NOW.Obj;
            bool tmpStartWorkingPrinted = false;
            bool tmpExitMainLoop = false;
            if (NVG.Settings.LocalNode == true)
            {
                ValidatorQueueObj.WaitForEnoughNode = false;
            }

            NVG.LocalBlockLoaded = true;

            // her node için ayrılan süre
            ulong queueTimePeriod = NVD.Calculate();

            bool start_FirstQueueGroupTime = false;
            bool prepareNextQueue = false;
            byte nodeOrderCount = 0;
            string selectedWalletId = string.Empty;
            ulong CurrentQueueTime = NVG.NodeQueue.Starting;
            bool myTurnPrinted = false;
            bool notMyTurnPrinted = false;

            NVG.ShowWhoseTurnOrNot = false;

            if (NVG.OtherValidatorSelectedMe == true)
            {
                NP.Basic("Node Is Waiting In The Waiting Room");

                ulong nowUtcValue = NVG.NOW.Int;
                string controlSignForReadyMsg = Notus.Wallet.ID.Sign(
                    nowUtcValue.ToString() +
                        NVC.Delimeter +
                    NVG.Settings.Nodes.My.IP.Wallet,
                    NVG.Settings.Nodes.My.PrivateKey
                );

                foreach (var iE in NVG.NodeList)
                {
                    if (string.Equals(iE.Key, NVG.Settings.Nodes.My.HexKey) == false)
                    {
                        string resultStr = NCH.SendMessageED(
                            iE.Key, iE.Value.IP,
                            "<waitingRoomNodeReady>" +
                                NVG.Settings.Nodes.My.IP.Wallet +
                                    NVC.Delimeter +
                                nowUtcValue.ToString() +
                                    NVC.Delimeter +
                                controlSignForReadyMsg +
                            "</waitingRoomNodeReady>"
                        );
                    }
                }
                ValidatorQueueObj.StartingPing();
            }

            while (
                tmpExitMainLoop == false &&
                NVG.Settings.NodeClosing == false &&
                NVG.Settings.GenesisCreated == false
            )
            {
                if (NVG.OtherValidatorSelectedMe == true)
                {
                    // diğer validatörler tarafından ağa dahil edilecek olan node
                    // burada yapılan kontrol ile ağa dahil edilecek.
                    if ((NVG.NOW.Obj - LastPrintTime).TotalSeconds > 20)
                    {
                        LastPrintTime = NVG.NOW.Obj;
                        if (NVG.Settings.GenesisCreated == true)
                        {
                            tmpExitMainLoop = true;
                        }
                        else
                        {
                            NP.WaitDot();
                        }
                    }
                }
                else
                {
                    if (NVC.MinimumNodeCount > NVG.OnlineNodeCount)
                    {
                        NP.Warning("Closing The Node Because There Are Not Enough Nodes");
                        NGF.CloseMyNode();
                    }
                    if (prepareNextQueue == false)
                    {
                        prepareNextQueue = true;
                        if (NVG.Settings.Nodes.Queue.ContainsKey(CurrentQueueTime))
                        {
                            selectedWalletId = NVG.Settings.Nodes.Queue[CurrentQueueTime].Wallet;
                            if (selectedWalletId.Length == 0)
                            {
                                if (NVG.Settings.NodeClosing == true)
                                {
                                    tmpExitMainLoop = true;
                                    NVG.Settings.ClosingCompleted = true;
                                }
                            }
                        }
                        else
                        {
                            if (NVG.NOW.Int > NVG.NodeList[NVG.Settings.Nodes.My.HexKey].JoinTime)
                            {
                                NP.Danger("Queue Time Info Does Not In The List");
                            }
                        }
                    }

                    /*
                    hatanın kaynağı şu:
                    validatör blok oluşturuyor, bu bloğu belirlenen süre içerisinde diğer validatörlere iletemiyor.

                    diğer validatör sırası gelince blok üretme işlemi yapıyor ancak o blok numarası diğer validatör
                    tarafından üretildiği için çakışma gerçekleşiyor.

                    Validatörler şu şekilde çalışacak.
                    sırsı gelen bloğunu oluşturacak.
                    eğer doğru sırada iletilmezse o zaman gelen blok reddedilmeyecek
                    alınacak ve bloklar sırasıyla ileri doğru atılacak.
                    */

                    if (NGF.NowInt() > CurrentQueueTime)
                    {
                        nodeOrderCount++;
                        if (nodeOrderCount == 1)
                        {
                            FirstQueueGroupTime = CurrentQueueTime;
                            TimeBaseBlockUidList.Add(CurrentQueueTime, "");
                            TimeBaseBlockUidList[CurrentQueueTime] = "id:" + CurrentQueueTime.ToString();
                        } // if (nodeOrderCount == 1)

                        if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, selectedWalletId))
                        {
                            while (NVG.Settings.WaitForGeneratedBlock == true)
                            {
                                Thread.Sleep(1);
                            }

                            bool txExecuted = false;
                            bool emptyBlockChecked = false;

                            ulong endingTime = ND.AddMiliseconds(CurrentQueueTime, queueTimePeriod - 10);
                            if (myTurnPrinted == false)
                            {
                                myTurnPrinted = true;
                                if (NVG.ShowWhoseTurnOrNot == true)
                                {
                                    NP.Info("My Turn : " + CurrentQueueTime.ToString() + " -> " + endingTime.ToString());
                                }
                            }

                            while (endingTime > NGF.NowInt())
                            {
                                if (txExecuted == false)
                                {
                                    if (emptyBlockChecked == false)
                                    {
                                        ControlEmptyBlockGenerationTime();
                                        emptyBlockChecked = true;
                                    } // if (emptyBlockChecked == false)

                                    (List<string>? poolList, NVS.PoolBlockRecordStruct? TmpBlockStruct) = NGF.BlockQueue.Get(
                                        ND.AddMiliseconds(CurrentQueueTime, NVC.BlockListeningForPoolTime)
                                    );
                                    if (TmpBlockStruct != null)
                                    {
                                        txExecuted = true;
                                        NVClass.BlockData? PreBlockData = JsonSerializer.Deserialize<NVClass.BlockData>(TmpBlockStruct.data);
                                        if (PreBlockData != null)
                                        {
                                            PreBlockData = NGF.BlockQueue.OrganizeBlockOrder(PreBlockData);
                                            NVClass.BlockData PreparedBlockData = new Notus.Block.Generate(NVG.Settings.NodeWallet.WalletKey).Make(PreBlockData, 1000);
                                            bool processResult = ProcessBlock(PreparedBlockData, 4);
                                            if (processResult == true)
                                            {
                                                ValidatorQueueObj.Distrubute(
                                                    PreBlockData.info.rowNo,
                                                    PreBlockData.info.type,
                                                    CurrentQueueTime
                                                );
                                                NGF.BlockQueue.RemovePermanentlyFromDb(poolList);
                                                NGF.BlockQueue.RemovePoolIdList(poolList);
                                            }
                                            else
                                            {
                                                if (PreparedBlockData.info.type == NVE.BlockTypeList.AirDrop)
                                                {
                                                    Console.WriteLine("if(PreparedBlockData.info.type== NVE.BlockTypeList.AirDrop)");
                                                }
                                                else
                                                {
                                                    Console.WriteLine("NGF.BlockQueue.ReloadPoolList(poolList);");
                                                    NGF.BlockQueue.ReloadPoolList(poolList);
                                                }
                                            }
                                            NGF.WalletUsageList.Clear();
                                        } // if (PreBlockData != null)
                                        else
                                        {
                                            NP.Danger(NVG.Settings, "Pre Block Is NULL");
                                        } // if (PreBlockData != null) ELSE
                                    } //if (TmpBlockStruct != null)
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
                                                NP.WaitDot();
                                            }
                                        }
                                    } // if (TmpBlockStruct != null) ELSE 
                                } // if (txExecuted == false)
                            } // while (endingTime >= NGF.NowInt())
                        }// if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, selectedWalletId))
                        else
                        {
                            if (notMyTurnPrinted == false)
                            {
                                notMyTurnPrinted = true;
                                if (NVG.ShowWhoseTurnOrNot == true)
                                {
                                    NP.Info("Not My Turn : " + CurrentQueueTime.ToString());
                                }
                            }

                            NGF.BlockQueue.LoadFromPoolDb(false);
                        }// if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, selectedWalletId)) ELSE 

                        if (NVC.RegenerateNodeQueueCount == nodeOrderCount)
                        {
                            // eğer yeterli sayıda node yokse
                            // zamanları hazırlasın ancak node verileri boş oluşturulsun
                            if (NVC.MinimumNodeCount > NVG.OnlineNodeCount)
                            {
                                NP.Warning("Closing The Node Because There Are Not Enough Nodes");
                                NGF.CloseMyNode();
                            }
                            else
                            {
                                ValidatorQueueObj.ReOrderNodeQueue(
                                    CurrentQueueTime,
                                    (
                                        start_FirstQueueGroupTime == true
                                            ?
                                        TimeBaseBlockUidList[FirstQueueGroupTime]
                                            :
                                        ""
                                    )
                                );
                                NVJ.TellTheNodeToJoinTime(CurrentQueueTime);
                            }
                        } //if (NVC.RegenerateNodeQueueCount == nodeOrderCount)

                        if (nodeOrderCount == NVC.NodeOrderGroupSize)
                        {
                            nodeOrderCount = 0;
                            start_FirstQueueGroupTime = true;
                            NVG.Settings.PeerManager.MovePeerList();
                        } //if (nodeOrderCount == 6)

                        prepareNextQueue = false;
                        myTurnPrinted = false;
                        notMyTurnPrinted = false;
                        CurrentQueueTime = ND.AddMiliseconds(CurrentQueueTime, queueTimePeriod);
                    }  // if (NGF.NowInt() >= currentQueueTime)
                }
            } // while ( tmpExitMainLoop == false && NVG.Settings.NodeClosing == false && NVG.Settings.GenesisCreated == false )
            NP.MainClassClosingControl();
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
            if (Notus.Sync.Block.downloadDone == true)
            {
                string blockGeneratorWalletId = NVH.BlockValidator(blockData);
                if (string.Equals(blockGeneratorWalletId, NVG.Settings.Nodes.My.IP.Wallet))
                {
                    blockGeneratorWalletId = "ME";
                }
                NP.Basic("Block Generated By [ " + fixedRowNoLength(blockData) + " ] : " + blockGeneratorWalletId);
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
            if (blockSource == 2 || blockSource == 4)
            {
                bool innerSendToMyChain = false;
                if (NVG.OtherValidatorSelectedMe == false)
                {
                    try
                    {
                        innerSendToMyChain = NVH.RightBlockValidator(blockData, "Process Block - Main.cs Line 1095 -> blockSource => " + blockSource.ToString());
                    }
                    catch (Exception innerErr)
                    {
                        Console.WriteLine("Main.cs -> tmpNewBlockIncome.info.time : " + blockData.info.time);
                        Console.WriteLine("Main.cs -> innerErr.Message : " + innerErr.Message);
                    }
                    if (innerSendToMyChain == true)
                    {
                        NVG.Settings.BlockOrder.Add(blockData.info.rowNo, blockData.info.uID);
                        NVG.Settings.BlockSign.Add(blockData.info.rowNo, blockData.sign);
                        NVG.Settings.BlockPrev.Add(blockData.info.rowNo, blockData.prev);
                        if (string.Equals(NVH.BlockValidator(blockData), NVG.Settings.Nodes.My.IP.Wallet) == false)
                        {
                            NP.Info("New Block Arrived : " + blockData.info.uID.Substring(0, 15));
                        }
                    }
                    else
                    {
                        if (Notus.Sync.Block.downloadDone == true)
                        {
                            if (blockSource == 2)
                            {
                                NP.Warning("That block came from validator and wrong block");
                                NP.PrintQueue(true, "Main.cs -> Line 1099");
                            }
                            if (blockSource == 4)
                            {
                                WrongBlockCount++;
                                NP.Warning("That Block Came My Validator But Wrong Queue Order");
                                NP.Danger("We Ignored This Block [ " + fixedRowNoLength(blockData) + " ] -> " + WrongBlockCount.ToString());
                                return false;
                            }
                        }
                        return false;
                    }
                }
                else
                {
                    /*

                    eğer validatör diğer validatörler tarafından ağa dahil edilen bir validatör ise,
                    gelen bloğu diğer validatörler tarafından doğrulayacak
                    eğer blok imzası doğru ise
                    validatör bloğu kabul edecek

                    */
                    innerSendToMyChain = true;
                    NVG.Settings.BlockOrder.Add(blockData.info.rowNo, blockData.sign);
                    NVG.Settings.BlockOrder.Add(blockData.info.rowNo, blockData.info.uID);
                    if (string.Equals(NVH.BlockValidator(blockData), NVG.Settings.Nodes.My.IP.Wallet) == false)
                    {
                        NP.Info("New Block Arrived : " + blockData.info.uID.Substring(0, 15));
                    }
                }
            }
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
                Notus.Validator.Helper.CheckBlockAndEmptyCounter(blockData.info.type);
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
                        if (blockSource != 1)
                        {
                            NP.Status(NVG.Settings, "Insert Block To Temporary Block List");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Block Convert Error -> Main.Cs -> Line 986");
                }
                return true;
            }

            // eğer gelen blok zaten işlenmiş blok ise gözardı edilecek
            if (CurrentBlockRowNo > blockData.info.rowNo)
            {
                if (blockSource != 1)
                {
                    if (NVG.Settings.SyncBlockIsDone == true)
                    {
                    }
                    bool innerSendToMyChain = NVH.RightBlockValidator(blockData, "Process Block -> Zaten işlenmiş blok -> Line 1200 -> blockSource => " + blockSource.ToString());
                    if (innerSendToMyChain == true)
                    {
                        NP.Success("This Is Correct Block - Store Your Local Data");
                    }
                    else
                    {
                        ProcessBlock_PrintSection(blockData, blockSource);
                        NP.Warning(NVG.Settings, "We Already Processed The Block -> [ " + blockData.info.rowNo.ToString() + " ]");
                        return false;
                    }
                }
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

            if (addBlockToChain == true)
            {
                NGF.BlockQueue.AddToChain(blockData);
            }

            //gelen blok burada işleniyor...
            Obj_Api.AddForCache(blockData, blockSource);

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
                        //NVG.NodeList[NVG.Settings.Nodes.My.HexKey].Ready = true;
                    }
                }
            }

            return true;
        }

        private void Start_HttpListener()
        {
            SelectedPortVal = NVG.Settings.Nodes.My.IP.Port;
            if (NVG.Settings.GenesisCreated == false)
            {
                IPAddress NodeIpAddress = IPAddress.Parse(
                    NVG.Settings.LocalNode == false
                        ?
                    NVG.Settings.IpInfo.Public
                        :
                    NVG.Settings.IpInfo.Local
                );

                NP.Basic("Listining : " + Notus.Network.Node.MakeHttpListenerPath(NodeIpAddress.ToString(), SelectedPortVal));
                HttpObj.DefaultResult_OK = "null";
                HttpObj.DefaultResult_ERR = "null";
                HttpObj.OnReceive(Fnc_OnReceiveData);
                HttpObj.ResponseType = "application/json";
                HttpObj.StoreUrl = false;
                HttpObj.Start(NodeIpAddress, SelectedPortVal);
                NP.Success("Http Has Started");
            }
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
