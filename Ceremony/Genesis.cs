using NVS = Notus.Variable.Struct;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using NGF = Notus.Variable.Globals.Functions;
using NTN = Notus.Toolbox.Network;
using NVG = Notus.Variable.Globals;
using NP = Notus.Print;

namespace Notus.Ceremony
{
    public class Genesis : IDisposable
    {
        private int DefaultBlockGenerateInterval = 3000;

        public int PreviousId
        {
            get { return DefaultBlockGenerateInterval; }
        }
        public void Start()
        {
            bool exitInnerWhile = false;
            NP.Info("Finding Online Nodes");
            while (exitInnerWhile == false)
            {
                foreach (var iE in NGF.ValidatorList)
                {
                    if (string.Equals(iE.Key, NVG.Settings.Nodes.My.HexKey) == false)
                    {
                        if (NTN.PingToNode(iE.Value) == NVS.NodeStatus.Online)
                        {
                            NGF.ValidatorList[iE.Key].Status = NVS.NodeStatus.Online;
                            exitInnerWhile = true;
                            break;
                        }
                    }
                }
                if (exitInnerWhile == false)
                    Thread.Sleep(100);
            }
        }
        public Genesis(bool AutoStart = true)
        {
            if (AutoStart == true)
            {
                Start();
            }
        }
        public void Dispose()
        {

        }
        ~Genesis()
        {
            Dispose();
        }
    }
}
