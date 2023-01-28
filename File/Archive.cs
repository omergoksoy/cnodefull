using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Linq;
using System.Collections.Concurrent;
using System.Text.Json;
using Notus.Compression.TGZ;
using System;

namespace Notus
{
    public static class Archive
    {
        public static void ClearBlocks(Notus.Globals.Variable.Settings objSettings)
        {
            ClearBlocks(objSettings.Network, objSettings.Layer);
        }
        public static void ClearBlocks(
            Notus.Variable.Enum.NetworkType networkType,
            Notus.Variable.Enum.NetworkLayer networkLayer
        )
        {
            DirectoryInfo d = new DirectoryInfo(
                Notus.IO.GetFolderName(
                    networkType,
                    networkLayer,
                    Notus.Variable.Constant.StorageFolderName.Block
                )
            );
            FileInfo[] filesList = d.GetFiles("*.zip");
            foreach (FileInfo fileObj in filesList)
            {
                File.Delete(fileObj.FullName);
            }
        }
    }

}