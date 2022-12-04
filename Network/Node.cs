using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NVG = Notus.Variable.Globals;
using NVC = Notus.Variable.Constant;
using NVE = Notus.Variable.Enum;
using NGV = Notus.Globals.Variable;
namespace Notus.Network
{
    public static class Node
    {
        public static async Task<string> FindAvailable(
            string UrlText,
            NVE.NetworkType currentNetwork,
            NVE.NetworkLayer networkLayer,
            bool sslActive = false
        )
        {
            string MainResultStr = string.Empty;
            if (sslActive == false)
            {
                bool exitInnerLoop = false;
                while (exitInnerLoop == false)
                {
                    List<string> ListMainNodeIp = Notus.Validator.List.Get(networkLayer, currentNetwork);

                    for (int a = 0; a < ListMainNodeIp.Count && exitInnerLoop == false; a++)
                    {
                        try
                        {
                            MainResultStr = await Notus.Communication.Request.Get(MakeHttpListenerPath(
                                ListMainNodeIp[a],
                                GetNetworkPort(currentNetwork, networkLayer)) + UrlText, 10, true);
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine(err.Message);
                            Notus.Date.SleepWithoutBlocking(5, true);
                        }
                        exitInnerLoop = (MainResultStr.Length > 0);
                    }
                }
            }
            else
            {
                try
                {
                    MainResultStr = await Notus.Communication.Request.Get(
                        MakeHttpListenerPath(
                            NVC.DefaultNetworkUrl[currentNetwork],
                            0,
                            true
                        ) +
                        UrlText, 10, true);
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        NVE.LogLevel.Info,
                        77007700,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );

                    Console.WriteLine(err.Message);
                }
            }
            return MainResultStr;
        }
        public static async Task<string> FindAvailable(
            string UrlText,
            Dictionary<string, string> PostData,
            NVE.NetworkType currentNetwork,
            NVE.NetworkLayer networkLayer,
            bool sslActive = false
        )
        {
            string MainResultStr = string.Empty;
            if (sslActive == false)
            {
                bool exitInnerLoop = false;
                while (exitInnerLoop == false)
                {
                    List<string> ListMainNodeIp = Notus.Validator.List.Get(networkLayer, currentNetwork);
                    for (int a = 0; a < ListMainNodeIp.Count && exitInnerLoop == false; a++)
                    {
                        try
                        {
                            MainResultStr = await Notus.Communication.Request.Post(
                                MakeHttpListenerPath(ListMainNodeIp[a],
                                GetNetworkPort(currentNetwork, networkLayer)) + UrlText,
                                PostData
                            );
                        }
                        catch (Exception err)
                        {
                            Notus.Print.Log(
                                NVE.LogLevel.Info,
                                9000877,
                                err.Message,
                                "BlockRowNo",
                                null,
                                err
                            );

                            Console.WriteLine(err.Message);
                            Notus.Date.SleepWithoutBlocking(5, true);
                        }
                        exitInnerLoop = (MainResultStr.Length > 0);
                    }
                }
            }
            else
            {
                try
                {
                    MainResultStr = await Notus.Communication.Request.Post(
                        MakeHttpListenerPath(
                            NVC.DefaultNetworkUrl[currentNetwork],
                            0, true
                        ) +
                        UrlText, PostData);
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        NVE.LogLevel.Info,
                        90778400,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );

                    Console.WriteLine(err.Message);
                }
            }
            return MainResultStr;
        }

        public static string FindAvailableSync(
            string UrlText,
            NVE.NetworkType currentNetwork,
            NVE.NetworkLayer networkLayer,
            bool showError = true,
            NGV.Settings objSettings = null
        )
        {
            string MainResultStr = string.Empty;
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                List<string> ListMainNodeIp = Notus.Validator.List.Get(networkLayer, currentNetwork);

                for (int a = 0; a < ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    try
                    {
                        MainResultStr = Notus.Communication.Request.GetSync(
                            MakeHttpListenerPath(ListMainNodeIp[a],
                            GetNetworkPort(currentNetwork, networkLayer)) + UrlText,
                            10,
                            true,
                            showError,
                            objSettings
                        );
                    }
                    catch (Exception err)
                    {
                        Notus.Print.Log(
                            NVE.LogLevel.Info,
                            77700000,
                            err.Message,
                            "BlockRowNo",
                            objSettings,
                            err
                        );

                        Notus.Print.Danger(objSettings, "Notus.Network.Node.FindAvailableSync -> Line 92 -> " + err.Message);
                        Notus.Date.SleepWithoutBlocking(5, true);
                    }
                    exitInnerLoop = (MainResultStr.Length > 0);
                }
            }
            return MainResultStr;
        }
        public static string FindAvailableSync(
            string UrlText,
            Dictionary<string, string> PostData,
            NVE.NetworkType currentNetwork,
            NVE.NetworkLayer networkLayer,
            NGV.Settings objSettings = null
        )
        {
            string MainResultStr = string.Empty;
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                List<string> ListMainNodeIp = Notus.Validator.List.Get(networkLayer, currentNetwork);

                for (int a = 0; a < ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    try
                    {
                        (bool worksCorrent, string tmpMainResultStr) = Notus.Communication.Request.PostSync(
                            MakeHttpListenerPath(ListMainNodeIp[a],
                            GetNetworkPort(currentNetwork, networkLayer)) + UrlText,
                            PostData
                        );
                        if (worksCorrent == true)
                        {
                            MainResultStr = tmpMainResultStr;
                        }
                    }
                    catch (Exception err)
                    {
                        Notus.Print.Log(
                            NVE.LogLevel.Info,
                            80000888,
                            err.Message,
                            "BlockRowNo",
                            objSettings,
                            err
                        );

                        Console.WriteLine(err.Message);
                        Notus.Date.SleepWithoutBlocking(5, true);
                    }
                    exitInnerLoop = (MainResultStr.Length > 0);
                }
            }
            return MainResultStr;
        }

        public static int GetP2PPort(NGV.Settings? objSetting = null)
        {
            return GetNetworkPort(objSetting) + 10;
        }
        public static int GetNetworkPort(NGV.Settings? objSetting=null)
        {
            if (objSetting == null)
            {
                return GetNetworkPort(NVG.Settings.Network, NVG.Settings.Layer);
            }
            return GetNetworkPort(objSetting.Network, objSetting.Layer);
        }
        public static int GetNetworkPort(NVE.NetworkType currentNetwork, NVE.NetworkLayer currentLayer)
        {
            return NVC.PortNo[currentLayer][currentNetwork];
        }
        public static string MakeHttpListenerPath(string IpAddress, int PortNo = 0, bool UseSSL = false)
        {
            if (PortNo == 0)
            {
                return "http" + (UseSSL == true ? "s" : "") + "://" + IpAddress + "/";
            }
            return "http" + (UseSSL == true ? "s" : "") + "://" + IpAddress + ":" + PortNo + "/";
        }
    }
}
