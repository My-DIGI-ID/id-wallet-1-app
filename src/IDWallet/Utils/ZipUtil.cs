using System;
using System.IO;
using System.IO.Compression;

namespace IDWallet.Utilities
{
    public class ZipUtil
    {
        public static bool UnzipFiles(string zippedFilePath, string destinationPath)
        {
            try
            {
                if (Directory.Exists(destinationPath))
                {
                    Directory.Delete(destinationPath, true);
                }

                byte[] head = new byte[2];
                using (FileStream file = new FileStream(zippedFilePath, FileMode.Open, FileAccess.Read))
                {
                    file.Read(head, 0, 2);
                }

                if (head[0] != 80 || head[1] != 75)
                {
                    return false;
                }

                using (ZipArchive archive = ZipFile.Open(Path.GetFullPath(zippedFilePath), ZipArchiveMode.Update))
                {
                    archive.ExtractToDirectory(destinationPath);
                }

                return File.Exists(Directory.GetFiles(destinationPath)[0]);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}