using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Block
{
    public class Controller : IDisposable
    {
        public long LastBlockRowNo= 1;
        private long currentBlockNo= 1;
        private int timerInterval = 1000;
        private bool timerRunning = false;
        private Notus.Threads.Timer UtcTimerObj = new Notus.Threads.Timer();

        public void Start()
        {
            Console.WriteLine("Last Block Number : " + NVG.BlockController.LastBlockRowNo.ToString());
            if (NVG.Settings.LocalNode == true)
                return;
            NP.Success(NVG.Settings, "Block Controller Timer Has Started");
            UtcTimerObj.Start(timerInterval, () =>
            {
                if(timerRunning==false)
                {
                    timerRunning = true;
                    var currentBlock=NVG.BlockMeta.ReadBlock(currentBlockNo);


                    timerRunning = false;
                }
            }, true);  //TimerObj.Start(() =>            
        }

        public Controller()
        {
        }
        ~Controller()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                UtcTimerObj.Dispose();
            }
            catch { }
        }
    }
}
