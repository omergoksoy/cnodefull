using System.Collections.Generic;

namespace Notus.ExternalResources.Nethereum.RLP
{
    public class RLPCollection : List<IRLPElement>, IRLPElement
    {
        public byte[] RLPData { get; set; }
    }
}