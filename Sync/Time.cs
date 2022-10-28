using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Notus.Communication;
using NVG = Notus.Variable.Globals;
using NP = Notus.Print;
//using NT = Notus.Time;
using NVS = Notus.Variable.Struct;
using NGF = Notus.Variable.Globals.Functions;
namespace Notus.Sync
{
    public class Time : IDisposable
    {
        private double TimeShift;
        private DateTime LocalUtcTime;
        private UDP serverObj;
        private UDP joinObj;
        private Notus.Threads.Timer? UtcTimerObj;
        public void Start(int portNo, int joinPortNo)
        {
            UpdateUtcTimeTimerFunc();

            joinObj = new UDP(joinPortNo);
            joinObj.OnReceive((incomeTime, incomeText) =>
            {
                Console.WriteLine("Income Text : " + incomeText);
            });

            serverObj = new UDP(portNo);
            serverObj.OnReceive((incomeTime, incomeText) =>
            {
                Console.WriteLine("Income Text : " + incomeText);
            });
            //s.Server("127.0.0.1", 27000, true);
        }
        private void UpdateUtcTimeTimerFunc()
        {
            UtcTimerObj = new Notus.Threads.Timer(1);
            UtcTimerObj.Start(() =>
            {
                LocalUtcTime = DateTime.UtcNow;
                //NVG.NOW.Obj
                //RefreshNtpTime();
                /*
                NVG.NOW.Int = NT.DateTimeToUlong(NVG.NOW.Obj);
                NVG.NOW.Int = NT.DateTimeToUlong(Settings.UTCTime.Now);
                */
                if (int.Parse(LocalUtcTime.ToString("fff")) % 100 == 0)
                {
                    try
                    {
                        int onlineNodeCount = 0;
                        foreach (KeyValuePair<string, NVS.NodeQueueInfo> entry in NVG.NodeList)
                        {
                            if (entry.Value.Status == NVS.NodeStatus.Online)
                            {
                                onlineNodeCount++;
                            }
                        }
                        NVG.OnlineNodeCount = onlineNodeCount;
                    }
                    catch { }
                }
            }, true);  //TimerObj.Start(() =>
        }
        public Time()
        {
            TimeShift = 0;
            NP.Success(NVG.Settings, "Time Synchronizer Has Started");
        }
        ~Time()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (serverObj != null)
            {
                serverObj = null;
            }
            if (UtcTimerObj != null)
            {
                UtcTimerObj.Dispose();
            }
        }

    }
}
