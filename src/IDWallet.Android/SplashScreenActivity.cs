using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.App;

namespace IDWallet.Droid
{
    [Activity(Label = WalletParams.WalletName, Icon = "@mipmap/ic_launcher", Theme = "@style/SplashScreenTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, NoHistory = true, ScreenOrientation = ScreenOrientation.Portrait)]

    public class SplashScreenActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;

            StartActivity(typeof(MainActivity));
        }
    }
}