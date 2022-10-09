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
namespace Notus.Variable
{
    static class Globals
    {
        public static List<string> AirdropAddressList { get; set; }
        public static int AirdropAddressNo { get; set; }
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
            public static ConcurrentDictionary<string, string> LockWalletList { get; set; }
            public static ConcurrentDictionary<string,byte> WalletUsageList { get; set; }
            public static ConcurrentDictionary<long,string> BlockOrder { get; set; }
            //public static Notus.Mempool BlockOrder { get; set; }
            public static Notus.Block.Storage Storage { get; set; }
            public static Notus.Wallet.Balance Balance { get; set; }
            public static Notus.TGZArchiver Archiver { get; set; }
            public static Notus.Block.Queue BlockQueue { get; set; }
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
                    if (Settings.UTCTime == null)
                    {
                        Settings.UTCTime = Notus.Time.GetNtpTime();
                    }
                    else
                    {
                        Settings.UTCTime = Notus.Time.RefreshNtpTime(Settings.UTCTime);
                    }
                    uidTime = Settings.UTCTime.Now;
                }
                return Notus.Block.Key.Generate(uidTime, seedStr);
            }
            public static string GetAirdropPrivateKey()
            {
                string tmpPrivKey=AirdropAddressList[AirdropAddressNo];
                AirdropAddressNo++;
                if (AirdropAddressNo > 19) { AirdropAddressNo = 0;  }
                //Console.WriteLine("Current AirDrop Address : " + tmpPrivKey);
                return tmpPrivKey;
            }
            public static void Start()
            {
                AirdropAddressNo = 0;
                string airDropAdressList = @"[""e866851dfe79fe4bc49b232b3e9715d3e571b52274c768faa52ed3f5f949e997"",""79f61b02c15f4ed76e64f4a0221bf15fa9b62401701ca31597addb7a48a3c58b"",""562c1571fad084441e5b77b421cba85dd68c4cbc47a05e31554b09c2dbe511a1"",""f1a02e4931bec192f786a35dde169d7aa5008de7fa5176eba60341749b388250"",""a38ccda7ff75b82fd160d70fe67eaa36d8c2b0d9fee52838379832b4c5d1671f"",""9ed549159a1fa85f417b9fe44afcb027cd851c08c9ce7e28c53c25281b0abe05"",""f308e556075c4afaa9a55176a9a97ae1c56d32a7df206d9231f97a6764f6d3ce"",""57cdba96686a7d9fc9426493a4f37a58fcb29f010dc62a9bb91a1c4668cccf8a"",""3eebf758c440ce65128e9209d90e30aeb7ee30d105d5353c97e5ea6c11841296"",""7706d1d616fdec0d882986d1ed2d5c099174b273485b9c1c2a233d9daebe0323"",""56155fca1fb01aad6ac39c6c126c7293fc21ee0201b075cefc747f79afa87de5"",""0e29880d65e868234db81f9eaf5d518ab69987aa811fae6c5d66ae6c0f864b77"",""e8964812e72233e3e8969acdf42f62706bc68c413eb4e102ab04ec434fba3af6"",""76eeacc4071de7e4353623732c4d212f201ef800e6fc84a107605dcf4d765447"",""f704bbfe6bdb3554d5bc41c65e2c3e19283ee075bcf09a7a5220eb484365ab68"",""2723adc2705cf390f2e3d05542cd1d5c97dbd48ae750922b96630d1f6357cb6f"",""82a0d3b469c2e8ed31925bfac2c2682f1ff709b8797544b696c6efaefc68841f"",""ea2fb39d0155e11860970cdbe8d2a23a0e570e43e367d0cfc759b4b7cd724592"",""e363f464754a3f357b44e3bdee0cb30585a4277cfd185b0050977a65928783c5"",""8d20bf1db830923a696445607f5cd7001e9d2d673e1b03dcc726859cfeb9eb4b""]";
                AirdropAddressList = JsonSerializer.Deserialize<List<string>>(airDropAdressList);
                WalletUsageList = new ConcurrentDictionary<string,byte>();
                LockWalletList = new ConcurrentDictionary<string,string>();
                BlockOrder = new ConcurrentDictionary<long, string>();
                Storage = new Notus.Block.Storage();
                BlockQueue = new Notus.Block.Queue();
                Archiver = new Notus.TGZArchiver();
                Balance = new Notus.Wallet.Balance();
                if (Settings.GenesisCreated == false)
                {
                    Balance.Start();
                }

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
