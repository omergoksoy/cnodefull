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

AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
Notus.Validator.Node.Start(args);