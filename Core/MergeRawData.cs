using NVC = Notus.Variable.Constant;
using System;
using System.Collections.Generic;
using NVG = Notus.Variable.Globals;
namespace Notus.Core
{
    public class MergeRawData
    {
        public static string MultiWalletID
        (
            string creatorWallet,
            List<string> walletList,
            Notus.Variable.Enum.MultiWalletType walletType
        )
        {
            walletList.Sort();
            string walletListText = string.Join(NVC.Delimeter, walletList.ToArray());
            string signRawStr =
                creatorWallet + NVC.Delimeter +
                walletListText + NVC.Delimeter +
                walletType.ToString();

            return signRawStr;
        }
        public static string WalletSafe(
            string walletKey,
            string publicKey,
            string pass,
            ulong unlockTime
        )
        {
            return
                walletKey + NVC.Delimeter +
                publicKey + NVC.Delimeter +
                pass + NVC.Delimeter +
                unlockTime.ToString();
        }

        public static Notus.Variable.Struct.GenericSignStruct GenericSign(string PrivateKey)
        {
            string PublicKeyStr = Notus.Wallet.ID.Generate(PrivateKey);
            string TimeStr = NVG.NOW.Obj.ToString(Notus.Variable.Constant.DefaultDateTimeFormatText);
            return new Notus.Variable.Struct.GenericSignStruct()
            {
                PublicKey = PublicKeyStr,
                Time = TimeStr,
                Sign = Notus.Wallet.ID.Sign(PublicKeyStr + TimeStr, PrivateKey)
            };
        }
        public static string Transaction(Notus.Variable.Struct.CryptoTransactionStruct transactionData)
        {
            return Transaction(
                transactionData.Sender,
                transactionData.Receiver,
                transactionData.Volume,
                transactionData.CurrentTime.ToString(),
                transactionData.UnlockTime.ToString(),
                transactionData.Currency
            );
        }
        public static string Transaction(
            string Sender, 
            string Receiver, 
            string Volume, 
            string CurrentTime, 
            string UnlockTime, 
            string Currency
        )
        {
            return Sender + NVC.Delimeter +
            Receiver + NVC.Delimeter +
            Volume + NVC.Delimeter +
            CurrentTime + NVC.Delimeter +
            UnlockTime + NVC.Delimeter +
            Currency;
        }

        public static Notus.Variable.Struct.FileTransferStruct FileUpload(Notus.Variable.Struct.FileTransferStruct uploadFile)
        {

            uploadFile.Sign =
                uploadFile.BlockType.ToString() + NVC.Delimeter +
                uploadFile.FileName + NVC.Delimeter +
                uploadFile.FileSize.ToString() + NVC.Delimeter +
                uploadFile.FileHash + NVC.Delimeter +
                uploadFile.ChunkSize.ToString() + NVC.Delimeter +
                uploadFile.ChunkCount.ToString() + NVC.Delimeter +
                uploadFile.Level.ToString() + NVC.Delimeter +
                Notus.Toolbox.Text.BoolToStr(uploadFile.WaterMarkIsLight) + NVC.Delimeter +
                uploadFile.PublicKey;
            return uploadFile;
        }
        public static string StorageOnChain(Notus.Variable.Struct.StorageOnChainStruct StorageData)
        {

            return 
                StorageData.Name + NVC.Delimeter +
                StorageData.Size.ToString() + NVC.Delimeter +
                StorageData.Hash + NVC.Delimeter +
                Notus.Toolbox.Text.BoolToStr(StorageData.Encrypted) + NVC.Delimeter +
                StorageData.PublicKey;
        }

        public static string ApproveMultiWalletTransaction(bool Approve, string TransactionId, ulong CurrentTime)
        {
            return
                Notus.Toolbox.Text.BoolToStr(Approve) + NVC.Delimeter +
                TransactionId + NVC.Delimeter +
                CurrentTime.ToString().Substring(0, 14);
        }
        public static string TokenGenerate(
            string PublicKey,
            Notus.Variable.Struct.TokenInfoStruct InfoData,
            Notus.Variable.Struct.SupplyStruct TokenSupplyData
        )
        {
            //Notus.Variable.Struct.
            return
                PublicKey + NVC.Delimeter +

                InfoData.Name + NVC.Delimeter +
                InfoData.Tag + NVC.Delimeter +

                    Notus.Toolbox.Text.BoolToStr(InfoData.Logo.Used) + NVC.Delimeter +
                    InfoData.Logo.Base64 + NVC.Delimeter +
                    InfoData.Logo.Url + NVC.Delimeter +
                    InfoData.Logo.Source + NVC.Delimeter +

                TokenSupplyData.Supply.ToString() + NVC.Delimeter +
                TokenSupplyData.Decimal.ToString() + NVC.Delimeter +
                Notus.Toolbox.Text.BoolToStr(TokenSupplyData.Resupplyable);
        }
    }
}
