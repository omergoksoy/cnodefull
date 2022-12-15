using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
using NTN = Notus.Toolbox.Network;
namespace Notus.Network
{
    public class Ping : IDisposable
    {
        private bool TimerRunning;
        private Notus.Threads.Timer TimerObj;
        public void Start()
        {
            TimerObj.Start(2000, () =>
            {
                if (TimerRunning == false)
                {
                    TimerRunning = true;
                    KeyValuePair<string, NVS.NodeQueueInfo>[]? nList = NVG.NodeList.ToArray();
                    if (nList != null)
                    {
                        for (int count = 0; count < nList.Length; count++)
                        {
                            if (string.Equals(nList[count].Value.IP.Wallet, NVG.Settings.Nodes.My.IP.Wallet) == false)
                            {
                                var nodeStatus = NTN.PingToNode(nList[count].Value.IP.IpAddress, nList[count].Value.IP.Port);
                                if (nodeStatus == NVS.NodeStatus.Offline)
                                {
                                    Console.WriteLine("Offline : " + nList[count].Value.IP.IpAddress + ":" + nList[count].Value.IP.Port);
                                    NVG.NodeList[nList[count].Key].Status = NVS.NodeStatus.Offline;
                                }
                            }
                        }
                    }
                    TimerRunning = false;
                }
            }, true);

        }

        public Ping()
        {
            TimerRunning = false;
        }
        ~Ping()
        {

        }
        public void Dispose()
        {

        }
    }
}
