using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text.Json;
using NCG = Notus.Ceremony.Genesis;
using NH = Notus.Hash;
using NP = Notus.Print;
using NTT = Notus.Toolbox.Text;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVS = Notus.Variable.Struct;
using NCR = Notus.Communication.Request;

namespace Notus.Ceremony
{
    public static class Genesis
    {
        public static Notus.Variable.Genesis.GenesisBlockData GenesisObj = new();
        public static string NextWalletId = "";
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
            NP.ReadLine();

            NCG.MakeMembersOrders();
            NP.ReadLine();
        }
        public static void DistributeTheNext()
        {
            string genesisText = JsonSerializer.Serialize(GenesisObj);
            foreach (var validatorItem in NVG.NodeList)
            {
                bool genesisSended = false;
                while (genesisSended == false)
                {
                    genesisSended = true;
                    if (NextWalletId.Length == 0 || string.Equals(NextWalletId, validatorItem.Value.IP.Wallet))
                    {
                        genesisSended = NVG.Settings.PeerManager.Send(
                            validatorItem.Key,
                            "<genesis>" + genesisText + "</genesis>",
                            false
                        );
                        if (genesisSended == false)
                        {
                            //Console.WriteLine("validatorItem.Key" + validatorItem.Key);
                            Thread.Sleep(50);
                        }
                    }
                }
            }
        }
        public static void SocketDataControl(string incomeText)
        {
            incomeText = NTT.GetPureText(incomeText, "genesis");
            bool textConverted = false;
            //Console.WriteLine(incomeMessage);
            try
            {
                NCG.GenesisObj = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(incomeText);
                textConverted = true;
            }
            catch
            {
                Console.WriteLine("Json  Convert Error : " + incomeText);
            }
            if (textConverted == true)
            {
                if (Notus.Block.Genesis.Verify(NCG.GenesisObj, (NCG.MyOrderNo - 1)) == false)
                {
                    Console.WriteLine("Gelen Blok Hatali");
                }
                else
                {
                    Console.WriteLine("Gelen Blok Uygun");
                    NCG.GenesisObj.Ceremony[NCG.MyOrderNo].PublicKey = NVG.Settings.Nodes.My.PublicKey;
                    string rawGenesisDataStr = Notus.Block.Genesis.CalculateRaw(
                        NCG.GenesisObj,
                        NCG.MyOrderNo
                    );

                    Console.WriteLine("Ozet : " + new Notus.Hash().CommonHash("sha1", rawGenesisDataStr));
                    NCG.GenesisObj.Ceremony[NCG.MyOrderNo].Sign = Notus.Wallet.ID.Sign(rawGenesisDataStr, NVG.Settings.Nodes.My.PrivateKey);

                    Console.WriteLine("JsonSerializer.Serialize(NCG.GenesisObj.Ceremony)");
                    Console.WriteLine(JsonSerializer.Serialize(NCG.GenesisObj.Ceremony));
                    DistributeTheNext();
                }
            }
        }
        public static bool Generate()
        {
            int myOrderNo = 1;
            GenesisObj = Notus.Block.Genesis.Generate(
                //NVG.Settings.NodeWallet.WalletKey, 
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
            GenesisObj.Ceremony[myOrderNo].PublicKey = NVG.Settings.Nodes.My.PublicKey;
            GenesisObj.Ceremony[myOrderNo].Sign = "";

            string rawGenesisDataStr = Notus.Block.Genesis.CalculateRaw(GenesisObj, myOrderNo);
            Console.WriteLine("Ozet : " + new Notus.Hash().CommonHash("sha1", rawGenesisDataStr));
            GenesisObj.Ceremony[myOrderNo].Sign = Notus.Wallet.ID.Sign(rawGenesisDataStr, NVG.Settings.Nodes.My.PrivateKey);

            if (Notus.Block.Genesis.Verify(GenesisObj, myOrderNo) == false)
            {
                return false;
            }
            return true;
        }
        public static void MakeMembersOrders()
        {
            NextWalletId = string.Empty;
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

            MyOrderNo = 0;
            foreach (var item in resultList)
            {
                Console.WriteLine(item.Key.ToString() + " - " + item.Value);
            }
            for (int i = 0; i < resultList.Count; i++)
            {
                string? currentWalletId = resultList.Values.ElementAt(i);
                if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, currentWalletId))
                {
                    MyOrderNo = i + 1;
                }
            }
            if (6 > MyOrderNo && resultList.Count > MyOrderNo)
            {
                NextWalletId = resultList.Values.ElementAt(MyOrderNo);
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
            Console.WriteLine(incomeFullUrlPath);
            Console.WriteLine(JsonSerializer.Serialize(NVG.NodeList));
            if (string.Equals(incomeFullUrlPath, "nodeinfo"))
            {
                return JsonSerializer.Serialize(NVG.NodeList[NVG.Settings.Nodes.My.HexKey]);
            }

            if (string.Equals(incomeFullUrlPath, "infostatus"))
            {
                return JsonSerializer.Serialize(NVG.NodeList);
            }

            //string resultData = Obj_Api.Interpret(IncomeData);

            return "false";
        }

        public static void StartGenesisConnection()
        {
            int SelectedPortVal = NVG.Settings.Nodes.My.IP.Port + 5;
            Console.WriteLine("SelectedPortVal : " + SelectedPortVal.ToString());
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
                        try
                        {
                            string requestUrl = Notus.Network.Node.MakeHttpListenerPath(
                                    validatorItem.Value.IP.IpAddress, SelectedPortVal
                                ) + "nodeinfo";
                            Console.WriteLine(requestUrl);
                            string MainResultStr = NCR.GetSync(requestUrl, 2, true, false);
                            if (MainResultStr.Length > 0)
                            {
                            }
                            else
                            {
                                Thread.Sleep(1000);
                            }
                        }
                        catch (Exception err)
                        {
                            Thread.Sleep(200);
                        }
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
    }
}
