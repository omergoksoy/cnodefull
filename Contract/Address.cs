using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NC = Notus.Convert;
using ND = Notus.Date;
using NER = Notus.Encode.RLP;
using NGF = Notus.Variable.Globals.Functions;
using NH = Notus.HashLib;
using NVC = Notus.Variable.Constant;
using NVClass = Notus.Variable.Class;
using NVE = Notus.Variable.Enum;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
using NWT = Notus.Wallet.Toolbox;

namespace Notus.Contract
{
    public static class Address
    {
        public static string Generate(string walletId, long nonce)
        {
            string contractRLPText = NER.Encode(
                new List<string>() {
                    walletId,
                    nonce.ToString()
                }
            );

            using (NH.BLAKE3 hasher = new NH.BLAKE3())
            {
                byte[]? hashed = hasher.ComputeHash(
                    System.Text.Encoding.ASCII.GetBytes(
                        contractRLPText
                    )
                );

                if (hashed == null)
                    return string.Empty;

                BigInteger number = BigInteger.Parse(
                    "0" +
                    NC.Byte2Hex(hashed),
                    NumberStyles.AllowHexSpecifier
                );
                string contractAddressText = NWT.EncodeBase58(
                    number,
                    NVC.WalletEncodeTextLength
                );
                return NVC.ContractAddressPrefix + contractAddressText;
            }
        }
    }
}
