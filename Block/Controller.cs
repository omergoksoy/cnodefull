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
        private int timerInterval = 3000;
        private bool timerRunning = false;
        private Notus.Threads.Timer UtcTimerObj = new Notus.Threads.Timer();

        private void PrintError(long rownNo,string errorText)
        {
            Console.WriteLine(errorText + " : " + rownNo.ToString());
            Console.WriteLine(errorText + " : " + rownNo.ToString());
            Console.WriteLine(errorText + " : " + rownNo.ToString());
            Environment.Exit(0);
        }
        private void ControlBlock(long rownNo)
        {
            string currentBlockUid = NVG.BlockMeta.Order(rownNo);
            string currentBlockSign = NVG.BlockMeta.Sign(rownNo);
            string currentBlockPrev = NVG.BlockMeta.Prev(rownNo);
            var currentBlockData = NVG.BlockMeta.ReadBlock(currentBlockUid);

            Console.WriteLine("currentBlockUid : " + currentBlockUid);
            Console.WriteLine("blockSign : "  + currentBlockSign);
            Console.WriteLine("blockPrev : " + currentBlockPrev);


            if (currentBlockData == null)
            {
                PrintError(rownNo, "BLOK EKSIK");
                Environment.Exit(0);
            }

            if(string.Equals(currentBlockData.sign, currentBlockSign) == false)
            {
                PrintError(rownNo, "SIGN HATALI");
            }

            if(string.Equals(currentBlockData.prev, currentBlockPrev) == false)
            {
                PrintError(rownNo, "PREV HATALI");
            }

            if(string.Equals(currentBlockData.info.uID, currentBlockUid) == false)
            {
                PrintError(rownNo, "BLOK UID HATALI");
            }

            if (rownNo < 2)
            {
                Console.WriteLine("İlk Blok oldugu için timer'a geri dönüyor.");
                return;
            }
            long nextRownNo = rownNo + 1;
            string nextBlockUid = NVG.BlockMeta.Order(nextRownNo);
            string nextBlockSign = NVG.BlockMeta.Sign(nextRownNo);
            string nextBlockPrev = NVG.BlockMeta.Prev(nextRownNo);
            var nextBlockData = NVG.BlockMeta.ReadBlock(nextBlockUid);

            Console.WriteLine("nextBlockUid : " + nextBlockUid);
            Console.WriteLine("blockSign : " + nextBlockSign);
            Console.WriteLine("blockPrev : " + nextBlockPrev);


            if (string.Equals(nextBlockData.sign, nextBlockSign) == false)
            {
                PrintError(rownNo, "SIGN HATALI");
            }

            if (string.Equals(nextBlockData.prev, nextBlockPrev) == false)
            {
                PrintError(rownNo, "PREV HATALI");
            }

            if (string.Equals(nextBlockData.info.uID, nextBlockUid) == false)
            {
                PrintError(rownNo, "BLOK UID HATALI");
            }


            

            if (string.Equals(nextBlockPrev, currentBlockUid + currentBlockSign) == false)
            {
                PrintError(nextRownNo, "NEXT BLOK PREV HATALI");
            }

            
            // string currentBlockUid = NVG.BlockMeta.Order(rownNo);
            // string currentBlockSign = NVG.BlockMeta.Sign(rownNo);
            // string currentBlockPrev = NVG.BlockMeta.Prev(rownNo);
            // var currentBlockData = NVG.BlockMeta.ReadBlock(currentBlockUid);

            // string nextBlockUid = NVG.BlockMeta.Order(nextRownNo);
            // string nextBlockSign = NVG.BlockMeta.Sign(nextRownNo);
            // string nextBlockPrev = NVG.BlockMeta.Prev(nextRownNo);
            // var nextBlockData = NVG.BlockMeta.ReadBlock(nextBlockUid);



            //Console.WriteLine("blockData : " + JsonSerializer.Serialize(blockData));
            // Environment.Exit(0);
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
                    if(currentBlockNo> LastBlockRowNo)
                    {
                        ControlBlock(currentBlockNo);
                        currentBlockNo++;
                    }
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
