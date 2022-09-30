﻿using System;
using System.Text.Json;
using System.Threading;
using NVG = Notus.Variable.Globals;
namespace Notus.Validator
{
    public static class Node
    {
        public static void Start(string[] argsFromCLI)
        {
            bool LightNodeActive = false;
            /*
            
            burada ntp zaman bilgisi çekilecek
            burada ntp zaman bilgisi çekilecek

            Console.WriteLine(
                JsonSerializer.Serialize(
                    Notus.Time.GetNtpTime()
                )
            );
            Console.ReadLine();
            */

            using (Notus.Validator.Menu menuObj = new Notus.Validator.Menu())
            {
                menuObj.PreStart(argsFromCLI);
                menuObj.Start();
                menuObj.DefineMySetting();
            }
            if (NVG.Settings.NodeType != Notus.Variable.Enum.NetworkNodeType.Replicant)
            {
                LightNodeActive = false;
            }

            if (NVG.Settings.DevelopmentNode == true)
            {
                NVG.Settings.Network = Notus.Variable.Enum.NetworkType.DevNet;
                Notus.Validator.Node.Start(LightNodeActive);
            }
            else
            {
                NVG.Settings.Network = Notus.Variable.Enum.NetworkType.MainNet;
                Notus.Validator.Node.Start(LightNodeActive);
            }
        }
        public static void Start(bool LightNodeActive)
        {
            if (NVG.Settings.LocalNode == true)
            {
                Notus.Print.Info(NVG.Settings, "LocalNode Activated");
            }
            Notus.IO.NodeFolderControl();

            Notus.Print.Info(NVG.Settings, "Activated DevNET for " + Notus.Variable.Constant.LayerText[NVG.Settings.Layer]);
            NVG.Settings = Notus.Toolbox.Network.IdentifyNodeType(NVG.Settings, 5);
            switch (NVG.Settings.NodeType)
            {
                // if IP and port node written in the code
                case Notus.Variable.Enum.NetworkNodeType.Main:
                    StartAsMain();
                    break;

                // if node join the network
                case Notus.Variable.Enum.NetworkNodeType.Master:
                    //StartAsMaster(NodeSettings);
                    StartAsMain();
                    //StartAsMaster(NodeSettings);
                    break;

                // if node only store the data
                case Notus.Variable.Enum.NetworkNodeType.Replicant:
                    StartAsReplicant( LightNodeActive);
                    break;

                default:
                    break;
            }
            Notus.Print.Warning(NVG.Settings, "Task Ended");
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

                Notus.Print.Basic(NVG.Settings, "Sleep For 2.5 Seconds");
                Thread.Sleep(2500);
            }
        }
        private static void StartAsMain()
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                using (Notus.Validator.Main MainObj = new Notus.Validator.Main())
                {
                    //MainObj.Settings = NodeSettings;
                    MainObj.Start();
                }

                Notus.Print.Basic(NVG.Settings, "Sleep For 2.5 Seconds");
                Thread.Sleep(2500);
            }
        }
        private static void StartAsReplicant(bool LightNodeActive)
        {
            bool exitOuterLoop = false;
            while (exitOuterLoop == false)
            {
                try
                {
                    using (Notus.Validator.Replicant ReplicantObj = new Notus.Validator.Replicant())
                    {
                        ReplicantObj.LightNode = LightNodeActive;
                        ReplicantObj.Start();
                    }
                }
                catch (Exception err)
                {
                    Notus.Print.Log(
                        Notus.Variable.Enum.LogLevel.Info,
                        660050,
                        err.Message,
                        "BlockRowNo",
                        null,
                        err
                    );

                    Notus.Print.Danger(NVG.Settings, "Replicant Outer Error Text : " + err.Message);
                }
            }
        }
    }
}