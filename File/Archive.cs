using System.IO.Compression;

namespace Notus
{
    public static class Archive
    {
        public static void DeleteFromInside(string blockUid, Notus.Variable.Common.ClassSetting objSettings)
        {
            DeleteFromInside(blockUid, objSettings.Network, objSettings.Layer);
        }
        public static void DeleteFromInside(
            string blockUid,
            Notus.Variable.Enum.NetworkType networkType,
            Notus.Variable.Enum.NetworkLayer networkLayer
        )
        {
            string ZipFileName = Notus.IO.GetFolderName(
                networkType, networkLayer,
                Notus.Variable.Constant.StorageFolderName.Block
            ) + Notus.Block.Key.GetBlockStorageFileName(blockUid, true) + ".zip";
            DeleteFromInside(ZipFileName, blockUid);
        }
        public static void DeleteFromInside(string ZipFileName, List<string> insideFileList)
        {
            using (ZipArchive archive = ZipFile.Open(ZipFileName, ZipArchiveMode.Update))
            {
                for (int i = 0; i < insideFileList.Count; i++)
                {
                    ZipArchiveEntry? entry = archive.GetEntry(AddExtension(insideFileList[i]));
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
                ZipArchiveEntry? entry = archive.GetEntry(AddExtension(insideFileName));
                if (entry != null)
                {
                    entry.Delete();
                }
            }
        }
        private static string AddExtension(string blockUid)
        {
            return blockUid + (blockUid.IndexOf(".") >= 0 ? "" : ".json");
        }
    }
}