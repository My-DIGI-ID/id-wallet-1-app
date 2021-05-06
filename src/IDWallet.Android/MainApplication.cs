using Android.App;
using Android.Runtime;
using Android.Support.V7.App;
using Plugin.CurrentActivity;
using System;

namespace IDWallet.Droid
{
#if DEBUG
    [Application(Debuggable = true)]
#else
    [Application(Debuggable = false)]
#endif
    public class MainApplication : Application
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transer)
            : base(handle, transer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;
            CrossCurrentActivity.Current.Init(this);
        }
    }
}