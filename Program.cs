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
string sender_PublicKey = "04a098229795f2989cb4e0d6c608c674a677ee4f69a80f52e3fdebe6f2c0b787bd9c85e56bcdf7960b9b382e88a913442ed5ecfeddcdfc9d29a6a406fc3f715f7f";
string sender_PrivateKey = "0000000011111111";
string sender_WalletKey = "NTSEPDFu7JTq34pBsKm4GLSv63joCiosyrZ3rjM";

string calculatedPublicKey= Notus.Wallet.ID.GetPublicKeyFromPrivateKey(sender_PrivateKey);
Console.WriteLine("calculatedPublicKey : " + calculatedPublicKey);
Console.WriteLine("sender_PublicKey    : " + sender_PublicKey);
calculatedPublicKey : 0000f5d044d93197f31a147b6a4e373b21d36b9948b3f37349f1afd00c372485 03ad8a16c44c21e26806261b1dfb4b5bcd2e6ffea30f2504a40c8685c2551269
sender_PublicKey    :     f5d044d93197f31a147b6a4e373b21d36b9948b3f37349f1afd00c372485  3ad8a16c44c21e26806261b1dfb4b5bcd2e6ffea30f2504a40c8685c2551269
128 - 123
public key HATALI
*********************
Console.WriteLine(calculatedPublicKey.Length.ToString()+" - " + sender_PublicKey.Length.ToString());
*/

/*
if (string.Equals("04"+calculatedPublicKey, sender_PublicKey))
{
        Console.WriteLine("public key esit");
}
else
{
    Console.WriteLine("public key HATALI");
}
Console.WriteLine("*********************");
string calculatedWalletKey = Notus.Wallet.ID.GetAddressWithPublicKey(sender_PublicKey, Notus.Variable.Enum.NetworkType.MainNet);
Console.WriteLine("calculatedWalletKey : " + calculatedWalletKey);
Console.WriteLine("sender_WalletKey    : " + sender_WalletKey);
if (string.Equals(calculatedWalletKey, sender_WalletKey))
{
    Console.WriteLine("wallet key esit");
}
else
{
    Console.WriteLine("wallet key HATALI");
}
Console.ReadLine();

bb45c3b5f3c1294062ea6cd6aab2d2ffda4ef8e781a817298b8f02361062ede0 007f265bf3fe844d60e105ce04e06711583adf8ac02f0feac83647bccfcb18ab
bb45c3b5f3c1294062ea6cd6aab2d2ffda4ef8e781a817298b8f02361062ede0 7f265bf3fe844d60e105ce04e06711583adf8ac02f0feac83647bccfcb18ab
*/

//Console.WriteLine("calculatedPublicKey : " + calculatedPublicKey);

//Console.WriteLine("calculatedWalletKey1 : " + calculatedWalletKey1);
//Console.WriteLine("calculatedWalletKey2 : " + calculatedWalletKey2);

//Console.WriteLine("sender_PublicKey : " + sender_PublicKey);
//Console.WriteLine("calculatedPublicKey : " + calculatedPublicKey);

/*
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




/*
string tmpPublic = "046c429c90e8af7a2ee66e0a228de732390b0954b7ef33738d95b044cff63a4148e5a51ccf43345dfdf3d0be340150f32765ff3c6ade9aadf27bf2fdde2ea29366";
string tmpSign = "3045022100a60b3dbd73911d001f744d34cb7ca25a69738fadde8e7ec5bd72286a0bf7c09302205ba6162cf2808ee336aefcbccbad99da0a4c62872e472c65f36bb6450c809599";
//30450220531a6e3cfd9932bd42b865a44974cdfbeafa24cd080cf6329b54ffcdcebc99fd0221008ead3bc51c03a13d021d4ff1a7816dc4c1bb181d27dbca903affbd545201626b
//3045022100b83c26deb5d7674144f995f1ab2ec5fcd3e65a4f194c4aeabe138f50a7e9873b0220242add68a05145b70877ba52008a49ef936795dbb885a0325a475fc2a555d60f
//3045022100885b2a4eca469f5e724d1da5633bc7c7e38d19e0b08cba961014e1d9e385425b0220267624e397576266dd813073114125eb2b3413737be2bcdb0930e2cefebffdeb
string rawDataStr = Notus.Core.MergeRawData.Transaction(new Notus.Variable.Struct.CryptoTransactionStruct()
{
    Currency = "NOTUS",
    CurrentTime = 20220919214903,
    UnlockTime = 20220919214903,
    Sender = "NODYEeb5cZ9jZxUA9U6y48KNEG28BPDaBG3PWKG",
    Receiver = "NODY4Zdqt3ZRTjtuMFR4Mox6EYzUXs9t3W3N2Vc",
    Volume = "1000000000",
    PublicKey = tmpPublic,
    ErrorNo = 0,
    Network = Notus.Variable.Enum.NetworkType.DevNet,
    Sign = tmpSign,
    CurveName = Notus.Variable.Constant.Default_EccCurveName

});
*/




/*

https://devnet.notus.network/valid/
049d444e6c831ce1e87a3b1b3da5e996c51216de75914c0f7b0a6cc8ac6670315decd205540e5f7b7638df51dfed076e73d71f2d3d3cc2cd1be261e5ea9961c16d/
3046022100ab56b88ec3556b44e80720338ccf8ecf802744c2c4943071868cbeb4513f619c022100efcf1a99dbefe76cd0f709586bba71eceeb7c8d4f2a541eb7ea35dfb74a9534c/




calculatedSignStr : 3045022100cd504a92820da36c7eefd45e023064e4726d5b2b20f791d3d0e74b5eeb242ed002201009c45d5b0a12c7f485a8de916da96ef165adfb929e47e5c35125e14b8654fa
*/
string rawDataStr = "NODVofLyZLAjRrDXBRN3qTybDP1yBfSfN5yoV6d:NODVofLyZLAjRrDXBRN3qTybDP1yBfSfN5yoV6b:1000000000:20220920082001:20220920082001:NOTUS";
//Notus.HashLib.BLAKE2B blake2b_obj = new Notus.HashLib.BLAKE2B();
//Notus.HashLib.MD5 md5_obj = new Notus.HashLib.MD5();
//Notus.HashLib.SHA1 sha1_obj = new Notus.HashLib.SHA1();
//Notus.HashLib.RIPEMD160 ripemd160_obj = new Notus.HashLib.RIPEMD160();

/*

282a5ee709a5c22903e34c54b882d625
d4d7f0bf52acddf96be9a8d075ef5692e98be1f6
bbe7e6daaaccb778c0371091768946f78f9bd4b4730be526a4e9ebcc961be2985ee82735ff25d811033ce71d23756e7e8448f5e3accb63781197f7694d67a955
576d277373d50d56c71d34470becd872b885ca15



SashaHash:  bbe7e6daaaccb778c6d4d4d7f576d2c0371091768946f713420bf52773738f9bd4b4730be5267e4cacddfd50d5a4e9ebcc961be2983df696be96c71d34470a8d0725a25ee82735ff25d811becd85ef56f7b5033ce71d23756e7e72b8892e98bc988448f5e3accb63785ca15be1f677bb1197f7694d67a955
            bbe7e6daaaccb778c6d4d4d7f576d2c0371091768946f713420bf52773738f9bd4b4730be5267e4cacddfd50d5a4e9ebcc961be2983df696be96c71d34470a8d0725a25ee82735ff25d811becd85ef56f7b5033ce71d23756e7e72b8892e98bc988448f5e3accb63785ca15be1f677bb1197f7694d67a955
Blake2BHash:  bbe7e6daaaccb778c0371091768946f78f9bd4b4730be526a4e9ebcc961be2985ee82735ff25d811033ce71d23756e7e8448f5e3accb63781197f7694d67a955
Ripemd160Hash:  576d277373d50d56c71d34470becd872b885ca15
Sha1Hash:  d4d7f0bf52acddf96be9a8d075ef5692e98be1f6
Md5Hash:  c6d413427e4c3df625a2f7b5bc9877bb
Console.WriteLine(
    new Notus.Hash().CommonHash("sasha", rawDataStr)
);
Console.WriteLine(
    new Notus.Hash().CommonHash("md5", rawDataStr)
);
Console.WriteLine(
    new Notus.Hash().CommonHash("sha1", rawDataStr)
);
Console.WriteLine(
    new Notus.Hash().CommonHash("blake2b", rawDataStr)
);
Console.WriteLine(
    new Notus.Hash().CommonHash("ripemd160", rawDataStr)
);
*/

/*
//string rawDataStr = "NOTUS";
string privateKeyStr = "63d6cee7ca7a9571e9bcd2eb2794519fe06ade63ea4ed55573e4b7ab0fcb62fd";
string calculatedPublicKeyStr = Notus.Wallet.ID.GetPublicKeyFromPrivateKey(privateKeyStr);
string calculatedSignStr = Notus.Wallet.ID.Sign(rawDataStr, privateKeyStr);
//calculatedSignStr = "3046022100ab56b88ec3556b44e80720338ccf8ecf802744c2c4943071868cbeb4513f619c022100efcf1a99dbefe76cd0f709586bba71eceeb7c8d4f2a541eb7ea35dfb74a9534c";

//calculatedPublicKeyStr = "049d444e6c831ce1e87a3b1b3da5e996c51216de75914c0f7b0a6cc8ac6670315decd205540e5f7b7638df51dfed076e73d71f2d3d3cc2cd1be261e5ea9961c16d";
Console.WriteLine("calculatedPublicKeyStr : " + calculatedPublicKeyStr);
//Console.WriteLine();
Console.WriteLine("calculatedSignStr : " + calculatedSignStr);

//transaction sign
bool verifyTmp = Notus.Wallet.ID.Verify(
    rawDataStr,
    calculatedSignStr,
    calculatedPublicKeyStr
);
Console.WriteLine(verifyTmp);
Console.ReadLine();


*/
Notus.Validator.Node.Start(args);