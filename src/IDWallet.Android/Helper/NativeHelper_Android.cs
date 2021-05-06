using Android.Content;
using Android.OS;
using IDWallet.Droid.Helper;
using IDWallet.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(NativeHelper_Android))]
namespace IDWallet.Droid.Helper
{
    public class NativeHelper_Android : INativeHelper
    {

        public string GetAppVersion()
        {
            Context context = Xamarin.Essentials.Platform.AppContext;
            return context.PackageManager.GetPackageInfo(context.PackageName, 0).VersionName;
        }

        public string GetOsVersion()
        {
            return ((int)Build.VERSION.SdkInt).ToString();
        }
    }
}