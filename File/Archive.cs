using System.IO;
using System.IO.Compression;

namespace Notus
{
    public static class Archive
    {
        public static void DeleteFromInside(string ZipFileName, List<string> insideFileList)
        {
            using (ZipArchive archive = ZipFile.Open(ZipFileName, ZipArchiveMode.Update))
            {
                for (int i = 0; i < insideFileList.Count; i++)
                {
                    ZipArchiveEntry? entry = archive.GetEntry(insideFileList[i]);
                    if (entry != null)
                    {
                        entry.Delete();
                    }
                }
            }
        }
        public static void DeleteFromInside(string ZipFileName, string insideFileName)
        {
            using (ZipArchive archive = ZipFile.Open(ZipFileName, ZipArchiveMode.Update))
            {
                ZipArchiveEntry? entry = archive.GetEntry(insideFileName);
                if (entry != null)
                {
                    entry.Delete();
                }
            }
        }
    }
}