using Notus.MsgPack;
using Notus.Communication;
using NH = Notus.HashLib;
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

/*
Console.WriteLine(Notus.Toolbox.Text.ShrinkHex("a80bfb19e29f52310b61d18dce08cb9a904fdf37a141726ff5598fec5e2cdcda", 8));
Console.ReadLine();

Console.WriteLine(JsonSerializer.Serialize(Notus.Wallet.ID.GenerateKeyPair()));
Console.WriteLine();
Console.WriteLine(new Notus.Hash().CommonHash("sasha", "denemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedenemedeneme"));
Console.ReadLine();
*/
//10.000 'lik döngüde aşağıdaki metin için ortalama değerler
//("NSX6PPCjyiaBpA37d5JX2uHNQ3KELerXHFAEZ8g" + i.ToString()));
//sha1      ->  0,002774100999997264
//md5       ->  0,005158651999998687
//RIPEMD160 ->  0,008106142999993727
//blake3    ->  0,011409777000002041
//blake2b   ->  0,011670115000006021


//Console.WriteLine(Notus.Data.Sharding.Node.BelongsToMe("NSX6PPCjyiaBpA37d5JX2uHNQ3KELerXHFAEZ8g"));
/*
Console.ReadLine();
Console.WriteLine(Notus.Contract.Address.Generate("NSX6PPCjyiaBpA37d5JX2uHNQ3KELerXHFAEZ8g",1453));
Console.WriteLine(Notus.Contract.Address.Generate("NSX6PPCjyiaBpA37d5JX2uHNQ3KELerXHFAEZ8g",1454));
Console.WriteLine(Notus.Contract.Address.Generate("NSX6PPCjyiaBpA37d5JX2uHNQ3KELerXHFAEZ8g",1455));
Console.ReadLine();


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

// unpack filedata 
//unpack_msgpack.ForcePathObject("p.filedata").SaveBytesToFile("C:\\b.png");
*/



/*
http://18.156.37.61:5002/tx/f84583312e308b616c6963695f61647265738574757461728c69736c656d5f7563726574698c6e6f6e63655f646567657269846461746184696d7a618a7075626c69635f6b6579
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




