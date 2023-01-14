using NVClass = Notus.Variable.Class;
using RocksDbSharp;
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
using NVS = Notus.Variable.Struct;
using System.IO.Compression;

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



akıllı kontrat kodlarını gömmek için bi tane blok yapısı oluştur.
blok tip numarası 200 olsun




/*






boyut testi yapmak için oluşturulmuş kod grubu



string ZipFileName = "transfer_1000.zip";
FileMode fileModeObj = FileMode.Open;
ZipArchiveMode zipModeObj = ZipArchiveMode.Update;
if (File.Exists(ZipFileName) == false)
{
    fileModeObj = FileMode.Create;
    zipModeObj = ZipArchiveMode.Create;
}


//string rawData = "{'In':{'134afdd31db0080062d3a85fcca69b3ca52cc05d1d36b2c01aea3f2d2e598c25b0417c5fd18b40ac6559b8b29a':{'Wallet':'NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT','Balance':{'NOTUS':{'20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000'},'NOTUS':{'20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000'},'NOTUS':{'20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000'}},'RowNo':0,'UID':''}},'Out':{'NSX4cPr9DkwDKkB4oEom13MeAm388awj1JfQxQT':{'NOTUS':{'20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000'},'NOTUS':{'20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000'},'NOTUS':{'20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000','20230109204208538':'2000000000'}}},'Validator':'NSX4jmTMPuq5JZnGrXb2DsGxt85B5svJL8PnFKb'}";
string rawData = "";
const int length = 2400;
const string chars = "'{:},QWERTYUOPASDFGHJKLİZXCVBNMABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
NVClass.BlockStruct_120 tmpBlockCipherData = new Notus.Variable.Class.BlockStruct_120()
{
    In = new Dictionary<string, Notus.Variable.Class.BlockStruct_120_In_Struct>(),
    Out = new Dictionary<string, Dictionary<string, Dictionary<ulong, string>>>(),
    Validator = new Notus.Variable.Struct.ValidatorStruct()
};
for (int i = 0; i < 1000; i++) {
    string blockFileName = NGF.GenerateTxUid();
    string senderWalletKey = "SENDER_8xuArjhNQ2DDviVWpTSyS8r9Rv7sATAH" + i.ToString();
    string receiverWalletKey = "RECEIVER_uArjhNQ2DDviVWpTSyS8r9Rv7sATAH" + i.ToString();
    tmpBlockCipherData.In.Add(blockFileName, new NVClass.BlockStruct_120_In_Struct()
    {
        Fee = "5454",
        PublicKey = "04b8430f330f2132176e09e66723f11fa158053e0f7ef3dfe1628144aaf3132e1a2d237e710e90b5dd2a17ec20119e10d037b0d158461aec96abaaf74e9b748a40",
        Sign = "3046022100dcdd055d5365833cfb0eeadb7e805248e388dd5b7a1099180e12f701497387c4022100a2847c0cc67f32f473a24eb2c73006f152920e6fe544e42085744dd9553f6419",
        CurrentTime = 202212121212123565,
        Volume = "123131321312",
        Currency = "notus",
        Receiver = new NVClass.WalletBalanceStructForTransaction()
        {
            Balance = new Dictionary<string, Dictionary<ulong, string>>(),
            Wallet = senderWalletKey,
            WitnessBlockUid = blockFileName,
            WitnessRowNo = 4
        },
        Sender = new NVClass.WalletBalanceStructForTransaction()
        {
            Balance = new Dictionary<string, Dictionary<ulong, string>>(),
            Wallet = receiverWalletKey,
            WitnessBlockUid = blockFileName,
            WitnessRowNo = 4
        }
    });
    tmpBlockCipherData.In[blockFileName].Sender.Balance.Add("notus", new Dictionary<ulong, string>() { });
    tmpBlockCipherData.In[blockFileName].Sender.Balance["notus"].Add(20230111195310191, "365465464654");
    tmpBlockCipherData.In[blockFileName].Sender.Balance["notus"].Add(20230111195310192, "365465464654");
    tmpBlockCipherData.In[blockFileName].Sender.Balance["notus"].Add(20230111195310193, "365465464654");
    tmpBlockCipherData.In[blockFileName].Sender.Balance["notus"].Add(20230111195310194, "365465464654");
    tmpBlockCipherData.In[blockFileName].Sender.Balance["notus"].Add(20230111195310195, "365465464654");
    tmpBlockCipherData.In[blockFileName].Sender.Balance["notus"].Add(20230111195310196, "365465464654");
    tmpBlockCipherData.In[blockFileName].Sender.Balance["notus"].Add(20230111195310197, "365465464654");
    tmpBlockCipherData.In[blockFileName].Sender.Balance["notus"].Add(20230111195310198, "365465464654");
    tmpBlockCipherData.In[blockFileName].Sender.Balance["notus"].Add(20230111195310199, "365465464654");
    tmpBlockCipherData.In[blockFileName].Sender.Balance["notus"].Add(20230111195310190, "365465464654");
    tmpBlockCipherData.In[blockFileName].Receiver.Balance.Add("notus", new Dictionary<ulong, string>() { });
    tmpBlockCipherData.In[blockFileName].Receiver.Balance["notus"].Add(20230111195310191, "365465464654");
    tmpBlockCipherData.In[blockFileName].Receiver.Balance["notus"].Add(20230111195310192, "365465464654");
    tmpBlockCipherData.In[blockFileName].Receiver.Balance["notus"].Add(20230111195310193, "365465464654");
    tmpBlockCipherData.In[blockFileName].Receiver.Balance["notus"].Add(20230111195310194, "365465464654");
    tmpBlockCipherData.In[blockFileName].Receiver.Balance["notus"].Add(20230111195310195, "365465464654");
    tmpBlockCipherData.In[blockFileName].Receiver.Balance["notus"].Add(20230111195310196, "365465464654");
    tmpBlockCipherData.In[blockFileName].Receiver.Balance["notus"].Add(20230111195310197, "365465464654");
    tmpBlockCipherData.In[blockFileName].Receiver.Balance["notus"].Add(20230111195310198, "365465464654");
    tmpBlockCipherData.In[blockFileName].Receiver.Balance["notus"].Add(20230111195310199, "365465464654");
    tmpBlockCipherData.In[blockFileName].Receiver.Balance["notus"].Add(20230111195310190, "365465464654");

    tmpBlockCipherData.Out.Add(senderWalletKey, new Dictionary<string, Dictionary<ulong, string>>()
    {

    });

    tmpBlockCipherData.Out[senderWalletKey].Add("NOTUS", new Dictionary<ulong, string>() { });
    tmpBlockCipherData.Out[senderWalletKey]["NOTUS"].Add(20230111195310191, "365465464654");
    tmpBlockCipherData.Out[senderWalletKey]["NOTUS"].Add(20230111195310192, "365465464654");
    tmpBlockCipherData.Out[senderWalletKey]["NOTUS"].Add(20230111195310193, "365465464654");
    tmpBlockCipherData.Out[senderWalletKey]["NOTUS"].Add(20230111195310194, "365465464654");
    tmpBlockCipherData.Out[senderWalletKey]["NOTUS"].Add(20230111195310195, "365465464654");
    tmpBlockCipherData.Out[senderWalletKey]["NOTUS"].Add(20230111195310196, "365465464654");
    tmpBlockCipherData.Out[senderWalletKey]["NOTUS"].Add(20230111195310197, "365465464654");
    tmpBlockCipherData.Out[senderWalletKey]["NOTUS"].Add(20230111195310198, "365465464654");
    tmpBlockCipherData.Out[senderWalletKey]["NOTUS"].Add(20230111195310199, "365465464654");
    tmpBlockCipherData.Out[senderWalletKey]["NOTUS"].Add(20230111195310190, "365465464654");

    tmpBlockCipherData.Out.Add(receiverWalletKey, new Dictionary<string, Dictionary<ulong, string>>()
    {

    });

    tmpBlockCipherData.Out[receiverWalletKey].Add("NOTUS", new Dictionary<ulong, string>() { });
    tmpBlockCipherData.Out[receiverWalletKey]["NOTUS"].Add(20230111195310191, "365465464654");
    tmpBlockCipherData.Out[receiverWalletKey]["NOTUS"].Add(20230111195310192, "365465464654");
    tmpBlockCipherData.Out[receiverWalletKey]["NOTUS"].Add(20230111195310193, "365465464654");
    tmpBlockCipherData.Out[receiverWalletKey]["NOTUS"].Add(20230111195310194, "365465464654");
    tmpBlockCipherData.Out[receiverWalletKey]["NOTUS"].Add(20230111195310195, "365465464654");
    tmpBlockCipherData.Out[receiverWalletKey]["NOTUS"].Add(20230111195310196, "365465464654");
    tmpBlockCipherData.Out[receiverWalletKey]["NOTUS"].Add(20230111195310197, "365465464654");
    tmpBlockCipherData.Out[receiverWalletKey]["NOTUS"].Add(20230111195310198, "365465464654");
    tmpBlockCipherData.Out[receiverWalletKey]["NOTUS"].Add(20230111195310199, "365465464654");
    tmpBlockCipherData.Out[receiverWalletKey]["NOTUS"].Add(20230111195310190, "365465464654");
    //Console.WriteLine(JsonSerializer.Serialize(tmpBlockCipherData));
    //Console.ReadLine();
    //Console.ReadLine();
}
rawData=JsonSerializer.Serialize(tmpBlockCipherData);
Console.WriteLine(tmpBlockCipherData.In.Count);
Console.WriteLine(tmpBlockCipherData.Out.Count);
//Console.WriteLine(rawData.Length);
//Console.ReadLine();
using (FileStream fileStream = new FileStream(ZipFileName, fileModeObj))
{
    using (ZipArchive archive = new ZipArchive(fileStream, zipModeObj, true))
    {
        Random random = new Random();
        //for (int i = 0; i < 1000; i++)
        //{
        string blockFileName = NGF.GenerateTxUid();
            ZipArchiveEntry zipArchiveEntry = archive.CreateEntry(blockFileName, CompressionLevel.Optimal);
            using (Stream zipStream = zipArchiveEntry.Open())
            {
                /*
                rawData = new string(Enumerable.Repeat(chars, length).Select(
                    s => s[random.Next(s.Length)]).ToArray()
                );
                rawData=System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(rawData));
                byte[] blockBytes = Encoding.UTF8.GetBytes(rawData);
                zipStream.Write(blockBytes, 0, blockBytes.Length);
            }
        //}
    }
}
Console.WriteLine("bitti");
Console.ReadLine();
Console.ReadLine();
Console.ReadLine();
*/



*/



/*
DbOptions options = new DbOptions().SetCreateIfMissing(true);
RocksDb db = RocksDb.Open(options, "dev-net/layer-1/deneme/ornek");
DateTime baslangic = DateTime.Now;

for (int i = 0; i < 1000000; i++)
{
    db.Put(i.ToString(),(i+5).ToString());
}
Console.WriteLine(DateTime.Now- baslangic);
Console.ReadLine();

Iterator iterator = db.NewIterator().SeekToFirst();
while (iterator.Valid())
{
    Console.WriteLine(iterator.StringKey());
    Console.WriteLine(iterator.StringValue());
    iterator.Next();
}


Console.ReadLine();
*/
//fix-ing-control-point


//control-point


//key value db'sini kontrol et

/*
Notus.Data.KeyValue keyValue = new Notus.Data.KeyValue();
keyValue.SetSettings(
    new NVS.KeyValueSettings()
    {
        MemoryLimitCount = 1000,
        Name = "balance"
    }
);

buyuk küçük harf hassasiyeti var
keyValue.Set("omer","kucuk_harf");
keyValue.Set("Omer", "ilk_harf");
keyValue.Set("OMER", "buyuk_harf");
string deneme1=keyValue.Get("omer");
string deneme2=keyValue.Get("Omer");
string deneme3=keyValue.Get("OMER");
Console.WriteLine(deneme1);
Console.WriteLine(deneme2);
Console.WriteLine(deneme3);

Console.ReadLine();
*/
/*
DateTime baslangic = DateTime.Now;
Console.WriteLine(JsonSerializer.Serialize(keyValue.GetList()));
Console.ReadLine();
//keyValue.Set("0123456789abcdea", "deger");
keyValue.FirstLoad();
keyValue.Each((string blockTransactionKey, string TextBlockDataString) =>
{
    Console.WriteLine(blockTransactionKey +" ->> " + TextBlockDataString);
});
*/
/*
for (int i = 0; i < 100; i++)
{
    keyValue.Set("deneme-" + i.ToString(), "deger-" + i.ToString());
}
//Console.ReadLine();
for (int i = 0; i < 50; i++)
{
    keyValue.Delete("deneme-" + i.ToString());
}
Console.WriteLine(DateTime.Now - baslangic);
*/
/*

Console.WriteLine(DateTime.Now);
string rrr = keyValue.Get("deneme-500");
Console.WriteLine(rrr);
rrr = keyValue.Get("deneme-100");
Console.WriteLine(rrr);

Console.ReadLine();
Console.ReadLine();
Console.ReadLine();
*/
/*
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
 
BIP39'un diğer diller için olan verisyonları da eklenecek 
 
 
*/

/*
var id = Notus.Wallet.ID.GenerateKeyPair();
Console.WriteLine(JsonSerializer.Serialize(id));
Console.ReadLine();
*/

Notus.Validator.Node.Start(args);



