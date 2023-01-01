using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text.Json;
using NCG = Notus.Ceremony.Genesis;
using NCR = Notus.Communication.Request;
using NH = Notus.Hash;
using NP = Notus.Print;
using NTT = Notus.Toolbox.Text;
using NVC = Notus.Variable.Constant;
using NVClass = Notus.Variable.Class;
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVS = Notus.Variable.Struct;
namespace Notus.Ceremony
{
    public class Genesis : IDisposable
    {
        private SortedDictionary<BigInteger, string> ValidatorOrder = new SortedDictionary<BigInteger, string>();
        private int CeremonyMemberCount = 6;
        private bool Signed = false;
        private NVClass.BlockData genesisBlock = new();
        private string BlockSignHash = string.Empty;
        private Notus.Variable.Genesis.GenesisBlockData GenesisObj = new();
        private int MyOrderNo = 0;
        private Notus.Communication.Http HttpObj = new Notus.Communication.Http(true);
        public void Start()
        {
            //kontrollü bir şekilde dosyayı silerek sıfırlıyor
            Notus.IO.DeleteFile(NVC.MemoryPoolName["ValidatorList"] + ".db");

            NVH.PrepareValidatorList(true);
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
            using (Notus.Block.Storage BS_Storage = new Notus.Block.Storage(false))
            {
                BS_Storage.AddSync(genesisBlock, true);
            }
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
                            string requestUrl = Notus.Network.Node.MakeHttpListenerPath(
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
            string leaderWalletId = ValidatorOrder.Values.ElementAt(5);

            genesisBlock = NVClass.Block.GetEmpty();

            genesisBlock.info.type = 360;
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
            genesisBlock = new Notus.Block.Generate(leaderWalletId).Make(genesisBlock, 1000);
            BlockSignHash = genesisBlock.sign;
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
                        string requestUrl = Notus.Network.Node.MakeHttpListenerPath(
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
                                for (int count = 1; count < CeremonyMemberCount+1; count++)
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
                            string requestUrl = Notus.Network.Node.MakeHttpListenerPath(
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
                                            GenesisObj = tmpGenObj;
                                            exitFromWhileLoop = true;
                                        }
                                        else
                                        {
                                            NP.Success("Un Verified -> " + count.ToString());
                                            Environment.Exit(0);
                                        }
                                    }
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
            for (int i = 1; i < CeremonyMemberCount+1; i++)
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
                                NVC.CommonDelimeterChar +
                                entry.Value.Begin.ToString() +
                                NVC.CommonDelimeterChar +
                                "executing_genesis_ceremony" +
                                NVC.CommonDelimeterChar +
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
                if (Signed == true)
                {
                    return JsonSerializer.Serialize(GenesisObj);
                }
                return "false";
            }
            if (string.Equals(incomeFullUrlPath, "sign"))
            {
                return BlockSignHash;
            }

            if (string.Equals(incomeFullUrlPath, "infostatus"))
            {
                return JsonSerializer.Serialize(NVG.NodeList);
            }

            NP.Warning("Unknown Or Unready Url : " + incomeFullUrlPath);
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
            HttpObj.StoreUrl = false;
            HttpObj.DefaultResult_OK = "null";
            HttpObj.DefaultResult_ERR = "null";

            NP.Basic("Listining : " + Notus.Network.Node.MakeHttpListenerPath(NodeIpAddress.ToString(), SelectedPortVal));
            HttpObj.OnReceive(Fnc_OnReceiveData);
            HttpObj.ResponseType = "application/json";
            HttpObj.StoreUrl = false;
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
                        string requestUrl = Notus.Network.Node.MakeHttpListenerPath(
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
                                    NVG.NodeList[validatorItem.Key].Status = NVS.NodeStatus.Online;
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
            CeremonyMemberCount = NVG.NodeList.Count;
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
