using System.Collections.Concurrent;
using System.Net;
using System.Numerics;
using System.Text.Json;
using NBK = Notus.Block.Key;
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
        private Notus.Sync.Time TimeSyncObj = new Notus.Sync.Time();
        private Notus.Sync.Date NtpDateSyncObj = new Notus.Sync.Date();
        private Notus.Reward.Block RewardBlockObj = new Notus.Reward.Block();
        private Notus.Communication.Http HttpObj = new Notus.Communication.Http(true);
        private Notus.Block.Integrity Obj_Integrity;
        private Notus.Validator.Api Obj_Api;

        private bool FileStorageTimerIsRunning = false;
        private DateTime FileStorageTime = NVG.NOW.Obj;

        public SortedDictionary<ulong, string> TimeBaseBlockUidList = new SortedDictionary<ulong, string>();

        //bu liste diğer nodelardan gelen yeni blokları tutan liste
        public ulong FirstQueueGroupTime = 0;

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
                                    });

                                    ObjMp_FileStatus.Set(tmpStorageId, JsonSerializer.Serialize(NVE.BlockStatusCode.InProgress));
                                    try
                                    {
                                        File.Delete(outputFileName);
                                    }
                                    catch (Exception err3)
                                    {
                                        NP.Danger(NVG.Settings, "Error Text : [9abc546ac] : " + err3.Message);
                                    }
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
        private bool ControlEmptyBlockGenerationTime(ulong CurrentQueueTime)
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
            return executeEmptyBlock;
        }
        private void OrganizeAndDistributeBlock(NVClass.BlockData RawBlock, ulong CurrentQueueTime)
        {
            RawBlock = NGF.BlockQueue.OrganizeBlockOrder(RawBlock);
            NVClass.BlockData PreparedBlockData = new Notus.Block.Generate(NVG.Settings.NodeWallet.WalletKey).Make(RawBlock, 1000);

            if (ProcessBlock(PreparedBlockData, 4) == true)
            {
                //NP.Basic("blockIsValid Result : " + (blockIsValid == true ? "true" : "false"));
                ValidatorQueueObj.Distrubute(
                    RawBlock.info.rowNo,
                    RawBlock.info.type,
                    CurrentQueueTime
                );
                NGF.BlockQueue.RemoveTempPoolList();
                //Console.WriteLine("Block uId : " + RawBlock.info.uID);
            }
            else
            {
                if (RawBlock.info.type != NVE.BlockTypeList.EmptyBlock)
                {
                    //Console.WriteLine("Reload Tx From Temp List");
                    NGF.BlockQueue.ReloadPoolList();
                }
            }
            //NP.Basic("NGF.WalletUsageList.Clear(); -> CLEARED -> Main.cs");
            //NP.Basic(JsonSerializer.Serialize(NGF.WalletUsageList
            //NGF.WalletUsageList.Clear();
        }
        private void StartExecuteDistribiton(string incomeMessage, string messageResponse)
        {
            if (string.Equals(messageResponse, "state") == true)
            {
                return;
            }

            if (string.Equals(messageResponse, "distribute") != true)
            {
                Console.WriteLine("StartExecuteDistribiton : " + incomeMessage);
                Console.WriteLine("messageResponse: " + messageResponse);
            }

            NVS.HttpRequestDetails? tmpIncomeData =
                JsonSerializer.Deserialize<NVS.HttpRequestDetails>(NTT.GetPureText(incomeMessage, "poolData"));
            if (tmpIncomeData == null)
            {
                Console.WriteLine("Distribute : NULL");
                return;
            }

            NVS.CryptoTransferStatus requestStatus = NVG.BlockMeta.Status(tmpIncomeData.RequestUid);
            if (requestStatus.Code == NVE.BlockStatusCode.Completed)
            {
                Console.WriteLine("Distribute Data Income But It's Already Done: ");
                return;
            }
            Obj_Api.Interpret(tmpIncomeData, false);
        }
        public void Start()
        {
            NP.PrintOnScreenTimer();

            NVR.NetworkSelectorList.Clear();

            NVH.PrepareValidatorList();

            NGF.GetUtcTimeFromNode(20, true);

            TimeBaseBlockUidList.Clear();

            NVG.BlockMeta.LoadState();

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
                    var incomeMsgList = incomeMessage.Split("><");
                    for (int innerCount = 0; innerCount < incomeMsgList.Count(); innerCount++)
                    {
                        string tmpMessage = incomeMsgList[innerCount];
                        if (tmpMessage.Length > 0)
                        {
                            if (tmpMessage.Substring(tmpMessage.Length - 1) != ">")
                                tmpMessage = tmpMessage + ">";

                            string innerResponseStr = ValidatorQueueObj.ProcessIncomeData(incomeMessage);
                            //NP.Basic("Function Response : " + innerResponseStr);
                            StartExecuteDistribiton(incomeMessage, innerResponseStr);
                        }
                    }
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
                Dictionary<long, string> orderListResult = NVG.BlockMeta.Order();
                bool removeAllNextData = false;
                foreach (KeyValuePair<long, string> item in orderListResult)
                {
                    if (removeAllNextData == false)
                    {
                        NVClass.BlockData? tmpBlockData = NVG.BlockMeta.ReadBlock(item.Value);
                        if (tmpBlockData != null)
                        {
                            ProcessBlock(tmpBlockData, 1);
                        }
                        else
                        {
                            removeAllNextData = true;
                        }
                    }

                    if (removeAllNextData == true)
                    {
                        NP.Danger("Notus.Block.Integrity -> Block Does Not Exist -> [" + item.Key.ToString() + " ]");
                        NVG.BlockMeta.Remove(item.Key.ToString(), NVE.MetaDataDbTypeList.All);
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

                if (NVG.Settings.Layer == NVE.NetworkLayer.Layer2)
                {
                }
                if (NVG.Settings.Layer == NVE.NetworkLayer.Layer3)
                {
                    FileStorageTimer();
                }

                NVG.Settings.SyncBlockIsDone = true;
                NP.Success("First Synchronization Is Done");
            }
            DateTime LastPrintTime = NVG.NOW.Obj;
            bool tmpExitMainLoop = false;
            NVG.LocalBlockLoaded = true;

            // her node için ayrılan süre
            ulong queueTimePeriod = NVD.Calculate();

            bool generateEmptyBlock = false;
            bool start_FirstQueueGroupTime = false;
            bool prepareNextQueue = false;
            byte nodeOrderCount = 0;
            string selectedWalletId = string.Empty;
            ulong CurrentQueueTime = NVG.NodeQueue.Starting;
            bool myTurnPrinted = false;
            bool notMyTurnPrinted = false;

            NVG.ShowWhoseTurnOrNot = true;
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

            try
            {
                NVG.BlockController.LastBlockRowNo = NVG.Settings.LastBlock.info.rowNo;
            }
            catch { }
            NVG.BlockController.Start();

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
                        NGF.CloseMyNode(true);
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
                            selectedWalletId = "";
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
                                    //burada empty blok oluşturulması sırası geldiyse oluşturuluyor
                                    //ancak sonrasında diğer blokları oluşturmaya geçmesin
                                    if (emptyBlockChecked == false)
                                    {
                                        generateEmptyBlock = ControlEmptyBlockGenerationTime(CurrentQueueTime);
                                        if (generateEmptyBlock == true)
                                        {
                                            txExecuted = true;
                                            NP.Success("Empty Block Executed");
                                            Notus.Validator.Helper.CheckBlockAndEmptyCounter(NVE.BlockTypeList.EmptyBlock);
                                            NVClass.BlockData rawBlock = Notus.Variable.Class.Block.GetOrganizedEmpty(NVE.BlockTypeList.EmptyBlock);
                                            rawBlock.cipher.ver = "NE";
                                            rawBlock.cipher.data = System.Convert.ToBase64String(
                                                System.Text.Encoding.ASCII.GetBytes(
                                                    NVG.Settings.LastBlock.info.rowNo.ToString()
                                                )
                                            );
                                            rawBlock.info.uID = NGF.GenerateTxUid();
                                            rawBlock.info.time = NBK.GetTimeFromKey(rawBlock.info.uID, true);
                                            OrganizeAndDistributeBlock(rawBlock, CurrentQueueTime);
                                            generateEmptyBlock = false;
                                        }
                                        emptyBlockChecked = true;
                                    } // if (emptyBlockChecked == false)

                                    if (txExecuted == false)
                                    {
                                        /*
                                        burada işlemlerin çekilmesinde hata oluşuyor
                                        kontrol edilsin
                                        */
                                        NVClass.BlockData? PreBlockData = NGF.BlockQueue.Get(
                                            ND.AddMiliseconds(CurrentQueueTime, NVC.BlockListeningForPoolTime)
                                        );
                                        //ND.ToDateTime(CurrentQueueTime)
                                        if (PreBlockData != null)
                                        {
                                            txExecuted = true;
                                            //NP.Basic("---------------- OrganizeAndDistributeBlock ----------------");
                                            OrganizeAndDistributeBlock(PreBlockData, CurrentQueueTime);
                                            //NP.Basic("------------ OrganizeAndDistributeBlock Executed -----------");
                                        } //if (TmpBlockStruct != null)

                                        if (PreBlockData == null)
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

                                        NGF.WalletUsageList.Clear();
                                    }
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
                            NGF.BlockQueue.LoadFromPoolDb();
                        }// if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, selectedWalletId)) ELSE 

                        if (NVC.RegenerateNodeQueueCount == nodeOrderCount)
                        {
                            // eğer yeterli sayıda node yokse
                            // zamanları hazırlasın ancak node verileri boş oluşturulsun
                            if (NVC.MinimumNodeCount > NVG.OnlineNodeCount)
                            {
                                NP.Warning("Closing The Node Because There Are Not Enough Nodes");
                                NGF.CloseMyNode(true);
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
                            NTT.FixedRowNoLength(blockData) + " : " + blockData.info.type.ToString().PadLeft(4, ' ') + " ]"
                        );
                    }
                }
            }
            if (blockSource == 2)
            {
                NP.Status("Block Came From The Validator Queue [ " +
                    NTT.FixedRowNoLength(blockData) + " : " + blockData.info.type.ToString().PadLeft(4, ' ') + " ]"
                );
            }
            if (blockSource == 3)
            {
                NP.Status("Block Came From The Block Sync [ " +
                    NTT.FixedRowNoLength(blockData) + " : " + blockData.info.type.ToString().PadLeft(4, ' ') + " ]"
                );
            }
            if (blockSource == 4)
            {
                NP.Status("Block Came From The Main Loop [ " +
                    NTT.FixedRowNoLength(blockData) + " : " + blockData.info.type.ToString().PadLeft(4, ' ') + " ]"
                );
            }
            if (blockSource == 5)
            {
                NP.Status("Block Came From The Dictionary List [ " +
                    NTT.FixedRowNoLength(blockData) + " : " + blockData.info.type.ToString().PadLeft(4, ' ') + " ]"
                );
            }
            if (Notus.Sync.Block.downloadDone == true)
            {
                string blockGeneratorWalletId = NVH.BlockValidator(blockData);
                if (string.Equals(blockGeneratorWalletId, NVG.Settings.Nodes.My.IP.Wallet))
                {
                    blockGeneratorWalletId = "ME";
                }
                NP.Basic("Block Generated By [ " + NTT.FixedRowNoLength(blockData) + " ] : " + blockGeneratorWalletId);
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
            //NP.Basic("CurrentBlockRowNo : " + CurrentBlockRowNo.ToString());
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
                        //NVG.BlockMeta.WriteBlock(blockData, "Main -> Line -> 828");
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
                            }
                            if (blockSource == 4)
                            {
                                WrongBlockCount++;
                                NP.Warning("That Block Came My Validator But Wrong Queue Order");
                                NP.Danger("We Ignored This Block [ " + NTT.FixedRowNoLength(blockData) + " ] -> " + WrongBlockCount.ToString());
                                return false;
                            }
                        }
                        NP.Info("Exit From ProcessBlock Function");
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
                    //NVG.BlockMeta.WriteBlock(blockData, "Main -> Line -> 865");
                    if (string.Equals(NVH.BlockValidator(blockData), NVG.Settings.Nodes.My.IP.Wallet) == false)
                    {
                        NP.Info("New Block Arrived : " + blockData.info.uID.Substring(0, 15));
                    }
                }
            }

            bool addBlockToChain = false;
            // yeni blok geldiğinde burası devreye girecek
            if (blockData.info.rowNo > NVG.Settings.LastBlock.info.rowNo)
            {
                addBlockToChain = true;
                //NP.Basic("NVG.Settings.LastBlock.info.rowNo [ DEFINED -> before ]: " + NVG.Settings.LastBlock.info.rowNo.ToString());
                NVG.Settings.LastBlock = JsonSerializer.Deserialize<
                    NVClass.BlockData>(JsonSerializer.Serialize(blockData)
                );
                //NP.Basic("NVG.Settings.LastBlock.info.rowNo [ DEFINED -> after  ]: " + NVG.Settings.LastBlock.info.rowNo.ToString());
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
                        if (NVG.Settings.SyncBlockIsDone == true)
                        {
                            NP.Warning("We Already Processed The Block -> [ " + blockData.info.rowNo.ToString() + " ]");
                        }
                        return false;
                    }
                }
                else
                {
                    NP.Info("Control-Point -> Line 1168");
                }
                return true;
            }

            // eğer gelen blok yeni blok ise buraya girecek ve blok işlenir
            if (blockData.info.rowNo > NVG.Settings.LastBlock.info.rowNo)
            {
                NP.Info("Other-Control-Point -> Line 1176");
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

            //NP.Info("addBlockToChain : " + (addBlockToChain == true ? "true" : "false"));
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
            string resultData = Obj_Api.Interpret(IncomeData, true);
            if (string.Equals(resultData, "queue-data"))
            {
                resultData = ValidatorQueueObj.ProcessIncomeData(IncomeData.PostParams["data"]);
                NGF.SetNodeOnline(IncomeData.UrlList[2].ToLower());
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
