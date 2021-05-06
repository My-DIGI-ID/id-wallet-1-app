using Android.Util;
using Autofac;
using Com.Governikus.Ausweisapp2;
using IDWallet.Models.AusweisSDK;
using IDWallet.Services;
using Newtonsoft.Json.Linq;

namespace IDWallet.Droid.AusweisSDK
{
    public class AusweisSdkCallback : AusweisApp2SdkCallbackStub
    {
        static readonly string TAG = typeof(AusweisSdkCallback).FullName;
        private readonly SDKMessageService _sdkService = App.Container.Resolve<SDKMessageService>();

        public string SessionID = null;

        public override void Receive(string p0)
        {
            try
            {
                SdkMessage message = JObject.Parse(p0).ToObject<SdkMessage>();
                switch (message.Msg)
                {
                    case nameof(SdkMessageType.ACCESS_RIGHTS):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.ACCESS_RIGHTS)} message received: {p0}");
                        _sdkService.ReceiveAccessRights(message);
                        break;
                    case nameof(SdkMessageType.API_LEVEL):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.API_LEVEL)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.AUTH):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.AUTH)} message received: {p0}");
                        _sdkService.ReceiveAuth(message);
                        break;
                    case nameof(SdkMessageType.BAD_STATE):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.BAD_STATE)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.CERTIFICATE):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.CERTIFICATE)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.CHANGE_PIN):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.CHANGE_PIN)} message received: {p0}");
                        _sdkService.ReceiveChangePIN(message);
                        break;
                    case nameof(SdkMessageType.ENTER_CAN):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.ENTER_CAN)} message received: {p0}");
                        _sdkService.ReceiveEnterCAN(message);
                        break;
                    case nameof(SdkMessageType.ENTER_NEW_PIN):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.ENTER_NEW_PIN)} message received: {p0}");
                        _sdkService.ReceiveEnterNewPIN(message);
                        break;
                    case nameof(SdkMessageType.ENTER_PIN):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.ENTER_PIN)} message received: {p0}");
                        _sdkService.ReceiveEnterPIN(message);
                        break;
                    case nameof(SdkMessageType.ENTER_PUK):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.ENTER_PUK)} message received: {p0}");
                        _sdkService.ReceiveEnterPUK(message);
                        break;
                    case nameof(SdkMessageType.INFO):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.INFO)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.INSERT_CARD):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.INSERT_CARD)} message received: {p0}");
                        _sdkService.ReceiveInsertCard(message);
                        break;
                    case nameof(SdkMessageType.INTERNAL_ERROR):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.INTERNAL_ERROR)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.INVALID):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.INVALID)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.READER):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.READER)} message received: {p0}");
                        _sdkService.ReceiveReader(message);
                        break;
                    case nameof(SdkMessageType.READER_LIST):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.READER_LIST)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.UNKNOWN_COMMAND):
                        Log.Debug(TAG, $"{nameof(SdkMessageType.UNKNOWN_COMMAND)} message received: {p0}");
                        break;
                    default:
                        Log.Debug(TAG, $"Unknown message received: {p0}");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Log.Debug(TAG, $"Error receiving a new message: {ex.Message}");
            }
        }

        public override void SdkDisconnected()
        {
            SessionID = null;
        }

        public override void SessionIdGenerated(string p0, bool p1)
        {
            SessionID = p0;
        }
    }
}