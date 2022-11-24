using System;
using System.Collections.Concurrent;
using System.Net;
using System.Numerics;
using System.Text.Json;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Validator
{
    public class Register : IDisposable
    {
        private bool timerRunning = false;
        // sıradaki cüzdan, sıradaki node'a haber verecek node
        private Notus.Threads.Timer NetworkSelectorTimer = new Notus.Threads.Timer();

        // sıradaki cüzdan, sıradaki node'a haber verecek node
        public static Dictionary<string, string> NetworkSelectorList = new Dictionary<string, string>();
        public void Start()
        {
            NetworkSelectorTimer.Start(5000, () =>
            {
                if (timerRunning == false)
                {
                    timerRunning= true;

                    string tmpNodeHexStr = string.Empty;
                    Dictionary<ulong, string> earliestNode = new();
                    KeyValuePair<string, NVS.NodeQueueInfo>[]? nList = NVG.NodeList.ToArray();
                    if (nList != null)
                    {
                        SortedDictionary<string, string> syncNodeList = new();
                        for (int i = 0; i < nList.Length; i++)
                        {
                            if (nList[i].Value.Status == NVS.NodeStatus.Online)
                            {
                                //beklemede olan nodeların listesi çıkartılıyor
                                if (nList[i].Value.SyncNo == 0)
                                {
                                    Console.WriteLine("Cevrim Ici Ve Sync No Sifir Olan -> " + nList[i].Value.IP.IpAddress);
                                    earliestNode.Add(nList[i].Value.Begin, nList[i].Value.IP.Wallet);
                                }// if (nList[i].Value.SyncNo == 0)
                                else
                                {
                                    // burada aynı SYNC_NO değerine sahip olan nodelardan bir liste yapılacak
                                    // yapılan liste ile ilk sıradaki node bildirecek
                                    if (NVG.CurrentSyncNo == nList[i].Value.SyncNo)
                                    {
                                        syncNodeList.Add(nList[i].Value.IP.Wallet, "");
                                    }
                                } // else if (nList[i].Value.SyncNo == 0)
                            }// if (nList[i].Value.Status == NVS.NodeStatus.Online)
                        }// for (int i = 0; i < nList.Length; i++)

                        /*
                        if (earliestNode.Count > 0 && syncNodeList.Count > 0 )
                        {
                            //bekleme listesindeki ilk node'u ağa dahil etmek için seçiyoruz
                            SortedDictionary<BigInteger, string> earlistNodeChoosing = new();
                            var firstNodeForWaitingList = earliestNode.First();
                            ulong earlistBeginTime = firstNodeForWaitingList.Key;
                            string selectedEarliestWalletId = firstNodeForWaitingList.Value;


                            foreach (var iEntry in syncNodeList)
                            {
                                earlistNodeChoosing.Add(
                                    BigInteger.Parse(
                                        "0" + new NH().CommonHash("sha1", iEntry.Key + NVC.CommonDelimeterChar + selectedEarliestWalletId)
                                        , NumberStyles.AllowHexSpecifier
                                    ),
                                    iEntry.Key
                                );
                            }

                            // burada seçilen node en eski başlangıç zamanına sahip olan node
                            // önce bu node'a onay verilerek ağa dahil edilecek
                            // sonra diğerleri sırasıyla içeri giriş yapacak
                            var earliestNodeSelector = earlistNodeChoosing.First();
                            NP.Info("The Node Will Join The Network : " + selectedEarliestWalletId);
                            if (NVG.NetworkSelectorList.ContainsKey(selectedEarliestWalletId) == false)
                            {
                                // sıradaki cüzdan, sıradaki node'a haber verecek node
                                NVG.NetworkSelectorList.Add(selectedEarliestWalletId, NVG.Settings.Nodes.My.IP.Wallet);
                            }
                            if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, earliestNodeSelector.Value))
                            {
                                // birinci sıradaki wallet diğer node'a başlangıç zamanını söyleyecek
                                // belirli bir süre sonra diğer wallet söyleyecek ( eğer birinci node düşürse diye )
                                Console.WriteLine("I Must Tell");
                                ValidatorQueueObj.TellSyncNoToEarlistNode(selectedEarliestWalletId);
                                ValidatorQueueObj.TeelTheNodeWhoWaitingRoom(selectedEarliestWalletId);
                            }
                            else
                            {
                                Console.WriteLine("Others Must Tell");
                            }
                        }// if (oldestNode.Count > 0)
                        */
                    }// if (nList != null)

                    timerRunning = false;
                }
            });
        }
        public Register()
        {
        }
        ~Register()
        {
            Dispose();
        }
        public void Dispose()
        {

        }
    }
}
