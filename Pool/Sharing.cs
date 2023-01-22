using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
using System;
using System.Text.Json;
using NVClass = Notus.Variable.Class;
namespace Notus.Pool
{
    public static class Sharing
    {
        public static void Distribute(NVS.HttpRequestDetails IncomeData)
        {
<<<<<<< HEAD
            if (ToDistribute == false)
                return;

            string poolMsgText = "<poolData>" + JsonSerializer.Serialize(IncomeData) + "</poolData>";
=======
            string incomeDataStr = JsonSerializer.Serialize(IncomeData);
            Console.WriteLine(incomeDataStr);
            string poolMsgText = "<poolData>" + incomeDataStr + "</poolData>";
>>>>>>> parent of 77329b3 (mem pool sağıtımı için test uygulamaları başladı)
            foreach (var validatorItem in NVG.NodeList)
            {
                if (validatorItem.Value.Status == NVS.NodeStatus.Online)
                {
                    if (string.Equals(validatorItem.Value.IP.Wallet, NVG.Settings.Nodes.My.IP.Wallet) == false)
                    {
                        Task.Run(() =>
                        {
                            NVG.Settings.PeerManager.Send(validatorItem.Value.IP.Wallet, poolMsgText);
                        });
                    }
                }
            }
        }
    }
}
