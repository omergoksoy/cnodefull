using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using NTN = Notus.Toolbox.Network;
using NVH = Notus.Validator.Helper;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
using NP = Notus.Print;
using NGF = Notus.Variable.Globals.Functions;
namespace Notus.Network
{
    public class Ping : IDisposable
    {
        private ConcurrentDictionary<string, int> ErrorCount = new();
        private bool TimerRunning;
        private Notus.Threads.Timer TimerObj = new();
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
                        List<string> removeList = new();
                        for (int count = 0; count < nList.Length; count++)
                        {
                            if (string.Equals(nList[count].Value.IP.Wallet, NVG.Settings.Nodes.My.IP.Wallet) == false)
                            {
                                var nodeStatus = NTN.PingToNode(nList[count].Value.IP.IpAddress, nList[count].Value.IP.Port);
                                if (nodeStatus == NVS.NodeStatus.Offline)
                                {
                                    if (ErrorCount.ContainsKey(nList[count].Key))
                                    {
                                        ErrorCount[nList[count].Key]++;
                                        if (ErrorCount[nList[count].Key] >= NVC.NodePingErrorLimit)
                                        {
                                            NGF.SetNodeOffline(nList[count].Key);
                                            removeList.Add(nList[count].Key);
                                        }
                                    }
                                    else
                                    {
                                        ErrorCount.TryAdd(nList[count].Key, 1);
                                    }
                                }
                            }
                        }

                        for (int count = 0; count < removeList.Count; count++)
                        {
                            NVH.RemoveFromValidatorList(removeList[count]);
                            ErrorCount.TryRemove(removeList[count], out _);
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
