using Notus.Compression.TGZ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using NVE = Notus.Variable.Enum;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
using NVC = Notus.Variable.Constant;
using NBD = Notus.Block.Decrypt;
using NVClass = Notus.Variable.Class;
namespace Notus.Block
{
    public class Queue : IDisposable
    {
        public bool CheckPoolDb = false;                // time difference before or after NTP Server

        private Notus.Mempool MP_BlockPoolList;
        private Notus.Block.Storage BS_Storage;
        private ConcurrentDictionary<string, byte> PoolIdList = new ConcurrentDictionary<string, byte>();
        private ConcurrentDictionary<int, List<NVS.List_PoolBlockRecordStruct>> Obj_PoolTransactionList =
            new ConcurrentDictionary<int, List<NVS.List_PoolBlockRecordStruct>>();
        private Queue<NVS.List_PoolBlockRecordStruct> Queue_PoolTransaction = new Queue<NVS.List_PoolBlockRecordStruct>();

        //bu foknsiyonun görevi blok sırası ve önceki değerlerini blok içeriğine eklemek
        public List<NVS.List_PoolBlockRecordStruct>? GetPoolList(int BlockType)
        {
            if (Obj_PoolTransactionList.ContainsKey(BlockType))
            {
                return Obj_PoolTransactionList[BlockType];
            }
            return null;
        }
        public Dictionary<int, int>? GetPoolCount()
        {
            Dictionary<int, int> resultList = new Dictionary<int, int>();
            foreach (KeyValuePair<int, List<NVS.List_PoolBlockRecordStruct>> entry in Obj_PoolTransactionList)
            {
                resultList.Add(entry.Key, entry.Value.Count);
            }
            return resultList;
        }

        public NVClass.BlockData OrganizeBlockOrder(NVClass.BlockData CurrentBlock)
        {
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
        public (List<string>?, NVS.PoolBlockRecordStruct?) Get(
            ulong WaitingForPool
        )
        {
            if (Queue_PoolTransaction.Count == 0)
            {
                //Console.WriteLine("Queue_PoolTransaction.Count.ToString() : " + Queue_PoolTransaction.Count.ToString());
                return (null, null);
            }

            int diffBetween = System.Convert.ToInt32(MP_BlockPoolList.Count() / Queue_PoolTransaction.Count);
            if (diffBetween > 10)
            {
                CheckPoolDb = true;
            }
            else
            {
                if (MP_BlockPoolList.Count() < 10)
                {
                    CheckPoolDb = true;
                }
                if (Queue_PoolTransaction.Count < 10)
                {
                    CheckPoolDb = true;
                }
            }

            int CurrentBlockType = -1;
            List<string> TempWalletList = new List<string>() { NVG.Settings.NodeWallet.WalletKey };

            List<string> TempBlockList = new List<string>();
            List<NVS.List_PoolBlockRecordStruct> TempPoolTransactionList = new List<NVS.List_PoolBlockRecordStruct>();
            bool exitLoop = false;
            string transactionId = string.Empty;
            while (exitLoop == false)
            {
                //NGF.UpdateUtcNowValue();
                if (Queue_PoolTransaction.Count > 0)
                {
                    NVS.List_PoolBlockRecordStruct? TmpPoolRecord = Queue_PoolTransaction.Peek();
                    if (TmpPoolRecord == null)
                    {
                        exitLoop = true;
                    }
                    else
                    {
                        if (CurrentBlockType == -1)
                        {
                            CurrentBlockType = TmpPoolRecord.type;
                        }

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
                                }
                                else
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
                                }
                                else
                                {
                                    // out işlemindeki cüzdanları kontrol ediyor...
                                    foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> tmpEntry in tmpBlockCipherData.Out)
                                    {
                                        if (TempWalletList.IndexOf(tmpEntry.Key) == -1)
                                        {
                                            TempWalletList.Add(tmpEntry.Key);
                                        }
                                        else
                                        {
                                            addToList = false;
                                        }
                                    }

                                    if (addToList == false)
                                    {
                                        Queue_PoolTransaction.Enqueue(TmpPoolRecord);
                                        Obj_PoolTransactionList[CurrentBlockType].Add(TmpPoolRecord);
                                    }
                                }
                            }

                            if (CurrentBlockType == NVE.BlockTypeList.CryptoTransfer)
                            {
                                NVClass.BlockStruct_120? tmpBlockCipherData = JsonSerializer.Deserialize<NVClass.BlockStruct_120>(TmpPoolRecord.data);
                                if (tmpBlockCipherData == null)
                                {
                                    addToList = false;
                                }
                                else
                                {
                                    // out işlemindeki cüzdanları kontrol ediyor...
                                    foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> tmpEntry in tmpBlockCipherData.Out)
                                    {
                                        if (TempWalletList.IndexOf(tmpEntry.Key) == -1)
                                        {
                                            TempWalletList.Add(tmpEntry.Key);
                                        }
                                        else
                                        {
                                            addToList = false;
                                        }
                                    }

                                    if (addToList == false)
                                    {
                                        Queue_PoolTransaction.Enqueue(TmpPoolRecord);
                                        Obj_PoolTransactionList[CurrentBlockType].Add(TmpPoolRecord);
                                        //exitLoop = true;
                                    }
                                }
                            }

                            if (addToList == true)
                            {
                                TempPoolTransactionList.Add(TmpPoolRecord);
                                TempBlockList.Add(TmpPoolRecord.data);
                            }

                            Queue_PoolTransaction.Dequeue();
                            Obj_PoolTransactionList[CurrentBlockType].RemoveAt(0);
                            if (
                                TempPoolTransactionList.Count == NVC.BlockTransactionLimit ||
                                CurrentBlockType == 240 || // layer1 - > dosya ekleme isteği
                                CurrentBlockType == 250 || // layer3 - > dosya içeriği
                                CurrentBlockType == NVE.BlockTypeList.EmptyBlock ||
                                CurrentBlockType == NVE.BlockTypeList.MultiWalletCryptoTransfer
                            )
                            {
                                exitLoop = true;
                            }
                        }
                        else
                        {
                            exitLoop = true;
                        }
                    }
                }
                else
                {
                    exitLoop = true;
                }
                if (NVG.NOW.Int >= WaitingForPool)
                {
                    exitLoop = true;
                }
            }
            if (TempPoolTransactionList.Count == 0)
            {
                Console.WriteLine("TempPoolTransactionList.Count : " + TempPoolTransactionList.Count.ToString());
                return (null, null);
            }

            NVClass.BlockData BlockStruct = NVClass.Block.GetEmpty();

            if (NVC.BlockNonceType.ContainsKey(CurrentBlockType) == true)
            {
                BlockStruct.info.nonce.type = NVC.BlockNonceType[CurrentBlockType];     // 1-Slide, 2-Bounce
            }
            else
            {
                BlockStruct.info.nonce.type = NVC.Default_BlockNonceType;     // 1-Slide, 2-Bounce
            }

            if (NVC.BlockNonceMethod.ContainsKey(CurrentBlockType) == true)
            {
                BlockStruct.info.nonce.method = NVC.BlockNonceMethod[CurrentBlockType];   // which hash algorithm
            }
            else
            {
                BlockStruct.info.nonce.method = NVC.Default_BlockNonceMethod;   // which hash algorithm
            }

            if (NVC.BlockDifficulty.ContainsKey(CurrentBlockType) == true)
            {
                BlockStruct.info.nonce.difficulty = NVC.BlockDifficulty[CurrentBlockType];  // block difficulty level
            }
            else
            {
                BlockStruct.info.nonce.difficulty = NVC.Default_BlockDifficulty;  // block difficulty level
            }

            string LongNonceText = string.Empty;

            BlockStruct.cipher.ver = "NE";
            if (transactionId.Length == 0)
            {
                BlockStruct.info.uID = NGF.GenerateTxUid();
                //BlockStruct.info.uID = Notus.Block.Key.Generate(GetNtpTime(), NVG.Settings.NodeWallet.WalletKey);
            }
            else
            {
                BlockStruct.info.uID = transactionId;
            }

            if (CurrentBlockType == NVE.BlockTypeList.GenesisBlock)
            {
                LongNonceText = TempPoolTransactionList[0].data;
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
                    //Console.WriteLine(JsonSerializer.Serialize( TempBlockList));
                    //Console.ReadLine();
                    if (TempBlockList.Count > 1)
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
                        //Console.WriteLine(JsonSerializer.Serialize(tmpBlockCipherData, NVC.JsonSetting));
                        //Console.ReadLine();
                        TempBlockList.Add(JsonSerializer.Serialize(tmpBlockCipherData));
                    }
                    //Console.WriteLine(TempBlockList);
                    //Console.ReadLine();
                }
                if (CurrentBlockType == NVE.BlockTypeList.CryptoTransfer)
                {
                    if (TempBlockList.Count > 1)
                    {
                        //Console.WriteLine(JsonSerializer.Serialize( TempBlockList));
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
                        //Console.WriteLine(tmpBlockCipherData.Out.Count)
                        TempBlockList.Add(JsonSerializer.Serialize(tmpBlockCipherData));
                    }
                }
                LongNonceText = string.Join(NVC.Delimeter, TempBlockList.ToArray());
            }

            BlockStruct.prev = "";
            BlockStruct.info.prevList.Clear();

            BlockStruct.info.time = Notus.Block.Key.GetTimeFromKey(BlockStruct.info.uID, true);

            BlockStruct.info.type = CurrentBlockType;
            /*
            if (CurrentBlockType == 300)
            {
                BlockStruct.cipher.data = LongNonceText;
            }
            else
            {
            }
            */
            BlockStruct.cipher.data = System.Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes(
                    LongNonceText
                )
            );

            List<string> removeRoolList = new();
            for (int i = 0; i < TempPoolTransactionList.Count; i++)
            {
                removeRoolList.Add(TempPoolTransactionList[i].key);
            }
            //burası pooldaki kayıtların fazla birikmesi ve para transferi işlemlerinin key'lerinin örtüşmemesinden
            //dolayı eklendi
            //Console.WriteLine("removeRoolList.Count : " + removeRoolList.Count.ToString());
            return (
                removeRoolList,
                new NVS.PoolBlockRecordStruct()
                {
                    type = CurrentBlockType,
                    data = JsonSerializer.Serialize(BlockStruct)
                }
            );
        }


        /*
        
        buraya blok sıra numarası ile okuma işlemi eklenecek
        public (bool, NVClass.BlockData) ReadWithRowNo(Int64 BlockRowNo)
        {
            if (Obj_LastBlock.info.rowNo >= BlockNumber)
            {
                bool exitPrevWhile = false;
                string PrevBlockIdStr = Obj_LastBlock.prev;
                while (exitPrevWhile == false)
                {
                    PrevBlockIdStr = PrevBlockIdStr.Substring(0, 90);
                    (bool BlockExist, NVClass.BlockData tmpStoredBlock) = ReadFromChain(PrevBlockIdStr);
                    if (BlockExist == true)
                    {
                        if (tmpStoredBlock.info.rowNo == BlockRowNo)
                        {
                            return (true,tmpStoredBlock);
                        }
                        else
                        {
                            PrevBlockIdStr = tmpStoredBlock.prev;
                        }
                    }
                    else
                    {
                        exitPrevWhile = true;
                    }
                }
            }
        }
        */

        public void ReloadPoolList(List<string>? poolList)
        {
            if (poolList != null)
            {
                if (poolList.Count > 0)
                {
                    RemovePoolIdList(poolList);
                }
            }
            LoadFromPoolDb(true);
        }
        public void RemovePermanentlyFromDb(List<string>? poolList)
        {
            if (poolList != null)
            {
                for (int i = 0; i < poolList.Count; i++)
                {
                    Console.WriteLine("Remove Key From DB : " + poolList[i]);
                    MP_BlockPoolList.Remove(poolList[i], true);
                }
            }
        }
        public void RemovePoolIdList(List<string>? poolList)
        {
            if (poolList != null)
            {
                for (int i = 0; i < poolList.Count; i++)
                {
                    Console.WriteLine("Remove Key From List : " + poolList[i]);
                    PoolIdList.TryRemove(poolList[i], out _);
                }
            }
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

            string rawDataStr = Notus.Toolbox.Text.RawCipherData2String(
                NewBlock.cipher.data
            );

            string RemoveKeyStr = string.Empty;
            if (NewBlock.info.type == NVE.BlockTypeList.AirDrop)
            {
                NVClass.BlockStruct_125? tmpLockBalance = NBD.Convert_125(rawDataStr,false);
                if (tmpLockBalance != null)
                {
                    foreach (var entry in tmpLockBalance.In)
                    {
                        NVG.Cache.Transaction.Add(entry.Key, NVE.BlockStatusCode.Completed);
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
                //Console.WriteLine("Silinecek Block Anahtari Bilinmiyor");
                //RemoveKeyStr = GiveBlockKey(rawDataStr);
            }
            //Console.WriteLine("Control-Point-a055");
            //Console.WriteLine("Remove Key From Pool : " + RemoveKeyStr);
            MP_BlockPoolList.Remove(RemoveKeyStr, true);
        }

        public void Reset()
        {
            Notus.Archive.ClearBlocks(NVG.Settings);
            MP_BlockPoolList.Clear("MP_BlockPoolList");
            Queue_PoolTransaction.Clear();
            Obj_PoolTransactionList.Clear();
        }
        public bool Add(NVS.PoolBlockRecordStruct? PreBlockData, bool addedToPoolDb = true)
        {
            // burada kuyruğa eklenen her iş
            // diğer nodelara da dağıtılacak

            if (PreBlockData == null)
            {
                Console.WriteLine("Block.Queue.Cs -> Line 668 -> PreBlockData = NULL");
                return false;
            }
            if (NVG.Settings == null)
            {
                Console.WriteLine("Block.Queue.Cs -> Line 673 -> NVG.Settings = NULL");
                return false;
            }
            if (NVG.Settings.NodeWallet == null)
            {
                Console.WriteLine("Block.Queue.Cs -> Line 678 -> NVG.Settings.NodeWallet = NULL");
                return false;
            }

            string PreBlockDataStr = JsonSerializer.Serialize(PreBlockData);
            if (PreBlockData.uid == null)
            {
                PreBlockData.uid = NGF.GenerateTxUid();
            }

            Add2Queue(PreBlockData, PreBlockData.uid);
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
            Console.WriteLine("keyStr : " + keyStr);
            if (keyStr.Length > 0)
            {
                if (addedToPoolDb == true)
                {
                    MP_BlockPoolList.Set(keyStr, PreBlockDataStr, true);
                }
            }
            return true;
        }

        public void AddEmptyBlock()
        {
            Add(new NVS.PoolBlockRecordStruct()
            {
                uid = NGF.GenerateTxUid(),
                type = NVE.BlockTypeList.EmptyBlock,
                data = JsonSerializer.Serialize(NVG.Settings.LastBlock.info.rowNo)
            }, false);
        }

        private void Add2Queue(NVS.PoolBlockRecordStruct PreBlockData, string BlockKeyStr)
        {
            //Console.WriteLine(PreBlockData.type.ToString() + " - " +BlockKeyStr.Substring(0, 20));
            if (PoolIdList.ContainsKey(BlockKeyStr) == false)
            {
                bool added = PoolIdList.TryAdd(BlockKeyStr, 1);
                if (Obj_PoolTransactionList.ContainsKey(PreBlockData.type) == false)
                {
                    Obj_PoolTransactionList.TryAdd(
                        PreBlockData.type,
                        new List<Variable.Struct.List_PoolBlockRecordStruct>() { }
                    );
                }
                Obj_PoolTransactionList[PreBlockData.type].Add(
                    new NVS.List_PoolBlockRecordStruct()
                    {
                        key = BlockKeyStr,
                        type = PreBlockData.type,
                        data = PreBlockData.data
                    }
                );
                Queue_PoolTransaction.Enqueue(new NVS.List_PoolBlockRecordStruct()
                {
                    key = BlockKeyStr,
                    type = PreBlockData.type,
                    data = PreBlockData.data
                });
            }
        }
        public void LoadFromPoolDb(bool forceToRun)
        {
            bool executeEach = false;
            if (NGF.BlockQueue.CheckPoolDb == true)
            {
                executeEach = true;
            }
            if (forceToRun == true)
            {
                executeEach = true;
            }
            if (executeEach == true)
            {
                NGF.BlockQueue.CheckPoolDb = false;
                MP_BlockPoolList.Each((string blockTransactionKey, string TextBlockDataString) =>
                {
                    if (PoolIdList.ContainsKey(blockTransactionKey) == false)
                    {
                        Console.WriteLine("Load : " + blockTransactionKey);
                        NVS.PoolBlockRecordStruct? PreBlockData =
                            JsonSerializer.Deserialize<NVS.PoolBlockRecordStruct>(TextBlockDataString);
                        if (PreBlockData != null)
                        {
                            Add2Queue(PreBlockData, blockTransactionKey);
                        }
                        else
                        {
                            Console.WriteLine("Queue -> Line 687");
                            Console.WriteLine("Queue -> Line 687");
                            Console.WriteLine(blockTransactionKey);
                            Console.WriteLine(TextBlockDataString);
                        }
                    }
                }, 3000);
            }
        }
        public void Start()
        {
            NP.Info("Pool Loaded From Local DB");
            BS_Storage = new Notus.Block.Storage(false);
            BS_Storage.Start();

            MP_BlockPoolList = new Notus.Mempool(
                Notus.IO.GetFolderName(NVG.Settings, NVC.StorageFolderName.Common) +
                NVC.MemoryPoolName["BlockPoolList"]
            );
            MP_BlockPoolList.AsyncActive = false;
            LoadFromPoolDb(true);
            CheckPoolDb = false;
        }
        public Queue()
        {
            Obj_PoolTransactionList.Clear();

            Queue_PoolTransaction.Clear();
        }
        ~Queue()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                MP_BlockPoolList.Dispose();
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error -> Notus.Block.Queue");
                NP.Danger(NVG.Settings, err.Message);
                NP.Danger(NVG.Settings, "Error -> Notus.Block.Queue");
            }
        }
    }
}
