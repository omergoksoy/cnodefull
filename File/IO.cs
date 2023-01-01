using System.IO;
using NVG = Notus.Variable.Globals;
using DirListConst = Notus.Variable.Constant.StorageFolderName;
namespace Notus
{
    public static class IO
    {
        public static void DeleteFile(string fileName)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);
        }
        public static void DeleteAllFileInsideDirectory(string directoryName,string extension)
        {
            foreach (string fileName in Notus.IO.GetFileList(directoryName, extension))
            {
                if (System.IO.File.Exists(fileName))
                {
                    System.IO.File.Delete(fileName);
                }
            }
        }
        public static string[] GetFileList(string directoryName,string extension)
        {
            if (Directory.Exists(directoryName) == false)
            {
                return new string[] { };
            }
            return Directory.GetFiles(directoryName, "*." + extension);
        }

        public static string[] GetFileList(
            Notus.Variable.Enum.NetworkType networkType,
            Notus.Variable.Enum.NetworkLayer networkLayer,
            string directoryName,
            string extension
        )
        {
            if (Directory.Exists(Notus.IO.GetFolderName(networkType, networkLayer,
                directoryName)) == false)
            {
                return new string[] { };
            }
            return Directory.GetFiles(
                Notus.IO.GetFolderName(networkType,networkLayer,directoryName),"*." + extension
            );
        }
        public static string[] GetFileList(
            Notus.Globals.Variable.Settings objSettings,
            string directoryName,
            string extension
        )
        {
            if (Directory.Exists(Notus.IO.GetFolderName(objSettings,
                directoryName)) == false)
            {
                return new string[] { };
            }
            return Directory.GetFiles(
                Notus.IO.GetFolderName(objSettings, directoryName),"*." + extension
            );
        }
        public static string[] GetZipFiles(Notus.Variable.Enum.NetworkType networkType,Notus.Variable.Enum.NetworkLayer networkLayer)
        {
            return GetFileList(networkType,networkLayer, DirListConst.Block,"zip");
        }
        public static string[] GetZipFiles(Notus.Globals.Variable.Settings objSettings)
        {
            return GetZipFiles(objSettings.Network, objSettings.Layer);
        }
        public static string GetFolderName(string folderName)
        {
            return GetFolderName(NVG.Settings.Network, NVG.Settings.Layer, folderName);
        }
        public static string GetFolderName(Notus.Globals.Variable.Settings objSettings, string folderName)
        {
            return GetFolderName(objSettings.Network, objSettings.Layer, folderName);
        }
        public static string GetFolderName(Variable.Enum.NetworkType networkType, Variable.Enum.NetworkLayer networkLayer, string folderName)
        {
            return
                Network.Text.NetworkTypeText(networkType) +
                Path.DirectorySeparatorChar +
                Network.Text.NetworkLayerText(networkLayer) +
                Path.DirectorySeparatorChar +
                folderName +
                Path.DirectorySeparatorChar;
        }
        public static void CreateDirectory(string DirectoryName)
        {
            if (!Directory.Exists(DirectoryName))
                Directory.CreateDirectory(DirectoryName);
        }
        public static void NodeFolderControl()
        {
            //cüzdan ve bakiye ile ilgili tüm işlemler bu klasör içinde kalacak
            CreateDirectory(GetFolderName(DirListConst.Wallet));

            CreateDirectory(GetFolderName(DirListConst.BlockInfo));
            CreateDirectory(GetFolderName(DirListConst.TxList));
            CreateDirectory(GetFolderName(DirListConst.BlockForTgz));
            CreateDirectory(GetFolderName(DirListConst.TempBlock));
            //CreateDirectory(GetFolderName(DirListConst.Balance));
            CreateDirectory(GetFolderName(DirListConst.Block));
            CreateDirectory(GetFolderName(DirListConst.Common));
            CreateDirectory(GetFolderName(DirListConst.File));
            CreateDirectory(GetFolderName(DirListConst.Node));
            CreateDirectory(GetFolderName(DirListConst.Pool));
        }
    }
}