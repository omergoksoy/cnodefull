using System.Runtime.ExceptionServices;

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
Notus.Validator.Node.Start(args);