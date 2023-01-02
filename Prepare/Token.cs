using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ND = Notus.Date;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
using NVE = Notus.Variable.Enum;
namespace Notus.Prepare
{
    public class Token
    {
        public static async Task<NVS.BlockResponseStruct> Generate(
            string PublicKeyHex,
            string Sign,
            NVS.TokenInfoStruct InfoData,
            NVS.SupplyStruct TokenSupplyData,
            NVE.NetworkType currentNetwork,
            string whichNodeIpAddress = ""
        )
        {
            //Notus.Wallet.ID.GetAddressWithPublicKey(PublicKeyHex, currentNetwork)
            NVS.BlockStruct_160 Obj_Token = new NVS.BlockStruct_160()
            {
                Version = 1000,
                Info = new NVS.TokenInfoStruct()
                {
                    Name = InfoData.Name,
                    Tag = InfoData.Tag,
                    Logo = new NVS.FileStorageStruct()
                    {
                        Base64 = InfoData.Logo.Base64,
                        Source = InfoData.Logo.Source,
                        Url = InfoData.Logo.Url,
                        Used = InfoData.Logo.Used
                    }
                },
                Creation = new NVS.CreationStruct()
                {
                    UID = Notus.Block.Key.Generate(ND.NowObj(), ""),
                    PublicKey = PublicKeyHex,
                    Sign = Sign
                },
                Reserve = new NVS.SupplyStruct()
                {
                    Decimal = TokenSupplyData.Decimal,
                    Resupplyable = TokenSupplyData.Resupplyable,
                    Supply = TokenSupplyData.Supply
                }
            };

            bool exitInnerLoop = false;
            string WalletKeyStr = Notus.Wallet.ID.GetAddressWithPublicKey(PublicKeyHex);
            while (exitInnerLoop == false)
            {
                List<string> ListMainNodeIp = Notus.Validator.List.Get(Variable.Enum.NetworkLayer.Layer1, currentNetwork);

                for (int a = 0; a < ListMainNodeIp.Count && exitInnerLoop == false; a++)
                {
                    string nodeIpAddress = ListMainNodeIp[a];
                    if (whichNodeIpAddress != "")
                    {
                        nodeIpAddress = whichNodeIpAddress;
                    }
                    string MainResultStr = string.Empty;
                    try
                    {
                        string fullUrlAddress =
                            Notus.Network.Node.MakeHttpListenerPath(
                                nodeIpAddress,
                                Notus.Network.Node.GetNetworkPort(currentNetwork, NVE.NetworkLayer.Layer1)
                            ) + "token/generate/" + WalletKeyStr + "/";
                        MainResultStr = await Notus.Communication.Request.Post(
                            fullUrlAddress,
                            new Dictionary<string, string>
                            {
                                { "data" , JsonSerializer.Serialize(Obj_Token) }
                            }
                        );
                        NVS.BlockResponseStruct tmpResponse = JsonSerializer.Deserialize<NVS.BlockResponseStruct>(MainResultStr);
                        return tmpResponse;
                    }
                    catch (Exception err)
                    {
                        //Notus.Print.Basic(true, "Error Text [9a5f4g12v3f]: " + err.Message);
                        return new NVS.BlockResponseStruct()
                        {
                            UID = "",
                            Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                            Status = "UnknownError -> " + MainResultStr + " >> " + err.Message
                        };
                    }
                }
            }
            return new NVS.BlockResponseStruct()
            {
                UID = "",
                Code = Notus.Variable.Constant.ErrorNoList.UnknownError,
                Status = "UnknownError"
            };
        }
    }
}
