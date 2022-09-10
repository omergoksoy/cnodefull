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

/*
string multiWalletId = Notus.Wallet.MultiID.GetWalletID(
    "NODVh23S4wEokC5VtPTrTeJepv5toDea1dckfMc",
    new List<string>() {
        "NODWim3a4UJZzRxKKaza6wzjkCrM2scMzAg5CWk",
        "NODaPfW4p7bF5mqbtzSDJXhwqnC3r3JRHb1hCKq",
        "NODXCths3ftVBF6vK7EWDucw54dNjB1HMJ1758t",
        "NODcKSATDP3eLwKyVRcRKPsUDMJz15WgjoxwHvB"
    },
    Notus.Variable.Enum.MultiWalletType.MajorityRequired,
    Notus.Variable.Enum.NetworkType.DevNet
);
Console.WriteLine(multiWalletId);
Console.WriteLine(multiWalletId);
Console.ReadLine();
Console.ReadLine();
Console.ReadLine();
*/

Notus.Validator.Node.Start(args);