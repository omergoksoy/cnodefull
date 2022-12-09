using System;
using System.Text.Json;
using System.Threading;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVD = Notus.Validator.Date;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVR = Notus.Validator.Register;
using NVS = Notus.Variable.Struct;
namespace Notus.Validator
{
    public static class Join
    {
        public static void TellTheNodeToJoinTime(ulong CurrentQueueTime)
        {
            if (NVR.NetworkSelectorList.Count == 0)
                return;

            KeyValuePair<string, string> firstNode = NVR.NetworkSelectorList.First();

            if (NVR.ReadyMessageFromNode.ContainsKey(firstNode.Key) == false)
                return;

            ulong queueTimePeriod = NVD.Calculate();
            NP.Info("This Node Ready For Join The Network : " + firstNode.Key);

            if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, firstNode.Value) == false)
            {
                NP.Info("Node Will Allow The Node -> " + firstNode.Value);
                return;
            }

            ulong tmpQueueTime = ND.AddMiliseconds(CurrentQueueTime, queueTimePeriod * 3);

            ulong tmpJoinTime = ND.AddMiliseconds(
                tmpQueueTime,
                queueTimePeriod * (ulong)(NVC.NodeOrderGroupSize * 10)
            );
            /*
            hatanın olduğu nokta
            üçüncü node ağa dahil olduğu anda kendisine blok iletilemiyor
            oluşturulan blok iletilemeyince 
                hatalı blok numarası ile kendisi başka bir blok üretiyor.

            üçüncü node oluşturduğu bloğu diğer nodelara iletemiyor...
            */

            NP.Info("I Will Allow The Node");
            Task.Run(() =>
            {
                NVH.TellToNetworkNewNodeJoinTime(firstNode.Key, tmpJoinTime);
            });
            NVR.ReadyMessageFromNode.Remove(firstNode.Key);
        }
    }
}