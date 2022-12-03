﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NVG = Notus.Variable.Globals;
using NGF = Notus.Variable.Globals.Functions;
using NVH = Notus.Validator.Helper;
namespace Notus.Network
{
    public static class Node
    {
        public static async Task<string> FindAvailable(
            string UrlText,
            Notus.Variable.Enum.NetworkType currentNetwork,
            Notus.Variable.Enum.NetworkLayer networkLayer,
            bool sslActive = false
        )
        {
            NVH.PrepareValidatorList();
            string MainResultStr = string.Empty;
            if (sslActive == false)
            {
                bool exitInnerLoop = false;
                while (exitInnerLoop == false)
                {
                    foreach (var item in NGF.ValidatorList)
                    {
                        try
                        {
                            MainResultStr = await Notus.Communication.Request.Get(MakeHttpListenerPath(
                                item.Value.IpAddress,
                                GetNetworkPort(currentNetwork, networkLayer)) + UrlText, 10, true);
                            if(MainResultStr.Length > 0)
                            {
                                exitInnerLoop = true;
                                break;
                            }
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine(err.Message);
                            Notus.Date.SleepWithoutBlocking(5, true);
                        }
                    }
                }
            }
            else
            {
                try
                {
                    MainResultStr = await Notus.Communication.Request.Get(
                        MakeHttpListenerPath(
                            Notus.Variable.Constant.DefaultNetworkUrl[currentNetwork],
                            0,
                            true
                        ) +
                        UrlText, 10, true);
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
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
            Notus.Variable.Enum.NetworkType currentNetwork,
            Notus.Variable.Enum.NetworkLayer networkLayer,
            bool sslActive = false
        )
        {
            NVH.PrepareValidatorList();
            string MainResultStr = string.Empty;
            if (sslActive == false)
            {
                bool exitInnerLoop = false;
                while (exitInnerLoop == false)
                {
                    foreach (var item in NGF.ValidatorList)
                    {
                        try
                        {
                            MainResultStr = await Notus.Communication.Request.Post(
                                MakeHttpListenerPath(item.Value.IpAddress,
                                GetNetworkPort(currentNetwork, networkLayer)) + UrlText,
                                PostData
                            );
                            if (MainResultStr.Length > 0)
                            {
                                exitInnerLoop = true;
                                break;
                            }
                        }
                        catch (Exception err)
                        {
                            Notus.Print.Log(
                                Notus.Variable.Enum.LogLevel.Info,
                                9000877,
                                err.Message,
                                "BlockRowNo",
                                null,
                                err
                            );

                            Console.WriteLine(err.Message);
                            Notus.Date.SleepWithoutBlocking(5, true);
                        }
                    }
                }
            }
            else
            {
                try
                {
                    MainResultStr = await Notus.Communication.Request.Post(
                        MakeHttpListenerPath(
                            Notus.Variable.Constant.DefaultNetworkUrl[currentNetwork],
                            0, true
                        ) +
                        UrlText, PostData);
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
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
            Notus.Variable.Enum.NetworkType currentNetwork,
            Notus.Variable.Enum.NetworkLayer networkLayer,
            bool showError = true,
            Notus.Globals.Variable.Settings objSettings = null
        )
        {
            NVH.PrepareValidatorList();
            string MainResultStr = string.Empty;
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                foreach (var item in NGF.ValidatorList)
                {
                    try
                    {
                        MainResultStr = Notus.Communication.Request.GetSync(
                            MakeHttpListenerPath(item.Value.IpAddress,
                            GetNetworkPort(currentNetwork, networkLayer)) + UrlText,
                            10,
                            true,
                            showError,
                            objSettings
                        );
                        if (MainResultStr.Length > 0)
                        {
                            exitInnerLoop = true;
                            break;
                        }
                    }
                    catch (Exception err)
                    {
                        Notus.Print.Log(
                            Notus.Variable.Enum.LogLevel.Info,
                            77700000,
                            err.Message,
                            "BlockRowNo",
                            objSettings,
                            err
                        );

                        Notus.Print.Danger(objSettings, "Notus.Network.Node.FindAvailableSync -> Line 92 -> " + err.Message);
                        Notus.Date.SleepWithoutBlocking(5, true);
                    }
                }
            }
            return MainResultStr;
        }
        public static string FindAvailableSync(
            string UrlText,
            Dictionary<string, string> PostData,
            Notus.Variable.Enum.NetworkType currentNetwork,
            Notus.Variable.Enum.NetworkLayer networkLayer,
            Notus.Globals.Variable.Settings objSettings = null
        )
        {
            NVH.PrepareValidatorList();
            string MainResultStr = string.Empty;
            bool exitInnerLoop = false;
            while (exitInnerLoop == false)
            {
                foreach (var item in NGF.ValidatorList)
                {
                    try
                    {
                        (bool worksCorrent, string tmpMainResultStr) = Notus.Communication.Request.PostSync(
                            MakeHttpListenerPath(item.Value.IpAddress,
                            GetNetworkPort(currentNetwork, networkLayer)) + UrlText,
                            PostData
                        );
                        if (worksCorrent == true)
                        {
                            MainResultStr = tmpMainResultStr;
                            if (MainResultStr.Length > 0)
                            {
                                exitInnerLoop = true;
                                break;
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        Notus.Print.Log(
                            Notus.Variable.Enum.LogLevel.Info,
                            80000888,
                            err.Message,
                            "BlockRowNo",
                            objSettings,
                            err
                        );

                        Console.WriteLine(err.Message);
                        Notus.Date.SleepWithoutBlocking(5, true);
                    }
                }
            }
            return MainResultStr;
        }

        public static int GetNetworkPort(Notus.Globals.Variable.Settings? objSetting=null)
        {
            if (objSetting == null)
            {
                return GetNetworkPort(NVG.Settings.Network, NVG.Settings.Layer);
            }
            return GetNetworkPort(objSetting.Network, objSetting.Layer);
        }
        public static int GetNetworkPort(Notus.Variable.Enum.NetworkType currentNetwork, Notus.Variable.Enum.NetworkLayer currentLayer)
        {
            return Notus.Variable.Constant.PortNo[currentLayer][currentNetwork];
        }
        public static string MakeHttpListenerPath(string IpAddress, int PortNo = 0, bool UseSSL = false)
        {
            if (PortNo == 0)
            {
                return "http" + (UseSSL == true ? "s" : "") + "://" + IpAddress + "/";
            }
            return "http" + (UseSSL == true ? "s" : "") + "://" + IpAddress + ":" + PortNo.ToString() + "/";
        }
    }
}
