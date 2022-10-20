﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Notus
{
    public static class Date
    {
        public static ulong AddMiliseconds(ulong convertTime, int miliseconds)
        {
            return AddMiliseconds(convertTime, (ulong)miliseconds);
        }
        public static ulong AddMiliseconds(ulong convertTime,ulong miliseconds)
        {
            return Notus.Date.ToLong(Notus.Date.ToDateTime(convertTime).AddMilliseconds(miliseconds));
        }
        public static ulong ToLong(string convertTime)
        {
            return ulong.Parse(convertTime.PadRight(17, '0').Substring(0, 17));
        }
        public static ulong ToLong(DateTime convertTime)
        {
            return ulong.Parse(convertTime.ToString(Variable.Constant.DefaultDateTimeFormatText));
        }

        public static string ToString(DateTime DateTimeObj)
        {
            try
            {
                return DateTimeObj.ToString(Variable.Constant.DefaultDateTimeFormatText);
            }
            catch(Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    988550000,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
                return "19810125020000000";
            }
        }
        public static DateTime ToDateTime(ulong ConverTime)
        {
            return ToDateTime(ConverTime.ToString().PadRight(17, '0'));
        }
        public static DateTime ToDateTime(string DateTimeStr)
        {
            try
            {
                return DateTime.ParseExact(DateTimeStr.Substring(0, 17), Variable.Constant.DefaultDateTimeFormatText, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch(Exception err)
            {
                Notus.Print.Log(
                    Notus.Variable.Enum.LogLevel.Info,
                    900000044,
                    err.Message,
                    "BlockRowNo",
                    null,
                    err
                );
                return new DateTime(1981, 01, 25, 2, 00, 00);
            }
        }
        public static void SleepWithoutBlocking(int SleepTime, bool UseAsSecond = false)
        {
            DateTime NextTime = (UseAsSecond == true ? DateTime.Now.AddSeconds(SleepTime) : DateTime.Now.AddMilliseconds(SleepTime));
            while (NextTime > DateTime.Now)
            {
            }
        }
        public static DateTime GetGenesisCreationTimeFromString(Notus.Variable.Class.BlockData blockData)
        {
            Notus.Variable.Genesis.GenesisBlockData currentGenesisData = JsonSerializer.Deserialize<Notus.Variable.Genesis.GenesisBlockData>(
                System.Text.Encoding.ASCII.GetString(
                    System.Convert.FromBase64String(
                        blockData.cipher.data
                    )
                )
            );
            return currentGenesisData.Info.Creation;
        }
    }
}
