using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;
namespace Notus.Transaction
{
    public class Pool : IDisposable
    {
        private Queue<Notus.Compiler.TxQueueStruct> TxList = new();
        public void Add(Notus.Compiler.TxQueueStruct NewTx)
        {
            TxList.Enqueue(NewTx);
            /*
            
            control-point-123456
            buraya işlemleri eklesin
            eklediği gibi dağıtma listesine async olarak dağıtsın
            */
        }
        public Pool()
        {
        }
        ~Pool()
        {
            Dispose();
        }
        public void Dispose()
        {
        }
    }
}