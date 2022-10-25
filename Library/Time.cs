using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NGF = Notus.Variable.Globals.Functions;
using NVG = Notus.Variable.Globals;
using ND = Notus.Date;
namespace Notus
{
    public class Time
    {
        public static Notus.Variable.Struct.UTCTimeStruct GetNtpTime(string ntpPoolServer = "pool.ntp.org")
        {
            Notus.Variable.Struct.UTCTimeStruct tmpReturn = new Notus.Variable.Struct.UTCTimeStruct();

            (long pingTime, ulong exactTimeLong) = FindFasterNtpServer();
            tmpReturn.Now = DateTime.Now;
            tmpReturn.pingTime = pingTime;
            tmpReturn.UtcTime = new DateTime(1900, 1, 1).AddMilliseconds(exactTimeLong);
            tmpReturn.ulongUtc = ND.ToLong(tmpReturn.UtcTime);
            //tmpReturn.UtcTime = Notus.Time.GetFromNtpServer(true, ntpPoolServer);
            tmpReturn.ulongNow = Notus.Time.DateTimeToUlong(tmpReturn.Now);

            tmpReturn.After = (tmpReturn.Now > tmpReturn.UtcTime);
            tmpReturn.Difference = (tmpReturn.After == true ? (tmpReturn.Now - tmpReturn.UtcTime) : (tmpReturn.UtcTime - tmpReturn.Now));
            return tmpReturn;
        }
        public static ulong NowNtpTimeToUlong()
        {
            return DateTimeToUlong(NGF.GetUtcNowFromNtp());
        }
        public static DateTime GetFromNtpServer(bool WaitUntilGetFromServer = false, string ntpPoolServer = "pool.ntp.org")
        {
            if (WaitUntilGetFromServer == true)
            {
                long exactTimeLong = 0;
                int count = 0;
                while (exactTimeLong == 0)
                {
                    exactTimeLong = (long)GetExactTime_UTC_SubFunc(ntpPoolServer);
                    if (exactTimeLong == 0)
                    {
                        Thread.Sleep((count > 10 ? 5000 : 500));
                        count++;
                    }
                }
                return new DateTime(1900, 1, 1).AddMilliseconds(exactTimeLong);
            }
            return new DateTime(1900, 1, 1).AddMilliseconds((long)GetExactTime_UTC_SubFunc(ntpPoolServer));
        }
        public static ulong BlockIdToUlong(string blockUid)
        {
            return ulong.Parse(Notus.Block.Key.GetTimeFromKey(blockUid).Substring(0, 17));
        }
        public static ulong NowToUlong(bool milisecondIncluded = true)
        {
            if (milisecondIncluded == true)
            {
                return ulong.Parse(
                    DateTime.Now.ToString(
                        Notus.Variable.Constant.DefaultDateTimeFormatText
                    )
                );
            }
            return ulong.Parse(
                DateTime.Now.ToString(
                    Notus.Variable.Constant.DefaultDateTimeFormatText.Substring(0, 14)
                )
            );
        }
        public static ulong DateTimeToUlong(DateTime ConvertTime)
        {
            return ulong.Parse(
                ConvertTime.ToString(
                    Notus.Variable.Constant.DefaultDateTimeFormatText
                )
            );
        }
        public static ulong DateTimeToUlong(DateTime ConvertTime, bool milisecondIncluded)
        {
            if (milisecondIncluded == true)
            {
                return DateTimeToUlong(ConvertTime);
            }
            return ulong.Parse(
                ConvertTime.ToString(
                    Notus.Variable.Constant.DefaultDateTimeFormatTextWithourMiliSecond
                )
            );
        }

        public static (long, ulong) FindFasterNtpServer()
        {
            string[] serverNameList = {
                "pool.ntp.org",
                "africa.pool.ntp.org",
                "asia.pool.ntp.org",
                "europe.pool.ntp.org",
                "north-america.pool.ntp.org",
                "oceania.pool.ntp.org",
                "south-america.pool.ntp.org"
            };
            const long ticksPerSecond = 10000000L;
            ulong resultVal = 0;
            long smallestPingTime = long.MaxValue;
            string smallestPingServerName = string.Empty;
            foreach (string serverName in serverNameList)
            {
                long pingDuration = 0;
                byte[] ntpData = new byte[48];
                ntpData[0] = 0x1B;
                IPAddress[] addresses = Dns.GetHostEntry(serverName).AddressList;
                bool itsDone = false;
                for (int i = 0; i < addresses.Length; i++)
                {
                    try
                    {
                        if (itsDone == false)
                        {
                            IPEndPoint ipEndpoint = new IPEndPoint(addresses[i], 123);
                            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            socket.ReceiveTimeout = 3000;

                            socket.Connect(ipEndpoint);
                            socket.Send(ntpData);
                            pingDuration = Stopwatch.GetTimestamp();
                            socket.Receive(ntpData);
                            pingDuration = Stopwatch.GetTimestamp() - pingDuration;
                            socket.Close();
                            ulong intPart = ((ulong)ntpData[40] << 24) | ((ulong)ntpData[41] << 16) | ((ulong)ntpData[42] << 8) | ntpData[43];
                            ulong fractPart = ((ulong)ntpData[44] << 24) | ((ulong)ntpData[45] << 16) | ((ulong)ntpData[46] << 8) | ntpData[47];
                            ulong milliseconds = intPart * 1000 + fractPart * 1000 / 0x100000000L;
                            if (smallestPingTime > pingDuration)
                            {
                                long pingTicks = pingDuration * ticksPerSecond / Stopwatch.Frequency;
                                //smallestPingTime = pingDuration;
                                smallestPingTime = pingTicks;
                                smallestPingServerName = serverName;
                                resultVal = milliseconds;
                            }
                            itsDone = true;
                            /*
                            float dddd = (float)(pingDuration * ticksPerSecond / Stopwatch.Frequency) / (float)ticksPerSecond;
                            Console.WriteLine(serverName + " -> " + (pingDuration * ticksPerSecond / Stopwatch.Frequency).ToString());
                            Console.WriteLine(serverName + " -> " + dddd.ToString());
                            */
                        }
                    }
                    catch { }
                }
            }
            NVG.NtpServerUrl = smallestPingServerName;
            return (smallestPingTime, resultVal);
        }
        public static ulong GetExactTime_UTC_SubFunc(string server)
        {
            long pingDuration = 0;
            byte[] ntpData = new byte[48];
            ntpData[0] = 0x1B;
            IPAddress[] addresses = Dns.GetHostEntry(server).AddressList;
            for (int i = 0; i < addresses.Length; i++)
            {
                try
                {
                    IPEndPoint ipEndpoint = new IPEndPoint(addresses[i], 123);
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.ReceiveTimeout = 3000;

                    socket.Connect(ipEndpoint);
                    socket.Send(ntpData);
                    pingDuration = Stopwatch.GetTimestamp();
                    socket.Receive(ntpData);
                    pingDuration = Stopwatch.GetTimestamp() - pingDuration;
                    socket.Close();
                    ulong intPart = ((ulong)ntpData[40] << 24) | ((ulong)ntpData[41] << 16) | ((ulong)ntpData[42] << 8) | ntpData[43];
                    ulong fractPart = ((ulong)ntpData[44] << 24) | ((ulong)ntpData[45] << 16) | ((ulong)ntpData[46] << 8) | ntpData[47];
                    ulong milliseconds = intPart * 1000 + fractPart * 1000 / 0x100000000L;
                    Console.WriteLine(pingDuration);
                    return milliseconds;
                }
                catch { }
            }
            return 0;
        }
    }
}
