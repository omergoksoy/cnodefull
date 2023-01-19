﻿using System.Collections.Concurrent;
using System.Text.Json;
using ND = Notus.Date;
using NGV = Notus.Globals.Variable;
using NT = Notus.Threads;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
using NVC = Notus.Variable.Constant;
using NGF = Notus.Variable.Globals.Functions;
using NCH = Notus.Communication.Helper;

namespace Notus
{
    public static class Print
    {
        private static bool WaitDotUsed = false;
        private static bool SubTimerIsRunning = false;
        private static int Counter = 0;
        private static NT.Timer SubTimer = new NT.Timer(100);
        public static ConcurrentDictionary<int, NGV.PrintQueueList> TextList = new ConcurrentDictionary<int, NGV.PrintQueueList>();
        public static void Log(
            NVE.LogLevel logType,
            int logNo,
            string messageText,
            string blockRowNo,
            Notus.Globals.Variable.Settings? objSettings,
            Exception? objException
        )
        {
            NVS.LogStruct logObject = new NVS.LogStruct()
            {
                BlockRowNo = blockRowNo,
                LogNo = logNo,
                LogType = logType,
                Message = messageText,
                WalletKey = "",
                StackTrace = "",
                ExceptionType = ""
            };
            if (objSettings != null)
            {
                if (objSettings.NodeWallet != null)
                {
                    logObject.WalletKey = objSettings.NodeWallet.WalletKey;
                }
            }
            if (objException != null)
            {
                if (objException.StackTrace != null)
                {
                    logObject.StackTrace = objException.StackTrace;
                }
            }

            (bool _, string _) = Notus.Communication.Request.PostSync(
                "http://3.121.218.78:3000/log",
                new Dictionary<string, string>()
                {
                    { "data", JsonSerializer.Serialize(logObject) }
                }, 0, true, true
            );
        }
        public static void PrintQueue(bool tellTheOtherNode, string printLoc)
        {
            if (tellTheOtherNode == true)
            {
                foreach (var iE in NGF.ValidatorList)
                {
                    if (string.Equals(iE.Key, NVG.Settings.Nodes.My.HexKey) == false)
                    {
                        NCH.SendMessageED(iE.Key, iE.Value, "<pQueue>1</pQueue>");
                    }
                }
            }
            /*
            Console.WriteLine("******************************************");
            Console.WriteLine(printLoc);
            Console.WriteLine("******************************************");
            Console.WriteLine("NVG.Settings.PeerManager.Old");
            Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.PeerManager.Old, NVC.JsonSetting));

            Console.WriteLine("NVG.Settings.PeerManager.Now");
            Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.PeerManager.Now, NVC.JsonSetting));

            Console.WriteLine("NVG.Settings.PeerManager.Next");
            Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.PeerManager.Next, NVC.JsonSetting));
            Console.WriteLine(JsonSerializer.Serialize(NVG.Settings.Nodes.Queue));
            */

            /*
            Console.WriteLine("Environment.Exit(0);");

            Thread.Sleep(2500);
            Environment.Exit(0);
            */
        }
        public static void MainClassClosingControl()
        {
            if (NVG.Settings.NodeClosing == true)
            {
                return;
            }
            if (NVG.Settings.GenesisCreated == true)
            {
                Warning("Main Class Temporary Ended");
            }
            else
            {
                Warning("Main Class Ended");
            }
        }
        public static void NodeCount(string functionClassName)
        {
            Info("Node Count : " + NVG.OnlineNodeCount.ToString() + " / " + NVG.NodeList.Count.ToString());
        }
        public static void ReadLine()
        {
            ReadLine(NVG.Settings);
        }
        public static void ReadLine(Notus.Globals.Variable.Settings NodeSettings)
        {
            Info(NodeSettings, "Press Enter To Continue");
            Console.ReadLine();
        }
        public static void WaitDot(string waitText = "+")
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(waitText);
            WaitDotUsed = true;
        }
        public static void Info(string DetailsStr = "")
        {
            Info(NVG.Settings, DetailsStr);
        }
        public static void Info(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "")
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.Cyan, DetailsStr);
        }
        public static void Danger(string DetailsStr = "")
        {
            Danger(NVG.Settings, DetailsStr);
        }
        public static void Danger(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "")
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.DebugMode, ConsoleColor.Red, DetailsStr);
        }
        public static void Warning(string DetailsStr = "")
        {
            Warning(NVG.Settings, DetailsStr);
        }
        public static void Warning(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "")
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.Yellow, DetailsStr);
        }
        public static void Status(string DetailsStr = "")
        {
            Status(NVG.Settings, DetailsStr);
        }
        public static void Status(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "")
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.White, DetailsStr);
        }
        public static void Basic(string DetailsStr = "")
        {
            Basic(NVG.Settings, DetailsStr);
        }
        public static void Basic(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "")
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.Gray, DetailsStr);
        }
        public static void Success(string DetailsStr = "")
        {
            Success(NVG.Settings, DetailsStr);
        }
        public static void Success(Notus.Globals.Variable.Settings NodeSettings, string DetailsStr = "")
        {
            subPrint(NodeSettings.Layer, NodeSettings.Network, NodeSettings.InfoMode, ConsoleColor.DarkGreen, DetailsStr);
        }
        public static void Danger(bool ShowOnScreen, string DetailsStr = "")
        {
            subPrint(NVE.NetworkLayer.Unknown, NVE.NetworkType.Unknown, ShowOnScreen, ConsoleColor.Red, DetailsStr);
        }
        public static void Basic(bool ShowOnScreen, string DetailsStr = "")
        {
            subPrint(NVE.NetworkLayer.Unknown, NVE.NetworkType.Unknown, ShowOnScreen, ConsoleColor.Gray, DetailsStr);
        }

        private static void PrintFunction(
            NVE.NetworkLayer tmpLayer,
            NVE.NetworkType tmpType,
            ConsoleColor TextColor,
            string DetailsStr
        )
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(ND.NowObj().ToString("HH:mm:ss.fff"));
            if (tmpLayer != NVE.NetworkLayer.Unknown && tmpType != NVE.NetworkType.Unknown)
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                if (tmpLayer == NVE.NetworkLayer.Layer1)
                    Console.Write(" L1");
                if (tmpLayer == NVE.NetworkLayer.Layer2)
                    Console.Write(" L2");
                if (tmpLayer == NVE.NetworkLayer.Layer3)
                    Console.Write(" L3");
                if (tmpLayer == NVE.NetworkLayer.Layer4)
                    Console.Write(" L4");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("-");
                Console.ForegroundColor = ConsoleColor.Magenta;
                if (tmpType == NVE.NetworkType.DevNet)
                    Console.Write("Dev ");
                if (tmpType == NVE.NetworkType.MainNet)
                    Console.Write("Main");
                if (tmpType == NVE.NetworkType.TestNet)
                    Console.Write("Test");
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" -> ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = TextColor;
            Console.WriteLine(DetailsStr);
        }
        public static void PrintOnScreenTimer()
        {
            SubTimer.Start(() =>
            {
                if (SubTimerIsRunning == false)
                {
                    SubTimerIsRunning = true;
                    if (TextList.Count > 0)
                    {
                        var item = TextList.First();

                        if (item.Value.Dot == false)
                        {
                            if (WaitDotUsed == true)
                            {
                                Console.WriteLine();
                                WaitDotUsed = false;
                            }
                            PrintFunction(item.Value.Layer, item.Value.Type, item.Value.Color, item.Value.Text);
                            TextList.TryRemove(item.Key, out _);
                        }
                    }
                    SubTimerIsRunning = false;
                }
            });
        }
        private static void subPrint(
            NVE.NetworkLayer tmpLayer,
            NVE.NetworkType tmpType,
            bool ShowOnScreen,
            ConsoleColor TextColor,
            string DetailsStr
        )
        {
            if (ShowOnScreen == false)
                return;

            if (DetailsStr.Length == 0)
                return;

            TextList.TryAdd(Counter, new NGV.PrintQueueList()
            {
                Dot = false,
                Text = DetailsStr,
                Color = TextColor,
                Layer = tmpLayer,
                Type = tmpType
            });
            Counter++;
        }
    }
}