using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
using System.Text.Json;
using Notus.Compression.TGZ;
using System;
using System.Threading.Tasks;
//using NVG = Notus.Variable.Globals;
namespace Notus.Variable
{
    static class Globals
    {
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
                }
            };
        }

        public static class Functions
        {
            public static Notus.Wallet.Balance Balance;
            public static Notus.TGZArchiver Archiver { get; set; }

            public static void Start()
            {
                Archiver = new Notus.TGZArchiver();
                Balance = new Notus.Wallet.Balance();
                Balance.Start();
            }
            static Functions()
            {
            }
        }
    }
}
