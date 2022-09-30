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

namespace Notus.Variable
{
    static class Globals
    {
        public static Notus.Variable.Common.ClassSetting Settings { get; set; }

        static Globals()
        {
            Settings = new Notus.Variable.Common.ClassSetting()
            {
                LocalNode = true,
                InfoMode = true,
                DebugMode = true,

                EncryptMode = false,
                HashSalt = Notus.Encryption.Toolbox.GenerateSalt(),
                EncryptKey = "key-password-string",

                SynchronousSocketIsActive = false,
                Layer = Notus.Variable.Enum.NetworkLayer.Layer1,
                Network = Notus.Variable.Enum.NetworkType.MainNet,
                NodeType = Notus.Variable.Enum.NetworkNodeType.Suitable,

                PrettyJson = true,
                GenesisAssigned = false,

                WaitTickCount = 4,

                DevelopmentNode = false,
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
            public static Notus.TGZArchiver archiver { get; set; }

            static Functions()
            {
                archiver = new Notus.TGZArchiver(Settings);
            }
        }
    }
}
