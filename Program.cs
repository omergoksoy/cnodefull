using Notus.MsgPack;
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
using Notus.Compiler;

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


string hexStr = Notus.Encode.RLP.Encode(new List<string>()
{
    // "1.0",          // version
    "NSX6PPCjyiaBpA37d5JX2uHNQ3KELerXHFAEZ8g",  // alıcı adresi
    "100000000000000",        // gönderilmek istenen tutar
    "25000000", // işlem ücreti
    "125", // geçerli nonce değeri
    "",         // varsa işlem datası
    System.Convert.ToBase64String(Notus.Convert.Hex2Byte("30440220381975c8e029386eb86974b74561fdfbf0997712c6015c001ebb106408a6fbec0220621441855f847b9eda1f2a6cbc4469a814f3567e6cf26404a46af0363c6c5e37")),         // işlem imzası
    System.Convert.ToBase64String(Notus.Convert.Hex2Byte("aef3fb47cfbaf6aa2db0678a6bc73e18d0c6f266bc3621b5884169a793a31c74a309b1c7ef2ce8c097adc97f273168083515bba3242b2eda2b8fb3b2bff12117")),   // işlemi yapan public key
}, Notus.Variable.Enum.InputOrOutputType.AsBase64);
//Console.WriteLine(JsonSerializer.Serialize(NE.RLP.Decode(bytes)));
Console.WriteLine(hexStr);
Console.WriteLine(
    JsonSerializer.Serialize(
        Notus.Encode.RLP.Decode(hexStr, Notus.Variable.Enum.InputOrOutputType.AsBase64)
    )
);

/*


+QEKgzEuMKdOU1g2UFBDanlpYUJwQTM3ZDVKWDJ1SE5RM0tFTGVyWEhGQUVaOGePMTAwMDAwMDAwMDAwMDAwiDI1MDAwMDAwgzEyNYRkYXRhuGBNRVFDSURnWmRjamdLVGh1dUdsMHQwVmgvZnZ3bVhjU3hnRmNBQjY3RUdRSXB2dnNBaUJpRkVHRlg0UjdudG9mS215OFJHbW9GUE5XZm16eVpBU2thdkEyUEd4ZU53PT24WHJ2UDdSOCs2OXFvdHNHZUthOGMrR05ERzhtYThOaUcxaUVGcHA1T2pISFNqQ2JISDd5em93SmV0eVg4bk1XZ0lOUlc3b3lRckx0b3JqN095di9FaEZ3PT0=
["1.0","NSX6PPCjyiaBpA37d5JX2uHNQ3KELerXHFAEZ8g","100000000000000","25000000","125","data","MEQCIDgZdcjgKThuuGl0t0Vh/fvwmXcSxgFcAB67EGQIpvvsAiBiFEGFX4R7ntofKmy8RGmoFPNWfmzyZASkavA2PGxeNw==","rvP7R8\u002B69qotsGeKa8c\u002BGNDG8ma8NiG1iEFpp5OjHHSjCbHH7yzowJetyX8nMWgINRW7oyQrLtorj7Oyv/EhFw=="]


*/
/*

msgPack kullanımı

MsgPack msgpack = new MsgPack();
msgpack.ForcePathObject("p.name").AsString = "deneme";
msgpack.ForcePathObject("p.age").AsInteger = 132123456874125;
msgpack.ForcePathObject("p.datas").AsArray.Add(90);
msgpack.ForcePathObject("p.datas").AsArray.Add(80);
msgpack.ForcePathObject("p.datas").AsArray.Add("ornek");
msgpack.ForcePathObject("p.datas").AsArray.Add(3.1415926);
msgpack.ForcePathObject("Game.iGameID").AsInteger = 1;

// 可以直接打包文件数据
// msgpack.ForcePathObject("p.filedata").LoadFileAsBytes("C:\\a.png");

// 打包成msgPack协议格式数据
byte[] packData = msgpack.Encode2Bytes();

//Console.WriteLine("msgpack序列化数据:\n{0}", BytesTools.BytesAsHexString(packData));

MsgPack unpack_msgpack = new MsgPack();
// 从msgPack协议格式数据中还原
unpack_msgpack.DecodeFromBytes(packData);

System.Console.WriteLine("name:{0}, age:{1}",
      unpack_msgpack.ForcePathObject("p.name").AsString,
      unpack_msgpack.ForcePathObject("p.age").AsInteger);

Console.WriteLine("==================================");
System.Console.WriteLine("use index property, Length{0}:{1}",
      unpack_msgpack.ForcePathObject("p.datas").AsArray.Length,
      unpack_msgpack.ForcePathObject("p.datas").AsArray[0].AsString
      );

Console.WriteLine("==================================");
Console.WriteLine("use foreach statement:");
foreach (MsgPack item in unpack_msgpack.ForcePathObject("p.datas"))
{
    Console.WriteLine(item.AsString);
}

Console.WriteLine(unpack_msgpack.ForcePathObject("Game.iGameID").AsInteger);


*/

// unpack filedata 
//unpack_msgpack.ForcePathObject("p.filedata").SaveBytesToFile("C:\\b.png");
/*
Console.Read();


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
/*
object obj = new List<object>(){
    new List<string>(),
    "dog",
    new List<string>(){"cat"},
    ""
};
*/

Console.ReadLine();
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
byte[] signBytes = NE.RLP.Encode("30440220381975c8e029386eb86974b74561fdfbf0997712c6015c001ebb106408a6fbec0220621441855f847b9eda1f2a6cbc4469a814f3567e6cf26404a46af0363c6c5e37");
byte[] allBytes = NE.RLP.Encode(new List<byte[]> { signBytes });
IList<string> result = NE.RLP.Decode(signBytes);


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

*/
/*
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




