using System.Runtime.ExceptionServices;
using System.Text.Json;

static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    string fatalErrorText = (e.ExceptionObject as Exception).Message + "Unhandled UnhandledExceptionEventArgs Exception -> Sender(" + sender.ToString() + ")";
    Notus.Print.Log(
        Notus.Variable.Enum.LogLevel.Fatal,
        1,
        fatalErrorText,
        "FATAL-ERROR",
        null,
        null
    );
    Console.WriteLine(fatalErrorText);
    Console.WriteLine(sender.ToString());
    Console.WriteLine("press enter to continue");
    Console.ReadLine();
}

// DLL 'in version bilgisini çekiyor.
// Console.WriteLine("{0}", System.Reflection.AssemblyName.GetAssemblyName("Microsoft.Data.Sqlite.dll").Version);

AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

//int.TryParse("", out int a);
//int a = int.Parse("");
//Console.WriteLine(a);
/*
Console.ReadLine();

 

{"In":{"1348c50251860b3ffba48ed509dd56d1fc843f7413b7a8718e532b6c556b4f6b9dfd73eff6e2fa889229e95275":{"Sender":{"Wallet":"NODcYi8wprUNXbbKu5HoNnnv3BxYedXw7HkpcLf","Balance":{"NOTUS":{"16777216000000000":"997795999834700","20221005001052":"1000000000","20221005001117":"500000000","20221006121135":"100000000","20221006200344":"1000000000","20221006200412":"100000","20221007213442":"12000000","20221007213513":"99000000","20221007213740":"11200000","20221008053533":"200000000"}},"WitnessRowNo":8152,"WitnessBlockUid":"1348c502510800423c682a9e7ee53eebdf080586808b03a91900203aabdd7545dd633458269a830820a732f952"},"Receiver":{"Wallet":"NODZZGAaJdnJUPyAVyR8CLU2zHwamtXr4XW2VqU","Balance":{"NOTUS":{"20221008181942038":"0"}},"WitnessRowNo":0,"WitnessBlockUid":""},"CurrentTime":20221008181939074,"Currency":"NOTUS","Volume":"2000000000","Fee":"150","PublicKey":"3cfed72489b0688a2234ff07ba82f891681379942df1a74576d39ccfbfb3d2d7f4f6ad18e84068a0a66a2ecc58550bb69ba5eaae5252ae6fcd8fa40e961e75d4","Sign":"3046022100d09b5eb1a933744a74d0385a1af7dc38d3bb6848b77ed4c6fd1b7c54eb8636790221008f4494ad2ca0ec07eb85756f8c419d4060166c82ef37d5574bd204aaf11a27eb"}},"Out":{"NODcYi8wprUNXbbKu5HoNnnv3BxYedXw7HkpcLf":{"NOTUS":{"16777216000000000":"997793999834550","20221005001052":"1000000000","20221005001117":"500000000","20221006121135":"100000000","20221006200344":"1000000000","20221006200412":"100000","20221007213442":"12000000","20221007213513":"99000000","20221007213740":"11200000","20221008053533":"200000000"}},"NODZZGAaJdnJUPyAVyR8CLU2zHwamtXr4XW2VqU":{"NOTUS":{"20221008181939074":"2000000000"}}},"Validator":{"Reward":"150","NodeWallet":"NODCNaXc2hJcsCS4Wuo85W8PQQ9umgf9kms7bDF"}}
 
 
Notus.Mempool MP_BlockPoolList = new Notus.Mempool(
    Notus.IO.GetFolderName( Notus.Variable.Enum.NetworkType.DevNet,
     Notus.Variable.Enum.NetworkLayer.Layer1,
    Notus.Variable.Constant.StorageFolderName.Common) +
    Notus.Variable.Constant.MemoryPoolName["BlockPoolList"]
);

Console.ReadLine();
*/
//OMERGOKSOY
// kontrol edilecek alanlar

//tgz-exception
// TGZ ile ilgili eklenen kontrol noktaları

//block-order-exception
// blok sırasını yapan kontrol noktaları

//tgz-exception
Notus.Validator.Node.Start(args);