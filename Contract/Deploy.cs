using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ND = Notus.Date;
using NGF = Notus.Variable.Globals.Functions;
using NVC = Notus.Variable.Constant;
using NVClass = Notus.Variable.Class;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Contract
{
    public class Deploy : IDisposable
    {
        public string Request(NVS.HttpRequestDetails IncomeData)
        {
            if (NVG.Settings.Genesis == null)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 553268,
                    ErrorText = "AnErrorOccurred",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            if (IncomeData.PostParams.ContainsKey("code") == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 553268,
                    ErrorText = "AnErrorOccurred",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }
            IncomeData.PostParams["code"]= System.Web.HttpUtility.UrlDecode(IncomeData.PostParams["code"]);
            Console.WriteLine(JsonSerializer.Serialize(IncomeData));


            return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
            {
                ErrorNo = 35496,
                ErrorText = "NotSupported",
                ID = string.Empty,
                Result = NVE.BlockStatusCode.NotSupported
            });


            // mainnet ise hata gönderecek
            if (NVG.Settings.Network == Variable.Enum.NetworkType.MainNet)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 35496,
                    ErrorText = "NotSupported",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.NotSupported
                });
            }

            string ReceiverWalletKey = IncomeData.UrlList[1];

            if (ReceiverWalletKey.Length != NVC.SingleWalletTextLength)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 91983,
                    ErrorText = "WrongWallet",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WrongWallet
                });
            }

            /*
            if (LimitExceeded(ReceiverWalletKey) == true)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 371854,
                    ErrorText = "TooManyRequest",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.TooManyRequest
                });
            }
            */

            string airdropUid = NGF.GenerateTxUid();

            // eğer cüzdan kilitli ise hata gönderecek
            if (NGF.Balance.AccountIsLock(ReceiverWalletKey) == true)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 3827,
                    ErrorText = "WalletNotAllowed",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WalletNotAllowed
                });
            }

            /*
"rowNo":3,
{"In":{"134afde3707f":{"Wallet":"NSXhhh","Balance":{"NOTUS":{"20230110225407707":"0"}},"RowNo":0,"UID":""}},"Out":{"NSXhhhhhh888888888888888488888888822222":{"NOTUS":{"20230110225407708":"2000000000"}}},"Validator":"NSX6woSKz9hc4fUtd4K8iJpK99XsK7Y96rArN63"}

"rowNo":4
{"In":{"134afde37081":{"Wallet":"NSXhhh","Balance":{"NOTUS":{"20230110225407708":"2000000000"}},"RowNo":3,"UID":"134afde37080000787fff8b6746f19981d5aba47fb78e1143e3f0c99ea5fd7ca6fe0026000ee675560a36bce08"}},"Out":{"NSXhhhhhh888888888888888488888888822222":{"NOTUS":{"20230110225407708":"2000000000","20230110225409591":"2000000000"}}},"Validator":"NSX6woSKz9hc4fUtd4K8iJpK99XsK7Y96rArN63"}

"rowNo":5
{"In":{"134afde37080":{"Wallet":"NSXhhh","Balance":{"NOTUS":{"20230110225407708":"2000000000"}},"RowNo":3,"UID":"134afde37080000787fff8b6746f19981d5aba47fb78e1143e3f0c99ea5fd7ca6fe0026000ee675560a36bce08"}},"Out":{"NSXhhhhhh888888888888888488888888822222":{"NOTUS":{"20230110225407708":"2000000000","20230110225408273":"2000000000"}}},"Validator":"NSX6woSKz9hc4fUtd4K8iJpK99XsK7Y96rArN63"}

"rowNo":6
{"In":{"134afde3708c":{"Wallet":"NSXhhh","Balance":{"NOTUS":{"20230110225407708":"2000000000","20230110225408273":"2000000000"}},"RowNo":5,"UID":"134afde3708800044df265139bfffe5bec532b792bdee4b3d28ba4b8b8261e78a9dda97eef7de880a47ea971f3"}},"Out":{"NSXhhhhhh888888888888888488888888822222":{"NOTUS":{"20230110225407708":"2000000000","20230110225408273":"2000000000","20230110225420398":"2000000000"}}},"Validator":"NSX6woSKz9hc4fUtd4K8iJpK99XsK7Y96rArN63"}


            */
            lock (NGF.WalletUsageList)
            {
                bool returnWalletUsing = false;
                if (NGF.WalletUsageList.ContainsKey(ReceiverWalletKey) == true)
                {
                    if (string.Equals(NGF.WalletUsageList[ReceiverWalletKey], airdropUid) == false)
                    {
                        returnWalletUsing = true;
                    }
                }
                else
                {
                    if (NGF.WalletUsageList.TryAdd(ReceiverWalletKey, airdropUid) == false)
                    {
                        returnWalletUsing = true;
                    }
                    else
                    {
                        if (string.Equals(NGF.WalletUsageList[ReceiverWalletKey], airdropUid) == false)
                        {
                            returnWalletUsing = true;
                        }
                    }
                }

                if (returnWalletUsing == true)
                {
                    return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                    {
                        ErrorNo = 36789,
                        ErrorText = "WalletUsing",
                        ID = string.Empty,
                        Result = NVE.BlockStatusCode.WalletUsing
                    });
                }
            }

            //burada değişken geri dönecek
            //NVClass.BlockStruct_125 airDrop = Calculate(ReceiverWalletKey, airdropUid);
            NVClass.BlockStruct_125 airDrop = new();
            // Console.WriteLine("---------------------------------------");
            // Console.WriteLine(JsonSerializer.Serialize(airDrop, NVC.JsonSetting));
            // Console.WriteLine("---------------------------------------");

            bool tmpAddResult = NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
            {
                uid = airdropUid,
                type = NVE.BlockTypeList.AirDrop,
                data = JsonSerializer.Serialize(airDrop)
            });
            if (tmpAddResult == true)
            {
                //RequestList[ReceiverWalletKey].Add(airdropUid);
                //LimitDb.Set(ReceiverWalletKey,
                //JsonSerializer.Serialize(RequestList[ReceiverWalletKey])
                //);

                // burada transactionları belleğe alıyor böyle hızlı ulaşım sağlanıyor...
                NVG.Settings.TxStatus.Set(airdropUid, new NVS.CryptoTransferStatus()
                {
                    Code = NVE.BlockStatusCode.AddedToQueue,
                    RowNo = 0,
                    UID = "",
                    Text = "AddedToQueue"
                });


                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = "AddedToQueue",
                    ID = airdropUid,
                    Result = NVE.BlockStatusCode.AddedToQueue
                });
            }
            NVG.Settings.TxStatus.Set(airdropUid, new NVS.CryptoTransferStatus()
            {
                Code = NVE.BlockStatusCode.Unknown,
                RowNo = 0,
                UID = "",
                Text = "Unknown"
            });

            return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
            {
                ErrorNo = 55632,
                ErrorText = "Unknown",
                ID = string.Empty,
                Result = NVE.BlockStatusCode.Unknown
            });
        }

        public void Start()
        {

        }
        public Deploy()
        {
        }

        ~Deploy()
        {
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                // LimitDb.Dispose();
            }
            catch { }
        }
    }
}
