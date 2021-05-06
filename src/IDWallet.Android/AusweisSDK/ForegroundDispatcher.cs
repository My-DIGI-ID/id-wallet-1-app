using Android.App;
using Android.Nfc;
using Com.Governikus.Ausweisapp2;

namespace IDWallet.Droid.AusweisSDK
{
    public class ForegroundDispatcher
    {
        private readonly Activity _activity;
        private readonly NfcAdapter _adapter;
        private readonly int _flags;
        private readonly ReaderCallback _readerCallback;

        public ForegroundDispatcher(Activity activity)
        {
            _activity = activity;
            _adapter = NfcAdapter.GetDefaultAdapter(_activity);

            _flags = (int)NfcReaderFlags.NfcA | (int)NfcReaderFlags.NfcB | (int)NfcReaderFlags.SkipNdefCheck;
            _readerCallback = new ReaderCallback(MainActivity.AusweisSdkServiceConnection.AusweisSdk, MainActivity.AusweisSdkServiceConnection.AusweisSdkCallback.SessionID);
        }

        public void Enable()
        {
            if (_adapter != null)
            {
                try
                {
                    _adapter.EnableReaderMode(_activity, _readerCallback, (NfcReaderFlags)_flags, null);
                }
                catch (Java.Lang.IllegalStateException)
                {
                    //ignore
                }
            }
        }

        public void Disable()
        {
            if (_adapter != null)
            {
                _adapter.DisableReaderMode(_activity);
            }
        }
    }

    public class ReaderCallback : Java.Lang.Object, NfcAdapter.IReaderCallback
    {
        private readonly IAusweisApp2Sdk _ausweisApp2Sdk;
        private readonly string _sessionId;

        public Tag NfcTag { get; private set; }

        public ReaderCallback(IAusweisApp2Sdk ausweisApp2Sdk, string sessionId)
        {
            _ausweisApp2Sdk = ausweisApp2Sdk;
            _sessionId = sessionId;
        }

        public void OnTagDiscovered(Tag tag)
        {
            try
            {
                string[] techList = tag.GetTechList();
                bool tagUpdated = _ausweisApp2Sdk.UpdateNfcTag(_sessionId, tag);
            }
            catch (System.Exception)
            {
                //ignore
            }

        }
    }
}