using Com.Governikus.Ausweisapp2;
using IDWallet.Droid.AusweisSDK;
using IDWallet.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(AusweisSdk_Android))]
namespace IDWallet.Droid.AusweisSDK
{
    public class AusweisSdk_Android : IAusweisSdk
    {
        public void BindService()
        {
            MainActivity.BindAusweisService();
        }

        public bool DeviceHasNfc()
        {
            if (MainActivity.NfcAdapter != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void EnableNfcDispatcher()
        {
            MainActivity.EnableNfcDispatcher();
        }

        public bool IsConnected()
        {
            try
            {
                AusweisSdkServiceConnection ausweisSdkServiceConnection = MainActivity.AusweisSdkServiceConnection;
                
                if (ausweisSdkServiceConnection == null)
                {
                    return false;
                }

                return ausweisSdkServiceConnection.IsConnected;
            }
            catch
            {
                return false;
            }
        }

        public bool NfcEnabled()
        {
            if (MainActivity.NfcAdapter != null)
            {
                return MainActivity.NfcAdapter.IsEnabled;
            }
            else
            {
                return false;
            }
        }

        public bool SendCall(string command)
        {
            try
            {
                IAusweisApp2Sdk ausweisSdk = MainActivity.AusweisSdkServiceConnection.AusweisSdk;
                AusweisSdkCallback ausweisSdkCallback = MainActivity.AusweisSdkServiceConnection.AusweisSdkCallback;

                return ausweisSdk.Send(ausweisSdkCallback.SessionID, command);
            }
            catch (System.Exception e)
            {
                return false;
            }

        }

        public void StartSdkIos()
        {
            //Nothing to do
        }
    }
}