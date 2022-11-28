using System.Numerics;
using System.Text.Json;
using NH = Notus.Hash;
using NP = Notus.Print;
using NTN = Notus.Toolbox.Number;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVR = Notus.Validator.Register;
using NVS = Notus.Variable.Struct;
using NVH = Notus.Validator.Helper;
using NCH = Notus.Communication.Helper;
namespace Notus.Validator
{
    public class Register : IDisposable
    {
        private bool timerRunning = false;
        // sıradaki cüzdan, sıradaki node'a haber verecek node
        private Notus.Threads.Timer NetworkSelectorTimer = new Notus.Threads.Timer();

        public static Dictionary<string, ulong> ReadyMessageFromNode = new Dictionary<string, ulong>();

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

            if (earliestNode.Count > 0 && syncNodeList.Count > 0)
            {
                // Console.WriteLine("-----------------------------------");
                // Console.WriteLine("syncNodeList : " + JsonSerializer.Serialize(syncNodeList));
                // Console.WriteLine("earliestNode : " + JsonSerializer.Serialize(earliestNode));
                // Console.WriteLine("-----------------------------------");

                KeyValuePair<ulong, string> firstNodeForWaitingList = earliestNode.First();
                string selectedEarliestWalletId = firstNodeForWaitingList.Value;
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
                if (NVR.NetworkSelectorList.ContainsKey(selectedEarliestWalletId) == false)
                {
                    KeyValuePair<BigInteger, string> earliestNodeSelector = earlistNodeChoosing.First();
                    string whoWillSayToEarlistNode = earliestNodeSelector.Value;
                    NVR.NetworkSelectorList.Add(selectedEarliestWalletId, whoWillSayToEarlistNode);

                    NP.Info("The Node Will Join The Network : " + selectedEarliestWalletId);
                    // Console.WriteLine("selectedEarliestWalletId : " + selectedEarliestWalletId);
                    if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, whoWillSayToEarlistNode))
                    {
                        NP.Info("I Will Tell The Node");
                        NVH.TellTheNodeWhoWaitingRoom(selectedEarliestWalletId);
                        NVH.TellSyncNoToEarlistNode(selectedEarliestWalletId);
                    }

                    // sıradaki cüzdan, sıradaki node'a haber verecek node
                }
                else
                {
                    if (string.Equals(NVG.Settings.Nodes.My.IP.Wallet, NVR.NetworkSelectorList[selectedEarliestWalletId]))
                    {

                    }
                    else
                    {

                    }
                }
                return false;
            }
            else
            {
                /*
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
                */
                return false;
            }
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
