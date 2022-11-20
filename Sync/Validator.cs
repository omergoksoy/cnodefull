using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Sync
{
    public class Validator : IDisposable
    {
        private bool countTimerRunning = false;
        private bool enoughPrinted = false;
        private bool notEnoughPrinted = false;
        private Notus.Threads.Timer? ValidatorCountTimerObj;
        public Validator()
        {
            NP.Success(NVG.Settings, "Validator Count Sync Has Started");
        }
        ~Validator()
        {
            Dispose();
        }
        public void Start()
        {
            ValidatorCountTimerObj = new Notus.Threads.Timer(5);
            ValidatorCountTimerObj.Start(() =>
            {
                if (countTimerRunning == false)
                {
                    countTimerRunning = true;
                    if (NVG.NodeList != null)
                    {
                        KeyValuePair<string, NVS.NodeQueueInfo>[]? nList = NVG.NodeList.ToArray();
                        if (nList != null)
                        {
                            int onlineNodeCount = 0;
                            for (int i = 0; i < nList.Length; i++)
                            {
                                if (nList[i].Value.Status == NVS.NodeStatus.Online)
                                {
                                    onlineNodeCount++;
                                }
                            }
                            /*
                            if (onlineNodeCount == 2)
                            {
                                Console.WriteLine(JsonSerializer.Serialize(NVG.NodeList));
                                NP.ReadLine();
                            }
                            */
                            NVG.OnlineNodeCount = onlineNodeCount;
                            if (Notus.Variable.Constant.MinimumNodeCount >= NVG.OnlineNodeCount)
                            {
                                if (enoughPrinted == false)
                                {
                                    NP.Success("Enough NodeCount For Executing");
                                    enoughPrinted = true;
                                    notEnoughPrinted = false;
                                }
                            }
                            else
                            {
                                if (enoughPrinted == true)
                                {
                                    if (notEnoughPrinted == false)
                                    {
                                        NP.Success("Not Enough NodeCount For Executing");
                                        foreach (var iE in NVG.NodeList)
                                        {
                                            if (iE.Value.Status == NVS.NodeStatus.Offline)
                                            {
                                                Console.WriteLine(iE.Value.IP.IpAddress + " -> OFFLINE");
                                            }
                                            if (iE.Value.Status == NVS.NodeStatus.Unknown)
                                            {
                                                Console.WriteLine(iE.Value.IP.IpAddress + " -> UNKNOWN");
                                            }
                                            if (iE.Value.Status == NVS.NodeStatus.Error)
                                            {
                                                Console.WriteLine(iE.Value.IP.IpAddress + " -> ERROR");
                                            }
                                        }
                                        notEnoughPrinted = true;
                                    }
                                    enoughPrinted = false;
                                }
                            }
                        }
                    }
                    countTimerRunning = false;
                }
            }, true);  //TimerObj.Start(() =>
        }
        public void Dispose()
        {
            if (ValidatorCountTimerObj != null)
            {
                ValidatorCountTimerObj.Dispose();
            }
        }
    }
}
