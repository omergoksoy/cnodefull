﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
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
            }
            return null;
        }

        private (NVE.BlockIntegrityStatus, NVClass.BlockData?) ControlBlockIntegrity_FastTry()
        {

            Notus.Wallet.Fee.ClearFeeData(NVG.Settings.Network, NVG.Settings.Layer);
            NVClass.BlockData LastBlock = NVClass.Block.GetEmpty();
            string[] ZipFileList = Notus.IO.GetZipFiles(NVG.Settings);

            if (ZipFileList.Length == 0)
            {
                NP.Success(NVG.Settings, "Genesis Block Needs");
                return (NVE.BlockIntegrityStatus.GenesisNeed, null);
            }

            bool tmpGetListAgain = false;
            /*
            foreach (string fileName in ZipFileList)
            {
                int fileCountInZip = 0;
                using (ZipArchive archive = ZipFile.OpenRead(fileName))
                {
                    fileCountInZip = archive.Entries.Count;
                }
                if (fileCountInZip == 0)
                {
                    tmpGetListAgain = true;
                    Thread.Sleep(1);
                    File.Delete(fileName);
                }
            }
            if (tmpGetListAgain == true)
            {
                return (NVE.BlockIntegrityStatus.CheckAgain, null);
            }

            bool multiBlockFound = false;
            foreach (string fileName in ZipFileList)
            {
                List<string> deleteInnerFileList = new List<string>();
                using (ZipArchive archive = ZipFile.OpenRead(fileName))
                {
                    List<string> fileNameList = new List<string>();
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (fileNameList.IndexOf(entry.FullName) == -1)
                        {
                            fileNameList.Add(entry.FullName);
                        }
                        else
                        {
                            deleteInnerFileList.Add(entry.FullName);
                        }
                    }
                }
                if (deleteInnerFileList.Count > 0)
                {
                    Notus.Archive.DeleteFromInside(fileName, deleteInnerFileList, true);
                    multiBlockFound = true;
                }
            }
            if (multiBlockFound == true)
            {
                return (NVE.BlockIntegrityStatus.CheckAgain, null);
            }
            */
            ConcurrentDictionary<long, string> allBlock = new ConcurrentDictionary<long, string>();

            foreach (string fileName in ZipFileList)
            {
                using (ZipArchive archive = ZipFile.OpenRead(fileName))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) == true)
                        {
                            ZipArchiveEntry? zipEntry = archive.GetEntry(entry.FullName);
                            if (zipEntry != null)
                            {
                                using (StreamReader zipEntryStream = new StreamReader(zipEntry.Open()))
                                {
                                    try
                                    {
                                        NVClass.BlockData? ControlBlock = JsonSerializer.Deserialize<NVClass.BlockData>(zipEntryStream.ReadToEnd());
                                        if (ControlBlock != null)
                                        {
                                            if (new Notus.Block.Generate().Verify(ControlBlock))
                                            {
                                                if (allBlock.ContainsKey(ControlBlock.info.rowNo) == false)
                                                {
                                                    allBlock.TryAdd(ControlBlock.info.rowNo, ControlBlock.info.uID);
                                                }
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
            }

            NP.ReadLine();
            //StoreBlockWithRowNo(SmallestBlockHeight);
            return (NVE.BlockIntegrityStatus.Valid, LastBlock);
        }

        private (NVE.BlockIntegrityStatus, NVClass.BlockData?) ControlBlockIntegrity()
        {
            try
            {
                Notus.Wallet.Fee.ClearFeeData(NVG.Settings.Network, NVG.Settings.Layer);

            }
            catch (Exception err)
            {
                Console.WriteLine("Integrity.Cs -> Line 68");
                Console.WriteLine(err.Message);
                Console.WriteLine(err.Message);
            }

            NVClass.BlockData LastBlock = NVClass.Block.GetEmpty();
            string[] ZipFileList = Notus.IO.GetZipFiles(NVG.Settings);

            if (ZipFileList.Length == 0)
            {
                NP.Success(NVG.Settings, "Genesis Block Needs");
                //NGF.BlockOrder.Clear();
                return (NVE.BlockIntegrityStatus.GenesisNeed, null);
            }
            bool tmpGetListAgain = false;
            foreach (string fileName in ZipFileList)
            {
                int fileCountInZip = 0;
                using (ZipArchive archive = ZipFile.OpenRead(fileName))
                {
                    fileCountInZip = archive.Entries.Count;
                }
                if (fileCountInZip == 0)
                {
                    tmpGetListAgain = true;
                    Thread.Sleep(1);
                    File.Delete(fileName);
                }
            }
            if (tmpGetListAgain == true)
            {
                return (NVE.BlockIntegrityStatus.CheckAgain, null);
            }

            bool multiBlockFound = false;
            foreach (string fileName in ZipFileList)
            {
                List<string> deleteInnerFileList = new List<string>();
                using (ZipArchive archive = ZipFile.OpenRead(fileName))
                {
                    List<string> fileNameList = new List<string>();
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (fileNameList.IndexOf(entry.FullName) == -1)
                        {
                            fileNameList.Add(entry.FullName);
                        }
                        else
                        {
                            deleteInnerFileList.Add(entry.FullName);
                        }
                    }
                }
                if (deleteInnerFileList.Count > 0)
                {
                    Notus.Archive.DeleteFromInside(fileName, deleteInnerFileList, true);
                    multiBlockFound = true;
                }
            }
            if (multiBlockFound == true)
            {
                return (NVE.BlockIntegrityStatus.CheckAgain, null);
            }

            SortedDictionary<long, string> BlockOrderList = new SortedDictionary<long, string>();
            Dictionary<string, int> BlockTypeList = new Dictionary<string, int>();
            Dictionary<string, string> BlockPreviousList = new Dictionary<string, string>();
            Dictionary<string, bool> ZipArchiveList = new Dictionary<string, bool>();
            Dictionary<string, NVClass.BlockData> Control_RealBlockList = new Dictionary<string, NVClass.BlockData>();
            long BiggestBlockHeight = 0;
            long SmallestBlockHeight = long.MaxValue;

            foreach (string fileName in ZipFileList)
            {
                List<Int64> tmpUpdateBlockRowList = new List<Int64>();
                List<string> tmpDeleteFileList = new List<string>();
                bool returnForCheckAgain = false;
                using (ZipArchive archive = ZipFile.OpenRead(fileName))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase) == false)
                        {
                            tmpDeleteFileList.Add(entry.FullName);
                        }
                        else
                        {
                            ZipArchiveEntry? zipEntry = archive.GetEntry(entry.FullName);
                            if (zipEntry != null)
                            {
                                System.IO.FileInfo fif = new System.IO.FileInfo(entry.FullName);
                                using (StreamReader zipEntryStream = new StreamReader(zipEntry.Open()))
                                {
                                    try
                                    {
                                        NVClass.BlockData? ControlBlock = JsonSerializer.Deserialize<NVClass.BlockData>(zipEntryStream.ReadToEnd());
                                        if (ControlBlock != null)
                                        {
                                            Notus.Block.Generate BlockValidateObj = new Notus.Block.Generate();
                                            bool Val_BlockVerify = BlockValidateObj.Verify(ControlBlock);
                                            if (Val_BlockVerify == false)
                                            {
                                                NP.Danger(NVG.Settings, "Block Integrity = NonValid");
                                                tmpDeleteFileList.Add(entry.FullName);
                                            }
                                            else
                                            {
                                                if (BlockOrderList.ContainsKey(ControlBlock.info.rowNo))
                                                {
                                                    NP.Danger(NVG.Settings, "Block Integrity = MultipleHeight -> " + ControlBlock.info.rowNo.ToString());
                                                    tmpDeleteFileList.Add(entry.FullName);
                                                    returnForCheckAgain = true;
                                                }
                                                else
                                                {
                                                    if (BlockPreviousList.ContainsKey(ControlBlock.info.uID))
                                                    {
                                                        NP.Danger(NVG.Settings, "Block Integrity = MultipleId -> " + ControlBlock.info.uID);
                                                        tmpDeleteFileList.Add(entry.FullName);
                                                        returnForCheckAgain = true;
                                                    }
                                                    else
                                                    {
                                                        if (SmallestBlockHeight > ControlBlock.info.rowNo)
                                                        {
                                                            SmallestBlockHeight = ControlBlock.info.rowNo;
                                                        }
                                                        if (ControlBlock.info.rowNo > BiggestBlockHeight)
                                                        {
                                                            BiggestBlockHeight = ControlBlock.info.rowNo;
                                                            LastBlock = ControlBlock;
                                                        }
                                                        if (ControlBlock.info.rowNo == 1)
                                                        {
                                                            NVG.Settings.Genesis = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(
                                                                System.Convert.FromBase64String(
                                                                    ControlBlock.cipher.data
                                                                )
                                                            );
                                                            Notus.Wallet.Fee.StoreFeeData("genesis_block", JsonSerializer.Serialize(NVG.Settings.Genesis), NVG.Settings.Network, NVG.Settings.Layer, true);
                                                        }

                                                        ZipArchiveList.Add(fif.Name, Val_BlockVerify);
                                                        Control_RealBlockList.Add(ControlBlock.info.uID, ControlBlock);
                                                        BlockOrderList.Add(ControlBlock.info.rowNo, ControlBlock.info.uID);
                                                        BlockPreviousList.Add(ControlBlock.info.uID, ControlBlock.prev);
                                                        BlockTypeList.Add(ControlBlock.info.uID, ControlBlock.info.type);

                                                        NVG.Settings.BlockOrder.Add(ControlBlock.info.rowNo, ControlBlock.info.uID);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception err)
                                    {
                                        NP.Danger(NVG.Settings, "Error Text [235abc]: " + err.Message);
                                    }
                                }
                            }
                        }
                    }
                }
                if (tmpDeleteFileList.Count > 0)
                {
                    Thread.Sleep(1);
                    Notus.Archive.DeleteFromInside(
                        fileName,
                        tmpDeleteFileList,
                        true
                    );
                    NP.Danger(NVG.Settings, "Repair Block Integrity = Contains Wrong / Extra Data");
                    if (returnForCheckAgain == true)
                    {
                        return (NVE.BlockIntegrityStatus.CheckAgain, null);
                    }
                }
            }

            if (SmallestBlockHeight > 1)
            {
                NP.Danger(NVG.Settings, "Repair Block Integrity = Missing Block Available");
                bool exitInnerLoop = false;
                while (exitInnerLoop == false)
                {
                    SmallestBlockHeight--;
                    if (SmallestBlockHeight == 0)
                    {
                        exitInnerLoop = true;
                    }
                    else
                    {
                        if (
                            NVG.Settings.NodeType != NVE.NetworkNodeType.Main &&
                            NVG.Settings.NodeType != NVE.NetworkNodeType.Master
                        )
                        {
                            StoreBlockWithRowNo(SmallestBlockHeight);
                        }
                        else
                        {
                            if (BlockOrderList.ContainsKey(BiggestBlockHeight - 1))
                            {
                                Notus.Archive.DeleteFromInside(
                                    BlockOrderList[BiggestBlockHeight - 1],
                                    NVG.Settings,
                                    true
                                );
                                NP.Danger(NVG.Settings, "Repair Block Integrity = Missing Block [45abcfe713]");
                            }
                        }
                    }
                }
                return (NVE.BlockIntegrityStatus.CheckAgain, null);
            }

            long controlNumber = 1;
            bool rowNumberError = false;
            foreach (KeyValuePair<long, string> item in BlockOrderList)
            {
                if (item.Key != controlNumber)
                {
                    StoreBlockWithRowNo(controlNumber);
                    controlNumber = item.Key;
                    rowNumberError = true;
                }
                controlNumber++;
            }
            if (rowNumberError == true)
            {
                return (NVE.BlockIntegrityStatus.CheckAgain, null);
            }

            bool prevBlockRownNumberError = false;
            bool whileExit = false;
            while (whileExit == false)
            {
                string BlockIdStr = BlockOrderList[BiggestBlockHeight];
                if (BlockPreviousList[BlockIdStr].Length > 0)
                {
                    if (
                        string.Equals(
                            BlockPreviousList[BlockIdStr].Substring(0, BlockIdStr.Length),
                            BlockOrderList[BiggestBlockHeight - 1]
                        ) == false
                    )
                    {
                        Notus.Archive.DeleteFromInside(
                            BlockOrderList[BiggestBlockHeight - 1],
                            NVG.Settings,
                            true
                        );
                        prevBlockRownNumberError = true;
                        whileExit = true;
                    }
                }
                else
                {
                    if (BiggestBlockHeight == 1 && string.Equals(NVC.GenesisBlockUid, BlockIdStr))
                    {
                        whileExit = true;
                    }
                }
                if (BiggestBlockHeight > 0)
                {
                    BiggestBlockHeight--;
                }
                else
                {
                    whileExit = true;
                }
            }
            if (prevBlockRownNumberError == true)
            {
                NP.Danger(NVG.Settings, "Repair Block Integrity = Wrong Block Order");
                return (NVE.BlockIntegrityStatus.CheckAgain, null);
            }
            NP.Success(NVG.Settings, "Block Integrity Valid");

            NVG.Settings.BlockOrder.Clear();
            foreach (KeyValuePair<long, string> item in BlockOrderList)
            {
                NVG.Settings.BlockOrder.Add(item.Key, item.Value);
            }
            return (NVE.BlockIntegrityStatus.Valid, LastBlock);
        }

        private (string, string) GetBlockSign(Int64 BlockRowNo)
        {
            string tmpBlockKeyStr = string.Empty;
            string tmpBlockSignStr = string.Empty;
            bool exitInnerLoop = false;
            /*
            while (exitInnerLoop == false)
            {
                for (int a = 0; a < NVC.ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    string nodeIpAddress = NVC.ListMainNodeIp[a];
                    try
                    {
                        string MainResultStr = Notus.Communication.Request.GetSync(
                            Notus.Network.Node.MakeHttpListenerPath(
                                nodeIpAddress,
                                Notus.Network.Node.GetNetworkPort(NVG.Settings.Network, NVG.Settings.Layer)
                            ) + "block/hash/" + BlockRowNo.ToString(),
                            10,
                            true,
                            true,
                            NVG.Settings
                        );
                        if (MainResultStr.Length > 90)
                        {
                            exitInnerLoop = true;
                            tmpBlockKeyStr = MainResultStr.Substring(0, 90);
                            tmpBlockSignStr = MainResultStr.Substring(90);
                        }
                        else
                        {
                            Thread.Sleep(5000);
                        }
                    }
                    catch (Exception err)
                    {
                        NP.Basic(NVG.Settings.DebugMode, "Error Text [96a3c2]: " + err.Message);
                        Thread.Sleep(5000);
                    }
                }
            }
            */
            return (tmpBlockKeyStr, tmpBlockSignStr);
        }

        //control-local-block
        private bool AddFromLocalTemp(Int64 BlockRowNo)
        {
            string[] ZipFileList = Notus.IO.GetFileList(NVG.Settings, NVC.StorageFolderName.TempBlock, "tmp");
            for (int i = 0; i < ZipFileList.Length; i++)
            {
                string textBlockData = File.ReadAllText(ZipFileList[i]);
                NVClass.BlockData? tmpBlockData = JsonSerializer.Deserialize<NVClass.BlockData>(textBlockData);
                if (tmpBlockData != null)
                {
                    if (tmpBlockData.info.rowNo == BlockRowNo)
                    {
                        using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                        {
                            BS_Storage.AddSync(tmpBlockData, true);
                        }
                        return true;
                    }
                }
                else
                {
                }
            }
            return false;
        }
        private void StoreBlockWithRowNo(Int64 BlockRowNo)
        {
            /*
            Console.WriteLine("BlockRowNo Does Not Exist : " + BlockRowNo.ToString());

            eğer diğer node'un blok yükekliği daha düşük ise, kendisinden yüksek validatör gelene kadar bekliyor
            */
            //control-local-block
            //bool localFound=AddFromLocalTemp(BlockRowNo);

            bool localFound = false;
            if (localFound == false)
            {
                bool debugPrinted = false;
                bool exitInnerLoop = false;
                while (exitInnerLoop == false)
                {
                    List<string> ListMainNodeIp = Notus.Validator.List.Get(NVG.Settings.Layer, NVG.Settings.Network);
                    for (int a = 0; a < ListMainNodeIp.Count && exitInnerLoop == false; a++)
                    {
                        string myIpAddress = (NVG.Settings.LocalNode == true ? NVG.Settings.IpInfo.Local : NVG.Settings.IpInfo.Public);
                        string nodeIpAddress = ListMainNodeIp[a];
                        if (string.Equals(myIpAddress, nodeIpAddress) == false)
                        {
                            string MainResultStr = string.Empty;
                            try
                            {
                                string nodeUrl = Notus.Network.Node.MakeHttpListenerPath(
                                        nodeIpAddress,
                                        Notus.Network.Node.GetNetworkPort(NVG.Settings)
                                    );
                                MainResultStr = Notus.Communication.Request.GetSync(
                                     nodeUrl + "block/" + BlockRowNo.ToString() + "/raw",
                                    10,
                                    true,
                                    true,
                                    NVG.Settings
                                );
                                if (MainResultStr.Length > 0)
                                {
                                    NVClass.BlockData? tmpEmptyBlock = JsonSerializer.Deserialize<NVClass.BlockData>(MainResultStr);
                                    if (tmpEmptyBlock != null)
                                    {
                                        NP.Info("Getting Block Row No [ " + nodeUrl + " ]: " + BlockRowNo.ToString());
                                        using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                                        {
                                            BS_Storage.AddSync(tmpEmptyBlock, true);
                                        }
                                        exitInnerLoop = true;
                                    }
                                }
                            }
                            catch (Exception err)
                            {
                                if (debugPrinted == false)
                                {
                                    NP.Basic("Error Text [5a6e84]: " + err.Message);
                                    NP.Basic("Income Text [5a6e84]: " + MainResultStr);
                                    debugPrinted = true;
                                }
                                else
                                {
                                    NP.WaitDot(".");
                                }
                            }
                            if (exitInnerLoop == true)
                            {
                                Thread.Sleep(2500);
                            }
                        }
                    }
                    if (exitInnerLoop == false)
                    {
                        Thread.Sleep(5000);
                    }
                }
            }
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
        public void Synchronous()
        {

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
                using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                {
                    //tgz-exception
                    NVClass.BlockData? blockData = BS_Storage.ReadBlock(NVC.GenesisBlockUid);
                    if (blockData != null)
                    {
                        if (blockData.info.type == 360)
                        {
                            myGenesisSign = blockData.sign;
                            myGenesisTime = ND.GetGenesisCreationTimeFromString(blockData);
                        }
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

                foreach (NVS.IpInfo item in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
                {
                    if (string.Equals(NVG.Settings.IpInfo.Public, item.IpAddress) == false)
                    {
                        NP.Info("Checking From -> " + item.IpAddress);
                        NVClass.BlockData? tmpInnerBlockData =
                        Notus.Toolbox.Network.GetBlockFromNode(item.IpAddress, item.Port, 1, NVG.Settings);
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
                        using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                        {
                            NP.Warning(NVG.Settings, "Current Block Were Deleted");

                            Notus.TGZArchiver.ClearBlocks();
                            Notus.Archive.ClearBlocks(NVG.Settings);
                            BS_Storage.AddSync(signBlock[tmpBiggestSign], true);
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
                                        BS_Storage.AddSync(tmpInnerBlockData, true);
                                        secondBlockAdded = true;
                                    }
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
            NVE.BlockIntegrityStatus Val_Status = NVE.BlockIntegrityStatus.CheckAgain;
            NVClass.BlockData LastBlock = new NVClass.BlockData();
            bool exitInnerLoop = false;
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

                using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
                {
                    BS_Storage.AddSync(tmpGenesisBlock);
                    if (NVG.Settings.Layer == NVE.NetworkLayer.Layer1)
                    {
                        BS_Storage.AddSync(tmpEmptyBlock);
                    }
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
