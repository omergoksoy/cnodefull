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

namespace Notus.Data.Sharding
{
    public static class Node
    {

        public static string BelongsToMe(string walletId)
        {
            DateTime baslangic = DateTime.Now;
            Notus.HashLib.SHA1 hashObj = new Notus.HashLib.SHA1();
            hashObj.Calculate(System.Text.Encoding.UTF8.GetBytes(walletId));
            return "";
        }
    }
}
