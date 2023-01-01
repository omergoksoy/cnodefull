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
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVS = Notus.Variable.Struct;

namespace Notus.Ceremony
{
    public static class Genesis
    {
        public static SortedDictionary<BigInteger, string> ValidatorOrder = new SortedDictionary<BigInteger, string>();
        public static bool Signed = false;
        public static Notus.Variable.Genesis.GenesisBlockData GenesisObj = new();
        public static int MyOrderNo = 0;
        public static Notus.Communication.Http HttpObj = new Notus.Communication.Http(true);
        public static void PreStart()
        {
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
                NP.Danger("Diger nodelardan tanımlanmış Validatorlerden tarafından olusturulmus Genesis blogunu iste");
                NP.Danger("Diger nodelardan tanımlanmış Validatorlerden tarafından olusturulmus Genesis blogunu iste");
                NP.Danger("Genesis Ceremony Works With Only Defined Validators");
                Environment.Exit(0);
            }

            NVH.DefineMyNodeInfo();
            StartGenesisConnection();
            ControlOtherValidatorStatus();
            NCG.MakeMembersOrders();
        }
        public static void WaitPrevSigner()
        {
            int SelectedPortVal = NVG.Settings.Nodes.My.IP.Port + 5;
            if (NCG.MyOrderNo > 1)
            {
                //Console.WriteLine("NCG.MyOrderNo : " + NCG.MyOrderNo.ToString());
                int controlOrderNo = NCG.MyOrderNo - 2;
                string waitingWalletId = ValidatorOrder.Values.ElementAt(controlOrderNo);
                NP.Basic("Control Wallet : " + waitingWalletId);
                //NP.Basic("ValidatorOrder.Values.ElementAt(NCG.MyOrderNo-2) : " + ValidatorOrder.Values.ElementAt(NCG.MyOrderNo-2));
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
                                    //Console.WriteLine("Genesis Text Convert Error : " + MainResultStr);
                                }

                                if (tmpGenObj == null)
                                {
                                    //Console.WriteLine("Genesis Text Is NULL");
                                }
                                else
                                {
                                    //öncekini doğrula 
                                    if (Notus.Block.Genesis.Verify(tmpGenObj, NCG.MyOrderNo-1) == true)
                                    {
                                        NP.Success("Verified");
                                        //NP.ReadLine();
                                        SignedGenesis();
                                    }
                                    else
                                    {
                                        string rawGenesisDataStr = Notus.Block.Genesis.CalculateRaw(tmpGenObj, NCG.MyOrderNo-1);
                                        Console.WriteLine("Ozet : " + new Notus.Hash().CommonHash("sha1", rawGenesisDataStr));
                                        NP.Danger("Un Verified");
                                    }
                                }
                                NP.ReadLine();
                            }
                            else
                            {
                                //Console.WriteLine("Genesis Text Is Empty : " + MainResultStr);
                                Thread.Sleep(5000);
                            }
                        }
                    }
                }
            }
        }
        public static void SignedGenesis()
        {
            GenesisObj.Ceremony[NCG.MyOrderNo].PublicKey = NVG.Settings.Nodes.My.PublicKey;
            GenesisObj.Ceremony[NCG.MyOrderNo].Sign = "";

            string rawGenesisDataStr = Notus.Block.Genesis.CalculateRaw(GenesisObj, NCG.MyOrderNo);
            Console.WriteLine("Ozet : " + new Notus.Hash().CommonHash("sha1", rawGenesisDataStr));
            GenesisObj.Ceremony[NCG.MyOrderNo].Sign = Notus.Wallet.ID.Sign(rawGenesisDataStr, NVG.Settings.Nodes.My.PrivateKey);
            Signed = true;
        }
        public static void Generate()
        {
            GenesisObj = Notus.Block.Genesis.Generate(
                NVG.Settings.Nodes.My.IP.Wallet,
                NVG.Settings.Network,
                NVG.Settings.Layer
            );

            //burada birinci sıradaki validatörün imzası eklenece
            GenesisObj.Ceremony.Clear();
            for (int i = 1; i < 7; i++)
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
        public static void MakeMembersOrders()
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
        private static string Fnc_OnReceiveData(NVS.HttpRequestDetails IncomeData)
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

            if (string.Equals(incomeFullUrlPath, "genesis"))
            {
                if (Signed == true)
                {
                    return JsonSerializer.Serialize(GenesisObj);
                }
                return "false";
            }

            if (string.Equals(incomeFullUrlPath, "infostatus"))
            {
                return JsonSerializer.Serialize(NVG.NodeList);
            }

            Console.WriteLine(incomeFullUrlPath);
            Console.WriteLine(JsonSerializer.Serialize(NVG.NodeList));
            //string resultData = Obj_Api.Interpret(IncomeData);

            return "false";
        }

        public static void StartGenesisConnection()
        {
            int SelectedPortVal = NVG.Settings.Nodes.My.IP.Port + 5;
            // Console.WriteLine("SelectedPortVal : " + SelectedPortVal.ToString());
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
        public static void ControlOtherValidatorStatus()
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
    }
}
