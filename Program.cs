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

//tgz-exception
// TGZ ile ilgili eklenen kontrol noktaları

//airdrop-exception
// airdrop için eklenen kotrol noktaları


//tgz-exception
Notus.Validator.Node.Start(args);