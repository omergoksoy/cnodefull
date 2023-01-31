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

        private void ControlBlock(long rownNo)
        {
            string blockUid = NVG.BlockMeta.Order(rownNo);
            string blockSign = NVG.BlockMeta.Sign(rownNo);
            string blockPrev = NVG.BlockMeta.Prev(rownNo);

            Console.WriteLine("blockUid : " + blockUid);
            Console.WriteLine("blockSign : "  +blockSign);
            Console.WriteLine("blockPrev : " + blockPrev);

            var blockData = NVG.BlockMeta.ReadBlock(blockUid);
            Console.WriteLine("blockData : " + JsonSerializer.Serialize(blockData));
            Environment.Exit(0);
        }
        public void Start()
        {
            if (NVG.Settings.LocalNode == true)
                return;

            Console.WriteLine("Last Block Number : " + LastBlockRowNo.ToString());
            UtcTimerObj.Start(timerInterval, () =>
            {
                if(timerRunning==false)
                {
                    timerRunning = true;
                    ControlBlock(currentBlockNo);
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
