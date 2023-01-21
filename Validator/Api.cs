using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using NBD = Notus.Block.Decrypt;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVClass = Notus.Variable.Class;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Validator
{
    public class Api : IDisposable
    {

        private Notus.Data.KeyValue BlockDbObj = new Notus.Data.KeyValue();
        private DateTime LastNtpTime = Notus.Variable.Constant.DefaultTime;
        private TimeSpan NtpTimeDifference;
        private bool NodeTimeAfterNtpTime = false;      // time difference before or after NTP Server

        private List<string> AllMainList = new List<string>();
        private List<string> AllNodeList = new List<string>();
        private List<string> AllMasterList = new List<string>();
        private List<string> AllReplicantList = new List<string>();

        //private Notus.Coin.Transfer transferObj = new Notus.Coin.Transfer();

        private Notus.Mempool ObjMp_MultiSignPool;
        public Notus.Mempool Obj_MultiSignPool
        {
            get { return ObjMp_MultiSignPool; }
        }

        //private ConcurrentDictionary<string, NVE.BlockStatusCode> Obj_TransferStatusList;

        //public System.Func<NVS.PoolBlockRecordStruct, bool>? Func_AddToChainPool = null;

        private bool PrepareExecuted = false;

        //ffb_CurrencyList Currency list buffer
        private List<NVS.CurrencyList> ffb_CurrencyList = new List<NVS.CurrencyList>();
        private DateTime ffb_CurrencyList_LastCheck = ND.NowObj().Subtract(TimeSpan.FromDays(1));
        private NVE.NetworkType ffb_CurrencyList_Network = NVE.NetworkType.MainNet;
        private NVE.NetworkLayer ffb_CurrencyList_Layer = NVE.NetworkLayer.Layer1;

        private void Prepare_Layer1()
        {
            if (NVG.Settings.GenesisCreated == false)
            {
                //Obj_TransferStatusList = new ConcurrentDictionary<string, NVE.BlockStatusCode>();

                //NGF.BlockOrder.Clear();
                /*
                ObjMp_BlockOrderList = new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings, Notus.Variable.Constant.StorageFolderName.Common
                    ) + "ordered_block_list");

                ObjMp_BlockOrderList.AsyncActive = false;
                ObjMp_BlockOrderList.Clear();
                */

                ObjMp_MultiSignPool = new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings, Notus.Variable.Constant.StorageFolderName.Pool
                    ) + "multi_sign_tx");

                ObjMp_MultiSignPool.AsyncActive = false;

            }
        }
        private void Prepare_Layer2()
        {
            if (NVG.Settings.GenesisCreated == false)
            {
                //Obj_TransferStatusList = new ConcurrentDictionary<string, NVE.BlockStatusCode>();
                //NGF.Balance.Start();
            }
        }
        private void Prepare_Layer3()
        {
            if (NVG.Settings.GenesisCreated == false)
            {
                //Obj_TransferStatusList = new ConcurrentDictionary<string, NVE.BlockStatusCode>();
                //NGF.Balance.Start();
            }
        }
        public void Prepare()
        {
            if (PrepareExecuted == true)
            {
                return;
            }
            PrepareExecuted = true;
            BlockDbObj.SetSettings(new NVS.KeyValueSettings()
            {
                MemoryLimitCount = 1000,
                Name = "blocks"
            });


            if (NVG.Settings.Layer == NVE.NetworkLayer.Layer1)
            {
                Prepare_Layer1();
            }
            if (NVG.Settings.Layer == NVE.NetworkLayer.Layer2)
            {
                Prepare_Layer2();
            }
            if (NVG.Settings.Layer == NVE.NetworkLayer.Layer3)
            {
                Prepare_Layer3();
            }

        }

        public void AddForCache(NVClass.BlockData Obj_BlockData, int blockSource = 0)
        {
            string blockRowNoStr = Obj_BlockData.info.rowNo.ToString();
            // NP.Basic("Balance.Control Just Executed For -> " + blockRowNoStr);
            NGF.Balance.Control(Obj_BlockData);

            if (blockSource == 2 || blockSource == 4)
            {
                if (Obj_BlockData.info.rowNo == NVG.Settings.LastBlock.info.rowNo)
                {
                    if (string.Equals(NVG.Settings.LastBlock.prev, Obj_BlockData.prev))
                    {
                        if (string.Equals(NVG.Settings.LastBlock.info.uID, Obj_BlockData.info.uID))
                        {
                            NP.Info("Block Is Proccessing   -> " +
                                blockRowNoStr +
                                " -> " +
                                Obj_BlockData.info.uID.Substring(0, 20) +
                                " -> " +
                                Obj_BlockData.prev.Substring(0, 20)
                            );
                        }
                    }
                }
            }


            NVG.Settings.BlockOrder.Add(Obj_BlockData.info.rowNo, Obj_BlockData.info.uID);
            NVG.Settings.BlockSign.Add(Obj_BlockData.info.rowNo, Obj_BlockData.sign);
            NVG.Settings.BlockPrev.Add(Obj_BlockData.info.rowNo, Obj_BlockData.prev);

            //NP.Basic("Balance.Control Will Execute");

            // airdrop ise burada yapılan istekler veri tabanına kaydedilecek
            NVG.Settings.Airdrop.Process(Obj_BlockData);

            if (Obj_BlockData.info.type == NVE.BlockTypeList.CryptoTransfer)
            {
                NVClass.BlockStruct_120? tmpBalanceVal = NBD.Convert_120(Obj_BlockData.cipher.data, true);
                if (tmpBalanceVal != null)
                {
                    foreach (KeyValuePair<string, NVClass.BlockStruct_120_In_Struct> entry in tmpBalanceVal.In)
                    {
                        NVG.Settings.TxStatus.Set(entry.Key, new NVS.CryptoTransferStatus()
                        {
                            Code = NVE.BlockStatusCode.Completed,
                            RowNo = Obj_BlockData.info.rowNo,
                            UID = Obj_BlockData.info.uID,
                            Text = "Completed"
                        });
                    }
                }
            }
            if (Obj_BlockData.info.type == NVE.BlockTypeList.LockAccount)
            {
                /*
                 
                NVClass.BlockStruct_120? tmpBalanceVal = JsonSerializer.Deserialize<NVClass.BlockStruct_120>(System.Text.Encoding.UTF8.GetString(
                    System.Convert.FromBase64String(
                        Obj_BlockData.cipher.data
                    )
                ));
                if (tmpBalanceVal != null)
                {
                    Console.WriteLine("Node.Api.AddToBalanceDB [cba09834] : " + Obj_BlockData.info.type);
                    foreach (KeyValuePair<string, NVClass.BlockStruct_120_In_Struct> entry in tmpBalanceVal.In)
                    {
                        RequestSend_Done(entry.Key, Obj_BlockData.info.rowNo, Obj_BlockData.info.uID);
                    }
                }
                */
            }

            BlockDbObj.Set(blockRowNoStr, JsonSerializer.Serialize(Obj_BlockData));
        }

        //layer -1 kontrolünü sağla
        private string Interpret_Layer1(NVS.HttpRequestDetails IncomeData)
        {
            return "";
        }

        public string Interpret(NVS.HttpRequestDetails IncomeData)
        {
            if (PrepareExecuted == false)
            {
                Prepare();
            }

            if (IncomeData.UrlList.Length == 0)
            {
                return JsonSerializer.Serialize(false);
            }
            string incomeFullUrlPath = string.Join("/", IncomeData.UrlList).ToLower();

            if (incomeFullUrlPath.Length < 2)
            {
                return JsonSerializer.Serialize(false);
            }

            if (string.Equals(incomeFullUrlPath.Substring(incomeFullUrlPath.Length - 1), "/"))
            {
                incomeFullUrlPath = incomeFullUrlPath.Substring(0, incomeFullUrlPath.Length - 1);
            }

            // storage işlemleri

            if (IncomeData.UrlList.Length > 2)
            {
                if (string.Equals(IncomeData.UrlList[0].ToLower(), "storage"))
                {
                    if (string.Equals(IncomeData.UrlList[1].ToLower(), "file"))
                    {
                        //this parts need to organize
                        if (NVG.Settings.Layer == NVE.NetworkLayer.Layer1)
                        {
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "new") && IncomeData.PostParams.ContainsKey("data") == true)
                            {
                                // bu fonksiyon şimdilik devre dışı
                                // genesis tamamlandığında burası tekrar aktive edilecek
                                return Request_Layer1_StoreFile_New(IncomeData);
                            }
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "status"))
                            {
                                return Request_Layer1_StoreFile_Status(IncomeData);
                            }
                        }
                        //this parts need to organize
                        if (NVG.Settings.Layer == NVE.NetworkLayer.Layer2)
                        {
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "new") && IncomeData.PostParams.ContainsKey("data") == true)
                            {
                                return Request_StoreEncryptedFile_New(IncomeData);
                            }
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "update") && IncomeData.PostParams.ContainsKey("data") == true)
                            {
                                return Request_StoreEncryptedFile_Update(IncomeData);
                            }
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "status"))
                            {
                                return Request_StoreEncryptedFile_Status(IncomeData);
                            }
                        }

                        if (NVG.Settings.Layer == NVE.NetworkLayer.Layer3)
                        {
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "new") && IncomeData.PostParams.ContainsKey("data") == true)
                            {
                                return Request_Layer3_StoreFileNew(IncomeData);
                            }
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "update") && IncomeData.PostParams.ContainsKey("data") == true)
                            {
                                return Request_Layer3_StoreFileUpdate(IncomeData);
                            }
                            if (string.Equals(IncomeData.UrlList[2].ToLower(), "status"))
                            {
                                return Request_Layer3_StoreFileStatus(IncomeData);
                            }
                        }
                    }
                }
            }
            if (string.Equals(incomeFullUrlPath, "ping"))
            {
                return "pong";
            }
            if (string.Equals(incomeFullUrlPath, "metrics"))
            {
                return Request_Metrics(IncomeData);
            }
            if (string.Equals(incomeFullUrlPath, "online"))
            {
                return Request_Online(IncomeData);
            }
            if (string.Equals(incomeFullUrlPath, "node"))
            {
                return Request_Node();
            }
            if (string.Equals(incomeFullUrlPath, "main"))
            {
                return Request_Main();
            }

            if (string.Equals(incomeFullUrlPath, "master"))
            {
                return Request_Master();
            }

            if (string.Equals(incomeFullUrlPath, "replicant"))
            {
                return Request_Replicant();
            }

            if (incomeFullUrlPath.StartsWith("token/generate/"))
            {
                return Request_GenerateToken(IncomeData);
            }


            if (incomeFullUrlPath.StartsWith("multi/"))
            {

                // burada block uid verilirse, blok detayları gösterilecek
                // eğer cüzdan adresi verilirse hangi block id olduğu listesi verilir.
                if (incomeFullUrlPath.StartsWith("multi/pool/"))
                {
                    if (IncomeData.UrlList[2].Length == 90)
                    {
                        string tmpBlockUid = IncomeData.UrlList[2];
                        NVS.MultiWalletTransactionVoteStruct? tmpResult = null;
                        Dictionary<string, NVE.BlockStatusCode> SignList
                            = new Dictionary<string, NVE.BlockStatusCode>();
                        ObjMp_MultiSignPool.Each((string multiKeyId, string multiTransferList) =>
                        {
                            //Console.WriteLine(multiKeyId);
                            //Console.WriteLine(multiTransferList);
                            if (tmpResult == null)
                            {
                                Dictionary<ulong, NVS.MultiWalletTransactionVoteStruct>? uidList =
                                    JsonSerializer.Deserialize<Dictionary<
                                        ulong,
                                        NVS.MultiWalletTransactionVoteStruct>
                                    >(multiTransferList);

                                if (uidList != null)
                                {
                                    foreach (KeyValuePair<ulong, NVS.MultiWalletTransactionVoteStruct> entry in uidList)
                                    {
                                        if (string.Equals(tmpBlockUid, entry.Value.TransactionId))
                                        {
                                            if (tmpResult == null)
                                            {
                                                tmpResult = entry.Value;
                                            }
                                        }
                                    }
                                }
                            }
                        });
                        if (tmpResult == null)
                        {
                            return JsonSerializer.Serialize(false);
                        }
                        return JsonSerializer.Serialize(tmpResult);
                    }

                    //burada seçilen cüzdan detayları verilecek...
                    if (IncomeData.UrlList[2].Length == Notus.Variable.Constant.WalletFullTextLength)
                    {
                        string controlWalletId = IncomeData.UrlList[2];
                        bool multiWalletId = Notus.Wallet.MultiID.IsMultiId(controlWalletId);

                        Dictionary<string, NVE.BlockStatusCode> SignList
                            = new Dictionary<string, NVE.BlockStatusCode>();
                        ObjMp_MultiSignPool.Each((string multiKeyId, string multiTransferList) =>
                        {
                            //Console.WriteLine(multiKeyId);
                            //Console.WriteLine(multiTransferList);
                            Dictionary<ulong, NVS.MultiWalletTransactionVoteStruct>? uidList =
                                JsonSerializer.Deserialize<Dictionary<
                                    ulong,
                                    NVS.MultiWalletTransactionVoteStruct>
                                >(multiTransferList);

                            if (uidList != null)
                            {
                                foreach (KeyValuePair<ulong, NVS.MultiWalletTransactionVoteStruct> entry in uidList)
                                {
                                    if (multiWalletId == true)
                                    {
                                        if (string.Equals(entry.Value.Sender.Sender, controlWalletId))
                                        {
                                            SignList.Add(entry.Value.TransactionId, entry.Value.Status);
                                        }
                                    }
                                    else
                                    {
                                        foreach (var innerEntry in entry.Value.Approve)
                                        {
                                            if (string.Equals(innerEntry.Key, controlWalletId))
                                            {
                                                SignList.Add(entry.Value.TransactionId, entry.Value.Status);
                                            }
                                        }
                                    }
                                }
                            }
                        });
                        return JsonSerializer.Serialize(SignList);
                    }
                }

                //burada pool listesinde bekleyen işlemler id ile listelenecek...
                if (string.Equals(incomeFullUrlPath, "multi/pool"))
                {
                    Dictionary<string, NVE.BlockStatusCode> SignList
                        = new Dictionary<string, NVE.BlockStatusCode>();
                    ObjMp_MultiSignPool.Each((string multiKeyId, string multiTransferList) =>
                    {
                        //Console.WriteLine(multiKeyId);
                        //Console.WriteLine(multiTransferList);
                        Dictionary<ulong, NVS.MultiWalletTransactionVoteStruct>? uidList =
                            JsonSerializer.Deserialize<Dictionary<
                                ulong,
                                NVS.MultiWalletTransactionVoteStruct>
                            >(multiTransferList);

                        if (uidList != null)
                        {
                            foreach (KeyValuePair<ulong, NVS.MultiWalletTransactionVoteStruct> entry in uidList)
                            {
                                SignList.Add(entry.Value.TransactionId, entry.Value.Status);
                            }
                        }
                    });
                    return JsonSerializer.Serialize(SignList);
                }

                //multi transation'ın onaylandığı url
                if (incomeFullUrlPath.StartsWith("multi/transaction/approve/"))
                {
                    return Request_ApproveMultiTransaction(IncomeData);

                }

                //multi wallet oluşturma işlemi
                if (string.Equals(incomeFullUrlPath, "multi/wallet/add"))
                {
                    return Request_AddMultiWallet(IncomeData);
                }

                if (IncomeData.UrlList.Length > 1)
                {
                    string tmpWallet = IncomeData.UrlList[1];
                    string multiPrefix = Notus.Variable.Constant.MultiWalletPrefix;
                    string singlePrefix = Notus.Variable.Constant.SingleWalletPrefix;
                    if (tmpWallet.Length >= singlePrefix.Length)
                    {
                        if (string.Equals(singlePrefix, tmpWallet.Substring(0, singlePrefix.Length)))
                        {
                            return JsonSerializer.Serialize(NGF.Balance.WalletsICanApprove(tmpWallet));
                        }
                    }
                    if (tmpWallet.Length >= multiPrefix.Length)
                    {
                        if (string.Equals(multiPrefix, tmpWallet.Substring(0, multiPrefix.Length)))
                        {
                            return JsonSerializer.Serialize(NGF.Balance.GetParticipant(tmpWallet));
                        }
                    }
                }
                return JsonSerializer.Serialize(false);
            }

            if (IncomeData.UrlList.Length > 0)
            {
                //burada nft işlemleri yapılıyor...
                if (IncomeData.UrlList.Length > 2)
                {
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "nft"))
                    {
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "list"))
                        {
                            return Request_NFTImageList(IncomeData);
                        }
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "detail"))
                        {
                            if (IncomeData.UrlList.Length > 3)
                            {
                                return Request_NFTPublicImageDetail(IncomeData);
                            }
                            else
                            {
                                return Request_NFTPrivateImageDetail(IncomeData);
                            }
                        }
                        return JsonSerializer.Serialize(false);
                    }
                }

                if (IncomeData.UrlList[0].ToLower() == "pool")
                {
                    if (IncomeData.UrlList.Length > 1)
                    {
                        if (int.TryParse(IncomeData.UrlList[1], out int blockTypeNo))
                        {

                            List<NVS.List_PoolBlockRecordStruct>? tmpPoolList =
                                NGF.BlockQueue.GetPoolList(blockTypeNo);
                            if (tmpPoolList != null)
                            {
                                if (tmpPoolList.Count > 0)
                                {
                                    Dictionary<string, string> tmpResultList = new Dictionary<string, string>();
                                    for (int innerCount = 0; innerCount < tmpPoolList.Count; innerCount++)
                                    {
                                        NVS.List_PoolBlockRecordStruct? tmpItem = tmpPoolList[innerCount];
                                        if (tmpItem != null)
                                        {
                                            tmpResultList.Add(tmpItem.key, tmpItem.data);
                                        }
                                    }

                                    if (tmpResultList.Count > 0)
                                    {
                                        return JsonSerializer.Serialize(tmpResultList);
                                    }
                                }
                            }
                        }
                    }
                    return JsonSerializer.Serialize(NGF.BlockQueue.GetPoolCount());
                }

                if (IncomeData.UrlList.Length > 1)
                {
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "lock"))
                    {
                        return Request_LockAccount(IncomeData);
                    }
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "balance"))
                    {
                        return Request_Balance(IncomeData);
                    }

                    // gönderilen işlem transferini veriyor
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "tx"))
                    {
                        if (IncomeData.UrlList[1].Length == Notus.Variable.Constant.WalletFullTextLength)
                        {

                        }
                    }

                    // alınan işlem transferini veriyor
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "rx"))
                    {
                        if (IncomeData.UrlList[1].Length == Notus.Variable.Constant.WalletFullTextLength)
                        {

                        }
                    }

                    // blok içeriklerini veriyor...
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "block"))
                    {
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "summary"))
                        {
                            return Request_BlockSummary(IncomeData);
                        }

                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "last"))
                        {
                            return Request_BlockLast(IncomeData);
                        }
                        if (IncomeData.UrlList.Length > 2)
                        {
                            if (string.Equals(IncomeData.UrlList[1].ToLower(), "status"))
                            {
                                return JsonSerializer.Serialize(NVG.Settings.Transfer.Status(IncomeData));
                            }

                            if (string.Equals(IncomeData.UrlList[1].ToLower(), "hash"))
                            {
                                return Request_BlockHash(IncomeData);
                            }
                        }

                        return Request_Block(IncomeData);
                    }

                    // yapılan transferin durumunu geri gönderen fonksiyon
                    if (string.Equals(IncomeData.UrlList[0].ToLower(), "transaction"))
                    {
                        if (IncomeData.UrlList.Length > 2)
                        {
                            if (string.Equals(IncomeData.UrlList[1].ToLower(), "status"))
                            {
                                return JsonSerializer.Serialize(NVG.Settings.Transfer.Status(IncomeData));
                            }
                        }
                    }
                }

                if (string.Equals(IncomeData.UrlList[0].ToLower(), "currency") && IncomeData.UrlList.Length > 1)
                {
                    if (string.Equals(IncomeData.UrlList[1].ToLower(), "list"))
                    {
                        if ((ND.NowObj() - ffb_CurrencyList_LastCheck).TotalMinutes > 1)
                        {
                            ffb_CurrencyList_LastCheck = ND.NowObj();
                            ffb_CurrencyList = Notus.Wallet.Block.GetList(NVG.Settings.Network, NVG.Settings.Layer);
                        }
                        else
                        {
                            if (NVG.Settings.Network != ffb_CurrencyList_Network || NVG.Settings.Layer != ffb_CurrencyList_Layer)
                            {
                                ffb_CurrencyList = Notus.Wallet.Block.GetList(NVG.Settings.Network, NVG.Settings.Layer);
                            }
                        }
                        return JsonSerializer.Serialize(ffb_CurrencyList);
                    }
                }

                if (string.Equals(IncomeData.UrlList[0].ToLower(), "send") && IncomeData.PostParams.ContainsKey("data") == true)
                {
                    return NVG.Settings.Transfer.Request(IncomeData);
                }

                /*
                buradan contract içeriği alınacak ve ağa eklenecek
                gelen program komutları post ile gelsin
                post ile gelen data doğrudan kaydedilsin
                */
                if (string.Equals(IncomeData.UrlList[0].ToLower(), "contract"))
                {
                    if (IncomeData.UrlList.Length > 0)
                    {
                        return NVG.Settings.ContractDeploy.Request(IncomeData);
                    }
                    return JsonSerializer.Serialize(false);
                }
                if (string.Equals(IncomeData.UrlList[0].ToLower(), "airdrop"))
                {
                    if (IncomeData.UrlList.Length > 1)
                    {
                        return NVG.Settings.Airdrop.Request(IncomeData);
                    }
                }

                if (string.Equals(IncomeData.UrlList[0].ToLower(), "info"))
                {
                    if (IncomeData.UrlList.Length > 1)
                    {
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "genesis"))
                        {
                            return JsonSerializer.Serialize(NVG.Settings.Genesis);
                        }
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "reserve"))
                        {
                            return JsonSerializer.Serialize(NVG.Settings.Genesis.Reserve);
                        }
                        if (string.Equals(IncomeData.UrlList[1].ToLower(), "transfer"))
                        {
                            return JsonSerializer.Serialize(NVG.Settings.Genesis.Fee);
                        }
                    }
                }
            }

            // bu veri API class'ı tarafından değil, Queue Class'ı tarafından yorumlanacak
            if (IncomeData.UrlList.Length > 2)
            {
                if (
                    string.Equals(IncomeData.UrlList[0].ToLower(), "queue")
                    &&
                    string.Equals(IncomeData.UrlList[1].ToLower(), "node")
                    &&
                    IncomeData.PostParams.ContainsKey("data")
                )
                {
                    return "queue-data";
                }
            }
            return JsonSerializer.Serialize(false);
        }

        private NVClass.BlockData? GetBlockWithRowNo(Int64 BlockRowNo)
        {
            if (NVG.Settings == null)
            {
                return null;
            }
            if (NVG.Settings.LastBlock == null)
            {
                return null;
            }

            if (NVG.Settings.LastBlock.info.rowNo >= BlockRowNo)
            {
                if (NVG.Settings.LastBlock.info.rowNo == BlockRowNo)
                {
                    return NVG.Settings.LastBlock;
                }

                string tmpBlockKey = NVG.Settings.BlockOrder.Get(BlockRowNo);
                if (tmpBlockKey.Length > 0)
                {
                    NVClass.BlockData? tmpStoredBlock = NGF.BlockQueue.ReadFromChain(tmpBlockKey);
                    if (tmpStoredBlock != null)
                    {
                        return tmpStoredBlock;
                    }
                }

                bool exitPrevWhile = false;
                string PrevBlockIdStr = NVG.Settings.LastBlock.prev;
                while (exitPrevWhile == false)
                {
                    NVClass.BlockData? tmpStoredBlock = NGF.BlockQueue.ReadFromChain(PrevBlockIdStr.Substring(0, 90));
                    if (tmpStoredBlock != null)
                    {
                        if (tmpStoredBlock.info.rowNo == BlockRowNo)
                        {
                            return tmpStoredBlock;
                        }
                        PrevBlockIdStr = tmpStoredBlock.prev;
                    }
                    else
                    {
                        exitPrevWhile = true;
                    }
                }
            }
            return null;
        }

        private string Request_Layer3_StoreFileNew(NVS.HttpRequestDetails IncomeData)
        {
            // we have to communicate with layer1 for crypto balance
            // if its says have not enough coin return balance not efficent
            // if its says have enogh coin then add file upload transaction 
            Console.WriteLine("----------------------------------------------");
            Console.WriteLine(JsonSerializer.Serialize(IncomeData));
            Console.WriteLine("----------------------------------------------");
            int Val_Timeout = 86400 * 7; // it will wait 7 days, if its not completed during that time than delete file from db pool
            NVS.FileTransferStruct tmpFileData;
            //tmpFileData.
            try
            {
                tmpFileData = JsonSerializer.Deserialize<NVS.FileTransferStruct>(IncomeData.PostParams["data"]);
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error Text [a46cbe8d9] : " + err.Message);
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            //string tmpTransferIdKey = Notus.Core.Function.GenerateBlockKey(true);
            string tmpTransferIdKey = IncomeData.UrlList[3].ToLower();
            using (Notus.Mempool ObjMp_FileChunkList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list"
                )
            )
            {
                ObjMp_FileChunkList.AsyncActive = false;
                ObjMp_FileChunkList.Add(tmpTransferIdKey, JsonSerializer.Serialize(tmpFileData), Val_Timeout);
                ObjMp_FileChunkList.Add(tmpTransferIdKey + "_chunk", JsonSerializer.Serialize(new Dictionary<int, string>() { }), Val_Timeout);
            }

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
                ObjMp_FileStatus.Add(tmpTransferIdKey, JsonSerializer.Serialize(NVE.BlockStatusCode.InQueue), Val_Timeout);
            }

            return JsonSerializer.Serialize(new NVS.BlockResponse()
            {
                UID = tmpTransferIdKey,
                Status = "AddedToQueue",
                Result = NVE.BlockStatusCode.AddedToQueue
            });
        }

        public void Layer3_StorageFileDone(string BlockUid)
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
                ObjMp_FileList.AsyncActive = false;
                ObjMp_FileList.Set(BlockUid, JsonSerializer.Serialize(NVE.BlockStatusCode.Completed));
            }
        }
        private string Request_Layer3_StoreFileUpdate(NVS.HttpRequestDetails IncomeData)
        {
            const int Val_Timeout = 86400 * 7;
            NVS.FileChunkStruct tmpChunkData;

            try
            {
                tmpChunkData = JsonSerializer.Deserialize<NVS.FileChunkStruct>(System.Uri.UnescapeDataString(IncomeData.PostParams["data"]));
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error Text [a354cd67] : " + err.Message);
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            string tmpStorageIdKey = tmpChunkData.UID;
            string tmpChunkIdKey = Notus.Block.Key.Generate(ND.NowObj(), NVG.Settings.NodeWallet.WalletKey);
            int tmpStorageNo = Notus.Block.Key.CalculateStorageNumber(Notus.Convert.Hex2BigInteger(tmpChunkIdKey).ToString());

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
                ObjMp_FileChunkList.Add(tmpChunkIdKey, System.Uri.EscapeDataString(tmpChunkData.Data), Val_Timeout);
            }

            NVS.FileTransferStruct tmpFileObj = new NVS.FileTransferStruct();
            using (Notus.Mempool ObjMp_FileList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list"
                )
            )
            {
                ObjMp_FileList.AsyncActive = false;
                string tmpFileObjStr = ObjMp_FileList.Get(tmpStorageIdKey, "");
                if (tmpFileObjStr.Length == 0)
                {
                    return JsonSerializer.Serialize(new NVS.BlockResponse()
                    {
                        UID = tmpStorageIdKey,
                        Status = "Unknown",
                        Result = NVE.BlockStatusCode.Unknown
                    });
                }

                tmpFileObj = JsonSerializer.Deserialize<NVS.FileTransferStruct>(tmpFileObjStr);

                int calculatedChunkLength = ((int)Math.Ceiling(System.Convert.ToDouble(tmpFileObj.FileSize / tmpFileObj.ChunkSize))) - 1;
                string tmpCurrentList = ObjMp_FileList.Get(tmpStorageIdKey + "_chunk", "");
                Dictionary<int, string> tmpChunkList = new Dictionary<int, string>();
                if (tmpCurrentList.Length > 0)
                {
                    try
                    {
                        tmpChunkList = JsonSerializer.Deserialize<Dictionary<int, string>>(tmpCurrentList);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                    }
                }
                tmpChunkList.Add(tmpChunkData.Count, tmpChunkIdKey);
                ObjMp_FileList.Set(tmpStorageIdKey + "_chunk", JsonSerializer.Serialize(tmpChunkList));

                if (calculatedChunkLength == tmpChunkData.Count)
                {
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
                        ObjMp_FileStatus.Set(tmpStorageIdKey, JsonSerializer.Serialize(NVE.BlockStatusCode.Pending), true);
                    }
                }
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = tmpStorageIdKey,
                    Status = "AddedToQueue",
                    Result = NVE.BlockStatusCode.AddedToQueue
                });
            }

        }
        private string Request_Layer3_StoreFileStatus(NVS.HttpRequestDetails IncomeData)
        {
            string tmpstorageIdStr = IncomeData.UrlList[3];

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
                string tmpRawStr = ObjMp_FileStatus.Get(tmpstorageIdStr, "");
                try
                {
                    NVE.BlockStatusCode tmpUploadStatus = JsonSerializer.Deserialize<NVE.BlockStatusCode>(tmpRawStr);
                    return JsonSerializer.Serialize(new NVS.BlockResponse()
                    {
                        UID = string.Empty,
                        Status = tmpUploadStatus.ToString(),
                        Result = tmpUploadStatus
                    });
                }
                catch (Exception err)
                {
                }
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
        }

        private string Request_StoreEncryptedFile_New(NVS.HttpRequestDetails IncomeData)
        {
            int Val_Timeout = 86400;
            NVS.FileTransferStruct tmpFileData;
            try
            {
                tmpFileData = JsonSerializer.Deserialize<NVS.FileTransferStruct>(IncomeData.PostParams["data"]);
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error Text [a46cbe8d9] : " + err.Message);
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            string tmpTransferIdKey = Notus.Block.Key.Generate(ND.NowObj(), NVG.Settings.NodeWallet.WalletKey);
            using (Notus.Mempool ObjMp_FileChunkList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list"
                )
            )
            {
                ObjMp_FileChunkList.AsyncActive = false;
                ObjMp_FileChunkList.Add(tmpTransferIdKey, JsonSerializer.Serialize(tmpFileData), Val_Timeout);
                ObjMp_FileChunkList.Add(tmpTransferIdKey + "_chunk", JsonSerializer.Serialize(new Dictionary<int, string>() { }), Val_Timeout);
            }

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
                ObjMp_FileStatus.Add(tmpTransferIdKey, JsonSerializer.Serialize(NVE.BlockStatusCode.InQueue), Val_Timeout);
            }

            return JsonSerializer.Serialize(new NVS.BlockResponse()
            {
                UID = tmpTransferIdKey,
                Status = "AddedToQueue",
                Result = NVE.BlockStatusCode.AddedToQueue
            });
        }
        private string Request_StoreEncryptedFile_Update(NVS.HttpRequestDetails IncomeData)
        {
            const int Val_Timeout = 86400;
            NVS.FileChunkStruct tmpChunkData;

            /*
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine((IncomeData.PostParams["data"]));
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine(System.Uri.UnescapeDataString(IncomeData.PostParams["data"]));
            Console.WriteLine("----------------------------------------------------");
            Console.WriteLine(System.Uri.UnescapeDataString(System.Uri.UnescapeDataString(IncomeData.PostParams["data"])));
            Console.WriteLine("----------------------------------------------------");
            //Console.WriteLine((IncomeData.PostParams["data"]));
            //Console.WriteLine(JsonSerializer.Serialize(IncomeData.PostParams["data"]));
            //Console.WriteLine(JsonSerializer.Serialize(IncomeData.PostParams, Notus.Variable.Constant.JsonSetting));
            */
            try
            {
                tmpChunkData = JsonSerializer.Deserialize<NVS.FileChunkStruct>(System.Uri.UnescapeDataString(IncomeData.PostParams["data"]));
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error Text [a354cd67] : " + err.Message);
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            string tmpStorageIdKey = tmpChunkData.UID;
            string tmpChunkIdKey = Notus.Block.Key.Generate(ND.NowObj(), NVG.Settings.NodeWallet.WalletKey);
            int tmpStorageNo = Notus.Block.Key.CalculateStorageNumber(Notus.Convert.Hex2BigInteger(tmpChunkIdKey).ToString());

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
                ObjMp_FileChunkList.Add(tmpChunkIdKey, System.Uri.EscapeDataString(tmpChunkData.Data), Val_Timeout);
            }

            NVS.FileTransferStruct tmpFileObj = new NVS.FileTransferStruct();
            using (Notus.Mempool ObjMp_FileList =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(
                        NVG.Settings.Network,
                        NVG.Settings.Layer,
                        Notus.Variable.Constant.StorageFolderName.File) + "upload_list"
                )
            )
            {
                ObjMp_FileList.AsyncActive = false;
                string tmpFileObjStr = ObjMp_FileList.Get(tmpStorageIdKey, "");
                if (tmpFileObjStr.Length > 0)
                {
                    tmpFileObj = JsonSerializer.Deserialize<NVS.FileTransferStruct>(tmpFileObjStr);
                }


                int calculatedChunkLength = ((int)Math.Ceiling(System.Convert.ToDouble(tmpFileObj.FileSize / tmpFileObj.ChunkSize))) - 1;
                string tmpCurrentList = ObjMp_FileList.Get(tmpStorageIdKey + "_chunk", "");
                Dictionary<int, string> tmpChunkList = new Dictionary<int, string>();
                if (tmpCurrentList.Length > 0)
                {
                    try
                    {
                        tmpChunkList = JsonSerializer.Deserialize<Dictionary<int, string>>(tmpCurrentList);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                        Console.WriteLine("Notus.Validator.Api.Request_StoreFileUpdate -> Convertion Error - Line 595");
                    }
                }
                tmpChunkList.Add(tmpChunkData.Count, tmpChunkIdKey);
                ObjMp_FileList.Set(tmpStorageIdKey + "_chunk", JsonSerializer.Serialize(tmpChunkList));
                if (calculatedChunkLength == tmpChunkData.Count)
                {
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
                        ObjMp_FileStatus.Set(tmpStorageIdKey, JsonSerializer.Serialize(NVE.BlockStatusCode.Pending), true);
                    }
                }
            }

            return JsonSerializer.Serialize(new NVS.BlockResponse()
            {
                UID = tmpStorageIdKey,
                Status = "AddedToQueue",
                Result = NVE.BlockStatusCode.AddedToQueue
            });
        }
        private string Request_StoreEncryptedFile_Status(NVS.HttpRequestDetails IncomeData)
        {
            string tmpstorageIdStr = IncomeData.UrlList[3];

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
                string tmpRawStr = ObjMp_FileStatus.Get(tmpstorageIdStr, "");
                try
                {
                    NVE.BlockStatusCode tmpUploadStatus = JsonSerializer.Deserialize<NVE.BlockStatusCode>(tmpRawStr);
                    return JsonSerializer.Serialize(new NVS.BlockResponse()
                    {
                        UID = string.Empty,
                        Status = tmpUploadStatus.ToString(),
                        Result = tmpUploadStatus
                    });
                }
                catch (Exception err)
                {
                }
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
        }

        private string Request_Layer1_StoreFile_New(NVS.HttpRequestDetails IncomeData)
        {
            return "Genesis coin işlemleri tamamlanana kadar beklemeye alındı";
            /*
            NVS.StorageOnChainStruct tmpStorageData;
            try
            {
                tmpStorageData = JsonSerializer.Deserialize<NVS.StorageOnChainStruct>(IncomeData.PostParams["data"]);
            }
            catch (Exception err)
            {
                NP.Danger(NVG.Settings, "Error Text [bad849506] : " + err.Message);
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            //Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.Genesis.Fee, Notus.Variable.Constant.JsonSetting));
            //Console.WriteLine("Control_Point_4-a");
            // 1500 * 44304
            long StorageFee = NVG.Settings.Genesis.Fee.Data * tmpStorageData.Size;
            if (tmpStorageData.Encrypted == true)
            {
                StorageFee = StorageFee * 2;
            }

            string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(tmpStorageData.PublicKey);
            NVS.WalletBalanceStruct tmpWalletBalance = NGF.Balance.Get(tmpWalletKey);
            
            BigInteger tmpCurrentBalance = NGF.Balance.GetCoinBalance(tmpWalletBalance, NVS.MainCoinTagName);
            if (StorageFee > tmpCurrentBalance)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "InsufficientBalance",
                    Result = NVE.BlockStatusCode.InsufficientBalance
                });
            }
            if (Func_AddToChainPool == null)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            BigInteger tmpCoinLeft = tmpCurrentBalance - StorageFee;

            tmpWalletBalance.Balance[NVG.Settings.Genesis.CoinInfo.Tag] = tmpCoinLeft.ToString();

            tmpStorageData.Balance.Balance = tmpWalletBalance.Balance;
            tmpStorageData.Balance.RowNo = tmpWalletBalance.RowNo;
            tmpStorageData.Balance.UID = tmpWalletBalance.UID;
            tmpStorageData.Balance.Wallet = tmpWalletBalance.Wallet;
            tmpStorageData.Balance.Fee = StorageFee.ToString();

            Console.WriteLine(JsonSerializer.Serialize(tmpStorageData, Notus.Variable.Constant.JsonSetting));

            string tmpTransferIdKey = Notus.Core.Function.GenerateBlockKey(true);

            bool tmpAddResult = Func_AddToChainPool(new NVS.PoolBlockRecordStruct()
            {
                type = 240,
                data = JsonSerializer.Serialize(new List<string>() { tmpTransferIdKey, JsonSerializer.Serialize(tmpStorageData) })
            });
            if (tmpAddResult == true)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = tmpTransferIdKey,
                    Status = "AddedToQueue",
                    Result = NVE.BlockStatusCode.AddedToQueue
                });
            }
            return JsonSerializer.Serialize(new NVS.BlockResponse()
            {
                UID = tmpTransferIdKey,
                Status = "Unknown",
                Result = NVE.BlockStatusCode.Unknown
            });
            */
        }
        private string Request_Layer1_StoreFile_Status(NVS.HttpRequestDetails IncomeData)
        {
            string tmpstorageIdStr = IncomeData.UrlList[3];

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
                string tmpRawStr = ObjMp_FileStatus.Get(tmpstorageIdStr, "");
                try
                {
                    NVE.BlockStatusCode tmpUploadStatus = JsonSerializer.Deserialize<NVE.BlockStatusCode>(tmpRawStr);
                    return JsonSerializer.Serialize(new NVS.BlockResponse()
                    {
                        UID = string.Empty,
                        Status = tmpUploadStatus.ToString(),
                        Result = tmpUploadStatus
                    });
                }
                catch (Exception err)
                {
                }
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
        }

        private string Request_Block(NVS.HttpRequestDetails IncomeData)
        {
            bool prettyJson = PrettyCheckForRaw(IncomeData, 2);
            if (IncomeData.UrlList[1].Length == 90)
            {
                try
                {
                    NVClass.BlockData? tmpStoredBlock = NGF.BlockQueue.ReadFromChain(IncomeData.UrlList[1]);
                    if (tmpStoredBlock != null)
                    {
                        if (prettyJson == true)
                        {
                            return JsonSerializer.Serialize(tmpStoredBlock, Notus.Variable.Constant.JsonSetting);
                        }
                        return JsonSerializer.Serialize(tmpStoredBlock);
                    }
                }
                catch (Exception err)
                {
                    //NP.Danger(NVG.Settings, "Error Text [4a821b]: " + err.Message);
                    return JsonSerializer.Serialize(false);
                }
            }

            bool isNumeric = Int64.TryParse(IncomeData.UrlList[1], out Int64 BlockNumber);
            if (isNumeric == true)
            {
                NVClass.BlockData? tmpResultBlock = GetBlockWithRowNo(BlockNumber);
                if (tmpResultBlock != null)
                {
                    if (prettyJson == true)
                    {
                        return JsonSerializer.Serialize(tmpResultBlock, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(tmpResultBlock);
                }
            }
            return JsonSerializer.Serialize(false);
        }
        private string Request_BlockHash(NVS.HttpRequestDetails IncomeData)
        {
            if (IncomeData.UrlList[2].Length == 90)
            {
                try
                {
                    NVClass.BlockData? tmpStoredBlock = NGF.BlockQueue.ReadFromChain(IncomeData.UrlList[2]);
                    if (tmpStoredBlock != null)
                    {
                        return tmpStoredBlock.info.uID + tmpStoredBlock.sign;
                    }
                }
                catch (Exception err)
                {
                    //NP.Danger(NVG.Settings, "Error Text [1f95ce]: " + err.Message);
                }
                return JsonSerializer.Serialize(false);
            }

            bool isNumeric2 = Int64.TryParse(IncomeData.UrlList[2], out Int64 BlockNumber2);
            if (isNumeric2 == true)
            {
                NVClass.BlockData? tmpResultBlock = GetBlockWithRowNo(BlockNumber2);
                if (tmpResultBlock != null)
                {
                    return tmpResultBlock.info.uID + tmpResultBlock.sign;
                }
            }
            return JsonSerializer.Serialize(false);
        }
        private bool PrettyCheckForRaw(NVS.HttpRequestDetails IncomeData, int indexNo)
        {
            bool prettyJson = NVG.Settings.PrettyJson;
            if (IncomeData.UrlList.Length > indexNo)
            {
                if (string.Equals(IncomeData.UrlList[indexNo].ToLower(), "raw"))
                {
                    prettyJson = false;
                }
            }
            return prettyJson;
        }

        private string Request_BlockLast(NVS.HttpRequestDetails IncomeData)
        {
            if (PrettyCheckForRaw(IncomeData, 2) == true)
            {
                return JsonSerializer.Serialize(NVG.Settings.LastBlock, Notus.Variable.Constant.JsonSetting);
            }
            return JsonSerializer.Serialize(NVG.Settings.LastBlock);
        }
        private string Request_BlockSummary(NVS.HttpRequestDetails IncomeData)
        {
            if (PrettyCheckForRaw(IncomeData, 2) == true)
            {
                return JsonSerializer.Serialize(new NVS.LastBlockInfo()
                {
                    RowNo = NVG.Settings.LastBlock.info.rowNo,
                    uID = NVG.Settings.LastBlock.info.uID,
                    Sign = NVG.Settings.LastBlock.sign
                }, Notus.Variable.Constant.JsonSetting);

            }
            return JsonSerializer.Serialize(new NVS.LastBlockInfo()
            {
                RowNo = NVG.Settings.LastBlock.info.rowNo,
                uID = NVG.Settings.LastBlock.info.uID,
                Sign = NVG.Settings.LastBlock.sign
            });
        }
        private string Request_GenerateToken(NVS.HttpRequestDetails IncomeData)
        {
            if (IncomeData.UrlList.Length > 2)
            {
                if (IncomeData.UrlList[1].ToLower() != "generate")
                {
                    return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
                    {
                        UID = "",
                        Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                        Status = JsonSerializer.Serialize(IncomeData.UrlList)
                    });
                }
                string WalletKeyStr = IncomeData.UrlList[2];
                if (IncomeData.PostParams.ContainsKey("data") == false)
                {
                    return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
                    {
                        UID = "",
                        Code = Notus.Variable.Constant.ErrorNoList.MissingArgument,
                        Status = "MissingArgument"
                    });
                }

                bool walletLocked = false;
                try
                {
                    string tmpTokenStr = IncomeData.PostParams["data"];
                    const int transferTimeOut = 86400;
                    string CurrentCurrency = NVG.Settings.Genesis.CoinInfo.Tag;
                    NVS.WalletBalanceStruct tmpGeneratorBalanceObj = NGF.Balance.Get(WalletKeyStr, 0);
                    //Console.WriteLine("WalletKeyStr           : " + WalletKeyStr);
                    ////Console.WriteLine("WalletKeyStr           : " + WalletKeyStr);
                    //Console.WriteLine("tmpGeneratorBalanceObj : " + JsonSerializer.Serialize(tmpGeneratorBalanceObj));
                    //Console.WriteLine("tmpGeneratorBalanceObj : " + JsonSerializer.Serialize(tmpGeneratorBalanceObj));
                    if (tmpGeneratorBalanceObj.Balance.ContainsKey(CurrentCurrency) == false)
                    {
                        return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.NeedCoin,
                            Status = "NeedCoin"
                        });
                    }

                    NVS.BlockStruct_160 tmpTokenObj = JsonSerializer.Deserialize<NVS.BlockStruct_160>(tmpTokenStr);

                    if (Notus.Wallet.Block.Exist(NVG.Settings.Network, NVG.Settings.Layer, tmpTokenObj.Info.Tag) == true)
                    {
                        return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.TagExists,
                            Status = "TagExists"
                        });
                    }

                    string TokenRawDataForSignText = Notus.Core.MergeRawData.TokenGenerate(tmpTokenObj.Creation.PublicKey, tmpTokenObj.Info, tmpTokenObj.Reserve);

                    if (Notus.Wallet.ID.Verify(TokenRawDataForSignText, tmpTokenObj.Creation.Sign, tmpTokenObj.Creation.PublicKey) == false)
                    {
                        return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.WrongSign,
                            Status = "WrongSign"
                        });
                    }

                    string tmpOwnerWalletStr = Notus.Wallet.ID.GetAddressWithPublicKey(tmpTokenObj.Creation.PublicKey);
                    if (string.Equals(WalletKeyStr, tmpOwnerWalletStr) == false)
                    {
                        return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.WrongAccount,
                            Status = "WrongAccount"
                        });
                    }

                    if (NGF.Balance.WalletUsageAvailable(WalletKeyStr) == false)
                    {
                        return JsonSerializer.Serialize(new NVS.BlockResponse()
                        {
                            UID = string.Empty,
                            Status = "WalletUsing",
                            Result = NVE.BlockStatusCode.WalletUsing
                        });
                    }

                    if (NGF.Balance.StartWalletUsage(WalletKeyStr) == false)
                    {
                        return JsonSerializer.Serialize(new NVS.BlockResponse()
                        {
                            UID = string.Empty,
                            Status = "AnErrorOccurred",
                            Result = NVE.BlockStatusCode.AnErrorOccurred
                        });
                    }
                    walletLocked = true;
                    BigInteger WalletBalanceInt = NGF.Balance.GetCoinBalance(tmpGeneratorBalanceObj, NVG.Settings.Genesis.CoinInfo.Tag);
                    Int64 tmpFeeVolume = Notus.Wallet.Fee.Calculate(tmpTokenObj, NVG.Settings.Network);
                    if (tmpFeeVolume > WalletBalanceInt)
                    {
                        NGF.Balance.StopWalletUsage(WalletKeyStr);
                        return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.NeedCoin,
                            Status = "NeedCoin"
                        });
                    }
                    /*
                    if (Func_AddToChainPool == null)
                    {
                        NGF.Balance.StopWalletUsage(WalletKeyStr);
                        return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                            Status = "UnknownError"
                        });
                    }
                    */
                    // buraya token sahibinin önceki bakiyesi yazılacak,
                    // burada out ile nihai bakiyede belirtilecek
                    // tmpTokenObj.Validator = NVG.Settings.NodeWallet.WalletKey;
                    // tmpTokenObj.Balance
                    tmpTokenObj.Balance = new NVClass.WalletBalanceStructForTransaction()
                    {
                        Wallet = tmpGeneratorBalanceObj.Wallet,
                        WitnessBlockUid = tmpGeneratorBalanceObj.UID,
                        WitnessRowNo = tmpGeneratorBalanceObj.RowNo,
                        Balance = tmpGeneratorBalanceObj.Balance
                    };
                    tmpTokenObj.Validator = new NVS.ValidatorStruct()
                    {
                        NodeWallet = NVG.Settings.NodeWallet.WalletKey,
                        Reward = tmpFeeVolume.ToString()
                    };
                    (bool tmpBalanceResult, NVS.WalletBalanceStruct tmpNewGeneratorBalance) =
                        NGF.Balance.SubtractVolumeWithUnlockTime(
                            NGF.Balance.Get(WalletKeyStr, 0),
                            tmpFeeVolume.ToString(),
                            NVG.Settings.Genesis.CoinInfo.Tag
                        );

                    tmpTokenObj.Out = tmpNewGeneratorBalance.Balance;

                    string tmpChunkIdKey = NGF.GenerateTxUid();
                    //private string Request_GenerateToken(NVS.HttpRequestDetails IncomeData)
                    bool tmpAddResult = NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
                    {
                        uid = tmpChunkIdKey,
                        type = NVE.BlockTypeList.TokenGeneration,
                        data = JsonSerializer.Serialize(tmpTokenObj)
                    });
                    if (tmpAddResult == true)
                    {
                        return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
                        {
                            UID = tmpTokenObj.Creation.UID,
                            Code = Notus.Variable.Constant.ErrorNoList.AddedToQueue,
                            Status = "AddedToQueue"
                        });
                    }

                    NGF.Balance.StopWalletUsage(WalletKeyStr);
                    return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
                    {
                        UID = "",
                        Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                        Status = "UnknownError"
                    });
                }
                catch (Exception err)
                {
                    if (walletLocked == true)
                    {
                        NGF.Balance.StopWalletUsage(WalletKeyStr);
                    }
                    return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
                    {
                        UID = "",
                        Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                        Status = "UnknownError"
                    });
                }
            }

            return JsonSerializer.Serialize(new NVS.BlockResponseStruct()
            {
                UID = "",
                Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                Status = JsonSerializer.Serialize(IncomeData.UrlList)
            });
        }

        private string Request_ApproveMultiTransaction(NVS.HttpRequestDetails IncomeData)
        {
            // önce genel kontroller yapılıyor....
            if (IncomeData.PostParams.ContainsKey("data") == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 1111,
                    ErrorText = "WrongParameter",
                    Result = NVE.BlockStatusCode.WrongParameter
                });
            }
            /*
            if (Func_AddToChainPool == null)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 2398565,
                    ErrorText = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
            */
            if (NVG.Settings == null)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 2222,
                    ErrorText = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.Genesis == null)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 3333,
                    ErrorText = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.NodeWallet == null)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 4444,
                    ErrorText = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }

            // gelen data dönüştürülüyor....
            string tmpLockAccountStr = IncomeData.PostParams["data"];
            NVS.MultiWalletTransactionApproveStruct? TransctionApproveObj =
                JsonSerializer.Deserialize<NVS.MultiWalletTransactionApproveStruct>(tmpLockAccountStr);
            if (TransctionApproveObj == null)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 5555,
                    ErrorText = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }


            string multiWalletKey = IncomeData.UrlList[3];
            string rawDataStr = Core.MergeRawData.ApproveMultiWalletTransaction(
                TransctionApproveObj.Approve,
                TransctionApproveObj.TransactionId,
                TransctionApproveObj.CurrentTime
            );

            // gelenn verinin doğruluğu test ediliyor.... 
            bool verifyTx = Notus.Wallet.ID.Verify(
                rawDataStr,
                TransctionApproveObj.Sign,
                TransctionApproveObj.PublicKey
            );
            if (verifyTx == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 7777,
                    ErrorText = "WrongSignature",
                    Result = NVE.BlockStatusCode.WrongSignature
                });
            }


            string voter_WalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(TransctionApproveObj.PublicKey);
            Dictionary<string, NVE.BlockStatusCode> SignList
                = new Dictionary<string, NVE.BlockStatusCode>();
            string multiTxText = string.Empty;
            ulong txTime = 0;
            string multiKeyId = string.Empty;
            ObjMp_MultiSignPool.Each((string tmpMultiKeyId, string multiTransferList) =>
            {
                if (multiTxText.Length == 0)
                {
                    Dictionary<ulong, NVS.MultiWalletTransactionVoteStruct>? uidList =
                        JsonSerializer.Deserialize<Dictionary<
                            ulong,
                            NVS.MultiWalletTransactionVoteStruct>
                        >(multiTransferList);
                    if (uidList != null)
                    {
                        foreach (KeyValuePair<ulong, NVS.MultiWalletTransactionVoteStruct> entry in uidList)
                        {
                            if (string.Equals(TransctionApproveObj.TransactionId, entry.Value.TransactionId))
                            {
                                txTime = entry.Key;
                                if (entry.Value.Approve.ContainsKey(voter_WalletKey))
                                {
                                    multiTxText = multiTransferList;
                                    multiKeyId = tmpMultiKeyId;
                                }
                            }
                        }
                    }
                }
            });

            if (multiTxText.Length == 0)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 8888,
                    ErrorText = "UnknownTransaction",
                    Result = NVE.BlockStatusCode.UnknownTransaction
                });
            }

            Dictionary<ulong, NVS.MultiWalletTransactionVoteStruct>? uidList =
                JsonSerializer.Deserialize<Dictionary<
                    ulong,
                    NVS.MultiWalletTransactionVoteStruct>
                >(multiTxText);

            if (uidList == null)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 9999,
                    ErrorText = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            // sadece bekliyor durumunda aşağıya devam edecek.
            if (uidList[txTime].Status != Variable.Enum.BlockStatusCode.Pending)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 1212,
                    ErrorText = uidList[txTime].Status.ToString(),
                    Result = uidList[txTime].Status
                });
            }

            // ilemi onaylayan kullanıcının imzası yapıya eşitlenecek...
            uidList[txTime].Approve[voter_WalletKey].Approve = TransctionApproveObj.Approve;
            uidList[txTime].Approve[voter_WalletKey].TransactionId = TransctionApproveObj.TransactionId;
            uidList[txTime].Approve[voter_WalletKey].CurrentTime = TransctionApproveObj.CurrentTime;
            uidList[txTime].Approve[voter_WalletKey].Sign = TransctionApproveObj.Sign;
            uidList[txTime].Approve[voter_WalletKey].PublicKey = TransctionApproveObj.PublicKey;


            // verilen oylar veya red'ler sayılacak
            int voterCount = 0;
            int approveCount = 0;
            int refuseCount = 0;
            foreach (KeyValuePair<string, NVS.MultiWalletTransactionApproveStruct> entry in uidList[txTime].Approve)
            {
                voterCount++;
                if (entry.Value.Approve == true)
                {
                    approveCount++;
                }
                else
                {
                    if (entry.Value.PublicKey.Length > 0)
                    {
                        refuseCount++;
                    }
                }
            }
            bool acceptTx = false;
            bool refuseTx = false;
            if (uidList[txTime].VoteType == Variable.Enum.MultiWalletType.AllRequired)
            {
                if (refuseCount == 0)
                {
                    if (voterCount == approveCount)
                    {
                        acceptTx = true;
                    }
                }
                else
                {
                    refuseTx = true;
                }
            }
            if (uidList[txTime].VoteType == Variable.Enum.MultiWalletType.MajorityRequired)
            {
                int needVote = System.Convert.ToInt32(Math.Ceiling((decimal)voterCount / 2)) + 1;
                if (approveCount >= needVote)
                {
                    acceptTx = true;
                }
            }

            if (acceptTx == true)
            {
                uidList[txTime].Status = Variable.Enum.BlockStatusCode.InProgress;
            }
            else
            {
                if (refuseTx == true)
                {
                    uidList[txTime].Status = Variable.Enum.BlockStatusCode.Rejected;
                }
                else
                {
                    uidList[txTime].Status = Variable.Enum.BlockStatusCode.Pending;
                }
            }

            // eğer verilen oy yeterli değilse havuzda beklemeye devam edecek
            if (uidList[txTime].Status != Variable.Enum.BlockStatusCode.InProgress)
            {
                ObjMp_MultiSignPool.Set(multiKeyId, JsonSerializer.Serialize(uidList));
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 0,
                    ErrorText = uidList[txTime].Status.ToString(),
                    Result = uidList[txTime].Status
                });
            }

            // tüm süreçler tamamsa, şimdi sırada
            // cüzdanların kilitlenmesi işlemi başlıyor
            string validatorWalletKey = NVG.Settings.NodeWallet.WalletKey;
            string receiverWalletKey = uidList[txTime].Sender.Receiver;
            if (
                NGF.Balance.WalletUsageAvailable(multiWalletKey) == false
                ||
                NGF.Balance.WalletUsageAvailable(receiverWalletKey) == false
                ||
                NGF.Balance.WalletUsageAvailable(validatorWalletKey) == false
            )
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 987987,
                    ErrorText = "WalletUsing",
                    Result = NVE.BlockStatusCode.WalletUsing
                });
            }

            if (
                NGF.Balance.StartWalletUsage(multiWalletKey) == false
                ||
                NGF.Balance.StartWalletUsage(receiverWalletKey) == false
                ||
                NGF.Balance.StartWalletUsage(validatorWalletKey) == false
            )
            {
                NGF.Balance.StopWalletUsage(multiWalletKey);
                NGF.Balance.StopWalletUsage(receiverWalletKey);
                NGF.Balance.StopWalletUsage(validatorWalletKey);
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 953268,
                    ErrorText = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }
            string transferCoinName = uidList[txTime].Sender.Currency;


            // burada gelen bakiyeyi zaman kiliti ile kontrol edecek.
            NVS.WalletBalanceStruct tmpValidatorWalletBalanceObj_Current = NGF.Balance.Get(NVG.Settings.NodeWallet.WalletKey, 0);
            NVS.WalletBalanceStruct tmpValidatorWalletBalanceObj_New = NGF.Balance.Get(NVG.Settings.NodeWallet.WalletKey, 0);

            NVS.WalletBalanceStruct tmpReceiverWalletBalanceObj_Current = NGF.Balance.Get(uidList[txTime].Sender.Receiver, 0);
            NVS.WalletBalanceStruct tmpReceiverWalletBalanceObj_New = NGF.Balance.Get(uidList[txTime].Sender.Receiver, 0);

            NVS.WalletBalanceStruct tmpMultiWalletBalanceObj_Current = NGF.Balance.Get(multiWalletKey, 0);
            NVS.WalletBalanceStruct tmpMultiWalletBalanceObj_New = NGF.Balance.Get(multiWalletKey, 0);

            // yeterli coin ve / veya token kontrolü yapılıyor
            Int64 transferFee = Notus.Wallet.Fee.Calculate(
                NVE.Fee.CryptoTransfer_MultiSign,
                NVG.Settings.Network, NVG.Settings.Layer
            );

            BigInteger tokenNeeded = 0;
            BigInteger coinNeeded = 0;
            BigInteger coinTransferVolume = 0;

            if (string.Equals(transferCoinName, NVG.Settings.Genesis.CoinInfo.Tag))
            {
                coinTransferVolume = BigInteger.Parse(uidList[txTime].Sender.Volume);
                coinNeeded = coinTransferVolume + transferFee;
                BigInteger CoinBalanceInt = NGF.Balance.GetCoinBalance(
                    tmpMultiWalletBalanceObj_New,
                    NVG.Settings.Genesis.CoinInfo.Tag
                );

                if (coinNeeded > CoinBalanceInt)
                {
                    NGF.Balance.StopWalletUsage(multiWalletKey);
                    NGF.Balance.StopWalletUsage(receiverWalletKey);
                    NGF.Balance.StopWalletUsage(validatorWalletKey);
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
                coinNeeded = transferFee;
                BigInteger coinFeeBalance = NGF.Balance.GetCoinBalance(
                    tmpMultiWalletBalanceObj_New,
                    NVG.Settings.Genesis.CoinInfo.Tag
                );
                if (transferFee > coinFeeBalance)
                {
                    NGF.Balance.StopWalletUsage(multiWalletKey);
                    NGF.Balance.StopWalletUsage(receiverWalletKey);
                    NGF.Balance.StopWalletUsage(validatorWalletKey);
                    return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                    {
                        ErrorNo = 7523,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = NVE.BlockStatusCode.InsufficientBalance
                    });
                }
                BigInteger tokenCurrentBalance = NGF.Balance.GetCoinBalance(
                    tmpMultiWalletBalanceObj_New,
                    transferCoinName
                );
                tokenNeeded = BigInteger.Parse(uidList[txTime].Sender.Volume);
                if (tokenNeeded > tokenCurrentBalance)
                {
                    NGF.Balance.StopWalletUsage(multiWalletKey);
                    NGF.Balance.StopWalletUsage(receiverWalletKey);
                    NGF.Balance.StopWalletUsage(validatorWalletKey);
                    return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                    {
                        ErrorNo = 2365,
                        ErrorText = "InsufficientBalance",
                        ID = string.Empty,
                        Result = NVE.BlockStatusCode.InsufficientBalance
                    });
                }
            }


            // eklenecek ve çıkarılacak token / coinler hesaplardan çıkartılıyor...
            (bool volumeError, tmpMultiWalletBalanceObj_New) =
                NGF.Balance.SubtractVolumeWithUnlockTime(
                    tmpMultiWalletBalanceObj_New,
                    coinNeeded.ToString(),
                    NVG.Settings.Genesis.CoinInfo.Tag
                );
            if (volumeError == true)
            {
                NGF.Balance.StopWalletUsage(multiWalletKey);
                NGF.Balance.StopWalletUsage(receiverWalletKey);
                NGF.Balance.StopWalletUsage(validatorWalletKey);
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 9102,
                    ErrorText = "AnErrorOccurred",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }
            if (tokenNeeded > 0)
            {
                (volumeError, tmpMultiWalletBalanceObj_New) =
                    NGF.Balance.SubtractVolumeWithUnlockTime(
                        tmpMultiWalletBalanceObj_New,
                        tokenNeeded.ToString(),
                        uidList[txTime].Sender.Currency
                    );

                tmpReceiverWalletBalanceObj_New = NGF.Balance.AddVolumeWithUnlockTime(
                    tmpReceiverWalletBalanceObj_New,
                    tokenNeeded.ToString(),
                    uidList[txTime].Sender.Currency,
                    uidList[txTime].Sender.UnlockTime
                );
            }
            else
            {
                tmpReceiverWalletBalanceObj_New = NGF.Balance.AddVolumeWithUnlockTime(
                    tmpReceiverWalletBalanceObj_New,
                    coinTransferVolume.ToString(),
                    NVG.Settings.Genesis.CoinInfo.Tag,
                    uidList[txTime].Sender.UnlockTime
                );
            }

            ulong validatorUnlockTime = Notus.Date.ToLong(
                Notus.Date.ToDateTime(uidList[txTime].Sender.CurrentTime).AddDays(30)
            );

            tmpValidatorWalletBalanceObj_New = NGF.Balance.AddVolumeWithUnlockTime(
                tmpValidatorWalletBalanceObj_New,
                transferFee.ToString(),
                NVG.Settings.Genesis.CoinInfo.Tag,
                validatorUnlockTime
            );

            // içeriği boş olan zaman bilgili alanlar Dictionary'den çıkartılıyor
            tmpMultiWalletBalanceObj_New.Balance[NVG.Settings.Genesis.CoinInfo.Tag] =
                NGF.Balance.RemoveZeroUnlockTime(
                    tmpMultiWalletBalanceObj_New.Balance[NVG.Settings.Genesis.CoinInfo.Tag]
                );

            if (string.Equals(uidList[txTime].Sender.Currency, NVG.Settings.Genesis.CoinInfo.Tag) != false)
            {
                tmpMultiWalletBalanceObj_New.Balance[uidList[txTime].Sender.Currency] =
                    NGF.Balance.RemoveZeroUnlockTime(
                        tmpMultiWalletBalanceObj_New.Balance[uidList[txTime].Sender.Currency]
                    );
            }

            tmpReceiverWalletBalanceObj_New.Balance[uidList[txTime].Sender.Currency] =
                NGF.Balance.RemoveZeroUnlockTime(
                    tmpReceiverWalletBalanceObj_New.Balance[uidList[txTime].Sender.Currency]
                );

            string transactionId = TransctionApproveObj.TransactionId;
            Dictionary<string, NVS.MultiWalletTransactionStruct> multiTx = new
                Dictionary<string, NVS.MultiWalletTransactionStruct>(){
                {
                    transactionId,
                    new NVS.MultiWalletTransactionStruct()
                    {
                        Sender = new NVS.CryptoTransaction()
                        {
                            Currency = uidList[txTime].Sender.Currency,
                            CurrentTime = uidList[txTime].Sender.CurrentTime,
                            CurveName = uidList[txTime].Sender.CurveName,
                            PublicKey = uidList[txTime].Sender.PublicKey,
                            Receiver = uidList[txTime].Sender.Receiver,
                            Sender = uidList[txTime].Sender.Sender,
                            Sign = uidList[txTime].Sender.Sign,
                            Volume = uidList[txTime].Sender.Volume
                        },
                        Approve = new Dictionary<string, NVS.MultiTransactionApproveStruct>(),
                        Before = new Dictionary<string, NVS.BeforeBalanceStruct>()
                        {
                            {
                                multiWalletKey,
                                new NVS.BeforeBalanceStruct(){
                                     Balance=tmpMultiWalletBalanceObj_Current.Balance,
                                     Witness=new NVS.WitnessBlock()
                                     {
                                        RowNo=tmpMultiWalletBalanceObj_Current.RowNo,
                                        UID=tmpMultiWalletBalanceObj_Current.UID
                                     }
                                }
                            },
                            {
                                receiverWalletKey,
                                new NVS.BeforeBalanceStruct(){
                                     Balance=tmpReceiverWalletBalanceObj_Current.Balance,
                                     Witness=new NVS.WitnessBlock()
                                     {
                                        RowNo=tmpReceiverWalletBalanceObj_Current.RowNo,
                                        UID=tmpReceiverWalletBalanceObj_Current.UID
                                     }
                                }
                            },
                            {
                                NVG.Settings.NodeWallet.WalletKey,
                                new NVS.BeforeBalanceStruct(){
                                     Balance=tmpValidatorWalletBalanceObj_Current.Balance,
                                     Witness=new NVS.WitnessBlock()
                                     {
                                        RowNo=tmpValidatorWalletBalanceObj_Current.RowNo,
                                        UID=tmpValidatorWalletBalanceObj_Current.UID
                                     }
                                }
                            }
                        },
                        After = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>()
                        {
                            {
                                multiWalletKey, tmpMultiWalletBalanceObj_New.Balance
                            },
                            {
                                receiverWalletKey,tmpReceiverWalletBalanceObj_New.Balance
                            },
                            {
                                NVG.Settings.NodeWallet.WalletKey,tmpValidatorWalletBalanceObj_New.Balance
                            }
                        },
                        Fee = transferFee.ToString()
                    }
                }
            };
            foreach (KeyValuePair<string, NVS.MultiWalletTransactionApproveStruct> entry in uidList[txTime].Approve)
            {
                multiTx[transactionId].Approve.Add(
                    entry.Key,
                    new NVS.MultiTransactionApproveStruct()
                    {
                        Approve = entry.Value.Approve,
                        CurrentTime = entry.Value.CurrentTime,
                        PublicKey = entry.Value.PublicKey,
                        Sign = entry.Value.Sign
                    }
                );
            }

            bool tmpAddResult = NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
            {
                uid = multiKeyId,
                type = NVE.BlockTypeList.MultiWalletCryptoTransfer,
                data = JsonSerializer.Serialize(multiTx)
            });
            if (tmpAddResult == true)
            {
                ObjMp_MultiSignPool.Set(multiKeyId, JsonSerializer.Serialize(uidList));
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ID = string.Empty,
                    ErrorNo = 0,
                    ErrorText = "AddedToQueue",
                    Result = NVE.BlockStatusCode.AddedToQueue
                });
            }
            NGF.Balance.StopWalletUsage(multiWalletKey);
            NGF.Balance.StopWalletUsage(receiverWalletKey);
            NGF.Balance.StopWalletUsage(validatorWalletKey);
            return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
            {
                ID = string.Empty,
                ErrorNo = 9879871,
                ErrorText = "AnErrorOccurred",
                Result = NVE.BlockStatusCode.AnErrorOccurred
            });
        }

        private string Request_AddMultiWallet(NVS.HttpRequestDetails IncomeData)
        {
            if (IncomeData.PostParams.ContainsKey("data") == false)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WrongParameter",
                    Result = NVE.BlockStatusCode.WrongParameter
                });
            }

            if (NVG.Settings == null)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.Genesis == null)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.NodeWallet == null)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }

            string tmpLockAccountStr = IncomeData.PostParams["data"];
            NVS.MultiWalletStruct? WalletObj = JsonSerializer.Deserialize<NVS.MultiWalletStruct>(tmpLockAccountStr);
            if (WalletObj == null)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            if (2 > WalletObj.WalletList.Count)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "NotEnoughParticipant",
                    Result = NVE.BlockStatusCode.NotEnoughParticipant
                });
            }
            if (NGF.Balance.WalletUsageAvailable(WalletObj.Founder.WalletKey) == false)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WalletUsing",
                    Result = NVE.BlockStatusCode.WalletUsing
                });
            }

            if (NGF.Balance.StartWalletUsage(WalletObj.Founder.WalletKey) == false)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            BigInteger howMuchCoinNeed = BigInteger.Parse((WalletObj.WalletList.Count * NVG.Settings.Genesis.Fee.MultiWallet.Addition).ToString());
            if (NGF.Balance.HasEnoughCoin(WalletObj.Founder.WalletKey, howMuchCoinNeed) == false)
            {
                NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "InsufficientBalance",
                    Result = NVE.BlockStatusCode.InsufficientBalance
                });
            }
            /*
            if (Func_AddToChainPool == null)
            {
                NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
            */
            if (Notus.Wallet.ID.CheckAddress(WalletObj.Founder.WalletKey) == false)
            {
                NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WrongWallet",
                    Result = NVE.BlockStatusCode.WrongWallet
                });
            }

            NVS.WalletBalanceStruct tmpGeneratorBalanceObj =
                NGF.Balance.Get(WalletObj.Founder.WalletKey, 0);

            BigInteger currentVolume = NGF.Balance.GetCoinBalance(
                tmpGeneratorBalanceObj,
                NVG.Settings.Genesis.CoinInfo.Tag
            );

            if (howMuchCoinNeed > currentVolume)
            {
                NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "InsufficientBalance",
                    Result = NVE.BlockStatusCode.InsufficientBalance
                });
            }

            // cüzdanın kilitlenme ve açılma işlemleri eklenecek
            (bool volumeError, NVS.WalletBalanceStruct newBalance) =
                NGF.Balance.SubtractVolumeWithUnlockTime(
                    tmpGeneratorBalanceObj,
                    howMuchCoinNeed.ToString(),
                    NVG.Settings.Genesis.CoinInfo.Tag
                );
            if (volumeError == true)
            {
                NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }
            string tmpChunkIdKey = Notus.Block.Key.Generate(
                ND.NowObj(),
                NVG.Settings.NodeWallet.WalletKey
            );
            NVS.MultiWalletStoreStruct tmpLockObj = new NVS.MultiWalletStoreStruct()
            {
                UID = tmpChunkIdKey,
                Founder = new NVS.MultiWalletFounderStruct()
                {
                    PublicKey = WalletObj.Founder.PublicKey,
                    WalletKey = WalletObj.Founder.WalletKey
                },
                MultiWalletKey = WalletObj.MultiWalletKey,
                VoteType = WalletObj.VoteType,
                WalletList = WalletObj.WalletList,
                Sign = WalletObj.Sign,
                Fee = howMuchCoinNeed.ToString(),
                Balance = tmpGeneratorBalanceObj,
                Out = newBalance.Balance
            };

            bool tmpAddResult = NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
            {
                uid = tmpChunkIdKey,
                type = NVE.BlockTypeList.MultiWalletContract,
                data = JsonSerializer.Serialize(tmpLockObj)
            });
            if (tmpAddResult == true)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = tmpChunkIdKey,
                    Status = "AddedToQueue",
                    Result = NVE.BlockStatusCode.AddedToQueue
                });
            }
            NGF.Balance.StopWalletUsage(WalletObj.Founder.WalletKey);
            return JsonSerializer.Serialize(new NVS.BlockResponse()
            {
                UID = string.Empty,
                Status = "Unknown",
                Result = NVE.BlockStatusCode.Rejected
            });
        }
        private string Request_LockAccount(NVS.HttpRequestDetails IncomeData)
        {
            if (IncomeData.PostParams.ContainsKey("data") == false)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WrongParameter",
                    Result = NVE.BlockStatusCode.WrongParameter
                });
            }

            if (NVG.Settings == null)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.Genesis == null)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
            if (NVG.Settings.NodeWallet == null)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }

            string tmpLockAccountStr = IncomeData.PostParams["data"];
            NVS.LockWalletStruct? LockObj = JsonSerializer.Deserialize<NVS.LockWalletStruct>(tmpLockAccountStr);
            if (LockObj == null)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            bool hasCoin = NGF.Balance.HasEnoughCoin(
                LockObj.WalletKey,
                BigInteger.Parse(NVG.Settings.Genesis.Fee.BlockAccount.ToString())
            );
            if (hasCoin == false)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "InsufficientBalance",
                    Result = NVE.BlockStatusCode.InsufficientBalance
                });
            }

            /*
            if (Func_AddToChainPool == null)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "Unknown",
                    Result = NVE.BlockStatusCode.Unknown
                });
            }
            */
            if (Notus.Wallet.ID.CheckAddress(LockObj.WalletKey) == false)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WrongWallet",
                    Result = NVE.BlockStatusCode.WrongWallet
                });
            }

            if (NGF.Balance.WalletUsageAvailable(LockObj.WalletKey) == false)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "WalletUsing",
                    Result = NVE.BlockStatusCode.WalletUsing
                });
            }

            if (NGF.Balance.StartWalletUsage(LockObj.WalletKey) == false)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "AnErrorOccurred",
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }
            string tmpChunkIdKey = NGF.GenerateTxUid();
            BigInteger howMuchCoinNeed = BigInteger.Parse(NVG.Settings.Genesis.Fee.BlockAccount.ToString());
            NVS.WalletBalanceStruct tmpGeneratorBalanceObj = NGF.Balance.Get(LockObj.WalletKey, 0);

            BigInteger currentVolume = NGF.Balance.GetCoinBalance(tmpGeneratorBalanceObj, NVG.Settings.Genesis.CoinInfo.Tag);
            if (howMuchCoinNeed > currentVolume)
            {
                NGF.Balance.StopWalletUsage(LockObj.WalletKey);
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = string.Empty,
                    Status = "InsufficientBalance",
                    Result = NVE.BlockStatusCode.InsufficientBalance
                });
            }

            NVS.LockWalletBeforeStruct tmpLockObj = new NVS.LockWalletBeforeStruct()
            {
                UID = tmpChunkIdKey,
                WalletKey = LockObj.WalletKey,
                UnlockTime = LockObj.UnlockTime,
                PublicKey = LockObj.PublicKey,
                Sign = LockObj.Sign
            };

            bool tmpAddResult = NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
            {
                uid = tmpChunkIdKey,
                type = NVE.BlockTypeList.LockAccount,
                data = JsonSerializer.Serialize(tmpLockObj)
            });
            if (tmpAddResult == true)
            {
                return JsonSerializer.Serialize(new NVS.BlockResponse()
                {
                    UID = tmpChunkIdKey,
                    Status = "AddedToQueue",
                    Result = NVE.BlockStatusCode.AddedToQueue
                });
            }
            NGF.Balance.StopWalletUsage(LockObj.WalletKey);
            return JsonSerializer.Serialize(new NVS.BlockResponse()
            {
                UID = string.Empty,
                Status = "Unknown",
                Result = NVE.BlockStatusCode.Rejected
            });
        }

        private string Request_Balance(NVS.HttpRequestDetails IncomeData)
        {
            if (IncomeData.UrlList[1].Length != Notus.Variable.Constant.WalletFullTextLength)
            {
                return JsonSerializer.Serialize(false);
            }
            if (NVG.Settings.Genesis == null)
            {
                return JsonSerializer.Serialize(false);
            }

            NVS.WalletBalanceStruct balanceResult = new NVS.WalletBalanceStruct()
            {
                Balance = new Dictionary<string, Dictionary<ulong, string>>(){
                    {
                        NVG.Settings.Genesis.CoinInfo.Tag,
                        new Dictionary<ulong, string>(){
                            {
                                NVG.NOW.Int ,
                                "0"
                            }
                        }
                    }
                },
                UID = "",
                Wallet = IncomeData.UrlList[1],
                RowNo = 0
            };
            if (IncomeData.UrlList[1].Length == Notus.Variable.Constant.WalletFullTextLength)
            {
                balanceResult = NGF.Balance.Get(IncomeData.UrlList[1], 0);
            }

            if (PrettyCheckForRaw(IncomeData, 2) == true)
            {
                return JsonSerializer.Serialize(balanceResult, Notus.Variable.Constant.JsonSetting);
            }
            return JsonSerializer.Serialize(balanceResult);
        }

        private string Request_NFTImageList(NVS.HttpRequestDetails IncomeData)
        {
            string tmpWalletKey = IncomeData.UrlList[2];

            string tmpListingDir = Notus.IO.GetFolderName(
                NVG.Settings.Network,
                NVG.Settings.Layer,
                Notus.Variable.Constant.StorageFolderName.Storage
            ) + tmpWalletKey + System.IO.Path.DirectorySeparatorChar;
            Notus.IO.CreateDirectory(tmpListingDir);

            List<string> imageListId = new List<string>();
            string[] fileLists = Directory.GetFiles(tmpListingDir, "*.*");
            foreach (string fileName in fileLists)
            {
                string extension = Path.GetExtension(fileName);
                if (string.Equals(".marked", extension) == false)
                {
                    string tmpOnlyFileName = Path.GetFileName(fileName);
                    tmpOnlyFileName = tmpOnlyFileName.Substring(0, tmpOnlyFileName.Length - extension.Length);
                    imageListId.Add(tmpOnlyFileName);
                }
            }
            return JsonSerializer.Serialize(imageListId);
        }

        private string Request_NFTPublicImageDetail_SubFunction(string tmpWalletKey, string tmpStorageId)
        {
            string tmpListingDir = Notus.IO.GetFolderName(
                NVG.Settings.Network,
                NVG.Settings.Layer,
                Notus.Variable.Constant.StorageFolderName.Storage
            ) + tmpWalletKey + System.IO.Path.DirectorySeparatorChar;
            Notus.IO.CreateDirectory(tmpListingDir);

            string[] fileLists = Directory.GetFiles(tmpListingDir, tmpStorageId + ".*");
            foreach (string fileName in fileLists)
            {
                string extension = Path.GetExtension(fileName);
                if (string.Equals(".marked", extension) == true)
                {
                    //string tmpOnlyFileName = fileName.Substring(0, tmpOnlyFileName.Length - extension.Length);
                    using (FileStream reader = new FileStream(fileName, FileMode.Open))
                    {
                        byte[] buffer = new byte[reader.Length];
                        reader.Read(buffer, 0, (int)reader.Length);
                        return System.Convert.ToBase64String(buffer);

                        //burada dosya türü bulunacak ve base64 metni tam olarak yazılı gönderilecek.
                        //burada dosya türü bulunacak ve base64 metni tam olarak yazılı gönderilecek.
                        //burada dosya türü bulunacak ve base64 metni tam olarak yazılı gönderilecek.
                        //return "data:image/" + extension.Substring(1) + ";base64," + Convert.ToBase64String(buffer);
                    }
                }
            }
            return JsonSerializer.Serialize("");
        }
        private string Request_NFTPublicImageDetail(NVS.HttpRequestDetails IncomeData)
        {
            return Request_NFTPublicImageDetail_SubFunction(IncomeData.UrlList[2], IncomeData.UrlList[3]);
        }

        private string Request_NFTPrivateImageDetail(NVS.HttpRequestDetails IncomeData)
        {
            if (IncomeData.PostParams.ContainsKey("data") == true)
            {
                NVS.GenericSignStruct signData = JsonSerializer.Deserialize<NVS.GenericSignStruct>(IncomeData.PostParams["data"]);
                string tmpWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(signData.PublicKey);

                string tmpNftStorageId = IncomeData.UrlList[2];
                string publicKey = "";
                string signStr = "";
                string timeStr = "";

                string tmpListingDir = Notus.IO.GetFolderName(
                    NVG.Settings.Network,
                    NVG.Settings.Layer,
                    Notus.Variable.Constant.StorageFolderName.Storage
                ) + tmpWalletKey + System.IO.Path.DirectorySeparatorChar;
                string[] fileLists = Directory.GetFiles(tmpListingDir, tmpNftStorageId + ".*");
                foreach (string fileName in fileLists)
                {
                    string extension = Path.GetExtension(fileName);
                    if (string.Equals(".marked", extension) == false)
                    {

                        using (FileStream reader = new FileStream(fileName, FileMode.Open))
                        {
                            byte[] buffer = new byte[reader.Length];
                            reader.Read(buffer, 0, (int)reader.Length);
                            return "data:image/" + extension.Substring(1) + ";base64," + System.Convert.ToBase64String(buffer);
                        }
                    }
                }
            }
            return JsonSerializer.Serialize("");
        }

        // return metrics and system status
        private string Request_Main()
        {
            if (NVG.Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        NVE.NetworkNodeType.Main
                    ), Notus.Variable.Constant.JsonSetting
                );
            }
            return JsonSerializer.Serialize(
                GiveMeList(
                    NVE.NetworkNodeType.Main
                )
            );
        }
        private string Request_Replicant()
        {
            if (NVG.Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        NVE.NetworkNodeType.Replicant
                    ), Notus.Variable.Constant.JsonSetting
                );
            }
            return JsonSerializer.Serialize(
                GiveMeList(
                    NVE.NetworkNodeType.Replicant
                )
            );
        }
        private string Request_Master()
        {
            if (NVG.Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        NVE.NetworkNodeType.Master
                    ), Notus.Variable.Constant.JsonSetting
                );
            }
            return JsonSerializer.Serialize(
                GiveMeList(
                    NVE.NetworkNodeType.Master
                )
            );
        }
        private string Request_Node()
        {
            if (NVG.Settings.PrettyJson == true)
            {
                return JsonSerializer.Serialize(
                    GiveMeList(
                        NVE.NetworkNodeType.All
                    ), Notus.Variable.Constant.JsonSetting
                );
            }
            return JsonSerializer.Serialize(
                GiveMeList(
                    NVE.NetworkNodeType.All
                )
            );
        }
        private string Request_Metrics(NVS.HttpRequestDetails IncomeData)
        {

            if (IncomeData.UrlList.Length > 1)
            {
                if (IncomeData.UrlList[1].ToLower() == "node")
                {
                    UInt64 tmpTotalBlock = (UInt64)GiveMeList(NVE.NetworkNodeType.All).Count;
                    if (NVG.Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new NVS.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(
                        new NVS.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }
                    );
                }
                if (IncomeData.UrlList[1].ToLower() == "master")
                {
                    UInt64 tmpTotalBlock = (UInt64)GiveMeList(NVE.NetworkNodeType.Master).Count;
                    if (NVG.Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new NVS.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(
                        new NVS.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }
                    );
                }
                if (IncomeData.UrlList[1].ToLower() == "main")
                {
                    UInt64 tmpTotalBlock = (UInt64)GiveMeList(NVE.NetworkNodeType.Main).Count;
                    if (NVG.Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new NVS.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(
                        new NVS.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }
                    );
                }
                if (IncomeData.UrlList[1].ToLower() == "replicant")
                {
                    UInt64 tmpTotalBlock = (UInt64)GiveMeList(NVE.NetworkNodeType.Replicant).Count;
                    if (NVG.Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new NVS.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(
                        new NVS.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }
                    );
                }
                if (IncomeData.UrlList[1].ToLower() == "block")
                {
                    UInt64 tmpTotalBlock = (UInt64)NVG.Settings.LastBlock.info.rowNo;
                    if (NVG.Settings.PrettyJson == true)
                    {
                        return JsonSerializer.Serialize(new NVS.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }, Notus.Variable.Constant.JsonSetting);
                    }
                    return JsonSerializer.Serialize(
                        new NVS.MetricsResponseStruct()
                        {
                            Count = tmpTotalBlock
                        }
                    );
                }
            }

            return JsonSerializer.Serialize(false);
        }
        private string Request_Online(NVS.HttpRequestDetails IncomeData)
        {
            if (PrettyCheckForRaw(IncomeData, 1))
            {
                return JsonSerializer.Serialize(IncomeData, Notus.Variable.Constant.JsonSetting);
            }
            return JsonSerializer.Serialize(IncomeData);
        }

        private int GiveMeCount(NVE.NetworkNodeType WhichList)
        {
            if (WhichList == NVE.NetworkNodeType.Main)
            {
                return AllMainList.Count;
            }
            if (WhichList == NVE.NetworkNodeType.Master)
            {
                return AllMasterList.Count;
            }
            if (WhichList == NVE.NetworkNodeType.Replicant)
            {
                return AllReplicantList.Count;
            }
            if (WhichList == NVE.NetworkNodeType.Connectable)
            {
                return AllMasterList.Count + AllMainList.Count;
            }

            return AllMasterList.Count + AllMainList.Count + AllReplicantList.Count;
        }

        private List<string> GiveMeList(NVE.NetworkNodeType WhichList)
        {
            if (WhichList == NVE.NetworkNodeType.Main)
            {
                return AllMainList;
            }
            if (WhichList == NVE.NetworkNodeType.Master)
            {
                return AllMasterList;
            }
            if (WhichList == NVE.NetworkNodeType.Replicant)
            {
                return AllReplicantList;
            }
            if (WhichList == NVE.NetworkNodeType.Connectable)
            {
                List<string> tmpFullList = new List<string>();
                for (int a = 0; a < AllMainList.Count; a++)
                {
                    tmpFullList.Add(AllMainList[a]);
                }
                for (int a = 0; a < AllMasterList.Count; a++)
                {
                    tmpFullList.Add(AllMasterList[a]);
                }
                return tmpFullList;
            }
            if (WhichList == NVE.NetworkNodeType.All)
            {
                List<string> tmpFullList = GiveMeList(NVE.NetworkNodeType.Connectable);
                for (int a = 0; a < AllReplicantList.Count; a++)
                {
                    tmpFullList.Add(AllReplicantList[a]);
                }
                return tmpFullList;
            }
            return new List<string>();
        }
        public Api()
        {

        }
        ~Api()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (NGF.Balance != null)
            {
                try
                {
                    NGF.Balance.Dispose();
                }
                catch { }
            }
            if (BlockDbObj != null)
            {
                try
                {
                    BlockDbObj.Dispose();
                }
                catch { }
            }
        }
    }
}
