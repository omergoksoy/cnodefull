using System.Collections.Concurrent;
using System.Text.Json;
using NCH = Notus.Communication.Helper;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NGV = Notus.Globals.Variable;
using NT = Notus.Threads;
using NVC = Notus.Variable.Constant;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;

namespace Notus
{
    public static class Print
    {
        private static bool WaitDotUsed = false;
        private static bool SubTimerIsRunning = false;
        private static NT.Timer SubTimer = new NT.Timer(100);
        public static ConcurrentQueue<NGV.PrintQueueList> TextList = new();
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
                        if (TextList.TryDequeue(out NGV.PrintQueueList item))
                        {
                            if (item != null)
                            {
                                if (item.Dot == false)
                                {
                                    if (WaitDotUsed == true)
                                    {
                                        Console.WriteLine();
                                        WaitDotUsed = false;
                                    }
                                    PrintFunction(item.Layer, item.Type, item.Color, item.Text);
                                }
                            }
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
            TextList.Enqueue(new NGV.PrintQueueList()
            {
                Dot = false,
                Text = DetailsStr,
                Color = TextColor,
                Layer = tmpLayer,
                Type = tmpType
            });
        }
    }
}