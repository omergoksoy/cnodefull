using System;
using System.Text.Json;
using System.Threading;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Validator
{
    public static class Node
    {
        public static void Start(string[] argsFromCLI)
        {
            NGF.PreStart();
            using (Notus.Validator.Menu menuObj = new Notus.Validator.Menu())
            {
                menuObj.PreStart(argsFromCLI);
                menuObj.Start();
                menuObj.DefineMySetting();
            }
            NVG.Settings.Network = (NVG.Settings.DevelopmentNode == true ? NVE.NetworkType.DevNet : NVE.NetworkType.MainNet);
            Notus.Validator.Node.Start();
        }
        public static void Start()
        {
            NVG.Settings.BlockOrder.Start();
            NVG.Settings.BlockSign.Start();
            NVG.Settings.BlockPrev.Start();
            NVG.Settings.TxStatus.Start();
            //NVG.Settings.UidTypeList.Start();

            NP.ExecuteTime();

            if (NVG.Settings.LocalNode == true)
            {
                NP.Info(NVG.Settings, "LocalNode Activated");
            }
            Notus.IO.NodeFolderControl();

            /*
            Notus.Block.Storage storageObj = new Notus.Block.Storage(false);

            //tgz-exception
            string LastBlockUid = "1348c02274960011734a5d9a654b68e8355d6a80586560b60a9cd4f6314f6234dd43851e7d88da27b4f879f02d";
            Notus.Variable.Class.BlockData? tmpBlockData = storageObj.ReadBlock(LastBlockUid);
            Console.WriteLine(JsonSerializer.Serialize(tmpBlockData, NVC.JsonSetting));
            Console.ReadLine();
            */

            NP.Info(NVG.Settings, "Activated DevNET for " + NVC.LayerText[NVG.Settings.Layer]);
            Notus.Toolbox.Network.IdentifyNodeType(5);
            NGF.Start();

            switch (NVG.Settings.NodeType)
            {
                // if IP and port node written in the code
                case NVE.NetworkNodeType.Main:
                    StartAsMain();
                    break;

                // if node join the network
                case NVE.NetworkNodeType.Master:
                    StartAsMain();
                    break;

                // if node only store the data
                case NVE.NetworkNodeType.Replicant:
                    StartAsReplicant();
                    break;

                default:
                    break;
            }
            if (NVG.Settings.NodeClosing == false)
            {
                NP.Warning(NVG.Settings, "Task Ended");
            }
        }
        private static void StartAsMaster()
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                using (Notus.Validator.Main MainObj = new Notus.Validator.Main())
                {
                    //MainObj.Settings = NodeSettings;
                    MainObj.Start();
                }

                if (NVG.Settings.NodeClosing == false)
                {
                    NP.Basic(NVG.Settings, "Sleep For 2.5 Seconds");
                    Thread.Sleep(2500);
                }
            }
        }
        private static void StartAsMain()
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false && NVG.Settings.NodeClosing == false)
            {
                using (Notus.Validator.Main MainObj = new Notus.Validator.Main())
                {
                    MainObj.Start();
                }

                if (NVG.Settings.GenesisCreated == true)
                {
                    Environment.Exit(0);
                }
                if (NVG.Settings.NodeClosing == false)
                {
                    NP.Basic(NVG.Settings, "Sleep For 0.5 Seconds");
                    Thread.Sleep(500);
                }
            }
        }
        private static void StartAsReplicant()
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                try
                {
                    using (Notus.Validator.Replicant ReplicantObj = new Notus.Validator.Replicant())
                    {
                        ReplicantObj.Start();
                    }
                }
                catch (Exception err)
                {
                    NP.Danger(NVG.Settings, "Replicant Outer Error Text : " + err.Message);
                }
            }
        }
    }
}