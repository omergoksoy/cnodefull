using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NM = Notus.Message;
namespace Notus.Message
{
    //socket-exception
    public class Orchestra : IDisposable
    {
        private NM.Publisher pubObj = new NM.Publisher();
        private Dictionary<string, NM.Subscriber> subListObj = new Dictionary<string, NM.Subscriber>();
        public void Start()
        {
            pubObj.Start();
        }
        public Orchestra()
        {

        }
        ~Orchestra()
        {
            Dispose();
        }
        public void Dispose()
        {
        }
    }
}
