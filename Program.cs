using Notus.Communication;
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

static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    string fatalErrorText = (e.ExceptionObject as Exception).Message + "Unhandled UnhandledExceptionEventArgs Exception -> Sender(" + sender.ToString() + ")";
    const string directoryName = "log_list";
    if (Directory.Exists(directoryName) == false)
    {
        Directory.CreateDirectory(directoryName);
    }

    Console.SetError(new StreamWriter(@".\" + directoryName + "\\" + DateTime.UtcNow.ToString("yyyy_MM_dd_HH_mm_ss_fff") + ".log"));
    Console.Error.WriteLine(fatalErrorText);
    Console.Error.Close();
    Console.WriteLine();
    Notus.Print.Danger("Fatal Error : " + fatalErrorText);
}

/*

auto update uygulaması
https://gist.github.com/sbrl/7709dfc5268e1acde6f3


run multiple exe on console app
https://www.codeproject.com/Questions/999027/Multiple-Consoles-in-single-Csharp-Console-Applica

*/
/*
// DLL 'in version bilgisini çekiyor.
Console.WriteLine("{0}", System.Reflection.AssemblyName.GetAssemblyName("Microsoft.Data.Sqlite.dll").Version);
Console.WriteLine("{0}", System.Reflection.AssemblyName.GetAssemblyName("cnodefull.dll").Version);
Console.ReadLine();
*/

static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
{
    NVG.Settings.NodeClosing = true;
    e.Cancel = true;
    Console.WriteLine();
    NGF.CloseMyNode();
}

System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);




/*
YAPILACAK İŞLEMLER

- belirli periyotlarda diğer nodelar ile haberleşip blokların doğru bir şekilde oluşturulduğu kontrol edilmeli

- SYNC class'ları için "Balance.Cs" sınıfı eklenmeli ve bu sınıf ile cüzdan bakiyeleri ve kilitli cüzdanlar
  liste halinde tutulmalı
  bellek optimizasyonu için yakın zamanda kullanımda olan cüzdanlar bellekte tutulmalı

- blok oluşturma işlemi için ayrılan 200 milisaniye süresi azaltılabilir çünkü node kendi sırasının 
  geleceğini önceden bildiği için kendi sırası geldiğinde doğrudan blok oluşturma işlemine geçebilir

- çakışmaların sebebi şu : node'un sırası gelip bloğu oluşturdaktan sonra diğer nodelara oluşturduğu
  bloğun numarasını gönderiyor ( distribute ) Bu gönderim esnasında soket bağlantısının kurulması
  yaklaşık 0.4 saniye sürüyor. Bu da sonraki node'un blok oluşturması için önceki bloğun bilgisini
  geç öğrenmesini sağlıyor ve böylece aynı blok numaralı bloklar oluşuyor.
  çözüm için kuyruktaki nodeların birbirleri ile soket bağlantısı kurmasını ve bağlantının o esnada
  açık kalmasını sağlamak gerekir.




YAPILDI

- her kuyruğun 4. turunda sıralamayı karıştırmak için birinci bloğun UID değeri özete eklenmeli

- gelen blokların geçerli cüzdan sahibi node tarafından gönderildiğinden emin olarak zincire ekleyeceğiz.
  blok yapısının içine bloğu oluşturan node'un imzasını ekleyerek bir başka node'un o node için sahte blok oluşturması
  engellenmeli

- belirli periyotlarda zaman sunucusuna bağlanarak zaman değeri güncellenmeli

*/
/*
Console.CancelKeyPress += delegate {
    // call methods to clean up
};


var fastServerName = Notus.Time.FindFasterNtpServer();
Console.WriteLine(fastServerName);
Console.ReadLine();

 

öncelikle node'ların zaman bilgileri alınsın
alınan zaman bilgileri ile NTP zaman farkı karşılaştırılsın
iki node arasındaki zaman farkı toplansın böylece iki node
arasındaki tüm zaman farkı eşitlenecek

Örneğin;
node-1 UTC saatinin alındığı zaman : 14:45:12.012 ise
    ve ping timeout süresi         : 0.65429 ise

node-2 UTC saatinin alındığı zaman : 14:46:28.873 ise
    ve ping timeout süresi         : 0.23510 ise

bu iki node arasındaki süre 

*/
//OMERGOKSOY
// kontrol edilecek alanlar

//sync-control
//node'ların blok senkronizasyonu kontrol ediliyor

//socket-exception
//soket ile ilgili yapılan işlemler için oluşturulan istisna durumu

//tgz-exception
// TGZ ile ilgili eklenen kontrol noktaları

//block-order-exception
// blok sırasını yapan kontrol noktaları

//sync-disable-exception
// senkronizasyon düzeltmesi için yapılan devre dışı bırakma işlemleri

//tgz-exception
Notus.Validator.Node.Start(args);


