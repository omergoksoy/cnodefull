using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Notus.Communication;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;
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

static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
{
    NVG.Settings.NodeClosing = true;
    e.Cancel = true;

    Console.WriteLine();
    NP.Warning(NVG.Settings, "Please Wait While Node Terminating");
    if (NVG.Settings.CommEstablished == true)
    {
        NGF.SendKillMessage();
        NVG.Settings.ClosingCompleted = true;
        while (NVG.Settings.ClosingCompleted == false)
        {
            Thread.Sleep(10);
        }
    }
    Environment.Exit(0);
}
AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);


/*


udp client burası ile çalışacak

https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.udpclient?view=net-6.0
*/

string myIpAddress = Notus.Toolbox.Network.GetPublicIPAddress();
Notus.Communication.UDP joinObj = new Notus.Communication.UDP();
joinObj.Client("13.229.56.127", 25000);
for (int i = 0; i < 10; i++)
{
    joinObj.Send("a:deneme:" + myIpAddress);
    Thread.Sleep(550);
}
joinObj = null;
//Console.ReadLine();


Notus.Communication.UDP serverObj = new Notus.Communication.UDP();
//serverObj.Server("89.252.134.111", 27000, true);
serverObj.Server("", 27000, true);
serverObj.OnReceive((incomeTime, incomeText) =>
{
    Console.WriteLine(incomeTime);
    Console.WriteLine(incomeText);
/*
    string[] sDizi = incomeText.Split(":");
    if (string.Equals(sDizi[0], "k"))
    {
        // k : cuzdan_adresi
        conIp.TryRemove(sDizi[1], out _);
        conList.TryRemove(sDizi[1], out _);
    }

    if (string.Equals(sDizi[0], "a"))
    {
        // a : cuzdan_addresi : ip_adresi
        if (conList.ContainsKey(sDizi[1]) == false)
        {
            conIp.TryAdd(sDizi[1], sDizi[2]);
            conList.TryAdd(sDizi[1], new Communication.UDP()
            {

            });

            conList[sDizi[1]].Client(
                conIp[sDizi[1]],
                27000
            );
        }
    }
*/
});

Console.ReadLine();

/*
var socket = new Socket(SocketType.Dgram, ProtocolType.Udp);
var ep = new IPEndPoint(IPAddress.Parse("13.229.56.127"),25000);
//socket.BeginReceive
//joinObj.Client("89.252.134.111", 25000);
//joinObj.Client("3.75.110.186", 41324);
//joinObj.OnReceive((incomeTime, incomeText) =>
//{
//    Console.WriteLine("Income Text : " + incomeText);
//    Console.WriteLine("income time : " + incomeTime);
//});
for (int i = 0; i < 5; i++)
{
    Console.WriteLine("Sending");
    socket.SendTo(System.Text.Encoding.ASCII.GetBytes("hello:" + ep.Address + ":" + "41235"), ep);
    Thread.Sleep(1500);
}
*/
Console.ReadLine();
//TimeSyncAddingCommPort
/*
Console.WriteLine(DateTime.UtcNow);
Console.CancelKeyPress += delegate {
    // call methods to clean up
};
*/

//NGF.UpdateUtcNowValue();
//ulong suAn = NVG.NOW.Int;

/*
var fastServerName = Notus.Time.FindFasterNtpServer();
Console.WriteLine(fastServerName);
Console.ReadLine();
*/

/*
öncelikle node'ların zaman bilgileri alınsın
alınan zaman bilgileri ile NTP zaman farkı karşılaştırılsın
iki node arasındaki zaman farkı toplansın böylece iki node
arasındaki tüm zaman farkı eşitlenecek

Örneğin;
node-1 UTC saatinin alındığı zaman : 14:45:12.012 ise
    ve ping timeout süresi         : 0.65429 ise

node-2 UTC saatinin alındığı zaman : 14:46:28.873 ise
    ve ping timeout süresi         : 0.23510 ise

bu iki node arasındaki süre 

*/

/*


string SessionPrivateKey = Notus.Wallet.ID.New();
var sign1=Notus.Wallet.ID.Sign("omer", SessionPrivateKey);
var sign2=Notus.Wallet.ID.Sign("omer", SessionPrivateKey);
var sign3=Notus.Wallet.ID.Sign("omer", SessionPrivateKey);
Console.WriteLine(SessionPrivateKey);
Console.WriteLine(sign1);
Console.WriteLine(sign2);
Console.WriteLine(sign3);
Console.ReadLine();
Console.ReadLine();
Console.ReadLine();
ulong baslangicSayi = Notus.Time.DateTimeToUlong(new DateTime(2022, 10, 12, 00, 55, 20));
ulong simdiSayi     = Notus.Time.DateTimeToUlong(new DateTime(2022, 10, 15, 01, 35, 36));
ulong fark1 = simdiSayi - baslangicSayi;
ulong count = fark1 / 500;

Console.WriteLine(baslangicSayi);
Console.WriteLine(simdiSayi);
Console.WriteLine(fark1);
Console.WriteLine(count);

Console.ReadLine();
10800227,0112
10800,2270112
03:00:00.2270112



11421741
11421,741
03:10:21.7410000



11320052
11320,052
11320052
03:08:40.0520000


*/

/*
DateTime suan = NVG.NOW.Obj;

Console.WriteLine(suan.ToString("yyyy MM dd HH:mm:ss - fff"));
suan = suan.AddMilliseconds(100);
Console.WriteLine(suan.ToString("yyyy MM dd HH:mm:ss - fff"));
Console.ReadLine();
Console.ReadLine();
*/
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

//sync-disable-exception
// senkronizasyon düzeltmesi için yapılan devre dışı bırakma işlemleri

//tgz-exception
Notus.Validator.Node.Start(args);