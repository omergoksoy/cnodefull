using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using NGV = Notus.Globals.Variable;
using NVG = Notus.Variable.Globals;
using NVC = Notus.Variable.Constant;
using NVS = Notus.Variable.Struct;
using NVClass = Notus.Variable.Class;
using NNN = Notus.Network.Node;
using NCR = Notus.Communication.Request;
using NP = Notus.Print;
using NVE = Notus.Variable.Enum;
using NTN = Notus.Toolbox.Network;
namespace Notus.Toolbox
{
    public class Network
    {
        private static bool Error_TestIpAddress = true;
        private static readonly string DefaultControlTestData = "notus-network-test-result-data";

        public static NVClass.BlockData? GetBlockFromNode(
            Variable.Struct.IpInfo? ipNode,
            long blockNo, NGV.Settings? objSettings = null
        )
        {
            return GetBlockFromNode(ipNode.IpAddress, ipNode.Port, blockNo, objSettings);
        }
        public static NVS.NodeStatus PingToNode(NVS.IpInfo NodeIp)
        {
            return PingToNode(NodeIp.IpAddress, NodeIp.Port);
        }
        public static NVS.NodeStatus PingToNode(string ipAddress, int portNo)
        {
            string requestUrl = NNN.MakeHttpListenerPath(ipAddress, portNo) + "ping/";
            string serverResponse = NCR.GetSync(requestUrl, 1, true, false);
            Console.WriteLine("requestUrl : " + requestUrl);
            Console.WriteLine("serverResponse : " + serverResponse);
            return string.Equals(serverResponse, "pong") == true ? NVS.NodeStatus.Online : NVS.NodeStatus.Offline;
        }
        public static string IpAndPortToHex(NVS.NodeInfo NodeIp)
        {
            return IpAndPortToHex(NodeIp.IpAddress, NodeIp.Port);
        }
        public static string IpAndPortToHex(NVS.IpInfo NodeIp)
        {
            return IpAndPortToHex(NodeIp.IpAddress, NodeIp.Port);
        }
        public static string IpAndPortToHex(string ipAddress, int portNo)
        {
            string resultStr = "";
            foreach (string byteStr in ipAddress.Split("."))
            {
                resultStr += int.Parse(byteStr).ToString("x").PadLeft(2, '0');
            }
            return resultStr.ToLower() + portNo.ToString("x").PadLeft(5, '0').ToLower();
        }

        public static NVClass.BlockData? GetBlockFromNode(
            string ipAddress,
            int portNo,
            long blockNo,
            NGV.Settings? objSettings = null
        )
        {
            string urlPath = NNN.MakeHttpListenerPath(ipAddress, portNo) + "block/" + blockNo.ToString() + "/raw";
            string incodeResponse = NCR.GetSync(
                urlPath, 2, true, false, objSettings
            );
            try
            {
                if (incodeResponse != null && incodeResponse != string.Empty && incodeResponse.Length > 0)
                {
                    NVClass.BlockData? tmpResultBlock =
                        JsonSerializer.Deserialize<NVClass.BlockData>(incodeResponse);
                    if (tmpResultBlock != null)
                    {
                        return tmpResultBlock;
                    }
                }
            }
            catch (Exception err)
            {
                if (objSettings != null)
                {
                    NP.Danger(objSettings, err.Message);
                }
            }
            return null;
        }
        public static NVClass.BlockData? GetLastBlock(NVS.IpInfo NodeIp, NGV.Settings? objSettings = null)
        {
            return GetLastBlock(NNN.MakeHttpListenerPath(NodeIp.IpAddress, NodeIp.Port), objSettings);
        }
        public static NVClass.BlockData? GetLastBlock(string NodeAddress, NGV.Settings? objSettings = null)
        {
            try
            {
                string MainResultStr = NCR.GetSync(
                    NodeAddress + "block/last/raw",
                    10,
                    true,
                    true,
                    objSettings
                );
                NVClass.BlockData? PreBlockData =
                    JsonSerializer.Deserialize<NVClass.BlockData>(MainResultStr);
                //Console.WriteLine(JsonSerializer.Serialize(PreBlockData));
                if (PreBlockData != null)
                {
                    return PreBlockData;
                }
            }
            catch (Exception err)
            {
                if (objSettings == null)
                {
                    Console.WriteLine("err : " + err.Message);
                }
                else
                {
                    NP.Danger(objSettings, "Error Point (GetLastBlock) : " + err.Message);
                }
            }
            return null;
        }

        public static int GetNetworkPort()
        {
            if (NVG.Settings.Network == Variable.Enum.NetworkType.TestNet)
                return NVG.Settings.Port.TestNet;

            if (NVG.Settings.Network == Variable.Enum.NetworkType.DevNet)
                return NVG.Settings.Port.DevNet;

            return NVG.Settings.Port.MainNet;
        }
        public static void IdentifyNodeType(int Timeout = 5)
        {
            NVG.Settings.IpInfo = NTN.GetNodeIP();
            if (NVG.Settings.LocalNode == true)
            {
                //NP.Basic(NVG.Settings, "Starting As Main Node");
                NVG.Settings.NodeType = NVE.NetworkNodeType.Main;
                //NVG.Settings.Nodes.My.IP.IpAddress = NVG.Settings.IpInfo.Local;
                NVG.Settings.Nodes.My.IP.IpAddress = "127.0.0.1";
                NVG.Settings.IpInfo.Local = NVG.Settings.Nodes.My.IP.IpAddress;
            }
            else
            {
                NVG.Settings.Nodes.My.IP.IpAddress = NVG.Settings.IpInfo.Public;
            }
            NVG.Settings.Nodes.My.HexKey = NTN.IpAndPortToHex(NVG.Settings.Nodes.My.IP.IpAddress, NVG.Settings.Nodes.My.IP.Port);

            List<string> ListMainNodeIp = Notus.Validator.List.Get(NVG.Settings.Layer, NVG.Settings.Network);
            if (ListMainNodeIp.IndexOf(NVG.Settings.IpInfo.Public) >= 0 || NVG.Settings.LocalNode == true)
            {
                //NVG.Settings.Nodes.My.InTheCode = true;
                NP.Basic(NVG.Settings, "Starting As Main Node");
                if (PublicIpIsConnectable(Timeout))
                {
                    NVG.Settings.NodeType = NVE.NetworkNodeType.Main;
                }
                else
                {
                    NP.Basic(NVG.Settings, "Main Node Port Error");
                }
            }
            else
            {
                //NVG.Settings.Nodes.My.InTheCode = false;
                NP.Basic(NVG.Settings, "Not Main Node");

                if (PublicIpIsConnectable(Timeout))
                {
                    NP.Basic(NVG.Settings, "Starting As Master Node");
                    NVG.Settings.NodeType = NVE.NetworkNodeType.Master;
                }
                else
                {
                    NP.Basic(NVG.Settings, "Not Master Node");
                    NP.Basic(NVG.Settings, "Starting As Replicant Node");
                    NVG.Settings.NodeType = NVE.NetworkNodeType.Replicant;
                }
            }
        }

        public static int FindFreeTcpPort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public static void WaitUntilPortIsAvailable(int PortNo)
        {
            bool PortAvailable = false;
            while (PortAvailable == false)
            {
                PortAvailable = PortIsAvailable(PortNo);
                Thread.Sleep(150);
            }
        }

        public static bool PortIsAvailable(int PortNo)
        {
            bool isAvailable = true;
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port == PortNo)
                {
                    isAvailable = false;
                    break;
                }
            }
            return isAvailable;
        }

        public static NVS.NodeIpInfo GetNodeIP()
        {
            return new NVS.NodeIpInfo()
            {
                Local = GetLocalIPAddress(false),
                Public = GetPublicIPAddress()
            };
        }

        private static string ReadFromNet(string urlPath)
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = client.GetAsync(urlPath).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception err)
            {
            }
            return string.Empty;
        }
        public static string GetPublicIPAddress()
        {
            string address = ReadFromNet("https://api.ipify.org");
            if (address.Length > 0)
            {
                return address;
            }

            address = ReadFromNet("http://checkip.dyndns.org/");
            if (address.Length > 0)
            {
                if (address.Contains("</body>") == true && address.Contains("Address: ") == true)
                {
                    int first = address.IndexOf("Address: ") + 9;
                    return address.Substring(
                        first,
                        address.LastIndexOf("</body>") - first
                    );
                }
            }

            return string.Empty;
        }

        public static string GetLocalIPAddress(bool returnLocalIp)
        {
            bool tmpNetworkAvailable = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            /*
            if (tmpNetworkAvailable == true)
            {
                Console.WriteLine("available");
            }
            else
            {
                Console.WriteLine("un available");
            }
            Console.ReadLine();
            */
            if (returnLocalIp == true)
            {
                return "127.0.0.1";
            }
            try
            {
                string dnsResult = Dns.GetHostName();
                IPHostEntry host = Dns.GetHostEntry(dnsResult);
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception err)
            {
                NP.Log(
                    NVE.LogLevel.Info,
                    98798700,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
            }
            return "127.0.0.1";
        }

        private static bool PublicIpIsConnectable(int Timeout)
        {
            Error_TestIpAddress = false;
            try
            {
                int ControlPortNo = NTN.FindFreeTcpPort();
                using (Notus.Communication.Http tmp_HttpObj = new Notus.Communication.Http())
                {
                    tmp_HttpObj.ResponseType = "text/html";
                    tmp_HttpObj.StoreUrl = false;
                    tmp_HttpObj.Timeout = 5;
                    tmp_HttpObj.DefaultResult_OK = DefaultControlTestData;
                    tmp_HttpObj.DefaultResult_ERR = DefaultControlTestData;
                    tmp_HttpObj.OnReceive(Fnc_TestLinkData);
                    string testIpAddressText = NVG.Settings.LocalNode == true ? NVG.Settings.IpInfo.Local : NVG.Settings.IpInfo.Public;
                    IPAddress testAddress = IPAddress.Parse(testIpAddressText);
                    tmp_HttpObj.Start(testAddress, ControlPortNo);
                    DateTime twoSecondsLater = NVG.NOW.Obj.AddSeconds(Timeout);
                    while (twoSecondsLater > NVG.NOW.Obj && tmp_HttpObj.Started == false)
                    {
                        try
                        {
                            _ = NCR.Get(
                                NNN.MakeHttpListenerPath(testIpAddressText, ControlPortNo) + "block/hash/1",
                                5, true, false
                            ).GetAwaiter().GetResult();
                        }
                        catch
                        {
                        }
                    }
                    if (tmp_HttpObj.Started == false)
                    {
                        Error_TestIpAddress = true;
                    }
                    tmp_HttpObj.Stop();
                }
            }
            catch (Exception err)
            {
                //NP.Danger(NVG.Settings, "Error [065]: " + err.Message);
                Error_TestIpAddress = true;
            }
            if (Error_TestIpAddress == true)
            {
                return false;
            }
            return true;
        }
        private static string Fnc_TestLinkData(NVS.HttpRequestDetails IncomeData)
        {
            return DefaultControlTestData;
        }

    }
}
