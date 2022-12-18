using System;
using System.Text.Json;
using NVClass = Notus.Variable.Class;
namespace Notus.Block
{
    // bu kitaplığın amacı
    // blok içine kaydedilecek class türünü string'ten o türe convert ediyor

    public static class Decrypt
    {
        public static NVClass.BlockStruct_120? Convert_120(string rawData, bool decryptFromBase64 = true)
        {
            return JsonSerializer.Deserialize<NVClass.BlockStruct_120>(
                decryptFromBase64 == true ? makeStr(rawData) : rawData
            );
        }
        public static NVClass.BlockStruct_125? Convert_125(string rawData, bool decryptFromBase64 = true)
        {
            return JsonSerializer.Deserialize<NVClass.BlockStruct_125>(
                decryptFromBase64 == true ? makeStr(rawData) : rawData
            );
        }
        private static string makeStr(string rawData)
        {
            return System.Text.Encoding.UTF8.GetString(
                System.Convert.FromBase64String(
                    rawData
                )
            );
        }
    }
}
