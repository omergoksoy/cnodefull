using System.Globalization;
using System.Text.Json;
using ND = Notus.Date;
using NP = Notus.Print;
using NGF = Notus.Variable.Globals.Functions;
using NVC = Notus.Variable.Constant;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;

namespace Notus.Communication
{
    public static class Helper
    {
        public static string SendMessageED(string nodeHex, NVS.NodeInfo ipInfo, string messageText)
        {
            return SendMessageED(nodeHex, ipInfo.IpAddress, ipInfo.Port, messageText);
        }
        public static string SendMessageED(string nodeHex, string ipAddress, int portNo, string messageText)
        {
            (bool worksCorrent, string incodeResponse) = Notus.Communication.Request.PostSync(
                Notus.Network.Node.MakeHttpListenerPath(ipAddress, portNo) +
                "queue/node/" + nodeHex,
                new Dictionary<string, string>()
                {
                    { "data",messageText }
                },
                2,
                true,
                false
            );
            if (worksCorrent == true)
            {
                Console.WriteLine(ipAddress + " -> " + messageText + " -> " + incodeResponse);
                return incodeResponse;
            }
            Console.WriteLine(ipAddress + " -> " + messageText + " -> NOT RESPONSE");
            return string.Empty;
        }
        public static string SendMessage(NVS.NodeInfo receiverIp, string messageText, string nodeHexStr = "")
        {
            return NGF.SendMessage(receiverIp.IpAddress, receiverIp.Port, messageText, nodeHexStr);
        }

        public static string SendMessageED(string nodeHex, NVS.IpInfo nodeInfo, string messageText)
        {
            return SendMessageED(nodeHex, nodeInfo.IpAddress, nodeInfo.Port, messageText);
        }
    }
}
