using Notus;
using System.Collections.Generic;
using System.Text.Json;
using NE = Notus.Encode;
using NVC = Notus.Variable.Constant;
using NVS = Notus.Variable.Struct;

namespace Notus.API
{
    public static class TX
    {
        public static string Process(NVS.HttpRequestDetails IncomeData, bool ToDistribute)
        {
            if (IncomeData.UrlList.Length < 2)
                return JsonSerializer.Serialize(false);

            //string tmpText = "f84583312e308b616c6963695f61647265738574757461728c69736c656d5f7563726574698c6e6f6e63655f646567657269846461746184696d7a618a7075626c69635f6b6579";
            string tmpText = IncomeData.UrlList[1];

            IList<string> result = NE.RLP.Decode(tmpText);

            if (result.Count != 8)
                return JsonSerializer.Serialize(false);

            if (string.Equals(result[0], "1.0") == false)
                return JsonSerializer.Serialize(false);

            if (result[1].Length != NVC.WalletFullTextLength)
                return JsonSerializer.Serialize(false);

            List<string> dataList = new List<string> {
                    "1.0",          // version
                    "alici_adres",  // alıcı adresi
                    "tutar",        // gönderilmek istenen tutar
                    "islem_ucreti", // işlem ücreti
                    "nonce_degeri", // geçerli nonce değeri
                    "data",         // varsa işlem datası
                    "imza",         // işlem imzası
                    "public_key",   // işlemi yapan public key
                };
            const int rawDataCount = 6;
            string rawDataText = string.Empty;
            for (int i = 0; i < rawDataCount; i++)
            {
                rawDataText += dataList[i];
                if (i < (rawDataCount - 1))
                    rawDataText += ":";
            }

            bool isIverified = Notus.Wallet.ID.Verify(rawDataText, dataList[6], dataList[7]);
            if (isIverified == false)
            {
                return JsonSerializer.Serialize(false);
            }
            return JsonSerializer.Serialize(result);
        }
    }
}
