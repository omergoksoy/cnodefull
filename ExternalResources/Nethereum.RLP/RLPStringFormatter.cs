using System;
using System.Text;

namespace Notus.ExternalResources.Nethereum.RLP
{
    public class RLPStringFormatter
    {
        public static string Format(IRLPElement element)
        {
            var output = new StringBuilder();
            if (element == null)
                throw new Exception("RLPElement object can't be null");
            if (element is RLPCollection rlpCollection)
            {
                output.Append("[");
                foreach (var innerElement in rlpCollection)
                    Format(innerElement);
                output.Append("]");
            }
            else
            {
                output.Append(Notus.Convert.Byte2Hex(element.RLPData) + ", ");
            }
            return output.ToString();
        }
    }
}