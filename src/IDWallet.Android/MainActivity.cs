using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Nfc;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using IDWallet.Droid.AusweisSDK;
using IDWallet.Views;
using Java.Lang;
using Plugin.CurrentActivity;
using Plugin.Fingerprint;
using Plugin.Iconize;
using Plugin.Permissions;
using Rg.Plugins.Popup;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android.AppLinks;
using Color = Android.Graphics.Color;
using Exception = System.Exception;
using Platform = Xamarin.Essentials.Platform;

namespace IDWallet.Droid
{
    [Activity(Label = WalletParams.WalletName, Theme = "@style/MainTheme", MainLauncher = false, LaunchMode = LaunchMode.SingleTop,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        ScreenOrientation = ScreenOrientation.Portrait)]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[]
        {
            Intent.ActionView,
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
        },
        DataScheme = WalletParams.AppLinkDidcommTag,
        AutoVerify = true)
    ]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[]
        {
            Intent.ActionView,
            Intent.CategoryDefault,
            Intent.CategoryBrowsable
        },
        DataScheme = WalletParams.AppLinkIdTag,
        AutoVerify = true)
    ]
    public class MainActivity : FormsAppCompatActivity
    {
        internal static MainActivity Instance { get; private set; }

        public static NfcAdapter NfcAdapter;

        public static AusweisSdkServiceConnection AusweisSdkServiceConnection { get; private set; }

        public static ForegroundDispatcher Dispatcher;

        public static string Alias = "";

        public override void OnBackPressed()
        {
            if (Xamarin.Forms.Application.Current.MainPage.Navigation.ModalStack.Any())
            {
                return;
            }

            if (Popup.SendBackPressed())
            {
                return;
            }

            base.OnBackPressed();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Instance = this;
            NfcAdapter = NfcAdapter.GetDefaultAdapter(this);

            AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;

            JavaSystem.LoadLibrary("gnustl_shared");
            JavaSystem.LoadLibrary("c++_shared");
            JavaSystem.LoadLibrary("indy");

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            Popup.Init(this);

            base.OnCreate(savedInstanceState);

            CrossFingerprint.SetCurrentActivityResolver(() => CrossCurrentActivity.Current.Activity);

            Platform.Init(this, savedInstanceState);
            Forms.Init(this, savedInstanceState);
            FFImageLoading.Forms.Platform.CachedImageRenderer.Init(true);

            AndroidAppLinks.Init(this);

            Iconize.Init(Resource.Id.toolbar, Resource.Id.sliding_tabs);

            Window.SetStatusBarColor(Color.ParseColor("#f2f2f9"));

            ZXing.Net.Mobile.Forms.Android.Platform.Init();

            Xamarin.Forms.Svg.Droid.SvgImage.Init(this);

            List<string> permissionsNeeded = new List<string>
            {
                Manifest.Permission.ReadExternalStorage,
                Manifest.Permission.WriteExternalStorage,
                Manifest.Permission.Internet,
                Manifest.Permission.Camera,
                Manifest.Permission.Nfc,
                Manifest.Permission.NfcTransactionEvent,
                Manifest.Permission.BindNfcService
            };
            List<string> permissionsNotGranted = new List<string>();
            foreach (string permission in permissionsNeeded)
            {
                if (ContextCompat.CheckSelfPermission(this, permission) != (int)Android.Content.PM.Permission.Granted)
                {
                    permissionsNotGranted.Add(permission);
                }
            }

            if (permissionsNotGranted.Any())
            {
                RequestPermissions(permissionsNotGranted.ToArray(), 10);
            }

            LoadApplication(new App());

            if (!IsPlayServiceAvailable())
            {
                throw new Exception("This device does not have Google Play Services and cannot receive push notifications.");
            }

            CreateNotificationChannel();
        }

        public static void BindAusweisService()
        {
            if (AusweisSdkServiceConnection == null)
            {
                AusweisSdkServiceConnection = new AusweisSdkServiceConnection(Instance);
            }

            Intent ausweisServiceToStart = new Intent("com.governikus.ausweisapp2.START_SERVICE");
            ausweisServiceToStart.SetPackage(WalletParams.PackageName);
            Instance.BindService(ausweisServiceToStart, AusweisSdkServiceConnection, Bind.AutoCreate);
        }

        protected override void OnDestroy()
        {
            if (NfcAdapter != null)
            {
                try
                {
                    UnbindService(AusweisSdkServiceConnection);
                }
                catch (System.Exception)
                { }

                try
                {
                    DisableNfcDispatcher();
                }
                catch (System.Exception)
                { }
            }

            base.OnDestroy();
        }

        public void CreateNfcDispatcher()
        {
            Dispatcher = new ForegroundDispatcher(this);
            if (Dispatcher != null)
            {
                EnableNfcDispatcher();
            }
        }

        public static void EnableNfcDispatcher()
        {
            if (Dispatcher != null)
            {
                Dispatcher.Enable();
            }
        }

        public void DisableNfcDispatcher()
        {
            if (Dispatcher != null)
            {
                Dispatcher.Disable();
            }
        }

        protected override void OnNewIntent(Intent intent)
        {
            if (intent.Extras != null)
            {
                string message = intent.GetStringExtra("message");
                (App.Current.MainPage as CustomTabbedPage)?.AddMessage(message);
            }
            base.OnNewIntent(intent);
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                string channelName = WalletParams.NotificationChannelName;
                string channelDescription = string.Empty;
                NotificationChannel channel = new NotificationChannel(channelName, channelName, NotificationImportance.Default)
                {
                    Description = channelDescription
                };
                NotificationManager notificationManager = (NotificationManager)GetSystemService(NotificationService);
                notificationManager.CreateNotificationChannel(channel);
            }
        }

        public static bool IsPlayServiceAvailable()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(Instance);
            if (resultCode != ConnectionResult.Success)
            {
                return false;
            }
            return true;
        }
    }
}