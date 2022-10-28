using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NVC = Notus.Variable.Constant;
//using NT = Notus.Time;
using ND = Notus.Date;
using NVG = Notus.Variable.Globals;
namespace Notus.Communication
{
    public class UDP
    {
        private Dictionary<string, double> timeOut = new Dictionary<string, double>();
        private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 512;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;
        private System.Action<DateTime, string>? Func_OnReceive = null;
        public void OnReceive(System.Action<DateTime, string> Func_OnReceive)
        {

        }
        private void Receive()
        {
            _socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndReceiveFrom(ar, ref epFrom);
                _socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                DateTime suAn = NVG.NOW.Obj;
                //long TamSuAn = long.Parse(suAn.ToString(NVC.DefaultDateTimeFormatText));
                if (Func_OnReceive != null)
                {
                    string gelenZaman = Encoding.ASCII.GetString(so.buffer, 0, bytes);
                    Func_OnReceive(suAn, gelenZaman);
                }
                /*
                string[] income = gelenZaman.Split(':');
                if (string.Equals(income[0], "s"))
                {
                    if (timeOut.ContainsKey(income[1]) == true)
                    {
                        Console.WriteLine("Istenen Sure : {0}", timeOut[income[1]]);
                    }
                    else
                    {
                        Console.WriteLine("Istenen Sure Listede Yok");
                    }
                }

                //zaman gönderildi bana
                if (string.Equals(income[0], "x"))
                {
                    if (long.TryParse(income[2], out long cikis))
                    {
                        double current = 0;
                        if (timeOut.ContainsKey(income[1]) == false)
                        {
                            timeOut.Add(income[1], 0);
                        }
                        current = timeOut[income[1]];
                        double aradakiFark = 0;
                        if (TamSuAn == cikis)
                        {
                            Console.WriteLine("Esit");
                        }
                        else
                        {
                            if (TamSuAn > cikis)
                            {
                                Console.WriteLine("Istemci Geride");
                                aradakiFark = (ND.ToDateTime(TamSuAn) - ND.ToDateTime(cikis)).TotalMilliseconds;
                            }
                            else
                            {
                                Console.WriteLine("Sunucu Geride");
                                aradakiFark = (ND.ToDateTime(cikis) - ND.ToDateTime(TamSuAn)).TotalMilliseconds;
                            }
                            if (current == 0)
                            {
                                current = aradakiFark;
                            }
                            else
                            {
                                current = (current + aradakiFark) / 2;
                            }
                            timeOut[income[1]] = current;
                            Console.WriteLine("Aradaki Fark  : " + aradakiFark.ToString());
                            Console.WriteLine("Ortalama Fark : " + timeOut[income[1]].ToString());
                        }
                        Console.WriteLine("my / client : {0}: {1}", TamSuAn, Encoding.ASCII.GetString(so.buffer, 0, bytes));
                    }
                    else
                    {
                        Console.WriteLine("Hatali Zaman Bilgisi");
                    }
                }
                */
            }, state);
        }
        public UDP(int port = 0)
        {
            if (port > 0)
            {
                Server("", port, true);
            }
        }
        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }
        public void Server(string address, int port, bool useIpAny = false)
        {
            _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            if (useIpAny == true)
            {
                _socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            else
            {
                _socket.Bind(new IPEndPoint(IPAddress.Parse(address), port));
            }
            Receive();
        }
        public void Client(string address, int port)
        {
            _socket.Connect(IPAddress.Parse(address), port);
            Receive();
        }
        public void Send(string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            _socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                State so = (State)ar.AsyncState;
                int bytes = _socket.EndSend(ar);
                //Console.WriteLine(bytes);
            }, state);
        }
    }
}