using System.Numerics;
using System.Text.Json;
using NH = Notus.Hash;
using NP = Notus.Print;
using NTN = Notus.Toolbox.Number;
using NVC = Notus.Variable.Constant;
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
        private bool TimerFunc()
        {
            //string tmpNodeHexStr = string.Empty;
            Dictionary<ulong, string> earliestNode = new();
            //Console.WriteLine("NVG.NodeList.Count : " + NVG.NodeList.Count.ToString());

            KeyValuePair<string, NVS.NodeQueueInfo>[]? nList = NVG.NodeList.ToArray();
            if (nList == null)
                return false;

            int waitingRoomCount = 0;
            int onlineCount = 0;
            for (int i = 0; i < nList.Length; i++)
            {
                if (nList[i].Value.Status == NVS.NodeStatus.Online)
                {
                    onlineCount++;
                    if (nList[i].Value.SyncNo == 0)
                    {
                        waitingRoomCount++;
                    }
                }
            }
            if (onlineCount < 3)
            {
                return false;
            }
            if (waitingRoomCount == 0)
            {
                return false;
            }


            SortedDictionary<string, string> syncNodeList = new();
            for (int i = 0; i < nList.Length; i++)
            {
                if (nList[i].Value.Status == NVS.NodeStatus.Online)
                {
                    //beklemede olan nodeların listesi çıkartılıyor
                    if (nList[i].Value.SyncNo == 0)
                    {
                        earliestNode.Add(nList[i].Value.Begin, nList[i].Value.IP.Wallet);
                    }// if (nList[i].Value.SyncNo == 0)
                    else
                    {
                        if (NVG.CurrentSyncNo == nList[i].Value.SyncNo)
                        {
                            syncNodeList.Add(nList[i].Value.IP.Wallet, "");
                        }
                    } // else if (nList[i].Value.SyncNo == 0)
                }// if (nList[i].Value.Status == NVS.NodeStatus.Online)
            }// for (int i = 0; i < nList.Length; i++)

            if (syncNodeList.Count > 0)
            {
                Console.WriteLine("syncNodeList");
                Console.WriteLine(JsonSerializer.Serialize(syncNodeList));
            }
            if (earliestNode.Count > 0)
            {
                Console.WriteLine("earliestNode");
                Console.WriteLine(JsonSerializer.Serialize(earliestNode));
            }

            /*
            if (earliestNode.Count > 0 && syncNodeList.Count > 0)
            {
                KeyValuePair<ulong, string> firstNodeForWaitingList = earliestNode.First();
                string selectedEarliestWalletId = firstNodeForWaitingList.Value;
                Console.WriteLine("Selected Node : " + selectedEarliestWalletId);
                //Console.WriteLine("syncNodeList : " + JsonSerializer.Serialize(syncNodeList, NVC.JsonSetting));
                //Console.WriteLine("earliestNode : " + JsonSerializer.Serialize(earliestNode, NVC.JsonSetting));
                //bekleme listesindeki ilk node'u ağa dahil etmek için seçiyoruz
                SortedDictionary<BigInteger, string> earlistNodeChoosing = new();

                foreach (var iEntry in syncNodeList)
                {
                    earlistNodeChoosing.Add(
                        NTN.HexToNumber(
                            new NH().CommonHash("sha1",
                                iEntry.Key + NVC.CommonDelimeterChar + selectedEarliestWalletId
                            )
                        ), iEntry.Key
                    );
                }

                // burada seçilen node en eski başlangıç zamanına sahip olan node
                // önce bu node'a onay verilerek ağa dahil edilecek
                // sonra diğerleri sırasıyla içeri giriş yapacak
                KeyValuePair<BigInteger, string> earliestNodeSelector = earlistNodeChoosing.First();
                string whoWillSayToEarlistNode = earliestNodeSelector.Value;
                NP.Info("The Node Will Join The Network : " + selectedEarliestWalletId);

                if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, whoWillSayToEarlistNode))
                {
                    Console.WriteLine("I Must Tell");
                    // omergoksoy-kontrol-noktası
                    // omergoksoy-kontrol-noktası
                    // omergoksoy-kontrol-noktası
                    // birinci sıradaki wallet diğer node'a başlangıç zamanını söyleyecek
                    // belirli bir süre sonra diğer wallet söyleyecek ( eğer birinci node düşürse diye )

                    // ValidatorQueueObj.TeelTheNodeWhoWaitingRoom(selectedEarliestWalletId);
                    // ValidatorQueueObj.TellSyncNoToEarlistNode(selectedEarliestWalletId);
                }
                else
                {
                    Console.WriteLine("Others Must Tell");
                }

                //hangi node'a kimin haber vereceğini tutan liste
                //if (NVG.NetworkSelectorList.ContainsKey(selectedEarliestWalletId) == false)
                //{
                    // sıradaki cüzdan, sıradaki node'a haber verecek node
                    //NVG.NetworkSelectorList.Add(selectedEarliestWalletId, NVG.Settings.Nodes.My.IP.Wallet);
                //}
            }// if (oldestNode.Count > 0)
            */
            return false;
        }
        public void Start()
        {
            NetworkSelectorTimer.Start(5000, () =>
            {
                if (timerRunning == false)
                {
                    timerRunning = true;
                    timerRunning = TimerFunc();
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
