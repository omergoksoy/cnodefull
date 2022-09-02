using Notus.Variable.Genesis;
using System;
using System.Text.Json;

namespace Notus.Block
{
    public class Genesis
    {
        private const string SelectedCurveName = Notus.Variable.Constant.Default_EccCurveName;
        private const bool Val_DefaultEncryptKeyPair = false;
        private const Notus.Variable.Enum.NetworkType Val_DefaultNetworkType = Notus.Variable.Enum.NetworkType.MainNet;
        private const Notus.Variable.Enum.NetworkLayer Val_DefaultNetworkLayer = Notus.Variable.Enum.NetworkLayer.Layer1;

        public static GenesisBlockData Generate(string CreatorWalletKey, Notus.Variable.Enum.NetworkType NetworkType, Notus.Variable.Enum.NetworkLayer NetworkLayer)
        {
            return GetGenesis_SubFunction(CreatorWalletKey, Val_DefaultEncryptKeyPair, NetworkType, NetworkLayer);
        }
        public static GenesisBlockData GetGenesis(string CreatorWalletKey, bool EncryptKeyPair)
        {
            return GetGenesis_SubFunction(CreatorWalletKey, EncryptKeyPair, Val_DefaultNetworkType, Val_DefaultNetworkLayer);
        }
        public static GenesisBlockData GetGenesis(string CreatorWalletKey, Notus.Variable.Enum.NetworkType NetworkType, Notus.Variable.Enum.NetworkLayer NetworkLayer)
        {
            return GetGenesis_SubFunction(CreatorWalletKey, Val_DefaultEncryptKeyPair, NetworkType, NetworkLayer);
        }

        public static GenesisBlockData GetGenesis_SubFunction(string CreatorWalletKey, bool EncryptKeyPair, Notus.Variable.Enum.NetworkType NetworkType, Notus.Variable.Enum.NetworkLayer NetworkLayer)
        {
            DateTime generationTime = Notus.Time.GetFromNtpServer();
            string EncKey = generationTime.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText);
            Notus.Variable.Struct.EccKeyPair KeyPair_PreSeed = Notus.Wallet.ID.GenerateKeyPair(SelectedCurveName, NetworkType);
            Notus.Variable.Struct.EccKeyPair KeyPair_Private = Notus.Wallet.ID.GenerateKeyPair(SelectedCurveName, NetworkType);
            Notus.Variable.Struct.EccKeyPair KeyPair_Public = Notus.Wallet.ID.GenerateKeyPair(SelectedCurveName, NetworkType);



            using (Notus.Mempool ObjMp_Genesis =
                new Notus.Mempool(
                    Notus.IO.GetFolderName(NetworkType, NetworkLayer, Notus.Variable.Constant.StorageFolderName.Common) + "genesis_accounts"
                )
            )
            {
                ObjMp_Genesis.AsyncActive = false;
                ObjMp_Genesis.Clear();
                if (EncryptKeyPair == true)
                {
                    using (Notus.Encryption.Cipher Obj_Cipher = new Notus.Encryption.Cipher())
                    {
                        ObjMp_Genesis.Set("seed_key",
                            Obj_Cipher.Encrypt(
                                JsonSerializer.Serialize(KeyPair_PreSeed), "", EncKey, EncKey
                            ),
                            true
                        );
                    }

                    using (Notus.Encryption.Cipher Obj_Cipher = new Notus.Encryption.Cipher())
                    {
                        ObjMp_Genesis.Set("private_key",
                            Obj_Cipher.Encrypt(
                                JsonSerializer.Serialize(KeyPair_Private), "", EncKey, EncKey
                            ),
                            true
                        );
                    }

                    using (Notus.Encryption.Cipher Obj_Cipher = new Notus.Encryption.Cipher())
                    {
                        ObjMp_Genesis.Set("public_key",
                            Obj_Cipher.Encrypt(
                                JsonSerializer.Serialize(KeyPair_Public), "", EncKey, EncKey
                            ),
                            true
                        );
                    }
                }
                else
                {
                    ObjMp_Genesis.Set("seed_key", JsonSerializer.Serialize(KeyPair_PreSeed), true);
                    ObjMp_Genesis.Set("private_key", JsonSerializer.Serialize(KeyPair_Private), true);
                    ObjMp_Genesis.Set("public_key", JsonSerializer.Serialize(KeyPair_Public), true);
                }
            }

            return new GenesisBlockData()
            {
                Version = 10000,
                Empty = new EmptyBlockType()
                {
                    TotalSupply = 550000000,
                    LuckyReward = 50,
                    Reward =2,
                    Active = true,
                    Interval = new IntervalType()
                    {
                        Time = 90,
                        Block = 10
                    },
                    SlowBlock = new SlowBlockType() /* peşpeşe empty block sonrası yavaşlatma süresi */
                    {
                        Active = true,
                        Count = 10,             // kaç adet empty blok sayılacak
                        Multiply = 10           // eğer sayı "Count" sayısı kadar empty blok mevcut ise, "Time" değişkeni kaç ile çarpılacak
                    },
                    Nonce = new EmptyBlockNonceType()
                    {
                        Type = 1,           // 1- kayar hesaplama, 2- atlamalı hesaplama
                        Method = 10,        // kullanılacak hash methodu
                        Difficulty = 1      // zorluk değeri
                    }
                },
                Reserve = new CoinReserveType()
                {
                    // Total + Digit * "0" , Decimal * "0"
                    // 100.000.000 , 000 000 000
                    // 100000000000000000

                    // 100 milyon
                    Value = 100000000000000000,     // Exact coin reserve
                    Total = 0,                      // Coin Reserve starts with this number
                    Digit = 0,                      // Add zero end of the "Total" number
                    Decimal = 9                     // Decimal zero count
                    /*
                    Value = 0,      // Exact coin reserve
                    Total = 1,      // Coin Reserve starts with this number
                    Digit = 8,      // Add zero end of the "Total" number
                    Decimal = 9     // Decimal zero count
                    */
                },
                CoinInfo = new CoinInformationType()
                {
                    Tag = "NOTUS",
                    Name = "Notus Coin",
                    Logo = new Notus.Variable.Struct.FileStorageStruct()
                    {
                        Used = true,
                        Base64 = "PHN2ZyBpZD0iTGF5ZXJfMSIgZGF0YS1uYW1lPSJMYXllciAxIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAxOTguNDMgMTk4LjQzIj48ZGVmcz48c3R5bGU+LmNscy0xe2ZpbGw6IzYwZjt9LmNscy0ye2ZpbGw6I2ZmZjt9PC9zdHlsZT48L2RlZnM+PHRpdGxlPm5vdHVzX3N5Ym1vbF9ibHVlIGNvcHk8L3RpdGxlPjxyZWN0IGNsYXNzPSJjbHMtMSIgd2lkdGg9IjE5OC40MyIgaGVpZ2h0PSIxOTguNDMiIHJ4PSI5OS4yMSIvPjxwYXRoIGNsYXNzPSJjbHMtMiIgZD0iTTEyNC4yOCw1OS4yNWguMDZhMjUuMTUsMjUuMTUsMCwwLDEsMjUuMDgsMjUuMDlsLjExLDU2Ljk1TDk5LjEyLDEwMC4ybDAtMTUuODhhMjUuMTMsMjUuMTMsMCwwLDEsMjUuMTMtMjUuMDdtLTc1LjM5LDAsNTAuMjMsNDEtLjA4LDM5LTUwLjE1LjEydi04MG03NS4zOS05LjY0QTM0Ljg0LDM0Ljg0LDAsMCwwLDg5Ljc2LDgwLjEyTDU1LDUxLjc4LDM5LjI1LDU5LjI1djgwbDkuNjcsOS41M0g5OS4wN2w5LjYyLTkuNjMsMC0xOC43MSwzNC43MSwyOC4yOSwxNS43NC03LjQ5LS4xMi01N2EzNC44NCwzNC44NCwwLDAsMC0zNC42OS0zNC43MVoiLz48L3N2Zz4=",
                        Source = "",
                        Url = ""
                    },
                },
                Supply = new CoinSupplyType()
                {
                    Decrease = 3,
                    Type = 1,
                    Modular = 4000
                },
                Fee = new FeeType()
                {
                    Data = 1500,
                    Token = new TokenPriceStructType()
                    {
                        Generate = 500000,
                        Update = 900000
                    },
                    Transfer = new CoinTransferFeeType()
                    {
                        Fast = 400,         // öncelik verilen işlem
                        Common = 150,       // standart transfer işlemi
                        NoName = 1000,      // önce merkezi bir hesaba ardından kişiye gönderilen 
                        ByPieces = 4000     // önce merkezi bir hesaba ardından paralı halde kişiye gönderilen 
                    },
                    BlockAccount = 1500     // standart işlem ücretinin 10 katı ile hesap bloke edilebilir.
                },
                Info = new GenesisInfoType()
                {
                    Creation = generationTime,
                    Creator = CreatorWalletKey,
                    CurveName = SelectedCurveName,
                    EncryptKeyPair = Val_DefaultEncryptKeyPair
                },
                Premining = new PreminingType()
                {
                    PreSeed = new SaleOptionGroupType()
                    {
                        Volume = 1000000,        //1 milyon
                        DecimalContains = false,
                        HowManyMonthsLater = 24,
                        PercentPerMonth = 5,
                        Wallet = KeyPair_PreSeed.WalletKey,
                        PublicKey = KeyPair_PreSeed.PublicKey
                    },
                    Private = new SaleOptionGroupType()
                    {
                        Volume = 2000000,        //2 milyon
                        DecimalContains = false,
                        HowManyMonthsLater = 18,
                        PercentPerMonth = 5,
                        Wallet = KeyPair_Private.WalletKey,
                        PublicKey = KeyPair_Private.PublicKey
                    },
                    Public = new SaleOptionGroupType()
                    {
                        Volume = 10000000,        //10 milyon
                        DecimalContains = false,
                        HowManyMonthsLater = 12,
                        PercentPerMonth = 5,
                        Wallet = KeyPair_Public.WalletKey,
                        PublicKey = KeyPair_Public.PublicKey
                    }
                }
            };
        }
    }
}
