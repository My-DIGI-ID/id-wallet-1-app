using IDWallet.Droid.PDF;
using IDWallet.Interfaces;
using System.IO;

[assembly: Xamarin.Forms.Dependency(typeof(AndroidExternalStorageWriter))]
namespace IDWallet.Droid.PDF
{
    public class AndroidExternalStorageWriter : IAndroidExternalStorageWriter
    {
        public string CreateFile(string filename, byte[] bytes)
        {
            if (!Directory.Exists(Path.Combine(MainActivity.Instance.GetExternalFilesDir(null).AbsolutePath, WalletParams.WalletName)))
            {
                Directory.CreateDirectory(Path.Combine(MainActivity.Instance.GetExternalFilesDir(null).AbsolutePath, WalletParams.WalletName));
            }

            string path = Path.Combine(MainActivity.Instance.GetExternalFilesDir(null).AbsolutePath, WalletParams.WalletName, filename);

            File.WriteAllBytes(path, bytes);

            return path;
        }
    }
}