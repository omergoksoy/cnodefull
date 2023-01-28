using System;
using System.Text.Json;
using NVClass = Notus.Variable.Class;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Pool
{
    public static class Sharing
    {
        public static void Distribute(NVS.HttpRequestDetails IncomeData, bool ToDistribute)
        {
            if (ToDistribute == false)
                return;

            string requestUidText = "<requestId>" + IncomeData.RequestUid + "</requestId>";
            string poolMsgText = "<poolData>" + JsonSerializer.Serialize(IncomeData) + "</poolData>";
            Console.WriteLine(requestUidText);
            Console.WriteLine(poolMsgText);

            foreach (var validatorItem in NVG.NodeList)
            {
                NVG.Settings.PeerManager.SendWithTask(validatorItem.Value, requestUidText);
            }

            foreach (var validatorItem in NVG.NodeList)
            {
                NVG.Settings.PeerManager.SendWithTask(validatorItem.Value, poolMsgText);
            }
        }
    }
}
