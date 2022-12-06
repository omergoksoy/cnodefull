using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using NP = Notus.Print;
using NP2P = Notus.P2P;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;
namespace Notus.Timer
{
    public class CryptoTransfer : IDisposable
    {
        private Notus.Threads.Timer TimerObj = new Notus.Threads.Timer();
        public CryptoTransfer()
        {
            NP.Basic("Garbage Collector starting");
            /*
            TimerObj.Start(250, () =>
            {
                if (TimeBaseBlockUidList.Count > 20000)
                {
                    TimeBaseBlockUidList.Remove(TimeBaseBlockUidList.First().Key);
                }
                if (NVG.Settings.Nodes.Queue.Count > 20000)
                {
                    NVG.Settings.Nodes.Queue.Remove(NVG.Settings.Nodes.Queue.First().Key);
                }
            });
            */
        }
        public void Dispose()
        {
            if (TimerObj != null)
            {
                try
                {
                    TimerObj.Dispose();
                }
                catch { }
            }
        }
    }
}
