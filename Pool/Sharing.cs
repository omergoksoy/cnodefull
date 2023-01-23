﻿using NVG = Notus.Variable.Globals;
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
            dağıtım esnasında diğer node'a gidince oda aynı işemi hemen oluşturuyor ve bu sebepten ötürüde 
            aynı işlem 2 kere oluşuyor

            Console.WriteLine("******************* Execute Distribute *******************");
            return;
            string poolMsgText = "<poolData>" + JsonSerializer.Serialize(IncomeData) + "</poolData>";
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
