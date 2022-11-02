using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NM = Notus.Message;
using NVG = Notus.Variable.Globals;
namespace Notus.Message
{
    //socket-exception
    public class Orchestra : IDisposable
    {
        private bool started = false;
        private bool subTimerIsRunning = false;
        private Notus.Threads.Timer subTimer = new Notus.Threads.Timer(250);
        private NM.Publisher pubObj = new NM.Publisher();
        private Dictionary<string, NM.Subscriber> subListObj = new Dictionary<string, NM.Subscriber>();
        public void Start()
        {
            if (started == false)
            {
                started = true;

                Task.Run(() =>
                {
                    pubObj.Start();
                });

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
                                        subListObj.Add(tList[i].Value.IP.Wallet, new NM.Subscriber() { });
                                        bool socketconnected = subListObj[tList[i].Value.IP.Wallet].Start(tList[i].Value.IP.IpAddress);
                                        if (socketconnected == false)
                                        {
                                            Console.WriteLine("Baglanti Hatasi");
                                            subListObj.Remove(tList[i].Value.IP.Wallet);
                                        }
                                    }
                                }

                                //çevrimdışı olanlar kapatılsın
                                if (tList[i].Value.Status == Variable.Struct.NodeStatus.Offline)
                                {
                                    if (subListObj.ContainsKey(tList[i].Value.IP.Wallet) == true)
                                    {
                                        Console.WriteLine("cevrim-disi-olanlar-siliniyor");
                                        subListObj.Remove(tList[i].Value.IP.Wallet);
                                    }
                                }
                            }
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
            if (pubObj != null)
            {
                pubObj.Dispose();
            }
        }
    }
}
