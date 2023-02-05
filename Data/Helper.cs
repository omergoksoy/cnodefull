//using Microsoft.Data.Sqlite;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
namespace Notus.Data
{
    public static class Helper
    {
        public static void ClearTable(string DbFileName)
        {
            using (Notus.Mempool objMpNodeList = new Notus.Mempool(DbFileName))
            {
                objMpNodeList.AsyncActive = false;
                objMpNodeList.Clear();
                objMpNodeList.Dispose();
            }
        }
    }
}
