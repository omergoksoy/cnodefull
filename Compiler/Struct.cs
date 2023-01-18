using System;

namespace Notus.Compiler
{
    public enum TxQueueType
    {
        Transaction,
        Contract
    }
    public struct FunctionList
    {
        public string Name { get; set; }             // çalıştırılacak komut adı
        public string Parameter { get; set; }        // komut için gönderilen parametre değeri
    }
    public struct TxQueueStruct
    {
        public TxQueueType Type { get; set; }         // işlemin türü
        public string ContractId { get; set; }     // çalıştırılacak kontratın id'si
        public string Uid { get; set; }             // işlemin uid değeri

        // para transferi için kullanılacak değişken
        // gömülü komut gönderilecek
        // public string Receiver{ get; set; }         // para transferi için alıcı adresi
        // public string Volume { get; set; }          // alıcı için para tutarı

        // kontrat için kullanılacak değişken
        public List<FunctionList> FunctionList { get; set; }     // çalıştırılacak fonksiyon listesi ve parametreleri

        public string PublicKey { get; set; }       // işlemi yapanın public adresi
        public string Fee { get; set; }             // işlemin ücreti
        public string Sign { get; set; }             // işlemin imzası
    }

}
