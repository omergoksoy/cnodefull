﻿using NVC = Notus.Variable.Constant;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Notus.Wallet
{
    public static class MultiID
    {
        public static bool IsMultiId(string walletId)
        {
            string keyPrefix = Notus.Variable.Constant.MultiWalletPrefix;
            return string.Equals(walletId.Substring(0, keyPrefix.Length),keyPrefix);
        }
        public static string GetWalletID
        (
            string creatorWallet,
            List<string> walletList,
            Notus.Variable.Enum.MultiWalletType walletType,
            Notus.Variable.Enum.NetworkType whichNetworkFor
        )
        {
            walletList.Sort();
            string walletListText = string.Join(NVC.Delimeter, walletList.ToArray());

            string keyPrefix = Notus.Variable.Constant.MultiWalletPrefix;

            Notus.HashLib.Sasha sashaObj = new Notus.HashLib.Sasha();
            string hashCreatorStr =
                Notus.Toolbox.Text.ShrinkHex(sashaObj.Calculate(sashaObj.Calculate(creatorWallet)), 6);

            string hashWalletListText =
                Notus.Toolbox.Text.ShrinkHex(sashaObj.Calculate(sashaObj.Calculate(walletListText)), 16);

            string checkSumStr = Notus.Toolbox.Text.ShrinkHex(
                sashaObj.Calculate(
                    sashaObj.Calculate(
                        creatorWallet + walletListText + walletType.ToString()
                    )
                ), 4
            );

            BigInteger number = BigInteger.Parse(
                "0" + 
                hashCreatorStr + 
                hashWalletListText + 
                checkSumStr,
                NumberStyles.AllowHexSpecifier
            );
            int howManyLen = Notus.Variable.Constant.WalletFullTextLength -
                Notus.Variable.Constant.MultiWalletPrefix.Length;
            string walletAddressStr = Notus.Wallet.Toolbox.EncodeBase58(number, howManyLen);
            return keyPrefix + walletAddressStr;
        }

        public static bool New(
            List<string> walletList,
            string publicKey,
            string sign,
            Notus.Variable.Enum.NetworkType whichNetworkFor = Notus.Variable.Enum.NetworkType.MainNet,
            string curveName = Notus.Variable.Constant.Default_EccCurveName
        )
        {

            return false;
        }
    }
}
