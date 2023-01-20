/*
//NP.Basic("transactionCount : " + transactionCount.ToString());
if (transactionCount > 0)
{
    foreach (KeyValuePair<string, Dictionary<string, Dictionary<ulong, string>>> walletEntry in tmpBlockCipherData.Out)
    {
        foreach (KeyValuePair<string, Dictionary<ulong, string>> currencyEntry in walletEntry.Value)
        {
            List<ulong> tmpRemoveList = new List<ulong>();
            foreach (KeyValuePair<ulong, string> balanceEntry in currencyEntry.Value)
            {
                if (balanceEntry.Value == "0")
                {
                    tmpRemoveList.Add(balanceEntry.Key);
                }
            }
            for (int innerForCount = 0; innerForCount < tmpRemoveList.Count; innerForCount++)
            {
                tmpBlockCipherData.Out[walletEntry.Key][currencyEntry.Key].Remove(tmpRemoveList[innerForCount]);
            }
        }
    }
    tmpBlockCipherData.Validator.Reward = totalBlockReward.ToString();
    Console.WriteLine("");
    Console.WriteLine(JsonSerializer.Serialize(tmpBlockCipherData));
    Console.WriteLine("--------  COIN TRANSFER DATA CHANGE -----------");

    // crypto / token transfer
    NGF.BlockQueue.Add(new NVS.PoolBlockRecordStruct()
    {
        uid = NGF.GenerateTxUid(),
        type = NVE.BlockTypeList.CryptoTransfer,
        data = JsonSerializer.Serialize(tmpBlockCipherData)
    });
}



*/