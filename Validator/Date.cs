using System;
using System.Text.Json;
using NVC = Notus.Variable.Constant;
using NP = Notus.Print;
namespace Notus.Validator
{
    public static class Date
    {
        public static ulong Calculate(ulong howManyTimes=1)
        {
            ulong queueTimePeriod = NVC.BlockListeningForPoolTime + NVC.BlockGeneratingTime + NVC.BlockDistributingTime;
            if (howManyTimes == 1) {
                return queueTimePeriod;
            }
            return (queueTimePeriod * howManyTimes);
        }
    }
}