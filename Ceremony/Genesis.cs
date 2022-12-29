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

            //ValidatorQueueObj.PreStart();
            Console.WriteLine(JsonSerializer.Serialize(NGF.ValidatorList));
            bool exitFromWhileLoop = false;
            NVG.Settings.PeerManager.RemoveAll();
            foreach (var validatorItem in NGF.ValidatorList)
            {
                NVG.Settings.PeerManager.AddPeer(validatorItem.Key, validatorItem.Value.IpAddress);
            }
            while (exitFromWhileLoop == false)
            {
                bool allValidatorIsOnline = true;
                foreach (var validatorItem in NGF.ValidatorList)
                {
                    if (string.Equals(NVG.Settings.Nodes.My.HexKey, validatorItem.Key) == false)
                    {
                        /*
                        Bu fonksiyon API ile çalışıyor
                        özel soket bağlantısını başlat ve o soket ile Ping fonksiyonunu çalıştır.
                        tüm nodelar online olunca sonraki sekansa geç
                        */
                        if (NVG.Settings.PeerManager.Send(validatorItem.Key, "<ping>") == true)
                        {

                        }
                        else
                        {

                        }
                        /*
                        if (NTN.PingToNode(validatorItem.Value) == NVS.NodeStatus.Online)
                        {
                            NGF.ValidatorList[validatorItem.Key].Status = NVS.NodeStatus.Online;
                        }
                        else
                        {
                            allValidatorIsOnline = false;
                        }
                        */
                    }
                }
                if (allValidatorIsOnline == true)
                {
                    Console.WriteLine("allValidatorIsOnline = TRUE");
                }
                else
                {
                    Console.WriteLine("allValidatorIsOnline = FALSE");
                }
            }
        }
        public void Start()
        {


        }
        public Genesis(bool AutoStart = true)
        {
            if (AutoStart == true)
            {
                //Start();
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
