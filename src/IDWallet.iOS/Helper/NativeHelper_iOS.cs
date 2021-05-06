using IDWallet.Interfaces;
using IDWallet.iOS.Helper;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(NativeHelper_iOS))]
namespace IDWallet.iOS.Helper
{
    public class NativeHelper_iOS : INativeHelper
    {

        public string GetAppVersion() { return Foundation.NSBundle.MainBundle.InfoDictionary[new Foundation.NSString("CFBundleVersion")].ToString(); }

        public string GetOsVersion() { return UIDevice.CurrentDevice.SystemVersion; }
    }
}