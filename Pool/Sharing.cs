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

            //string requestUidText = "<requestId>" + IncomeData.RequestUid + "</requestId>";
            string poolMsgText = "<poolData>" + JsonSerializer.Serialize(IncomeData) + "</poolData>";
            //Console.WriteLine(requestUidText);
            Console.WriteLine(poolMsgText);
            //return;
            foreach (var validatorItem in NVG.NodeList)
            {
                if (validatorItem.Value.Status == NVS.NodeStatus.Online)
                {
                    if (string.Equals(validatorItem.Value.IP.Wallet, NVG.Settings.Nodes.My.IP.Wallet) == false)
                    {
                        Task.Run(() =>
                        {
                            NVG.Settings.PeerManager.Send(validatorItem.Value.IP.Wallet, poolMsgText);

                            /*
                            bool requestSendStatus = NVG.Settings.PeerManager.Send(validatorItem.Value.IP.Wallet, requestUidText);
                            if (requestSendStatus == true)
                            {
                            }
                            */
                        });
                    }
                }
            }
        }
    }
}
