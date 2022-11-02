using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NM = Notus.Message;
using NP = Notus.Print;
using NT = Notus.Threads;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Message
{
    //socket-exception
    public class Orchestra : IDisposable
    {
        private bool started = false;

        private bool pingTimerIsRunning = false;
        private NT.Timer pingTimer = new NT.Timer(5000);

        private bool subTimerIsRunning = false;
        private NT.Timer subTimer = new NT.Timer(1000);

        private NM.Publisher pubObj = new NM.Publisher();
        private ConcurrentDictionary<string, NM.Subscriber> subListObj = new ConcurrentDictionary<string, NM.Subscriber>();
        public void SendMsg(string receiverIpAddress, int receiverPortNo, string messageText, string nodeHexStr = "")
        {
            if (nodeHexStr == "")
            {
                nodeHexStr = Notus.Toolbox.Network.IpAndPortToHex(receiverIpAddress, receiverPortNo);
            }

            NVG.NodeList[nodeHexStr].Status = NVS.NodeStatus.Online;
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

                pingTimer.Start(() =>
                {
                    if (pingTimerIsRunning == false)
                    {
                        pingTimerIsRunning = true;
                        foreach (KeyValuePair<string, NM.Subscriber> entry in subListObj)
                        {
                            Task.Run(() =>
                            {
                                try
                                {
                                    entry.Value.Send("ping");
                                }
                                catch (Exception err)
                                {
                                //Console.WriteLine("hata-olustu: " + err.Message);
                                }
                            });
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
                                if (tList[i].Value.Status == Variable.Struct.NodeStatus.Online)
                                {
                                    if (subListObj.ContainsKey(tList[i].Value.IP.Wallet) == false)
                                    {
                                        bool dictAdded = subListObj.TryAdd(tList[i].Value.IP.Wallet, new NM.Subscriber() { });
                                        if (dictAdded == true)
                                        {
                                            bool socketconnected = subListObj[tList[i].Value.IP.Wallet].Start(tList[i].Value.IP.IpAddress);
                                            if (socketconnected == false)
                                            {
                                                Console.WriteLine("Baglanti Hatasi");
                                                subListObj.TryRemove(tList[i].Value.IP.Wallet, out _);
                                            }
                                        }
                                    }
                                }

                                //çevrimdışı olanlar kapatılsın
                                if (tList[i].Value.Status == Variable.Struct.NodeStatus.Offline)
                                {
                                    if (subListObj.ContainsKey(tList[i].Value.IP.Wallet) == true)
                                    {
                                        Console.WriteLine("cevrim-disi-olanlar-siliniyor");
                                        subListObj.TryRemove(tList[i].Value.IP.Wallet, out _);
                                    }
                                    else
                                    {
                                        Console.WriteLine("key-does-not-contains");
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
