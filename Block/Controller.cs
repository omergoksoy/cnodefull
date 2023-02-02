using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Block
{
    public class Controller : IDisposable
    {
        public long LastBlockRowNo = 1;
        private long currentBlockNo = 1;
        private int timerInterval = 250;
        private bool timerRunning = false;
        private Notus.Threads.Timer UtcTimerObj = new Notus.Threads.Timer();

        private void PrintError(long rownNo, string errorText, bool isNextBlock)
        {
            Console.WriteLine("******************************************************");
            Console.WriteLine(errorText + " : " + rownNo.ToString());
            Console.WriteLine(errorText + " : " + rownNo.ToString());
            Console.WriteLine(errorText + " : " + rownNo.ToString());

            string currentBlockUid = NVG.BlockMeta.Order(rownNo);
            string currentBlockSign = NVG.BlockMeta.Sign(rownNo);
            string currentBlockPrev = NVG.BlockMeta.Prev(rownNo);
            var currentBlockData = NVG.BlockMeta.ReadBlock(currentBlockUid);

            Console.WriteLine("------------------------------------------------------------------------");
            Console.WriteLine("currentBlockUid : " + currentBlockUid);
            Console.WriteLine("blockSign : " + currentBlockSign);
            Console.WriteLine("blockPrev : " + currentBlockPrev);
            Console.WriteLine("blockData : " + JsonSerializer.Serialize(currentBlockData));
            if (isNextBlock == true)
            {
                Console.WriteLine("========================================================================");
                long nextRownNo = rownNo + 1;
                string nextBlockUid = NVG.BlockMeta.Order(nextRownNo);
                string nextBlockSign = NVG.BlockMeta.Sign(nextRownNo);
                string nextBlockPrev = NVG.BlockMeta.Prev(nextRownNo);
                var nextBlockData = NVG.BlockMeta.ReadBlock(nextBlockUid);

                Console.WriteLine("nextBlockUid : " + nextBlockUid);
                Console.WriteLine("nextBlockSign : " + nextBlockSign);
                Console.WriteLine("nextBlockPrev : " + nextBlockPrev);
                Console.WriteLine("nextBlockData : " + JsonSerializer.Serialize(nextBlockData));

            }

            Console.WriteLine("::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::");
            Environment.Exit(0);
        }
        private void ControlBlock(long rowNo)
        {
            //Console.WriteLine(rownNo.ToString() + " Numaralı Blok Kontrol Edildi.");

            string currentBlockUid = NVG.BlockMeta.Order(rowNo);
            string currentBlockSign = NVG.BlockMeta.Sign(rowNo);
            string currentBlockPrev = NVG.BlockMeta.Prev(rowNo);
            var currentBlockData = NVG.BlockMeta.ReadBlock(currentBlockUid);

            if (currentBlockData == null)
            {
                PrintError(rowNo, "BLOK EKSIK", false);
                Environment.Exit(0);
            }

            if (string.Equals(currentBlockData.sign, currentBlockSign) == false)
            {
                PrintError(rowNo, "SIGN HATALI", false);
            }

            if (string.Equals(currentBlockData.prev, currentBlockPrev) == false)
            {
                PrintError(rowNo, "PREV HATALI", false);
            }

            if (string.Equals(currentBlockData.info.uID, currentBlockUid) == false)
            {
                PrintError(rowNo, "BLOK UID HATALI", false);
            }

            if (rowNo < 2)
            {
                Console.WriteLine("İlk Blok oldugu için timer'a geri dönüyor.");
                return;
            }
            long nextRowNo = rowNo + 1;
            string nextBlockUid = NVG.BlockMeta.Order(nextRowNo);
            string nextBlockSign = NVG.BlockMeta.Sign(nextRowNo);
            string nextBlockPrev = NVG.BlockMeta.Prev(nextRowNo);
            var nextBlockData = NVG.BlockMeta.ReadBlock(nextBlockUid);

            // Console.WriteLine("nextBlockUid : " + nextBlockUid);
            // Console.WriteLine("blockSign : " + nextBlockSign);
            // Console.WriteLine("blockPrev : " + nextBlockPrev);

            if (string.Equals(nextBlockData.sign, nextBlockSign) == false)
            {
                PrintError(rowNo, "SIGN HATALI", true);
            }

            if (string.Equals(nextBlockData.prev, nextBlockPrev) == false)
            {
                PrintError(rowNo, "PREV HATALI", true);
            }

            if (string.Equals(nextBlockData.info.uID, nextBlockUid) == false)
            {
                PrintError(rowNo, "BLOK UID HATALI", true);
            }

            if (string.Equals(nextBlockPrev, currentBlockUid + currentBlockSign) == false)
            {
                PrintError(nextRowNo, "NEXT BLOK PREV HATALI", true);
            }

            long modNo = rowNo % NVC.NodeValidationModCount;
            if (modNo == 0)
            {
                NVG.BlockMeta.State(
                    NVG.Settings.Nodes.My.ChainId,
                    new NVS.NodeStateStruct()
                    {
                        rowNo = rowNo,
                        blockUid = currentBlockUid,
                        sign = currentBlockSign
                    }
                );
                CheckAllNodeState(rowNo);
            }


            // string currentBlockUid = NVG.BlockMeta.Order(rownNo);
            // string currentBlockSign = NVG.BlockMeta.Sign(rownNo);
            // string currentBlockPrev = NVG.BlockMeta.Prev(rownNo);
            // var currentBlockData = NVG.BlockMeta.ReadBlock(currentBlockUid);

            // string nextBlockUid = NVG.BlockMeta.Order(nextRownNo);
            // string nextBlockSign = NVG.BlockMeta.Sign(nextRownNo);
            // string nextBlockPrev = NVG.BlockMeta.Prev(nextRownNo);
            // var nextBlockData = NVG.BlockMeta.ReadBlock(nextBlockUid);


            // blok kontrolu yapıldıktan sonra her 100 blokta bir diğer node ile karşılıklı kontrol yapılacak...
            // blok kontrolu yapıldıktan sonra her 100 blokta bir diğer node ile karşılıklı kontrol yapılacak...
            // blok kontrolu yapıldıktan sonra her 100 blokta bir diğer node ile karşılıklı kontrol yapılacak...
            //Console.WriteLine("blockData : " + JsonSerializer.Serialize(blockData));
            // Environment.Exit(0);
        }
        public void CheckAllNodeState(long rownNo)
        {
            foreach (var validatorItem in NVG.NodeList)
            {
                var nodeState = NVG.BlockMeta.State(
                    NVG.BlockMeta.GetStateKey(
                        validatorItem.Value.ChainId,
                        validatorItem.Value.State.rowNo
                    )
                );
                Console.WriteLine(
                   "s : " + validatorItem.Key + " -> " +
                    JsonSerializer.Serialize(nodeState)
                );
            }
        }
        public void Start()
        {
            if (NVG.Settings.LocalNode == true)
                return;

            Console.WriteLine("Last Block Number : " + LastBlockRowNo.ToString());
            UtcTimerObj.Start(timerInterval, () =>
            {
                if (timerRunning == false)
                {
                    timerRunning = true;
                    if (LastBlockRowNo > currentBlockNo)
                    {
                        ControlBlock(currentBlockNo);
                        currentBlockNo++;
                        if (Math.Abs(LastBlockRowNo - currentBlockNo) > 10)
                        {
                            UtcTimerObj.SetInterval(250);
                        }
                    }
                    else
                    {
                        UtcTimerObj.SetInterval(3000);
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
