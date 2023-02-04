using NVD = Notus.Validator.Date;
using ND = Notus.Date;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text.Json;
using NCR = Notus.Communication.Request;
using NGF = Notus.Variable.Globals.Functions;
using NH = Notus.Hash;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVClass = Notus.Variable.Class;
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVS = Notus.Variable.Struct;
using NNN = Notus.Network.Node;
using NTT = Notus.Toolbox.Text;
using NVE = Notus.Variable.Enum;
namespace Notus.Ceremony
{
    public class Genesis : IDisposable
    {
        private Dictionary<int, string> ValidatorQueue = new();
        private SortedDictionary<BigInteger, string> ValidatorOrder = new();
        private int CeremonyMemberCount = 2;
        private bool Signed = false;
        private NVClass.BlockData genesisBlock = new();
        private NVClass.BlockData airdropBlock = new();
        private NVClass.BlockData emptyBlock1 = new();
        private NVClass.BlockData emptyBlock2 = new();
        private NVClass.BlockData emptyBlock3 = new();
        private NVClass.BlockData emptyBlock4 = new();

        private string BlockSignHash = string.Empty;
        private Notus.Variable.Genesis.GenesisBlockData GenesisObj = new();
        private int MyOrderNo = 0;
        private Notus.Communication.Http HttpObj = new Notus.Communication.Http(true);
        public void Start()
        {
            //kontrollü bir şekilde dosyayı silerek sıfırlıyor
            Notus.Data.Helper.ClearTable(NVC.MemoryPoolName["ValidatorList"]);
            bool predefinedValidator = false;
            foreach (Variable.Struct.IpInfo item in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
            {
                if (string.Equals(NVG.Settings.Nodes.My.IP.IpAddress, item.IpAddress) == true)
                {
                    predefinedValidator = true;
                }
            }
            if (predefinedValidator == false)
            {
                NP.Danger("Diger nodelardan tanımlanmış Validatorlerden tarafından olusturulmus Genesis blogunu iste");
                NP.Danger("Genesis Ceremony Works With Only Defined Validators");
                Environment.Exit(0);
            }

            NVH.DefineMyNodeInfo();
            StartGenesisConnection();
            ControlOtherValidatorStatus();
            MakeMembersOrders();

            NP.Success("Ceremony Order No : " + MyOrderNo.ToString() + " / " + NVG.NodeList.Count.ToString());

            if (MyOrderNo == 1)
            {
                Generate();
            }
            WaitPrevSigner();
            GetAllSignedGenesisFromValidator();

            RealGeneration();
            NP.Info("My Block Sign : " + BlockSignHash.Substring(0, 10) + "..." + BlockSignHash.Substring(BlockSignHash.Length - 10));

            ControlAllBlockSign();
            NVG.BlockMeta.Validator(
                ND.ToLong(genesisBlock.info.time), 
                genesisBlock.validator.count.First().Key
            );

            Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine(JsonSerializer.Serialize(ValidatorQueue));
            Console.WriteLine("-----------------------------------------------------");
            Environment.Exit(0);
            // omergoksoy();

            NVG.BlockMeta.WriteBlock(genesisBlock, "Genesis -> Line -> 66");
            NVG.BlockMeta.WriteBlock(airdropBlock, "Genesis -> Line -> 80");
            NVG.BlockMeta.WriteBlock(emptyBlock1, "Genesis -> Line -> 81");
            NVG.BlockMeta.WriteBlock(emptyBlock2, "Genesis -> Line -> 82");
            NVG.BlockMeta.WriteBlock(emptyBlock3, "Genesis -> Line -> 83");
            NVG.BlockMeta.WriteBlock(emptyBlock4, "Genesis -> Line -> 84");
            /*
            */
        }
        private void ControlAllBlockSign()
        {
            int SelectedPortVal = NVG.Settings.Nodes.My.IP.Port + 5;
            foreach (var validatorItem in NVG.NodeList)
            {
                if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, validatorItem.Value.IP.Wallet) == false)
                {
                    bool exitFromWhileLoop = false;
                    while (exitFromWhileLoop == false)
                    {
                        if (BlockSignHash.Length > 0)
                        {
                            string requestUrl = NNN.MakeHttpListenerPath(
                                    validatorItem.Value.IP.IpAddress, SelectedPortVal
                                ) + "sign";
                            string MainResultStr = NCR.GetSync(requestUrl, 2, true, false);
                            if (MainResultStr.Length > 10)
                            {
                                if (string.Equals(MainResultStr, BlockSignHash) == false)
                                {
                                    NP.Danger("Different Sign Exists");
                                    Environment.Exit(0);
                                }
                                exitFromWhileLoop = true;
                            }
                            else
                            {
                                Thread.Sleep(500);
                            }
                        }
                    }
                }
            }
            NP.Success("All Sign Are Equals");
        }
        private void RealGeneration()
        {
            int tmpOrderNo = 1;
            while (ValidatorQueue.Count < 6)
            {
                foreach (var item in ValidatorOrder)
                {
                    if(ValidatorQueue.Count < 6)
                    {
                        ValidatorQueue.Add(tmpOrderNo,item.Value);
                        //Console.WriteLine("item.Value : " + item.Value);
                        tmpOrderNo++;
                    }
                }
            }
            genesisBlock = NVClass.Block.GetEmpty();

            genesisBlock.info.type = NVE.BlockTypeList.GenesisBlock;
            genesisBlock.info.rowNo = 1;
            genesisBlock.info.multi = false;
            genesisBlock.info.uID = NVC.GenesisBlockUid;
            genesisBlock.prev = "";
            genesisBlock.info.prevList.Clear();
            genesisBlock.info.time = Notus.Block.Key.GetTimeFromKey(genesisBlock.info.uID, true);
            genesisBlock.cipher.ver = "NE";
            genesisBlock.cipher.data = System.Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes(
                    JsonSerializer.Serialize(GenesisObj)
                )
            );
            genesisBlock = new Notus.Block.Generate(ValidatorQueue[1]).Make(genesisBlock, 1000);
            BlockSignHash = genesisBlock.sign;



            airdropBlock = NVClass.Block.GetEmpty();
            airdropBlock.info.prevList.Clear();
            airdropBlock.info.type = NVE.BlockTypeList.SmartContract;
            airdropBlock.info.rowNo = 2;
            airdropBlock.info.multi = false;
            airdropBlock.info.uID = NVC.AirdropBlockUid;
            airdropBlock.prev = genesisBlock.info.uID + genesisBlock.sign;
            airdropBlock.info.prevList.Add( NVE.BlockTypeList.GenesisBlock, genesisBlock.info.uID + genesisBlock.sign);
            airdropBlock.info.time = Notus.Block.Key.GetTimeFromKey(airdropBlock.info.uID, true);

            //kontrat ile etkileşime girmek için senaryo düşün
            //burada airdrop kontratı olacak ve o kontrat ile etkileşime girilecek
            string airDropContractCode = 
                "IF current_network='main' THEN " + NVC.NewLine +
                    "PRINT 'AIRDROP NOT AVAILABLE FOR MAIN NETWORK' " + NVC.NewLine +
                    "KILL " + NVC.NewLine +
                "ENDIF " + NVC.NewLine +
                    
                "CONST AIRDROP_VOLUME = '2000000'  " + NVC.NewLine +
                "AIRDROP msg_sender " + NVC.NewLine +
                "KILL ";

            airdropBlock.cipher.ver = "NE";
            airdropBlock.cipher.data = System.Convert.ToBase64String(
                System.Text.Encoding.ASCII.GetBytes(airDropContractCode.Replace("'", "\""))
            );

            airdropBlock = new Notus.Block.Generate(ValidatorQueue[2]).Make(airdropBlock, 1000);

            ulong creationTimeAsLong = ND.ToLong(GenesisObj.Info.Creation);
            DateTime generationTime = ND.ToDateTime(creationTimeAsLong - (creationTimeAsLong % NVD.Calculate()));
            
            ulong emptBlockTime1 = ND.AddMiliseconds(ND.ToLong(generationTime), NVD.Calculate(2));
            ulong emptBlockTime2 = ND.AddMiliseconds(ND.ToLong(generationTime), NVD.Calculate(3));
            ulong emptBlockTime3 = ND.AddMiliseconds(ND.ToLong(generationTime), NVD.Calculate(4));
            ulong emptBlockTime4 = ND.AddMiliseconds(ND.ToLong(generationTime), NVD.Calculate(5));
            Console.WriteLine(creationTimeAsLong);
            Console.WriteLine(creationTimeAsLong);
            Console.WriteLine(ND.ToLong(generationTime));
            Console.WriteLine(ND.ToLong(generationTime));
            Console.WriteLine(emptBlockTime1);
            Console.WriteLine(emptBlockTime2);
            Console.WriteLine(emptBlockTime3);
            Console.WriteLine(emptBlockTime4);
            Environment.Exit(0);


            // 1. empty blok
            emptyBlock1 = NVClass.Block.GetEmpty();
            emptyBlock1.info.prevList.Clear();
            emptyBlock1.info.type = NVE.BlockTypeList.EmptyBlock;
            emptyBlock1.info.rowNo = 3;
            emptyBlock1.info.multi = false;
            emptyBlock1.info.uID = NVC.AirdropBlockUid;
            emptyBlock1.prev = airdropBlock.info.uID + airdropBlock.sign;
            emptyBlock1.info.prevList.Add(NVE.BlockTypeList.GenesisBlock, genesisBlock.info.uID + genesisBlock.sign);
            emptyBlock1.info.prevList.Add(NVE.BlockTypeList.SmartContract, airdropBlock.info.uID + airdropBlock.sign);

            emptyBlock1.info.time = Notus.Block.Key.GetTimeFromKey(emptyBlock1.info.uID, true);
            emptyBlock1.cipher.ver = "NE";
            emptyBlock1.cipher.data = NTT.NumberToBase64(1);
            emptyBlock1 = new Notus.Block.Generate(ValidatorQueue[3]).Make(emptyBlock1, 1000);



            // 2. empty blok
            emptyBlock2 = NVClass.Block.GetEmpty();
            emptyBlock2.info.prevList.Clear();
            emptyBlock2.info.type = NVE.BlockTypeList.EmptyBlock;
            emptyBlock2.info.rowNo = 4;
            emptyBlock2.info.multi = false;
            emptyBlock2.info.uID = NVC.AirdropBlockUid;
            emptyBlock2.prev = emptyBlock1.info.uID + emptyBlock1.sign;
            emptyBlock2.info.prevList.Add(NVE.BlockTypeList.GenesisBlock, genesisBlock.info.uID + genesisBlock.sign);
            emptyBlock2.info.prevList.Add(NVE.BlockTypeList.SmartContract, airdropBlock.info.uID + airdropBlock.sign);
            emptyBlock2.info.prevList.Add(NVE.BlockTypeList.EmptyBlock, emptyBlock1.info.uID + emptyBlock1.sign);

            emptyBlock2.info.time = Notus.Block.Key.GetTimeFromKey(emptyBlock2.info.uID, true);
            emptyBlock2.cipher.ver = "NE";
            emptyBlock2.cipher.data = NTT.NumberToBase64(1);
            emptyBlock2 = new Notus.Block.Generate(ValidatorQueue[4]).Make(emptyBlock2, 1000);


            // 3. empty blok
            emptyBlock3 = NVClass.Block.GetEmpty();
            emptyBlock3.info.prevList.Clear();
            emptyBlock3.info.type = NVE.BlockTypeList.EmptyBlock;
            emptyBlock3.info.rowNo = 5;
            emptyBlock3.info.multi = false;
            emptyBlock3.info.uID = NVC.AirdropBlockUid;
            emptyBlock3.prev = emptyBlock2.info.uID + emptyBlock2.sign;
            emptyBlock3.info.prevList.Add(NVE.BlockTypeList.GenesisBlock, genesisBlock.info.uID + genesisBlock.sign);
            emptyBlock3.info.prevList.Add(NVE.BlockTypeList.SmartContract, airdropBlock.info.uID + airdropBlock.sign);
            emptyBlock3.info.prevList.Add(NVE.BlockTypeList.EmptyBlock, emptyBlock2.info.uID + emptyBlock2.sign);

            emptyBlock3.info.time = Notus.Block.Key.GetTimeFromKey(emptyBlock3.info.uID, true);
            emptyBlock3.cipher.ver = "NE";
            emptyBlock3.cipher.data = NTT.NumberToBase64(1);
            emptyBlock3 = new Notus.Block.Generate(ValidatorQueue[5]).Make(emptyBlock3, 1000);

            // 4. empty blok
            emptyBlock4 = NVClass.Block.GetEmpty();
            emptyBlock4.info.prevList.Clear();
            emptyBlock4.info.type = NVE.BlockTypeList.EmptyBlock;
            emptyBlock4.info.rowNo = 3;
            emptyBlock4.info.multi = false;
            emptyBlock4.info.uID = NVC.AirdropBlockUid;
            emptyBlock4.prev = emptyBlock3.info.uID + emptyBlock3.sign;
            emptyBlock4.info.prevList.Add(NVE.BlockTypeList.GenesisBlock, genesisBlock.info.uID + genesisBlock.sign);
            emptyBlock4.info.prevList.Add(NVE.BlockTypeList.SmartContract, airdropBlock.info.uID + airdropBlock.sign);
            emptyBlock4.info.prevList.Add(NVE.BlockTypeList.EmptyBlock, emptyBlock3.info.uID + emptyBlock3.sign);

            emptyBlock4.info.time = Notus.Block.Key.GetTimeFromKey(emptyBlock4.info.uID, true);
            emptyBlock4.cipher.ver = "NE";
            emptyBlock4.cipher.data = NTT.NumberToBase64(1);
            emptyBlock4 = new Notus.Block.Generate(ValidatorQueue[6]).Make(emptyBlock4, 1000);

            BlockSignHash = emptyBlock4.sign;


        }
        private void GetAllSignedGenesisFromValidator()
        {
            if (MyOrderNo == CeremonyMemberCount)
            {
                return;
            }
            int SelectedPortVal = NVG.Settings.Nodes.My.IP.Port + 5;
            string waitingWalletId = ValidatorOrder.Values.ElementAt(CeremonyMemberCount - 1);
            foreach (var validatorItem in NVG.NodeList)
            {
                if (string.Equals(waitingWalletId, validatorItem.Value.IP.Wallet))
                {
                    bool exitFromWhileLoop = false;
                    while (exitFromWhileLoop == false)
                    {
                        string requestUrl = NNN.MakeHttpListenerPath(
                                validatorItem.Value.IP.IpAddress, SelectedPortVal
                            ) + "finalization";
                        string MainResultStr = NCR.GetSync(requestUrl, 2, true, false);
                        if (MainResultStr.Length > 20)
                        {
                            Notus.Variable.Genesis.GenesisBlockData? tmpGenObj = null;
                            try
                            {
                                tmpGenObj = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(MainResultStr);
                            }
                            catch
                            {
                            }

                            if (tmpGenObj != null)
                            {
                                //öncekileri doğrula 
                                for (int count = 1; count < CeremonyMemberCount + 1; count++)
                                {
                                    if (Notus.Block.Genesis.Verify(tmpGenObj, count) == false)
                                    {
                                        NP.Success("Un Verified -> " + count.ToString());
                                        Environment.Exit(0);
                                    }
                                }
                                exitFromWhileLoop = true;
                                GenesisObj = tmpGenObj;
                            }
                        }
                        else
                        {
                            Thread.Sleep(1500);
                        }
                    }
                }
            }
        }

        private void WaitPrevSigner()
        {
            int SelectedPortVal = NVG.Settings.Nodes.My.IP.Port + 5;
            if (MyOrderNo > 1)
            {
                int controlOrderNo = MyOrderNo - 2;
                string waitingWalletId = ValidatorOrder.Values.ElementAt(controlOrderNo);
                foreach (var validatorItem in NVG.NodeList)
                {
                    if (string.Equals(waitingWalletId, validatorItem.Value.IP.Wallet))
                    {
                        bool exitFromWhileLoop = false;
                        while (exitFromWhileLoop == false)
                        {
                            string requestUrl = NNN.MakeHttpListenerPath(
                                    validatorItem.Value.IP.IpAddress, SelectedPortVal
                                ) + "genesis";
                            string MainResultStr = NCR.GetSync(requestUrl, 2, true, false);
                            if (MainResultStr.Length > 20)
                            {
                                Notus.Variable.Genesis.GenesisBlockData? tmpGenObj = null;
                                try
                                {
                                    tmpGenObj = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(MainResultStr);
                                }
                                catch
                                {
                                }

                                if (tmpGenObj != null)
                                {
                                    //öncekileri doğrula 
                                    for (int count = 1; count < MyOrderNo; count++)
                                    {
                                        if (Notus.Block.Genesis.Verify(tmpGenObj, count) == true)
                                        {
                                            exitFromWhileLoop = true;
                                        }
                                        else
                                        {
                                            NP.Success("Un Verified -> " + count.ToString());
                                            Environment.Exit(0);
                                        }
                                    }
                                    GenesisObj = tmpGenObj;
                                    SignedGenesis();
                                }
                            }
                            else
                            {
                                Thread.Sleep(5000);
                            }
                        }
                    }
                }
            }
        }
        private void SignedGenesis()
        {
            GenesisObj.Ceremony[MyOrderNo].PublicKey = NVG.Settings.Nodes.My.PublicKey;
            GenesisObj.Ceremony[MyOrderNo].Sign = "";

            string rawGenesisDataStr = Notus.Block.Genesis.CalculateRaw(GenesisObj, MyOrderNo);
            GenesisObj.Ceremony[MyOrderNo].Sign = Notus.Wallet.ID.Sign(rawGenesisDataStr, NVG.Settings.Nodes.My.PrivateKey);
            Signed = true;
        }
        private void Generate()
        {
            GenesisObj = Notus.Block.Genesis.Generate(
                NVG.Settings.Nodes.My.IP.Wallet,
                NVG.Settings.Network,
                NVG.Settings.Layer
            );
            
            //burada birinci sıradaki validatörün imzası eklenece
            GenesisObj.Ceremony.Clear();
            for (int i = 1; i < CeremonyMemberCount + 1; i++)
            {
                GenesisObj.Ceremony.Add(i, new Variable.Genesis.GenesisCeremonyOrderType()
                {
                    PublicKey = "",
                    Sign = ""
                });
            }
            SignedGenesis();
            NP.Success("I Generate Genesis Block");
        }
        private void MakeMembersOrders()
        {
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
                                NVC.Delimeter +
                                entry.Value.Begin.ToString() +
                                NVC.Delimeter +
                                "executing_genesis_ceremony" +
                                NVC.Delimeter +
                                innerCount.ToString()
                            ),
                            NumberStyles.AllowHexSpecifier
                        );
                        if (ValidatorOrder.ContainsKey(intWalletNo) == false)
                        {
                            ValidatorOrder.Add(intWalletNo, entry.Value.IP.Wallet);
                            exitInnerWhileLoop = true;
                        }
                        else
                        {
                            innerCount++;
                        }
                    }
                }
            }


            MyOrderNo = 0;
            Console.WriteLine("=====================================");
            for (int i = 0; i < ValidatorOrder.Count; i++)
            {
                string? currentWalletId = ValidatorOrder.Values.ElementAt(i);
                if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, currentWalletId))
                {
                    MyOrderNo = i + 1;
                }
                Console.WriteLine("currentWalletId : " + currentWalletId);
            }
            Console.WriteLine("=====================================");
        }
        private string Fnc_OnReceiveData(NVS.HttpRequestDetails IncomeData)
        {
            if (IncomeData.UrlList.Length == 0)
            {
                return "false";
            }
            string incomeFullUrlPath = string.Join("/", IncomeData.UrlList).ToLower();
            if (string.Equals(incomeFullUrlPath.Substring(incomeFullUrlPath.Length - 1), "/"))
            {
                incomeFullUrlPath = incomeFullUrlPath.Substring(0, incomeFullUrlPath.Length - 1);
            }

            if (string.Equals(incomeFullUrlPath, "nodeinfo"))
            {
                return JsonSerializer.Serialize(NVG.NodeList[NVG.Settings.Nodes.My.HexKey]);
            }
            if (string.Equals(incomeFullUrlPath, "finalization"))
            {
                if (Signed == true && MyOrderNo == CeremonyMemberCount)
                {
                    return JsonSerializer.Serialize(GenesisObj);
                }
                return "false";
            }
            if (string.Equals(incomeFullUrlPath, "genesis"))
            {
                if (Signed == false)
                {
                    return "false";
                }
                return JsonSerializer.Serialize(GenesisObj);
            }
            if (string.Equals(incomeFullUrlPath, "sign"))
            {
                return BlockSignHash;
            }
            if (string.Equals(incomeFullUrlPath, "infostatus"))
            {
                return JsonSerializer.Serialize(NVG.NodeList);
            }

            NP.Warning("Unknown Url : " + incomeFullUrlPath);
            return "false";
        }
        private void StartGenesisConnection()
        {
            int SelectedPortVal = NVG.Settings.Nodes.My.IP.Port + 5;
            IPAddress NodeIpAddress = IPAddress.Parse(
                NVG.Settings.LocalNode == false
                    ?
                NVG.Settings.IpInfo.Public
                    :
                NVG.Settings.IpInfo.Local
            );
            NP.Basic("Listining : " + NNN.MakeHttpListenerPath(NodeIpAddress.ToString(), SelectedPortVal));
            HttpObj.OnReceive(Fnc_OnReceiveData);
            HttpObj.ResponseType = "application/json";
            HttpObj.Start(NodeIpAddress, SelectedPortVal);
            NP.Success("Http Has Started");
        }
        private void ControlOtherValidatorStatus()
        {
            bool exitFromWhileLoop = false;
            int SelectedPortVal = NVG.Settings.Nodes.My.IP.Port + 5;
            while (exitFromWhileLoop == false)
            {
                bool allValidatorIsOnline = true;
                foreach (var validatorItem in NVG.NodeList)
                {
                    if (validatorItem.Value.Status != NVS.NodeStatus.Online)
                    {
                        allValidatorIsOnline = false;
                        string requestUrl = NNN.MakeHttpListenerPath(
                                validatorItem.Value.IP.IpAddress, SelectedPortVal
                            ) + "nodeinfo";
                        string MainResultStr = NCR.GetSync(requestUrl, 2, true, false);
                        if (MainResultStr.Length > 0)
                        {
                            try
                            {
                                NVS.NodeQueueInfo? tmpNodeInfo = JsonSerializer.Deserialize<NVS.NodeQueueInfo>(MainResultStr);
                                if (tmpNodeInfo != null)
                                {
                                    NVG.NodeList[validatorItem.Key] = tmpNodeInfo;
                                    NGF.SetNodeOnline(validatorItem.Key);
                                }
                            }
                            catch { }
                        }
                    }
                }
                if (allValidatorIsOnline == true)
                {
                    exitFromWhileLoop = true;
                }
            }
        }
        public Genesis()
        {
            NVG.NodeList.Clear();
            NGF.ValidatorList.Clear();
            foreach (Variable.Struct.IpInfo item in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
            {
                NVH.AddToValidatorList(item.IpAddress, item.Port, false);
            }
            List<string> removeList = new();
            NVH.DefineMyNodeInfo();
            NVH.AddToValidatorList(NVG.Settings.Nodes.My.IP.IpAddress, NVG.Settings.Nodes.My.IP.Port);
            NVH.GenerateNodeInfoListViaValidatorList();
            CeremonyMemberCount = NVG.NodeList.Count;
            NVG.OnlineNodeCount = NVG.NodeList.Count;
        }
        ~Genesis()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                HttpObj.Dispose();
            }
            catch
            {
            }
        }

    }
}
