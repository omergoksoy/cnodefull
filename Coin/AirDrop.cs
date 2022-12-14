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
using NVS = Notus.Variable.Struct;
namespace Notus.Coin
{
    public class AirDrop : IDisposable
    {
        public AirDrop()
        {
            AirDrop drop işlemi buraya alınacak
            bu class doğrudan api kitaplığı içinden çağrılacak
        }

        public Notus.Variable.Struct.WalletBalanceStruct? Get(string WalletId)
        {
            return null;
        }
        ~AirDrop()
        {
            Dispose();
        }
        public void Dispose()
        {
           
        }
    }
}
