using Notus.Compression.TGZ;
using Notus.Globals.Variable;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NVC = Notus.Variable.Constant;
using NVE = Notus.Variable.Enum;
using NVS = Notus.Variable.Struct;

namespace Notus.Variable
{
    static class Globals
    {
        /*


        * minimum gereken node sayısı -> 6 adet node
        * 
            * EMPTY BLOK
            * empty blok belirlenen süre içerisinde oluşturulacak
                * eğer belirlenen süre içerisinde oluşturulması gereken empty blok oluşturulmazsa
                  o zaman önce eksik kalan empty bloklar oluşturulana kadar başka blok üretilmeyecek

                * 
                *
                *
        * 
        * bu nodelar sıraya girecek
            * 
            * 1. node 
            * 
            * 0 ile 0,2 saniye arasını işlem dinlemek için ayıracak
            * 0,2 ile 0,3 saniye arasını bloğu oluşturmak için ayıracak
            * 0,3 ile 0,5 saniye arasını ilk 20 arasındaki node'lara blokları dağıtmak için harcayacak.
            * eğer kendinden sonraki 5 node 1. node'dan haber alamazsa
                * birinci node'un oluşturduğu blok gözardı edilecek
                * 


        |-------|-------|-------|-------|-------|-------|-------|-------|-------|-------|-------|-------|
        0      0,2     0,5     0,7     1,0     1,2     1,5     1,7     2,0     2,2     2,5     2,7     3,0





        ilk başlangıçta 2 node senkron olarak başlayacak ve aralarında starting time için karar verecekler.
        sonrasında ağa eklenecek olan node önce ağdaki 2 node'un startingtime zamanını alacak.
        yeni node blok senkronizasyonunu tamamladıktan sonra
        şu an ki zamanın üzerine 1 dakika ekleyecek ve o zaman geldiğinde kuyruğa dahil edilmiş olacak

        */
        public static bool NodeListPrinted { get; set; }
        public static Notus.Globals.Variable.NodeQueueList NodeQueue { get; set; }

        public static Notus.Globals.Variable.Settings Settings { get; set; }
        static Globals()
        {
            Settings = new Notus.Globals.Variable.Settings()
            {
                LocalNode = true,
                InfoMode = true,
                DebugMode = true,
                EncryptMode = false,
                SynchronousSocketIsActive = false,
                PrettyJson = true,
                GenesisAssigned = false,
                DevelopmentNode = false,

                WaitTickCount = 4,

                //PacketReceive = 0,
                //PacketSend = 0,

                EncryptKey = "key-password-string",


                UTCTime = Notus.Time.GetNtpTime(),
                HashSalt = Notus.Encryption.Toolbox.GenerateSalt(),


                Layer = NVE.NetworkLayer.Layer1,
                Network = NVE.NetworkType.MainNet,
                NodeType = NVE.NetworkNodeType.Suitable,

                Nodes = new NVS.NodeQueueList()
                {
                    My = new Struct.NodeQueueInfo()
                    {
                        Begin = 0,
                        Tick = 0,
                        HexKey = "",
                        IP = new NVS.NodeInfo()
                        {
                            IpAddress = "",
                            Port = 0,
                            Wallet = ""
                        },
                        Ready = false,
                        Status = NVS.NodeStatus.Unknown,
                    },
                    Lists = new List<NVS.IpInfo>() { }
                },
                NodeWallet = new NVS.EccKeyPair()
                {
                    CurveName = "",
                    PrivateKey = "",
                    PublicKey = "",
                    WalletKey = "",
                    Words = new string[] { },
                },

                Port = new NVS.CommunicationPorts()
                {
                    MainNet = 0,
                    TestNet = 0,
                    DevNet = 0
                },
                BlockOrder = new Dictionary<ulong, string>() { }
            };
        }

        public static class Functions
        {
            public static ConcurrentDictionary<string, string> LockWalletList { get; set; }
            public static ConcurrentDictionary<string, byte> WalletUsageList { get; set; }
            public static ConcurrentDictionary<long, string> BlockOrder { get; set; }
            //public static Notus.Mempool BlockOrder { get; set; }
            public static Notus.Block.Storage Storage { get; set; }
            public static Notus.Wallet.Balance Balance { get; set; }
            public static Notus.TGZArchiver Archiver { get; set; }
            public static Notus.Block.Queue BlockQueue { get; set; }
            public static DateTime GetUtcNowFromNtp()
            {
                RefreshNtpTime();
                return Settings.UTCTime.Now;
            }
            public static void RefreshNodeQueueTime()
            {
                if (NodeQueue != null)
                {
                    if (NodeQueue.Begin == true)
                    {
                        RefreshNtpTime();
                        NodeQueue.Now = Notus.Time.DateTimeToUlong(Settings.UTCTime.Now);
                    }
                }
                //NVG.NodeQueue.Starting = Notus.Variable.Constant.DefaultTime;
            }
            public static void RefreshNtpTime()
            {
                if (Settings.UTCTime == null)
                {
                    Settings.UTCTime = Notus.Time.GetNtpTime();
                }
                else
                {
                    Settings.UTCTime = Notus.Time.RefreshNtpTime(Settings.UTCTime);
                }
            }
            public static string GenerateTxUid()
            {
                string seedStr = "node-wallet-key";
                DateTime uidTime = DateTime.Now;
                if (Settings != null)
                {
                    if (Settings.NodeWallet != null)
                    {
                        seedStr = Settings.NodeWallet.WalletKey;
                    }
                    RefreshNtpTime();
                    uidTime = Settings.UTCTime.Now;
                }
                return Notus.Block.Key.Generate(uidTime, seedStr);
            }
            public static void Dispose()
            {
                Storage.Dispose();
                BlockQueue.Dispose();
                //Archiver.
                Balance.Dispose();
            }
            public static void Start()
            {
                WalletUsageList = new ConcurrentDictionary<string, byte>();
                LockWalletList = new ConcurrentDictionary<string, string>();
                BlockOrder = new ConcurrentDictionary<long, string>();
                Storage = new Notus.Block.Storage();
                BlockQueue = new Notus.Block.Queue();
                Archiver = new Notus.TGZArchiver();
                Balance = new Notus.Wallet.Balance();
                if (Settings.GenesisCreated == false)
                {
                    Balance.Start();
                }
                RefreshNtpTime();
                Globals.NodeListPrinted = false;
                Globals.NodeQueue = new Notus.Globals.Variable.NodeQueueList();

                Globals.NodeQueue.Begin = false;
                Globals.NodeQueue.OrderCount = 1;

                Globals.NodeQueue.NodeOrder = new Dictionary<int, string>();
                Globals.NodeQueue.TimeBaseWalletList = new Dictionary<ulong, string>();


                BlockOrder.Clear();
                /*
                string tmpFolderName = Notus.IO.GetFolderName(
                    Settings,
                    Notus.Variable.Constant.StorageFolderName.Common
                );
                BlockOrder = new Notus.Mempool(tmpFolderName +"block_order_list");
                BlockOrder.AsyncActive = false;
                BlockOrder.Clear();
                */
            }

            static Functions()
            {
            }
        }
    }
}
