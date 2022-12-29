using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NTN = Notus.Toolbox.Network;
using NVG = Notus.Variable.Globals;
using NVH = Notus.Validator.Helper;
using NVS = Notus.Variable.Struct;

namespace Notus.Ceremony
{
    public class Genesis : IDisposable
    {
        private int DefaultBlockGenerateInterval = 3000;

        public int PreviousId
        {
            get { return DefaultBlockGenerateInterval; }
        }
        private void DefineCeremonyMembers()
        {

        }
        private 
        public void StartNodeSync()
        {
            NVH.PrepareValidatorList(true);
            bool definedValidator = false;
            foreach (Variable.Struct.IpInfo item in Notus.Validator.List.Main[NVG.Settings.Layer][NVG.Settings.Network])
            {
                if (string.Equals(NVG.Settings.Nodes.My.IP.IpAddress, item.IpAddress) == true)
                {
                    definedValidator = true;
                }
            }
            if (definedValidator == false)
            {
                NP.Danger("Diger nodelardan tanımlanmış Validatorlerden tarafından olusturulmus Genesis blogunu iste");
                NP.Danger("Diger nodelardan tanımlanmış Validatorlerden tarafından olusturulmus Genesis blogunu iste");
                NP.Danger("Diger nodelardan tanımlanmış Validatorlerden tarafından olusturulmus Genesis blogunu iste");
                NP.Danger("Genesis Ceremony Works With Only Defined Validators");
                Environment.Exit(0);
            }

            NP.Basic(JsonSerializer.Serialize(NGF.ValidatorList));
            //ValidatorQueueObj.PreStart();
            bool exitFromWhileLoop = false;
            while (exitFromWhileLoop == false)
            {
                NVG.Settings.PeerManager.RemoveAll();
                foreach (var validatorItem in NGF.ValidatorList)
                {
                    if (string.Equals(NVG.Settings.Nodes.My.HexKey, validatorItem.Key) == false)
                    {
                        NVG.Settings.PeerManager.AddPeer(
                            validatorItem.Key,
                            validatorItem.Value.IpAddress
                        );
                    }
                }
                foreach (var validatorItem in NGF.ValidatorList)
                {
                    if (string.Equals(NVG.Settings.Nodes.My.HexKey, validatorItem.Key) == false)
                    {
                        if (NVG.Settings.PeerManager.Send(validatorItem.Key, "<ping>1</ping>", false) == false)
                        {
                            NGF.ValidatorList[validatorItem.Key].Status = NVS.NodeStatus.Offline;
                        }
                        else
                        {
                            NGF.ValidatorList[validatorItem.Key].Status = NVS.NodeStatus.Online;
                        }
                    }
                }
                bool allValidatorIsOnline = true;
                foreach (var validatorItem in NGF.ValidatorList)
                {
                    if(validatorItem.Value.Status == NVS.NodeStatus.Offline)
                    {
                        allValidatorIsOnline = false;
                    }
                }
                if (allValidatorIsOnline == true)
                {
                    exitFromWhileLoop = true;
                }
                else
                {
                    Thread.Sleep(350);
                }
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
