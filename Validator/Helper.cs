using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NVG = Notus.Variable.Globals;
using NVC = Notus.Variable.Constant;
using ND = Notus.Date;
namespace Notus.Validator
{
    public static class Helper
    {
        public static void CheckBlockAndEmptyCounter(int blockTypeNo)
        {
            if (blockTypeNo == 300)
            {
                NVG.Settings.EmptyBlockCount++;
                NVG.Settings.OtherBlockCount = 0;
            }
            else
            {
                NVG.Settings.OtherBlockCount++;
                NVG.Settings.EmptyBlockCount = 0;
            }
        }
        public static bool RightBlockValidator(Notus.Variable.Class.BlockData incomeBlock)
        {
            ulong queueTimePeriod = (ulong)(NVC.BlockListeningForPoolTime + NVC.BlockGeneratingTime + NVC.BlockDistributingTime);
            ulong blockTimeVal = ND.ToLong(incomeBlock.info.time);
            ulong blockGenarationTime = blockTimeVal - (blockTimeVal % queueTimePeriod);

            if (NVG.Settings.Nodes.Queue.ContainsKey(blockGenarationTime) == true)
            {
                string blockValidator = incomeBlock.validator.count.First().Key;
                if (string.Equals(blockValidator, NVG.Settings.Nodes.Queue[blockGenarationTime].Wallet))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
