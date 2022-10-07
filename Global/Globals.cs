using Notus.Compression.TGZ;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
namespace Notus.Variable
{
    static class Globals
    {
        //airdrop-exception
        public static string AirdropExceptionWalletKey { get; set; }

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

                EncryptKey = "key-password-string",


                UTCTime = Notus.Time.GetNtpTime(),
                HashSalt = Notus.Encryption.Toolbox.GenerateSalt(),


                Layer = Notus.Variable.Enum.NetworkLayer.Layer1,
                Network = Notus.Variable.Enum.NetworkType.MainNet,
                NodeType = Notus.Variable.Enum.NetworkNodeType.Suitable,


                NodeWallet = new Notus.Variable.Struct.EccKeyPair()
                {
                    CurveName = "",
                    PrivateKey = "",
                    PublicKey = "",
                    WalletKey = "",
                    Words = new string[] { },
                },

                Port = new Notus.Variable.Struct.CommunicationPorts()
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
            public static Dictionary<long,string> BlockOrder { get; set; }
            //public static Notus.Mempool BlockOrder { get; set; }
            public static Notus.Block.Storage Storage { get; set; }
            public static Notus.Wallet.Balance Balance { get; set; }
            public static Notus.TGZArchiver Archiver { get; set; }
            public static Notus.Block.Queue BlockQueue { get; set; }
            public static void Start()
            {
                BlockOrder = new Dictionary<long, string>();
                Storage = new Notus.Block.Storage();
                BlockQueue = new Notus.Block.Queue();
                Archiver = new Notus.TGZArchiver();
                Balance = new Notus.Wallet.Balance();
                if (Settings.GenesisCreated == false)
                {
                    Balance.Start();
                }

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
