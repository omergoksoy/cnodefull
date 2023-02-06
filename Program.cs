using Notus.Communication;
using Notus.Network;
using RocksDbSharp;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using NE = Notus.Encode;
using NGF = Notus.Variable.Globals.Functions;
using NP = Notus.Print;
using NVClass = Notus.Variable.Class;
using NVG = Notus.Variable.Globals;
using NVS = Notus.Variable.Struct;

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
    NP.Danger("Fatal Error : " + fatalErrorText);
}

static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
{
    NVG.Settings.NodeClosing = true;
    e.Cancel = true;
    Console.WriteLine();
    NGF.CloseMyNode(false);
    System.Environment.Exit(0);
}


/*

  // byte[] emptyStr = RLP.Encode("");
            // Console.WriteLine(BitConverter.ToString(emptyStr));
            // System.Console.WriteLine(RLP.Decode(emptyStr));

            // [ 0x83, 'd', 'o', 'g' ]
            // byte[] shortStr = RLP.Encode("dog");
            // Console.WriteLine(BitConverter.ToString(shortStr));

            // [ 0xb8, 0x38, 'L', 'o', 'r', 'e', 'm', ' ', ... , 'e', 'l', 'i', 't' ]
            // byte[] longStr = RLP.Encode("Lorem ipsum dolor sit amet, consectetur adipisicing elit");
            // Console.WriteLine(BitConverter.ToString(longStr));

            // [ 0xc8, 0x83, 'c', 'a', 't', 0x83, 'd', 'o', 'g' ]
            // byte[] bytes = RLP.Encode(new string[]{"cat", "dog"});

            // byte[] bytes = RLP.Encode(new string[]{"this is a very long list", "you never guess how long it is", "indeed, how did you know it was this long", "good job, that I can tell you in honestlyyyyy"});
            // Console.Write(String.Join(',', bytes));
*/
object obj = new List<object>(){
    new List<string>(),
    "dog",
    new List<string>(){"cat"},
    ""
};
byte[] bytes = Notus.Encode.TestRLP.Encode(obj);
Console.WriteLine(JsonSerializer.Serialize(NE.RLP.Decode(bytes)));
/*
Console.WriteLine(Notus.Convert.Byte2Hex(bytes));
Console.WriteLine(Notus.Encode.TestRLP.Decode(Notus.Convert.Hex2Byte("cbc083646f67c48363617480")));

Console.WriteLine(BitConverter.ToString(bytes));
Console.WriteLine(BitConverter.ToString(Encoding.UTF8.GetBytes("dog")));
Console.WriteLine(BitConverter.ToString(Encoding.UTF8.GetBytes("cat")));
*/
Console.ReadLine();



/*
// "1.0:alici_adres:tutar:islem_ucreti:nonce_degeri:data"

var newKey = Notus.Wallet.ID.GenerateKeyPair();
string signStr=Notus.Wallet.ID.Sign("1.0:NSX6PPCjyiaBpA37d5JX2uHNQ3KELerXHFAEZ8g:100000000000000:25000000:125:data", newKey.PrivateKey);

Console.WriteLine(signStr);
Console.WriteLine(JsonSerializer.Serialize(newKey));
Console.ReadLine();

    "NSX6PPCjyiaBpA37d5JX2uHNQ3KELerXHFAEZ8g",  // alıcı adresi
    "100000000000000",        // gönderilmek istenen tutar
    "25000000", // işlem ücreti
    "125", // geçerli nonce değeri
    "data",         // varsa işlem datası

30440220381975c8e029386eb86974b74561fdfbf0997712c6015c001ebb106408a6fbec0220621441855f847b9eda1f2a6cbc4469a814f3567e6cf26404a46af0363c6c5e37
{"CurveName":"prime256v1","Words":["soup","client","monitor","debate","boss","attack","cute","utility","reunion","custom","lazy","frost","analyst","father","torch","abuse"],"PrivateKey":"d8a4cd6d4961561777f1fa1ad1d27c9111c2a633779f889ebf7bc96197b2068e","PublicKey":"aef3fb47cfbaf6aa2db0678a6bc73e18d0c6f266bc3621b5884169a793a31c74a309b1c7ef2ce8c097adc97f273168083515bba3242b2eda2b8fb3b2bff12117","WalletKey":"NSX36AA8Jg8zXyCxprJNvH1QWzA3oVVWmJsF3jg"}





bu link ile test edilebilir

http://18.156.37.61:5002/tx/f84583312e308b616c6963695f61647265738574757461728c69736c656d5f7563726574698c6e6f6e63655f646567657269846461746184696d7a618a7075626c69635f6b6579



List<string> islemList = new List<string> {
    "1.0",          // version
    "NSX6PPCjyiaBpA37d5JX2uHNQ3KELerXHFAEZ8g",  // alıcı adresi
    "100000000000000",        // gönderilmek istenen tutar
    "25000000", // işlem ücreti
    "125", // geçerli nonce değeri
    "data",         // varsa işlem datası
    "30440220381975c8e029386eb86974b74561fdfbf0997712c6015c001ebb106408a6fbec0220621441855f847b9eda1f2a6cbc4469a814f3567e6cf26404a46af0363c6c5e37",         // işlem imzası
    "aef3fb47cfbaf6aa2db0678a6bc73e18d0c6f266bc3621b5884169a793a31c74a309b1c7ef2ce8c097adc97f273168083515bba3242b2eda2b8fb3b2bff12117",   // işlemi yapan public key
};

byte[] bytes = NE.RLP.Encode(islemList);
IList<string> result = NE.RLP.Decode(bytes);


Console.WriteLine(islemList.Count);
Console.WriteLine(islemList.Count);
Console.WriteLine(islemList.Count);
Console.WriteLine(Notus.Convert.Byte2Hex(bytes));
Console.WriteLine(NE.RLP.Encode(islemList, Notus.Variable.Enum.ReturnType.AsBase64));
Console.WriteLine(NE.RLP.Encode(islemList, Notus.Variable.Enum.ReturnType.AsBase64));
Console.WriteLine(NE.RLP.Encode(islemList, Notus.Variable.Enum.ReturnType.AsBase64));
Console.WriteLine(JsonSerializer.Serialize(bytes));
Console.WriteLine(JsonSerializer.Serialize(result));
Console.ReadLine();

byte[] bytes = NE.RLP.Encode("30440220381975c8e029386eb86974b74561fdfbf0997712c6015c001ebb106408a6fbec0220621441855f847b9eda1f2a6cbc4469a814f3567e6cf26404a46af0363c6c5e37");
Console.WriteLine(JsonSerializer.Serialize(bytes));
Console.ReadLine();
*/


System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
Notus.Validator.Node.Start(args);


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
 
BIP39'un diğer diller için olan verisyonları da eklenecek 
*/




