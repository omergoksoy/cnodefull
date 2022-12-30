using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Text.Json;
using NGF = Notus.Variable.Globals.Functions;
using NH = Notus.Hash;
using NP = Notus.Print;
using NTN = Notus.Toolbox.Network;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVS = Notus.Variable.Struct;

namespace Notus.Ceremony
{
    public class Genesis : IDisposable
    {
        private int DefaultBlockGenerateInterval = 3000;

        public int PreviousId
        {
            get { return DefaultBlockGenerateInterval; }
        }
        public void DistributeTheNext(string walletId, string genesisText)
        {
            bool genesisSended = false;
            while (genesisSended == false)
            {
                foreach (var validatorItem in NVG.NodeList)
                {
                    if (string.Equals(walletId, validatorItem.Value.IP.Wallet))
                    {
                        genesisSended = NVG.Settings.PeerManager.Send(
                            validatorItem.Key,
                            "<genesis>" + genesisText + "</genesis>",
                            false
                        );
                    }
                }
            }
        }
        public Notus.Variable.Genesis.GenesisBlockData? Generate()
        {
            int myOrderNo = 1;
            Notus.Variable.Genesis.GenesisBlockData? newGenesisWithCeremony = Notus.Block.Genesis.Generate(
                //NVG.Settings.NodeWallet.WalletKey, 
                NVG.Settings.Nodes.My.IP.Wallet,
                NVG.Settings.Network,
                NVG.Settings.Layer
            );

            //burada birinci sıradaki validatörün imzası eklenece
            newGenesisWithCeremony.Ceremony.Clear();
            for (int i = 1; i < 7; i++)
            {
                newGenesisWithCeremony.Ceremony.Add(i, new Variable.Genesis.GenesisCeremonyOrderType()
                {
                    PublicKey = "",
                    Sign = ""
                });
            }
            newGenesisWithCeremony.Ceremony[myOrderNo].PublicKey = NVG.Settings.Nodes.My.PublicKey;
            newGenesisWithCeremony.Ceremony[myOrderNo].Sign = "";

            string rawGenesisDataStr = Notus.Block.Genesis.CalculateRaw(newGenesisWithCeremony, myOrderNo);
            newGenesisWithCeremony.Ceremony[myOrderNo].Sign = Notus.Wallet.ID.Sign(rawGenesisDataStr, NVG.Settings.Nodes.My.PrivateKey);

            if (Notus.Block.Genesis.Verify(newGenesisWithCeremony, myOrderNo) == false)
            {
                return null;
            }
            return newGenesisWithCeremony;
        }
        public (int, string) MakeMembersOrders()
        {
            string nextWalletId = string.Empty;
            SortedDictionary<BigInteger, string> resultList = new SortedDictionary<BigInteger, string>();
            foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
            {
                if (entry.Value.Status == NVS.NodeStatus.Online)
                {
                    bool exitInnerWhileLoop = false;
                    int innerCount = 1;
                    while (exitInnerWhileLoop == false)
                    {
                        BigInteger intWalletNo = BigInteger.Parse(
                            "0" +
                            new NH().CommonHash("sha1",
                                entry.Value.IP.Wallet +
                                NVC.CommonDelimeterChar +
                                entry.Value.Begin.ToString() +
                                NVC.CommonDelimeterChar +
                                "executing_genesis_ceremony" +
                                NVC.CommonDelimeterChar +
                                innerCount.ToString()
                            ),
                            NumberStyles.AllowHexSpecifier
                        );
                        if (resultList.ContainsKey(intWalletNo) == false)
                        {
                            resultList.Add(intWalletNo, entry.Value.IP.Wallet);
                            exitInnerWhileLoop = true;
                        }
                        else
                        {
                            innerCount++;
                        }
                    }
                }
            }

            int myOrderNo = 0;
            foreach (var item in resultList)
            {
                Console.WriteLine(item.Key.ToString() + " - " + item.Value);
            }
            for (int i = 0; i < resultList.Count; i++)
            {
                string? currentWalletId = resultList.Values.ElementAt(i);
                if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, currentWalletId))
                {
                    myOrderNo = i + 1;
                }
            }
            if (6 > myOrderNo && resultList.Count > myOrderNo)
            {
                nextWalletId = resultList.Values.ElementAt(myOrderNo);
            }
            return (myOrderNo, nextWalletId);
        }
        public void WaitForOtherNodeInfoDetails()
        {
            bool waitAllNodeInfoArrived = false;
            DateTime waitTimeDiff = DateTime.Now;
            while (waitAllNodeInfoArrived == false)
            {
                bool weWaitResponseFromNode = false;
                foreach (var validatorItem in NVG.NodeList)
                {
                    if (validatorItem.Value.Begin == 0)
                    {
                        weWaitResponseFromNode = true;
                        if ((DateTime.Now - waitTimeDiff).TotalSeconds > 5)
                        {
                            NVG.Settings.PeerManager.Send(validatorItem.Key, "<sNode>" + NVG.Settings.Nodes.My.IP.Wallet + "</sNode>", false);
                            waitTimeDiff = DateTime.Now;
                        }
                    }
                }
                if (weWaitResponseFromNode == false)
                {
                    waitAllNodeInfoArrived = true;
                }
                else
                {
                    Thread.Sleep(50);
                }
            }
        }
        public void SendNodeInfoToToMembers()
        {
            string msgText = "<node>" + JsonSerializer.Serialize(NVG.NodeList[NVG.Settings.Nodes.My.HexKey]) + "</node>";
            foreach (var validatorItem in NGF.ValidatorList)
            {
                if (string.Equals(NVG.Settings.Nodes.My.HexKey, validatorItem.Key) == false)
                {
                    bool sendedToValidator = false;
                    while (sendedToValidator == false)
                    {
                        if (NVG.Settings.PeerManager.Send(validatorItem.Key, msgText, false) == false)
                        {
                            //NP.Danger(validatorItem.Value.IpAddress + " -> " + validatorItem.Value.Port.ToString() + " - Error");
                            NGF.ValidatorList[validatorItem.Key].Status = NVS.NodeStatus.Offline;
                            Thread.Sleep(100);
                        }
                        else
                        {
                            //NP.Info(validatorItem.Value.IpAddress + " -> " + validatorItem.Value.Port.ToString() + " - Sended");
                            NGF.ValidatorList[validatorItem.Key].Status = NVS.NodeStatus.Online;
                            sendedToValidator = true;
                        }
                    }
                }
            }
        }
        public void StartNodeSync()
        {
            NVH.PrepareValidatorList(true);
            bool definedValidator = false;
            foreach (Variable.Struct.IpInfo item in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
            {
                if (string.Equals(NVG.Settings.Nodes.My.IP.IpAddress, item.IpAddress) == true)
                {
                    definedValidator = true;
                }
            }
            if (definedValidator == false)
            {
                NP.Danger("Diger nodelardan tanımlanmış Validatorlerden tarafından olusturulmus Genesis blogunu iste");
                NP.Danger("Diger nodelardan tanımlanmış Validatorlerden tarafından olusturulmus Genesis blogunu iste");
                NP.Danger("Diger nodelardan tanımlanmış Validatorlerden tarafından olusturulmus Genesis blogunu iste");
                NP.Danger("Genesis Ceremony Works With Only Defined Validators");
                Environment.Exit(0);
            }

            //NP.Basic(JsonSerializer.Serialize(NGF.ValidatorList));
            //ValidatorQueueObj.PreStart();
            bool exitFromWhileLoop = false;
            while (exitFromWhileLoop == false)
            {
                NVG.Settings.PeerManager.RemoveAll();
                foreach (var validatorItem in NGF.ValidatorList)
                {
                    if (string.Equals(NVG.Settings.Nodes.My.HexKey, validatorItem.Key) == false)
                    {
                        NVG.Settings.PeerManager.AddPeer(
                            validatorItem.Key,
                            validatorItem.Value.IpAddress
                        );
                    }
                }
                foreach (var validatorItem in NGF.ValidatorList)
                {
                    if (string.Equals(NVG.Settings.Nodes.My.HexKey, validatorItem.Key) == false)
                    {
                        if (NVG.Settings.PeerManager.Send(validatorItem.Key, "<ping>1</ping>", false) == false)
                        {
                            NGF.ValidatorList[validatorItem.Key].Status = NVS.NodeStatus.Offline;
                        }
                        else
                        {
                            NGF.ValidatorList[validatorItem.Key].Status = NVS.NodeStatus.Online;
                        }
                    }
                }
                bool allValidatorIsOnline = true;
                foreach (var validatorItem in NGF.ValidatorList)
                {
                    if (validatorItem.Value.Status == NVS.NodeStatus.Offline)
                    {
                        allValidatorIsOnline = false;
                    }
                }
                if (allValidatorIsOnline == true)
                {
                    exitFromWhileLoop = true;
                }
                else
                {
                    Thread.Sleep(350);
                }
            }
        }
        public void Dispose()
        {

        }
        ~Genesis()
        {
            Dispose();
        }
    }
}
