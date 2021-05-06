namespace IDWallet.Interfaces
{
    public interface IAusweisSdk
    {
        bool DeviceHasNfc();

        bool NfcEnabled();

        void BindService();

        void StartSdkIos();

        void EnableNfcDispatcher();

        bool IsConnected();

        bool SendCall(string command);
    }
}
