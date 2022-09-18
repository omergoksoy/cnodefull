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

// kontrol kelimesi
// wallet-lock
/*

string sender_PublicKey = "1da9ee76a0990bff9c56c71a62376f9573bf440f773df783ee6228427e59c09ab2dfd523ca8b9076e25408c45ec9b5a30ce6364e26a1bddef73c20961b4256c4";
string sender_PrivateKey = "b206cd128422f90b14a9d41f64baf9f88ba05bd3c68c79af81d48f284b7810ac";
string sender_WalletKey = "NODXtTL7AegjfSTTwMPPw2UQUQC5BzefUopbgZ1";

string calculatedWalletKey1 = Notus.Wallet.ID.GetAddressWithPublicKey(sender_PublicKey, Notus.Variable.Enum.NetworkType.DevNet);
string calculatedWalletKey2 = Notus.Wallet.ID.GetAddress(sender_PrivateKey, Notus.Variable.Enum.NetworkType.DevNet);
*/
//Console.WriteLine("sender_WalletKey : " + sender_WalletKey);

//Console.WriteLine("calculatedWalletKey1 : " + calculatedWalletKey1);
//Console.WriteLine("calculatedWalletKey2 : " + calculatedWalletKey2);

//Console.WriteLine("sender_PublicKey : " + sender_PublicKey);
//Console.WriteLine("calculatedPublicKey : " + calculatedPublicKey);


/*



//Console.ReadLine();
string fullPublicKeyStr = "bb45c3b5f3c1294062ea6cd6aab2d2ffda4ef8e781a817298b8f02361062ede07f265bf3fe844d60e105ce04e06711583adf8ac02f0feac83647bccfcb18ab";
Console.WriteLine("fullPublicKeyStr: " + fullPublicKeyStr);

string sender_PrivateKey = "9217fa538458d74f3baf4bbadfdd2ebcd1ca2d0ae0c709954a78804811ae61ce";
string calculatedPublicKey = Notus.Wallet.ID.GetPublicKeyFromPrivateKey(sender_PrivateKey);
Console.WriteLine("calculatedPublicKey: " + calculatedPublicKey);
Console.ReadLine();
//Console.WriteLine(fullPublicKeyStr);
string calculatedWalletKey1 = Notus.Wallet.ID.GetAddressWithPublicKey(
    fullPublicKeyStr, 
    Notus.Variable.Enum.NetworkType.DevNet
);
Console.WriteLine("calculatedWalletKey1 : " + calculatedWalletKey1);
Console.ReadLine();


fullPublicKeyStr:    bb45c3b5f3c1294062ea6cd6aab2d2ffda4ef8e781a817298b8f02361062ede
				     07f265bf3fe844d60e105ce04e06711583adf8ac02f0feac83647bccfcb18ab
calculatedPublicKey: bb45c3b5f3c1294062ea6cd6aab2d2ffda4ef8e781a817298b8f02361062ede
					0007f265bf3fe844d60e105ce04e06711583adf8ac02f0feac83647bccfcb18ab

*/
Notus.Validator.Node.Start(args);