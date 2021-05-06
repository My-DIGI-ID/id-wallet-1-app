using CoreNFC;
using IDWallet.Interfaces;
using IDWallet.iOS.AusweisSDK;
using System.Diagnostics;

[assembly: Xamarin.Forms.Dependency(typeof(AusweisSdk_iOS))]
namespace IDWallet.iOS.AusweisSDK
{
    class AusweisSdk_iOS : IAusweisSdk
    {
        public void BindService()
        {
            //Nothing to do
        }

        public bool DeviceHasNfc()
        {
            if (NFCNdefReaderSession.ReadingAvailable)
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
            //Nothing to do
        }

        public bool IsConnected()
        {
            Debug.WriteLine("IsConnected called, on iOS always connected");
            return true;
        }

        public bool NfcEnabled()
        {
            if (NFCNdefReaderSession.ReadingAvailable)
            {
                return true;
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
                AppDelegate.AusweisApp2Adapter.Send(command);
                Debug.WriteLine($"Send called with command: {command}");

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Send calling error {ex.Message}");

                return false;
            }

        }

        public void StartSdkIos()
        {
            AppDelegate.BindSdk();
        }
    }
}