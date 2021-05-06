using Android.Content;
using Android.OS;
using Android.Util;
using Com.Governikus.Ausweisapp2;
using System;

namespace IDWallet.Droid.AusweisSDK
{
    public class AusweisSdkServiceConnection : Java.Lang.Object, IServiceConnection
    {
        static readonly string TAG = typeof(AusweisSdkServiceConnection).FullName;
        public IAusweisApp2Sdk AusweisSdk { get; private set; }
        public AusweisSdkCallback AusweisSdkCallback { get; private set; }

        public bool IsConnected { get; private set; }
        private readonly MainActivity _mainActivity;

        public AusweisSdkServiceConnection(MainActivity mainActivity)
        {
            _mainActivity = mainActivity;
        }

        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            AusweisSdk = AusweisApp2SdkStub.AsInterface(service);

            IsConnected = AusweisSdk != null;
            Log.Debug(TAG, $"Service Connected = {IsConnected}");

            if (IsConnected)
            {
                try
                {
                    AusweisSdkCallback = new AusweisSdkCallback();
                    IsConnected = AusweisSdk.ConnectSdk(AusweisSdkCallback);

                    Log.Debug(TAG, $"SDK Connected = {IsConnected}");
                }
                catch (Exception e)
                {
                    Log.Debug(TAG, e.Message);
                }


                _mainActivity.CreateNfcDispatcher();
            }
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            AusweisSdk = null;
        }
    }
}