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
    public class Manager : IDisposable
    {
        private Notus.Threads.Timer TimerObj = new Notus.Threads.Timer();
        public Manager()
        {
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
