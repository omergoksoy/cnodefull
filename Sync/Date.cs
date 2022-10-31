using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;

namespace Notus.Sync
{
    public class Date : IDisposable
    {
        private bool timerRunning = false;
        private int counter = 0;
        private Notus.Threads.Timer? UtcTimerObj;
        public void Start()
        {
            NP.Success(NVG.Settings, "NTP Time Synchronizer Has Started");
            UtcTimerObj = new Notus.Threads.Timer(5000);
            UtcTimerObj.Start(() =>
            {
                if (timerRunning == false)
                {
                    timerRunning = true;
                    if (NVG.NOW.DiffUpdated == true)
                    {
                        counter++;
                        if (counter > 60)
                        {
                            Console.WriteLine("BEfore update ntp : " + NVG.NOW.LastDiffUpdate.ToString("HH mm ss fff"));
                            ulong lastUpdateTime = ND.ToLong(NVG.NOW.LastDiffUpdate);
                            bool updateNtpTimeDone = false;
                            while (updateNtpTimeDone == false)
                            {
                                NGF.KillTimeSync(false);
                                NGF.StartTimeSync();
                                if (ND.ToLong(NVG.NOW.LastDiffUpdate) > lastUpdateTime)
                                {
                                    updateNtpTimeDone = true;
                                }
                                else
                                {
                                    Thread.Sleep(5000);
                                }
                            }
                            Console.WriteLine("after refresh ntp : " + NVG.NOW.LastDiffUpdate.ToString("HH mm ss fff"));
                        }
                    }
                    timerRunning = false;
                }
            }, true);  //TimerObj.Start(() =>            
        }
        public Date()
        {
        }
        ~Date()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (UtcTimerObj != null)
            {
                UtcTimerObj.Dispose();
            }
        }
    }
}
