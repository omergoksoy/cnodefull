using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NC = Notus.Convert;
using NVE = Notus.Variable.Enum;

namespace Notus.Encode
{

    // bu kitaplık https://github.com/minicheddar/RLP adresinden alındı

    public static class RLP
    {
        public static string Encode(List<string> data, NVE.InputOrOutputType returnType = NVE.InputOrOutputType.AsHex)
        {
            var returnByteArray = new List<byte[]>();
            for (int i = 0; i < data.Count; i++)
            {
                returnByteArray.Add(
                    Notus.ExternalResources.Nethereum.RLP.RLP.EncodeElement(
                        System.Text.Encoding.ASCII.GetBytes(data[i])
                    )
                );
            }

            if (returnType == NVE.InputOrOutputType.AsBase64)
                return System.Convert.ToBase64String(Notus.ExternalResources.Nethereum.RLP.RLP.EncodeList(returnByteArray.ToArray()));

            return Notus.Convert.Byte2Hex(Notus.ExternalResources.Nethereum.RLP.RLP.EncodeList(returnByteArray.ToArray()));
        }
        //public static List<string> Decode(string input, NVE.ReturnType returnType = NVE.ReturnType.AsHex)
        public static List<string> Decode(string input, NVE.InputOrOutputType inputType = NVE.InputOrOutputType.AsHex)
        {
            List<string> resultList = new();
            if (inputType == NVE.InputOrOutputType.AsBase64)
            {
                Notus.ExternalResources.Nethereum.RLP.RLPCollection decodedElements64 =
                    (Notus.ExternalResources.Nethereum.RLP.RLPCollection)Notus.ExternalResources.Nethereum.RLP.RLP.Decode(
                        System.Convert.FromBase64String(input)
                    );
                for (int i = 0; i < decodedElements64.Count; i++)
                {
                    if (decodedElements64[i].RLPData == null)
                        resultList.Add("");
                    else
                        resultList.Add(Encoding.UTF8.GetString(decodedElements64[i].RLPData));
                }
                return resultList;
            }

            Notus.ExternalResources.Nethereum.RLP.RLPCollection decodedElements16 =
                (Notus.ExternalResources.Nethereum.RLP.RLPCollection)Notus.ExternalResources.Nethereum.RLP.RLP.Decode(
                    NC.Hex2Byte(input)
                );

            for (int i = 0; i < decodedElements16.Count; i++)
                resultList.Add(Encoding.UTF8.GetString(decodedElements16[i].RLPData));

            return resultList;
        }
    }
}