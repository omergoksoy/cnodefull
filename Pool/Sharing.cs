using NVS = Notus.Variable.Struct;
using System;
using System.Text.Json;
using NVClass = Notus.Variable.Class;
namespace Notus.Pool
{
    public static class Sharing
    {
        public static void Distribute(NVS.HttpRequestDetails IncomeData)
        {
            string incomeDataStr = JsonSerializer.Serialize(IncomeData);
            Console.WriteLine(incomeDataStr);
        }
    }
}
