using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVC = Notus.Variable.Constant;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Coin
{
    public class AirDrop : IDisposable
    {
        private Notus.Mempool ObjMp_AirdropLimit;

        public string Request(NVS.HttpRequestDetails IncomeData)
        {
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

            string airdropStr = NVC.AirDropVolume_Default;
            if (NVC.AirDropVolume.ContainsKey(NVG.Settings.Layer))
            {
                if (NVC.AirDropVolume[NVG.Settings.Layer].ContainsKey(NVG.Settings.Network))
                {
                    airdropStr = NVC.AirDropVolume[NVG.Settings.Layer][NVG.Settings.Network];
                }
            }

            string ReceiverWalletKey = IncomeData.UrlList[1];
            int requestCount = 0;
            string controlStr = ObjMp_AirdropLimit.Get(ReceiverWalletKey, "0");
            if (controlStr.Length > 0)
            {
                if(int.TryParse(controlStr, out int tmpRequestCount))
                {
                    requestCount = tmpRequestCount;
                }
            }

            if (requestCount > 1)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 371854,
                    ErrorText = "TooManyRequest",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.TooManyRequest
                });
            }

            requestCount++;
            ObjMp_AirdropLimit.Set(ReceiverWalletKey, requestCount, 4320, true);

            string tmpCoinCurrency = NVG.Settings.Genesis.CoinInfo.Tag;
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

            if (NGF.Balance.WalletUsageAvailable(ReceiverWalletKey) == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 36789,
                    ErrorText = "WalletUsing",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.WalletUsing
                });
            }

            if (NGF.Balance.StartWalletUsage(ReceiverWalletKey) == false)
            {
                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 27468,
                    ErrorText = "AnErrorOccurred",
                    ID = string.Empty,
                    Result = NVE.BlockStatusCode.AnErrorOccurred
                });
            }

            NVS.WalletBalanceStruct tmpBalanceBefore = NGF.Balance.Get(ReceiverWalletKey, 0);
            NVS.WalletBalanceStruct tmpBalanceAfter = NGF.Balance.Get(ReceiverWalletKey, 0);

            ulong tmpCoinKeyVal = NVG.NOW.Int;
            if (tmpBalanceAfter.Balance[tmpCoinCurrency].ContainsKey(tmpCoinKeyVal) == false)
            {
                tmpBalanceAfter.Balance[tmpCoinCurrency].Add(tmpCoinKeyVal, airdropStr);
            }
            else
            {
                BigInteger tmpResult =
                    BigInteger.Parse(tmpBalanceAfter.Balance[tmpCoinCurrency][tmpCoinKeyVal]) +
                    BigInteger.Parse(airdropStr);
                tmpBalanceAfter.Balance[tmpCoinCurrency][tmpCoinKeyVal] = tmpResult.ToString();
            }

            string tmpChunkIdKey = NGF.GenerateTxUid();

            Notus.Variable.Class.BlockStruct_125 airDrop = new Notus.Variable.Class.BlockStruct_125()
            {
                In = new Dictionary<string, NVS.WalletBalanceStruct>(),
                Out = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>(),
                Validator = NVG.Settings.NodeWallet.WalletKey
            };
            airDrop.In.Add(tmpChunkIdKey, tmpBalanceBefore);
            airDrop.Out.Add(ReceiverWalletKey, tmpBalanceAfter.Balance);

            bool tmpAddResult = NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
            {
                uid = tmpChunkIdKey,
                type = NVE.BlockTypeList.AirDrop,
                data = JsonSerializer.Serialize(airDrop)
            });
            if (tmpAddResult == true)
            {
                NVG.Cache.Transaction.Add(tmpChunkIdKey, NVE.BlockStatusCode.AddedToQueue);

                return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
                {
                    ErrorNo = 0,
                    ErrorText = "AddedToQueue",
                    ID = tmpChunkIdKey,
                    Result = NVE.BlockStatusCode.AddedToQueue
                });
            }
            NVG.Cache.Transaction.Add(tmpChunkIdKey, NVE.BlockStatusCode.Unknown);
            return JsonSerializer.Serialize(new NVS.CryptoTransactionResult()
            {
                ErrorNo = 55632,
                ErrorText = "Unknown",
                ID = string.Empty,
                Result = NVE.BlockStatusCode.Unknown
            });
        }

        public AirDrop()
        {
            ObjMp_AirdropLimit = new Notus.Mempool(
                Notus.IO.GetFolderName(
                    NVG.Settings, NVC.StorageFolderName.Pool
                ) + "airdrop_request");

            ObjMp_AirdropLimit.AsyncActive = false;
        }

        ~AirDrop()
        {
            Dispose();
        }
        public void Dispose()
        {
            try
            {
                ObjMp_AirdropLimit.Dispose();
            }
            catch { }
        }
    }
}
