using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using NC = Notus.Ceremony;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVClass = Notus.Variable.Class;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;

namespace Notus.Block
{
    public class Integrity : IDisposable
    {
        public NVClass.BlockData? GetSatus(bool ResetBlocksIfNonValid = false)
        {
            NVE.BlockIntegrityStatus Val_Status = NVE.BlockIntegrityStatus.CheckAgain;
            NVClass.BlockData LastBlock = new NVClass.BlockData();
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                (NVE.BlockIntegrityStatus tmpStatus, NVClass.BlockData tmpLastBlock) = ControlBlockIntegrity();
                if (tmpStatus != NVE.BlockIntegrityStatus.CheckAgain)
                {
                    Val_Status = tmpStatus;
                    LastBlock = tmpLastBlock;
                    exitInnerLoop = true;
                }
            }

            if (Val_Status == NVE.BlockIntegrityStatus.Valid)
            {
                return LastBlock;
            }
            if (ResetBlocksIfNonValid == true)
            {
                NVG.BlockMeta.ClearTable(NVE.MetaDataDbTypeList.All);
                /*
                var deded=NVG.BlockMeta.ReadBlock(NVC.GenesisBlockUid);
                string[] ZipFileList = Notus.IO.GetZipFiles(NVG.Settings);
                foreach (string fileName in ZipFileList)
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception err)
                    {
                        NP.Danger(NVG.Settings, "Error Text [7abc63]: " + err.Message);
                    }
                }
                */
            }
            return null;
        }
        //genesis bloğu oluşturuldumu diye kontrol ediyor
        public void IsGenesisNeed()
        {
            var blockData = NVG.BlockMeta.ReadBlock(NVC.GenesisBlockUid);
            if (blockData == null)
            {
                NP.Success("My Wallet : " + NVG.Settings.Nodes.My.IP.Wallet);
                NC.Genesis genesisCeremony = new NC.Genesis();
                genesisCeremony.Start();
                Thread.Sleep(60000);
                genesisCeremony.Dispose();
                NP.Success("Genesis Build With Ceremony");
                Environment.Exit(0);
            }
            else
            {
                NP.Warning("Burada Diger Genesisler Kontrol Edilmeli");
                NP.Warning("Burada Diger Genesisler Kontrol Edilmeli");
            }
        }
        // burası merkezi kontrol noktası
        private (NVE.BlockIntegrityStatus, NVClass.BlockData?) ControlBlockIntegrity()
        {
            (NVE.BlockIntegrityStatus tmpStatus, NVClass.BlockData? tmpLastBlock) = ControlBlockIntegrity_FastCheck();

            if (tmpStatus == NVE.BlockIntegrityStatus.Valid)
            {
                NP.Info("All Blocks Checked With Quick Method");
                return (tmpStatus, tmpLastBlock);
            }

            if (tmpStatus == NVE.BlockIntegrityStatus.GenesisNeed)
            {
                NP.Info("All Blocks Checked With Quick Method");
                return (tmpStatus, null);
            }

            NP.Info("All Blocks Checked With Full Method");
            return (NVE.BlockIntegrityStatus.NonValid, null);
        }

        // burası hızlı kontrol yapılan bölüm, eğer hata oluşursa tam kontrol bölümüne girecek
        private (NVE.BlockIntegrityStatus, NVClass.BlockData?) ControlBlockIntegrity_FastCheck()
        {
            Notus.Wallet.Fee.ClearFeeData(NVG.Settings.Network, NVG.Settings.Layer);
            var blockData = NVG.BlockMeta.ReadBlock(NVC.GenesisBlockUid);
            if (blockData == null)
            {
                return (NVE.BlockIntegrityStatus.GenesisNeed, null);
            }
            long blockRownNo = 1;
            bool exitFromLoop = false;
            bool genesisExist = false;
            while (exitFromLoop == false)
            {
                NVClass.BlockData? ControlBlock = NVG.BlockMeta.ReadBlock(blockRownNo);
                if (ControlBlock == null)
                {
                    Console.WriteLine("Last Block No [ null ]: " + blockRownNo.ToString());
                    exitFromLoop = true;
                }
                else
                {
                    if (new Notus.Block.Generate().Verify(ControlBlock))
                    {
                        Console.WriteLine("Last Block No [ valid ]: " + blockRownNo.ToString());
                        if (blockRownNo == 1)
                        {
                            NVG.Settings.Genesis = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(
                                System.Convert.FromBase64String(
                                    ControlBlock.cipher.data
                                )
                            );

                            genesisExist = true;
                        }
                        else
                        {
                            long prevBlockNo = blockRownNo - 1;
                            NVClass.BlockData? PrevBlock = NVG.BlockMeta.ReadBlock(prevBlockNo);

                            if (string.Equals(ControlBlock.prev, PrevBlock.info.uID + PrevBlock.sign) == false)
                                return (NVE.BlockIntegrityStatus.CheckAgain, null);

                        }
                    }
                    else
                    {
                        return (NVE.BlockIntegrityStatus.CheckAgain, null);
                    }
                    blockRownNo++;
                }
            }

            if (genesisExist == false)
                return (NVE.BlockIntegrityStatus.GenesisNeed, null);

            Notus.Wallet.Fee.StoreFeeData("genesis_block", JsonSerializer.Serialize(NVG.Settings.Genesis), NVG.Settings.Network, NVG.Settings.Layer, true);
            NVG.Settings.LastBlock = NVG.BlockMeta.ReadBlock(blockRownNo-1);
            return (
                NVE.BlockIntegrityStatus.Valid,
                NVG.BlockMeta.ReadBlock(blockRownNo-1)
            );
        }

        private NVClass.BlockData GiveMeEmptyBlock(NVClass.BlockData FreeBlockStruct, string PrevStr)
        {
            FreeBlockStruct.info.type = 300;
            FreeBlockStruct.info.rowNo = 2;
            FreeBlockStruct.info.multi = false;
            FreeBlockStruct.info.uID = Notus.Block.Key.Generate(
                ND.ToDateTime(NVG.NOW.Int),
                NVG.Settings.NodeWallet.WalletKey
            );

            FreeBlockStruct.info.time = Notus.Block.Key.GetTimeFromKey(FreeBlockStruct.info.uID, true);
            FreeBlockStruct.cipher.ver = "NE";
            FreeBlockStruct.cipher.data = System.Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes(
                    JsonSerializer.Serialize(1)
                )
            );

            FreeBlockStruct.prev = PrevStr;
            FreeBlockStruct.info.prevList.Clear();
            FreeBlockStruct.info.prevList.Add(360, PrevStr);
            return new Notus.Block.Generate(NVG.Settings.NodeWallet.WalletKey).Make(FreeBlockStruct, 1000);
        }
        private NVClass.BlockData GiveMeGenesisBlock(NVClass.BlockData GenBlockStruct)
        {
            if (NVG.Settings.Layer == NVE.NetworkLayer.Layer1)
            {
                NVG.Settings.Genesis = Notus.Block.Genesis.Generate(NVG.Settings.NodeWallet.WalletKey, NVG.Settings.Network, NVG.Settings.Layer);
                //NVG.Settings.Genesis.Info.Creation
            }
            else
            {
                string tmpResult = Notus.Network.Node.FindAvailableSync(
                    "block/" + NVC.GenesisBlockUid,
                    NVG.Settings.Network,
                    NVE.NetworkLayer.Layer1,
                    NVG.Settings.DebugMode,
                    NVG.Settings
                );
                NVClass.BlockData? ControlBlock = JsonSerializer.Deserialize<NVClass.BlockData>(tmpResult);
                if (ControlBlock != null)
                {
                    NVG.Settings.Genesis = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(
                        System.Convert.FromBase64String(
                            ControlBlock.cipher.data
                        )
                    );
                }
            }

            GenBlockStruct.info.type = 360;
            GenBlockStruct.info.rowNo = 1;
            GenBlockStruct.info.multi = false;
            GenBlockStruct.info.uID = NVC.GenesisBlockUid;
            GenBlockStruct.prev = "";
            GenBlockStruct.info.prevList.Clear();
            GenBlockStruct.info.time = Notus.Block.Key.GetTimeFromKey(GenBlockStruct.info.uID, true);
            GenBlockStruct.cipher.ver = "NE";
            GenBlockStruct.cipher.data = System.Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes(
                    JsonSerializer.Serialize(NVG.Settings.Genesis)
                )
            );
            return new Notus.Block.Generate(NVG.Settings.NodeWallet.WalletKey).Make(GenBlockStruct, 1000);
        }
        public bool ControlGenesisBlock()
        {
            //string[] ZipFileList = Notus.IO.GetZipFiles(NVG.Settings);
            string ZipFileName = Notus.IO.GetFolderName(
                NVG.Settings.Network,
                NVG.Settings.Layer,
                NVC.StorageFolderName.Block
            ) +
            Notus.Block.Key.GetBlockStorageFileName(
                NVC.GenesisBlockUid,
                true
            ) + ".zip";
            string myGenesisSign = string.Empty;

            DateTime myGenesisTime = NVG.NOW.Obj.AddDays(1);
            //if (ZipFileList.Length > 0)
            if (File.Exists(ZipFileName) == true)
            {
                //tgz-exception
                NVClass.BlockData? blockData = NVG.BlockMeta.ReadBlock(NVC.GenesisBlockUid);
                if (blockData != null)
                {
                    if (blockData.info.type == 360)
                    {
                        myGenesisSign = blockData.sign;
                        myGenesisTime = ND.GetGenesisCreationTimeFromString(blockData);
                    }
                }
            }
            if (NVG.Settings.LocalNode == false)
            {
                if (myGenesisSign.Length == 0)
                {
                    return false;
                }
            }
            //there is no layer on constant
            if (Notus.Validator.List.Main.ContainsKey(NVG.Settings.Layer) == false)
            {
                return false;
            }

            //there is no Network on constant
            if (Notus.Validator.List.Main[NVG.Settings.Layer].ContainsKey(NVG.Settings.Network) == false)
            {
                return false;
            }
            if (NVG.Settings.LocalNode == false)
            {
                Dictionary<string, NVClass.BlockData> signBlock = new Dictionary<string, NVClass.BlockData>();
                signBlock.Clear();

                Dictionary<string, int> signCount = new Dictionary<string, int>();
                signCount.Clear();

                Dictionary<string, List<NVS.IpInfo>> signNode = new Dictionary<string, List<NVS.IpInfo>>();
                signNode.Clear();

                //fix-ing-control-point
                //burası asenkron bir şekilde çalıştırılabilir
                //burası asenkron bir şekilde çalıştırılabilir
                //burası asenkron bir şekilde çalıştırılabilir
                //burası asenkron bir şekilde çalıştırılabilir
                foreach (NVS.IpInfo item in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
                {
                    if (string.Equals(NVG.Settings.IpInfo.Public, item.IpAddress) == false)
                    {
                        NP.Info("Checking From -> " + item.IpAddress);
                        NVClass.BlockData? tmpInnerBlockData =
                            Notus.Toolbox.Network.GetBlockFromNode(
                                item.IpAddress,
                                item.Port,
                                1,
                                NVG.Settings
                            );

                        if (tmpInnerBlockData != null)
                        {
                            if (signCount.ContainsKey(tmpInnerBlockData.sign) == false)
                            {
                                signNode.Add(tmpInnerBlockData.sign, new List<NVS.IpInfo>() { });
                                signCount.Add(tmpInnerBlockData.sign, 0);
                                signBlock.Add(tmpInnerBlockData.sign, tmpInnerBlockData);
                            }
                            signNode[tmpInnerBlockData.sign].Add(
                                new NVS.IpInfo()
                                {
                                    IpAddress = item.IpAddress,
                                    Port = item.Port
                                }
                            );
                            signCount[tmpInnerBlockData.sign] = signCount[tmpInnerBlockData.sign] + 1;
                        }
                        else
                        {
                            NP.Danger("Error Happened While Trying To Get Genesis From Other Node");
                            ND.SleepWithoutBlocking(100);
                        }
                    }
                }
                if (signCount.Count == 0)
                {
                    return false;
                }
                int tmpBiggestCount = 0;
                string tmpBiggestSign = string.Empty;
                foreach (KeyValuePair<string, int> entry in signCount)
                {
                    if (entry.Value > tmpBiggestCount)
                    {
                        tmpBiggestCount = entry.Value;
                        tmpBiggestSign = entry.Key;
                    }
                }
                if (string.Equals(tmpBiggestSign, myGenesisSign) == false)
                {
                    DateTime otherNodeGenesisTime = ND.GetGenesisCreationTimeFromString(signBlock[tmpBiggestSign]);
                    Int64 otherNodeGenesisTimeVal = Int64.Parse(
                        otherNodeGenesisTime.ToString(NVC.DefaultDateTimeFormatText)
                    );
                    Int64 myGenesisTimeVal = Int64.Parse(
                        myGenesisTime.ToString(NVC.DefaultDateTimeFormatText)
                    );
                    if (myGenesisTimeVal > otherNodeGenesisTimeVal)
                    {
                        NP.Warning(NVG.Settings, "Current Block Were Deleted");

                        //Notus.TGZArchiver.ClearBlocks();
                        Notus.Archive.ClearBlocks(NVG.Settings);
                        NVG.BlockMeta.WriteBlock(signBlock[tmpBiggestSign]);
                        NP.Basic(NVG.Settings, "Added Block : " + signBlock[tmpBiggestSign].info.uID);
                        bool secondBlockAdded = false;
                        foreach (NVS.IpInfo? entry in signNode[tmpBiggestSign])
                        {
                            if (secondBlockAdded == false)
                            {
                                NVClass.BlockData? tmpInnerBlockData =
                                Notus.Toolbox.Network.GetBlockFromNode(entry.IpAddress, entry.Port, 2, NVG.Settings);
                                if (tmpInnerBlockData != null)
                                {
                                    NP.Basic(NVG.Settings, "Added Block : " + tmpInnerBlockData.info.uID);
                                    NVG.BlockMeta.WriteBlock(tmpInnerBlockData);
                                    secondBlockAdded = true;
                                }
                            }
                        }
                        ND.SleepWithoutBlocking(150);
                    }
                    else
                    {
                        NP.Basic(NVG.Settings, "Hold Your Genesis Block - We Are Older");
                    }
                }
            }
            return true;
        }
        public void GetLastBlock()
        {
            DateTime baslangic = DateTime.Now;

            NVE.BlockIntegrityStatus Val_Status = NVE.BlockIntegrityStatus.CheckAgain;
            NVClass.BlockData? LastBlock = new NVClass.BlockData();
            bool exitInnerLoop = false;
            baslangic = DateTime.Now;
            while (exitInnerLoop == false)
            {
                (
                    NVE.BlockIntegrityStatus tmpStatus,
                    NVClass.BlockData? tmpLastBlock
                ) = ControlBlockIntegrity();

                if (tmpStatus != NVE.BlockIntegrityStatus.CheckAgain)
                {
                    Val_Status = tmpStatus;
                    LastBlock = tmpLastBlock;
                    exitInnerLoop = true;
                }
            }

            if (Val_Status == NVE.BlockIntegrityStatus.GenesisNeed)
            {
                //group-no-exception
                /*
                burada genesisi oluşturulurken ilk oluşturan kişi
                grup numarası oluşturulacak ve gup numarası buraya yazılarak
                sürekli olarak buradaki grup numarası devam edecek
                */
                NVClass.BlockData tmpGenesisBlock = GiveMeGenesisBlock(
                    NVClass.Block.GetEmpty()
                );
                string tmpPrevStr = tmpGenesisBlock.info.uID + tmpGenesisBlock.sign;
                NVClass.BlockData tmpEmptyBlock = GiveMeEmptyBlock(
                        NVClass.Block.GetEmpty(),
                        tmpPrevStr
                    );

                NVG.BlockMeta.WriteBlock(tmpGenesisBlock);
                NVG.BlockMeta.Store(tmpGenesisBlock);
                if (NVG.Settings.Layer == NVE.NetworkLayer.Layer1)
                {
                    NVG.BlockMeta.WriteBlock(tmpEmptyBlock);
                    NVG.BlockMeta.Store(tmpEmptyBlock);
                }
                NVG.Settings.GenesisCreated = true;
                if (NVG.Settings.Layer == NVE.NetworkLayer.Layer1)
                {
                    NVG.Settings.LastBlock = tmpEmptyBlock;
                }
                else
                {
                    NVG.Settings.LastBlock = tmpGenesisBlock;
                }
            }
            else
            {
                NVG.Settings.GenesisCreated = false;
                NVG.Settings.LastBlock = LastBlock;
            }
        }
        public Integrity()
        {
        }
        ~Integrity()
        {
            Dispose();
        }
        public void Dispose()
        {
        }
    }
}
