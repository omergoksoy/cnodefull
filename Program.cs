using System.Runtime.ExceptionServices;
using System.Text.Json;

static void FirstChanceExceptionEventHandler(object sender, FirstChanceExceptionEventArgs e)
{
    Console.WriteLine(e.Exception.Message, "Unhandled FirstChanceExceptionEventArgs Exception");
    Console.WriteLine(sender.ToString());
    Console.WriteLine("press enter to continue");
    Console.ReadLine();
}
static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    Console.WriteLine((e.ExceptionObject as Exception).Message, "Unhandled UnhandledExceptionEventArgs Exception");
    Console.WriteLine(sender.ToString());
    Console.WriteLine("press enter to continue");
    Console.ReadLine();
}

// DLL 'in version bilgisini çekiyor.
// Console.WriteLine("{0}", System.Reflection.AssemblyName.GetAssemblyName("Microsoft.Data.Sqlite.dll").Version);
// Console.ReadLine();
/*
for(int a=0; a < 256; a++)
{
    Console.Write(a);
    Console.Write( " -> ");
    Console.Write((char)a);
    Console.Write("  ");
}
Console.ReadLine();
Console.ReadLine();
Console.ForegroundColor = ConsoleColor.DarkGreen;
Console.Write(((char)30));
Console.ForegroundColor = ConsoleColor.Gray;
Console.ForegroundColor = ConsoleColor.DarkMagenta;
Console.Write(((char)31));
Console.ForegroundColor = ConsoleColor.Gray;

Console.ReadLine();
Console.ReadLine();
Console.ReadLine();
Console.WriteLine();
*/
AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

/*
Notus.Variable.Struct.BlockResponse bs1 = new Notus.Variable.Struct.BlockResponse()
{
    Result = Notus.Variable.Enum.BlockStatusCode.InQueue,
    Status = "status-1",
    UID = "uid-1"
};

Notus.Variable.Struct.BlockResponse bs2 = bs1;
//Notus.Variable.Struct.BlockResponse bs2 = bs1.ShallowCopy();
bs2.Status = "status-2";
bs2.UID = "uid-2";

Console.WriteLine(JsonSerializer.Serialize(bs1));
Console.WriteLine(JsonSerializer.Serialize(bs2));
Console.ReadLine();
*/

Notus.Print.Log(new Notus.Variable.Struct.LogStruct()
{
     BlockRowNo=DateTime.Now.ToLongTimeString(),
      LogNo=123,
       LogType= Notus.Variable.Enum.LogLevel.Info,
        Message="deneme mesaji",
         WalletKey="cuzdan_adresi",
          StackTrace="stack trace",
           ExceptionType= "exception type"
});
Console.ReadLine();

Notus.Validator.Node.Start(args);