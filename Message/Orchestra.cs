﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NM = Notus.Message;
using NP = Notus.Print;
using NT = Notus.Threads;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Message
{
    //socket-exception
    public class Orchestra : IDisposable
    {
        private bool started = false;

        private ConcurrentDictionary<string,int> errorCountList = new ConcurrentDictionary<string, int>();
        private bool pingTimerIsRunning = false;
        private NT.Timer pingTimer = new NT.Timer(5000);

        private bool subTimerIsRunning = false;
        private NT.Timer subTimer = new NT.Timer(1000);

        private NM.Publisher pubObj = new NM.Publisher();
        private ConcurrentDictionary<string, NM.Subscriber> subListObj = new ConcurrentDictionary<string, NM.Subscriber>();
        public void OnReceive(System.Action<string> incomeTextFunc)
        {
            pubObj.OnReceive(incomeTextFunc);
        }
        public string SendMsg(
            string walletId,
            string messageText
        )
        {
            if (subListObj.ContainsKey(walletId))
            {
                return subListObj[walletId].Send(messageText);
            }
            return string.Empty;
        }
        public void Start()
        {
            if (started == false)
            {
                started = true;

                Task.Run(() =>
                {
                    pubObj.Start();
                });

                //pingTimerIsRunning = true;
                pingTimer.Start(() =>
                {
                    if (pingTimerIsRunning == false)
                    {
                        pingTimerIsRunning = true;
                        foreach (KeyValuePair<string, NM.Subscriber> entry in subListObj)
                        {
                            /*
                            Task.Run(() =>
                            {
                            });
                            */
                            string selectedKey = string.Empty;
                            foreach (var iEntry in NVG.NodeList)
                            {
                                if (string.Equals(iEntry.Value.IP.Wallet, entry.Key) == true)
                                {
                                    selectedKey = iEntry.Key;
                                }
                            }
                            if (selectedKey.Length > 0)
                            {
                                bool isOnline = false;
                                try
                                {
                                    if (entry.Value.Send("ping") == "pong")
                                    {
                                        //Console.WriteLine("Orchestra.cs -> Line 94 -> PING onnline -> " + NVG.NodeList[selectedKey].IP.IpAddress);
                                        NVG.NodeList[selectedKey].Status = NVS.NodeStatus.Online;
                                        isOnline = true;
                                    }
                                }
                                catch (Exception err)
                                {
                                    // NVG.NodeList[entry.Key].Status = NVS.NodeStatus.Error;
                                    Console.WriteLine("PING -> hata-olustu: " + err.Message);
                                }
                                if (isOnline == false)
                                {
                                    Console.WriteLine("Orchestra.cs -> Line 94 -> PING Offline -> " + NVG.NodeList[selectedKey].IP.IpAddress);
                                    if (errorCountList.ContainsKey(selectedKey) == false)
                                    {
                                        errorCountList.TryAdd(selectedKey, 0);
                                    }
                                    errorCountList[selectedKey]++;
                                    if (errorCountList[selectedKey] > 3)
                                    {
                                        Console.WriteLine("Orchestra.cs -> Node Assigned As Offline -> " + NVG.NodeList[selectedKey].IP.IpAddress);
                                        NVG.NodeList[selectedKey].Status = NVS.NodeStatus.Offline;
                                    }
                                }
                                else
                                {
                                    if (errorCountList.ContainsKey(selectedKey) == true)
                                    {
                                        errorCountList.TryRemove(selectedKey, out _);
                                    }
                                }
                            }
                        }
                        pingTimerIsRunning = false;
                    }
                }, true);


                subTimer.Start(() =>
                {
                    if (subTimerIsRunning == false)
                    {
                        subTimerIsRunning = true;
                        KeyValuePair<string, Variable.Struct.NodeQueueInfo>[]? tList = NVG.NodeList.ToArray();
                        if (tList != null)
                        {
                            //eklenmeyenler eklensin
                            for (int i = 0; i < tList.Length; i++)
                            {
                                if (string.Equals(tList[i].Value.IP.Wallet, NVG.Settings.NodeWallet.WalletKey) == false)
                                {
                                    if (tList[i].Value.Status == Variable.Struct.NodeStatus.Online)
                                    {
                                        if (subListObj.ContainsKey(tList[i].Value.IP.Wallet) == false)
                                        {
                                            bool dictAdded = subListObj.TryAdd(tList[i].Value.IP.Wallet, new NM.Subscriber() { });
                                            if (dictAdded == true)
                                            {
                                                bool socketconnected = subListObj[tList[i].Value.IP.Wallet].Start(
                                                    tList[i].Value.IP.IpAddress
                                                );
                                                if (socketconnected == false)
                                                {
                                                    //Console.WriteLine("Baglanti Hatasi");
                                                    subListObj.TryRemove(tList[i].Value.IP.Wallet, out _);
                                                }
                                            }
                                        }
                                    }
                                }

                                //çevrimdışı olanlar kapatılsın
                                if (tList[i].Value.Status == Variable.Struct.NodeStatus.Offline)
                                {
                                    //Console.WriteLine("tList[i].Value.IP.IpAddress : " + tList[i].Value.IP.IpAddress);
                                    if (subListObj.ContainsKey(tList[i].Value.IP.Wallet) == true)
                                    {
                                        Console.WriteLine(JsonSerializer.Serialize(tList[i].Value, NVC.JsonSetting));
                                        NP.Info("Offline Node Remove From List");
                                        //Console.WriteLine("cevrim-disi-olanlar-siliniyor");
                                        //Console.WriteLine(JsonSerializer.Serialize(NVG.NodeList, NVC.JsonSetting));
                                        subListObj.TryRemove(tList[i].Value.IP.Wallet, out _);
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("list-value-is-null");
                        }
                        subTimerIsRunning = false;
                    }
                });
            }
        }
        public Orchestra()
        {
            errorCountList.Clear();
        }
        ~Orchestra()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (subTimer != null)
            {
                subTimer.Dispose();
            }
            if (pingTimer != null)
            {
                pingTimer.Dispose();
            }
            if (pubObj != null)
            {
                pubObj.Dispose();
            }
        }
    }
}
