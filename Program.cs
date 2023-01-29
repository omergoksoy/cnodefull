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




