using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text.Json;
using NBK = Notus.Block.Key;
using NCR = Notus.Communication.Request;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NH = Notus.Hash;
using NNN = Notus.Network.Node;
using NP = Notus.Print;
using NTT = Notus.Toolbox.Text;
using NVC = Notus.Variable.Constant;
using NVClass = Notus.Variable.Class;
using NVD = Notus.Validator.Date;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVS = Notus.Variable.Struct;
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

        private string BlockSignHash = string.Empty;
        private Notus.Variable.Genesis.GenesisBlockData GenesisObj = new();
        private int MyOrderNo = 0;
        private bool FirstStateIsReady = false;
        private NVS.NodeStateInfoStruct FirstState;
        private Notus.Communication.Http HttpObj = new Notus.Communication.Http(true);
        public void SaveCurrentState()
        {
            /*
            Console.WriteLine("Save Current State");
            Console.WriteLine("Save Current State");
            Console.WriteLine("Save Current State");
            */

            FirstState.sign = Notus.Wallet.ID.Sign(
                NVG.BlockMeta.GenerateRawTextForStateSign(FirstState),
                NVG.Settings.Nodes.My.PrivateKey
            );
            FirstStateIsReady = true;

            //string stateText = "<nodeState>" + JsonSerializer.Serialize(FirstState) + "</nodeState>";
            //Console.WriteLine(stateText);
            GetAllState();
            Console.ReadLine();
        }
        public void Start()
        {
            FirstState = new NVS.NodeStateInfoStruct()
            {
                chainId = NVG.Settings.Nodes.My.ChainId,
                time = NVG.NOW.Int,
                state = new NVS.NodeStateStruct()
                {
                    blockUid = "",
                    rowNo = 0,
                    sign = ""
                },
                sign = ""
            };

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
            NVG.BlockMeta.Validator(genesisBlock.info.uID, ValidatorQueue[1]);
            NVG.BlockMeta.Validator(airdropBlock.info.uID, ValidatorQueue[2]);
            //NVG.BlockMeta.Validator(ND.ToLong(genesisBlock.info.time), ValidatorQueue[1]);
            //NVG.BlockMeta.Validator(ND.ToLong(airdropBlock.info.time), ValidatorQueue[2]);
            Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine(JsonSerializer.Serialize(ValidatorQueue));
            Console.WriteLine("-----------------------------------------------------");

            Console.WriteLine("control-point-1");
            NVG.BlockMeta.WriteBlock(genesisBlock, "Genesis -> Line -> 66");
            Console.WriteLine("control-point-2");
            NVG.BlockMeta.WriteBlock(airdropBlock, "Genesis -> Line -> 80");
            Console.WriteLine("control-point-3");
            string prevText = airdropBlock.info.uID + airdropBlock.sign;

            Console.WriteLine(genesisBlock.info.time);
            Console.WriteLine(airdropBlock.info.time);
            for (int counter = 0; counter < 4; counter++)
            {
                string blockValidatorWalletId = ValidatorQueue[counter + 3];
                ulong emptyBlockTime = ND.AddMiliseconds(ND.ToLong(GenesisObj.Info.Creation), NVD.Calculate((ulong)counter + 2));

                NVClass.BlockData emptyBlock = new();
                emptyBlock = NVClass.Block.GetEmpty();
                emptyBlock.info.prevList.Clear();
                emptyBlock.info.type = NVE.BlockTypeList.EmptyBlock;
                emptyBlock.info.rowNo = counter + 3;
                emptyBlock.info.multi = false;
                emptyBlock.info.uID = NBK.GenerateStatic(ND.ToDateTime(emptyBlockTime), blockValidatorWalletId);
                emptyBlock.prev = prevText;
                emptyBlock.info.prevList.Add(NVE.BlockTypeList.GenesisBlock, genesisBlock.info.uID + genesisBlock.sign);
                emptyBlock.info.prevList.Add(NVE.BlockTypeList.SmartContract, airdropBlock.info.uID + airdropBlock.sign);
                if (counter > 0)
                {
                    emptyBlock.info.prevList.Add(NVE.BlockTypeList.EmptyBlock, prevText);
                }
                emptyBlock.info.time = Notus.Block.Key.GetTimeFromKey(emptyBlock.info.uID, true);
                emptyBlock.cipher.ver = "NE";
                emptyBlock.cipher.data = NTT.NumberToBase64(counter + 1);
                emptyBlock = new Notus.Block.Generate(blockValidatorWalletId).Make(emptyBlock, 1000);

                NVG.BlockMeta.Validator(emptyBlock.info.uID, blockValidatorWalletId);
                //NVG.BlockMeta.Validator(airdropBlock.info.uID, ValidatorQueue[2]);
                Console.WriteLine(emptyBlock.info.time);

                prevText = emptyBlock.info.uID + emptyBlock.sign;

                NVG.BlockMeta.WriteBlock(emptyBlock, "Genesis -> Line -> 107");
                FirstState.state.blockUid = emptyBlock.info.uID;
                FirstState.state.rowNo = emptyBlock.info.rowNo;
                FirstState.state.sign = emptyBlock.sign;
            }
        }
        private void GetAllState()
        {
            int SelectedPortVal = NVG.Settings.Nodes.My.IP.Port + 5;
            foreach (var validatorItem in NVG.NodeList)
            {
                if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, validatorItem.Value.IP.Wallet) == false)
                {
                    bool exitFromWhileLoop = false;
                    while (exitFromWhileLoop == false)
                    {
                        string requestUrl = NNN.MakeHttpListenerPath( validatorItem.Value.IP.IpAddress, SelectedPortVal ) + "state";
                        string MainResultStr = NCR.GetSync(requestUrl, 2, true, false);
                        if (MainResultStr.Length > 10)
                        {
                            NVS.NodeStateInfoStruct? tmpValidatorState = null;
                            try
                            {
                                tmpValidatorState = JsonSerializer.Deserialize<NVS.NodeStateInfoStruct>(MainResultStr);
                            }
                            catch { }

                            if (tmpValidatorState != null)
                            {
                                if(string.Equals(tmpValidatorState.state.blockUid, FirstState.state.blockUid) == false)
                                {
                                    NP.Danger("Block Uid Is Different");
                                    Environment.Exit(0);
                                }
                                if(string.Equals(tmpValidatorState.state.sign, FirstState.state.sign) == false)
                                {
                                    NP.Danger("Block Sign Is Different");
                                    Environment.Exit(0);
                                }
                                if(tmpValidatorState.state.rowNo!= FirstState.state.rowNo)
                                {
                                    NP.Danger("Block Row No Is Different");
                                    Environment.Exit(0);
                                }
                                exitFromWhileLoop = true;
                            }
                        }
                        else
                        {
                            Thread.Sleep(500);
                        }
                    }
                }
            }
            NP.Success("State Are Equals");
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
                    if (ValidatorQueue.Count < 6)
                    {
                        ValidatorQueue.Add(tmpOrderNo, item.Value);
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
            airdropBlock.info.prevList.Add(NVE.BlockTypeList.GenesisBlock, genesisBlock.info.uID + genesisBlock.sign);
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
            for (int i = 0; i < ValidatorOrder.Count; i++)
            {
                string? currentWalletId = ValidatorOrder.Values.ElementAt(i);
                if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, currentWalletId))
                {
                    MyOrderNo = i + 1;
                }
            }
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

            if (string.Equals(incomeFullUrlPath, "state"))
            {
                return (FirstStateIsReady == true ? JsonSerializer.Serialize(FirstState) : "false");
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
