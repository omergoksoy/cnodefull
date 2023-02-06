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


List<string> tmpIslemList = new List<string> {
    "1.0",          // version
    "alici_adres",  // alıcı adresi
    "tutar",        // gönderilmek istenen tutar
    "islem_ucreti", // işlem ücreti
    "nonce_degeri", // geçerli nonce değeri
    "data",         // varsa işlem datası
    "imza",         // işlem imzası
    "public_key",   // işlemi yapan public key
};
string rawDataText = string.Empty;
for (int i = 0; i < 6; i++)
{
    rawDataText += tmpIslemList[i];
    if (i < 5)
        rawDataText += ":";
}

Console.WriteLine(rawDataText);
Console.ReadLine();
Console.ReadLine();

List<string> islemList = new List<string> {
    "1.0",          // version
    "alici_adres",  // alıcı adresi
    "tutar",        // gönderilmek istenen tutar
    "islem_ucreti", // işlem ücreti
    "nonce_degeri", // geçerli nonce değeri
    "data",         // varsa işlem datası
    "imza",         // işlem imzası
    "public_key",   // işlemi yapan public key
};


/*
byte[] bytes = NE.RLP.Encode(islemList);
IList<string> result = NE.RLP.Decode(bytes);

f84583312e308b616c6963695f61647265738574757461728c69736c656d5f7563726574698c6e6f6e63655f646567657269846461746184696d7a618a7075626c69635f6b6579
"+EWDMS4wi2FsaWNpX2FkcmVzhXR1dGFyjGlzbGVtX3VjcmV0aYxub25jZV9kZWdlcmmEZGF0YYRpbXphinB1YmxpY19rZXk="
["1.0","alici_adres","tutar","islem_ucreti","nonce_degeri","data","imza","public_key"]

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




