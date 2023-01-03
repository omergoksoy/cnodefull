using Notus.Communication;
using Notus.Network;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVG = Notus.Variable.Globals;

static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    string fatalErrorText = "";
    if (e != null)
    {
        fatalErrorText = (e.ExceptionObject as Exception).Message + "Unhandled UnhandledExceptionEventArgs Exception";
    }
    if (sender != null)
    {
        fatalErrorText = fatalErrorText + " -> Sender(" + sender.ToString() + ")";
    }
    const string directoryName = "log_list";
    if (Directory.Exists(directoryName) == false)
    {
        Directory.CreateDirectory(directoryName);
    }

    Console.SetError(new StreamWriter(@".\" + directoryName + "\\" + DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss_fff") + ".log"));
    Console.Error.WriteLine(fatalErrorText);
    Console.Error.Close();
    Console.WriteLine();
    Notus.Print.Danger("Fatal Error : " + fatalErrorText);
}

static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
{
    NVG.Settings.NodeClosing = true;
    e.Cancel = true;
    Console.WriteLine();
    NGF.CloseMyNode();
}


//fix-ing-control-point



/*
Notus.Data.KeyValue keyValue = new Notus.Data.KeyValue(new Notus.Data.KeyValueSettings()
{
    Path = "test",
    MemoryLimitCount = 1000,
    Name = "balance"
});

DateTime baslangic = DateTime.Now;
//keyValue.Set("0123456789abcdea", "deger");

for (int i = 0; i < 100000; i++)
{
    keyValue.Set("deneme-" + i.ToString(), "deger-" + i.ToString());
}
Console.WriteLine(DateTime.Now - baslangic);


Console.WriteLine(DateTime.Now);
string rrr = keyValue.Get("deneme-500");
Console.WriteLine(rrr);
rrr = keyValue.Get("deneme-100");
Console.WriteLine(rrr);

Console.ReadLine();
Console.ReadLine();
Console.ReadLine();
*/


System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

// control-point-1453
/*


auto update uygulaması
https://gist.github.com/sbrl/7709dfc5268e1acde6f3


run multiple exe on console app
https://www.codeproject.com/Questions/999027/Multiple-Consoles-in-single-Csharp-Console-Applica

*/
/*
// DLL 'in version bilgisini çekiyor.
Console.WriteLine("{0}", System.Reflection.AssemblyName.GetAssemblyName("Microsoft.Data.Sqlite.dll").Version);
Console.WriteLine("{0}", System.Reflection.AssemblyName.GetAssemblyName("cnodefull.dll").Version);
Console.ReadLine();
*/

/*

_ ile -> 32 saniye
20.12.2022 01:25:10 ->
20.12.2022 01:25:42 ->

deger-100



await kullanmadan -> 39 saniye
20.12.2022 01:25:46 -> 14
20.12.2022 01:26:25 -> 25

deger-100


await ile -> 38 saniye
20.12.2022 01:25:52 -> 8
20.12.2022 01:26:30 -> 30

deger-100

*/


/*
YAPILACAK İŞLEMLER

- belirli periyotlarda diğer nodelar ile haberleşip blokların doğru bir şekilde oluşturulduğu kontrol edilmeli

- SYNC class'ları için "Balance.Cs" sınıfı eklenmeli ve bu sınıf ile cüzdan bakiyeleri ve kilitli cüzdanlar
  liste halinde tutulmalı
  bellek optimizasyonu için yakın zamanda kullanımda olan cüzdanlar bellekte tutulmalı

- blok oluşturma işlemi için ayrılan 200 milisaniye süresi azaltılabilir çünkü node kendi sırasının 
  geleceğini önceden bildiği için kendi sırası geldiğinde doğrudan blok oluşturma işlemine geçebilir



YAPILDI

- çakışmaların sebebi şu : node'un sırası gelip bloğu oluşturdaktan sonra diğer nodelara oluşturduğu
  bloğun numarasını gönderiyor ( distribute ) Bu gönderim esnasında soket bağlantısının kurulması
  yaklaşık 0.4 saniye sürüyor. Bu da sonraki node'un blok oluşturması için önceki bloğun bilgisini
  geç öğrenmesini sağlıyor ve böylece aynı blok numaralı bloklar oluşuyor.
  çözüm için kuyruktaki nodeların birbirleri ile soket bağlantısı kurmasını ve bağlantının o esnada
  açık kalmasını sağlamak gerekir.

- her kuyruğun 4. turunda sıralamayı karıştırmak için birinci bloğun UID değeri özete eklenmeli

- gelen blokların geçerli cüzdan sahibi node tarafından gönderildiğinden emin olarak zincire ekleyeceğiz.
  blok yapısının içine bloğu oluşturan node'un imzasını ekleyerek bir başka node'un o node için sahte blok oluşturması
  engellenmeli

- belirli periyotlarda zaman sunucusuna bağlanarak zaman değeri güncellenmeli

*/
/*
Console.CancelKeyPress += delegate {
    // call methods to clean up
};


var fastServerName = Notus.Time.FindFasterNtpServer();
Console.WriteLine(fastServerName);
Console.ReadLine();

 

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

//group-no-exception
// bu istisna blok içine eklenecek gup numaralarını bulmak için eklendi

//sync-control
//node'ların blok senkronizasyonu kontrol ediliyor

//tgz-exception
// TGZ ile ilgili eklenen kontrol noktaları

//block-order-exception
// blok sırasını yapan kontrol noktaları

//fast-empty-block-generation
// deneme amaçlı 15 saniyede bir empty blok oluşturuluyor
// böylece wrong validator sorununu çözmeyi hedefliyoruz.

// distrubute-node-control
// eğer node kontrolsüz bir şekilde kapatılırsa,
// burası ve empty blok oluşturma ile ilgili kontrol yapılırken hata olmadığı için
// empty blok oluşturulmaya devam ediyor

/*

ZK - konusu için incelenecek
* reed solomon fingerprinting
* freivalds algorithm
* univariate lagrange interpolation

*/
/*
var id = Notus.Wallet.ID.GenerateKeyPair();
Console.WriteLine(JsonSerializer.Serialize(id));
Console.ReadLine();
*/

/*
23:01:30.327 L1-Dev  -> Activated DevNET for Layer 1 ( Crypto Layer )
23:01:30.327 L1-Dev  -> Starting As Main Node
23:01:30.327 L1-Dev  -> Node P2P Port No : 5012
23:01:30.327 L1-Dev  -> Listining : http://13.229.56.127:5002/
23:01:30.327 L1-Dev  -> Http Has Started
23:01:30.327 L1-Dev  -> Waiting For Time Sync
23:02:03.097 L1-Dev  -> Time Sync Is Done
23:02:03.132 L1-Dev  -> Time Synchronizer Has Started
23:02:03.205 L1-Dev  -> NTP Time Synchronizer Has Started
23:02:03.232 L1-Dev  -> Checking From -> 3.75.110.186
23:02:03.734 L1-Dev  -> The JSON value could not be converted to Notus.Variable.Class.BlockData. Path: $ | LineNumber: 0 | BytePositionInLine: 5.
23:02:03.799 L1-Dev  -> Error Happened While Trying To Get Genesis From Other Node
23:02:03.838 L1-Dev  -> Checking From -> 3.68.233.67
23:02:24.904 L1-Dev  -> Notus.Core.Function.Get -> Line 116 -> Baglanilan uygun olarak belli bir süre içinde yanit vermediginden veya kurulan
baglanti baglanilan ana bilgisayar yanit vermediginden bir baglanti kurulamadi. (3.68.233.67:5002)
23:02:24.937 L1-Dev  -> Error Happened While Trying To Get Genesis From Other Node
23:02:24.998 L1-Dev  -> Checking From -> 3.75.243.44
23:02:46.029 L1-Dev  -> Notus.Core.Function.Get -> Line 116 -> Baglanilan uygun olarak belli bir süre içinde yanit vermediginden veya kurulan
baglanti baglanilan ana bilgisayar yanit vermediginden bir baglanti kurulamadi. (3.75.243.44:5002)
23:02:46.105 L1-Dev  -> Error Happened While Trying To Get Genesis From Other Node
23:02:46.136 L1-Dev  -> Checking From -> 3.125.159.102
23:03:07.208 L1-Dev  -> Notus.Core.Function.Get -> Line 116 -> Baglanilan uygun olarak belli bir süre içinde yanit vermediginden veya kurulan
baglanti baglanilan ana bilgisayar yanit vermediginden bir baglanti kurulamadi. (3.125.159.102:5002)
23:03:07.251 L1-Dev  -> Error Happened While Trying To Get Genesis From Other Node
23:03:07.299 L1-Dev  -> Checking From -> 18.156.37.61
23:03:28.298 L1-Dev  -> Notus.Core.Function.Get -> Line 116 -> Baglanilan uygun olarak belli bir süre içinde yanit vermediginden veya kurulan
baglanti baglanilan ana bilgisayar yanit vermediginden bir baglanti kurulamadi. (18.156.37.61:5002)
23:03:28.328 L1-Dev  -> Error Happened While Trying To Get Genesis From Other Node
Obj_Integrity.ControlGenesisBlock : FALSE
23:03:29.506 L1-Dev  -> All Blocks Checked With Quick Method
23:03:29.720 L1-Dev  -> Pool Loaded From Local DB
23:03:29.807 L1-Dev  -> Last Block Row No : 1
23:03:30.507 L1-Dev  -> All Blocks Loaded
23:03:30.535 L1-Dev  -> Main Validator Started
23:03:30.599 L1-Dev  -> Node Sync Starting
23:03:30.631 L1-Dev  -> Finding Online Nodes
23:03:51.831 L1-Dev  -> Removing Offline Nodes From List
23:05:17.101 L1-Dev  -> Node List Sync With Other Nodes
23:05:17.507 L1-Dev  -> Send My Node Full Info
23:05:17.814 L1-Dev  -> Ask Other Nodes Full Info
23:05:18.112 L1-Dev  -> Distribution Control Timer Has started
CalculateReadySign    [sign] : 3044022076da22d30925945f403c4d820fd3ae214f74f22df9df162726df978b63e981af02204fbf38552140536a42bfbba56853416bb3f30e2035dc5642a5dc3aac14fe547d
CalculateReadySign [private] : 657a3279c9ef660b5e4af412eed5382c6760b025bec8ed908d9950386e59e992
CalculateReadySign  [public] : dc9a87e9fd22514f1117079c75e4e64e342addf80874c36acff42c4755f0a33f10b1feb8d7daa5e5efefc6dcf4a72a315673de7ae06c47d9ed898068c294d16f
                               904d32c351be9319d6931dd4a9bc11e585d4800e277f6ba5a7f1b3d1c4a562d599d35490382485c3d739b714f1d81ba1e0364f90c9c48e673d7a3936b48e8afc
CalculateReadySign     [raw] : 20230102230518098:NSX4SmDpuvRG9JhBtJR9Qm51Z9W6maY3Hrnc6UA
CalculateReadySign  [verify] : False
23:05:18.141 L1-Dev  -> Wait Until Nodes Available
3.75.110.186 -> [0]
[
  "NSX4Jcf2RSTW8uazxkFNo9bWwfQr4EDqkmBSLs7",
  "20230102230555297",
  "304502205e149f154c96c6610ffc3d771cd794ac454f07ba06d38e79ece6796dfa92efeb022100e03688bb12d803425c91bffbc46d755f8e6772441f674be5f6249ce731d04041"
  "3046022100df4d3d7e478f6bcd648e91d6859ef4b46a63bc665a4fc6eb6b8988c9268106fd02210092dd0b6ea75a7bf1b487a45a304a15506ee0673d1a0b49e2afa5545e0df3eb5a"
]
Esit
NOT VERIFIED

//string SessionPrivateKey = Notus.Wallet.ID.New();
string SessionPrivateKey = "657a3279c9ef660b5e4af412eed5382c6760b025bec8ed908d9950386e59e992";
string PublicKey = Notus.Wallet.ID.GetPublicKeyFromPrivateKey(SessionPrivateKey);
string rawDataText = "20230102230518098:NSX4SmDpuvRG9JhBtJR9Qm51Z9W6maY3Hrnc6UA";
// string PublicKey= "dc9a87e9fd22514f1117079c75e4e64e342addf80874c36acff42c4755f0a33f10b1feb8d7daa5e5efefc6dcf4a72a315673de7ae06c47d9ed898068c294d16f";

string controlSignForReadyMsg = Notus.Wallet.ID.Sign(rawDataText, SessionPrivateKey);

string calcSign=Notus.Wallet.ID.Sign(rawDataText, NVG.SessionPrivateKey);
bool status=Notus.Wallet.ID.Verify(rawDataText, controlSignForReadyMsg, PublicKey);
Console.WriteLine("CalculateReadySign    [sign] : " + controlSignForReadyMsg);
Console.WriteLine("CalculateReadySign [private] : " + SessionPrivateKey);
Console.WriteLine("CalculateReadySign  [public] : " + PublicKey);
Console.WriteLine("CalculateReadySign     [raw] : " + rawDataText);
Console.Write("CalculateReadySign  [verify] : ");
Console.WriteLine(status);
Console.ReadLine();
*/
Notus.Validator.Node.Start(args);


