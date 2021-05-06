using AusweisApp2Adapters;
using Autofac;
using CoreFoundation;
using CoreNFC;
using IDWallet.Interfaces;
using IDWallet.Models.AusweisSDK;
using IDWallet.Services;
using IDWallet.Views;
using FormsPinView.iOS;
using Foundation;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using UIKit;
using UserNotifications;
using WindowsAzure.Messaging;

namespace IDWallet.iOS
{
    [Register("AppDelegate")]
    public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate, IAusweisApp2Delegate
    {
        public static AusweisApp2Adapter AusweisApp2Adapter;

        private static AppDelegate appDelegate;

        private SBNotificationHub Hub { get; set; }

        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Rg.Plugins.Popup.Popup.Init();

            global::Xamarin.Forms.Forms.Init();
            FFImageLoading.Forms.Platform.CachedImageRenderer.Init();
            Xamarin.Forms.Svg.iOS.SvgImage.Init();
            PinItemViewRenderer.Init();
            global::ZXing.Net.Mobile.Forms.iOS.Platform.Init();
            LoadApplication(new App());

            bool finished = base.FinishedLaunching(app, options);

            RegisterForRemoteNotifications();

            appDelegate = this;

            return finished;
        }

        [Export("didReceiveWithMessage:")]
        void DidReceive(string p0)
        {
            SDKMessageService _sdkService = App.Container.Resolve<SDKMessageService>();

            Debug.WriteLine($"New message {p0}");
            try
            {
                SdkMessage message = JObject.Parse(p0).ToObject<SdkMessage>();
                switch (message.Msg)
                {
                    case nameof(SdkMessageType.ACCESS_RIGHTS):
                        Debug.WriteLine($"{nameof(SdkMessageType.ACCESS_RIGHTS)} message received: {p0}");
                        _sdkService.ReceiveAccessRights(message);
                        break;
                    case nameof(SdkMessageType.API_LEVEL):
                        Debug.WriteLine($"{nameof(SdkMessageType.API_LEVEL)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.AUTH):
                        Debug.WriteLine($"{nameof(SdkMessageType.AUTH)} message received: {p0}");
                        _sdkService.ReceiveAuth(message);
                        break;
                    case nameof(SdkMessageType.BAD_STATE):
                        Debug.WriteLine($"{nameof(SdkMessageType.BAD_STATE)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.CERTIFICATE):
                        Debug.WriteLine($"{nameof(SdkMessageType.CERTIFICATE)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.CHANGE_PIN):
                        Debug.WriteLine($"{nameof(SdkMessageType.CHANGE_PIN)} message received: {p0}");
                        _sdkService.ReceiveChangePIN(message);
                        break;
                    case nameof(SdkMessageType.ENTER_CAN):
                        Debug.WriteLine($"{nameof(SdkMessageType.ENTER_CAN)} message received: {p0}");
                        _sdkService.ReceiveEnterCAN(message);
                        break;
                    case nameof(SdkMessageType.ENTER_NEW_PIN):
                        Debug.WriteLine($"{nameof(SdkMessageType.ENTER_NEW_PIN)} message received: {p0}");
                        _sdkService.ReceiveEnterNewPIN(message);
                        break;
                    case nameof(SdkMessageType.ENTER_PIN):
                        Debug.WriteLine($"{nameof(SdkMessageType.ENTER_PIN)} message received: {p0}");
                        _sdkService.ReceiveEnterPIN(message);
                        break;
                    case nameof(SdkMessageType.ENTER_PUK):
                        Debug.WriteLine($"{nameof(SdkMessageType.ENTER_PUK)} message received: {p0}");
                        _sdkService.ReceiveEnterPUK(message);
                        break;
                    case nameof(SdkMessageType.INFO):
                        Debug.WriteLine($"{nameof(SdkMessageType.INFO)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.INSERT_CARD):
                        Debug.WriteLine($"{nameof(SdkMessageType.INSERT_CARD)} message received: {p0}");
                        _sdkService.ReceiveInsertCard(message);
                        break;
                    case nameof(SdkMessageType.INTERNAL_ERROR):
                        Debug.WriteLine($"{nameof(SdkMessageType.INTERNAL_ERROR)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.INVALID):
                        Debug.WriteLine($"{nameof(SdkMessageType.INVALID)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.READER):
                        Debug.WriteLine($"{nameof(SdkMessageType.READER)} message received: {p0}");
                        _sdkService.ReceiveReader(message);
                        break;
                    case nameof(SdkMessageType.READER_LIST):
                        Debug.WriteLine($"{nameof(SdkMessageType.READER_LIST)} message received: {p0}");
                        break;
                    case nameof(SdkMessageType.UNKNOWN_COMMAND):
                        Debug.WriteLine($"{nameof(SdkMessageType.UNKNOWN_COMMAND)} message received: {p0}");
                        break;
                    default:
                        Debug.WriteLine($"Unknown message received: {p0}");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Error receiving a new message: {ex.Message}");
            }
        }

        public static void BindSdk()
        {
            if (NFCNdefReaderSession.ReadingAvailable)
            {
                NSError error;
                AusweisApp2Adapter = new AusweisApp2Adapter(appDelegate, out error);
            }
        }

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            IAppDeeplinkService _appDeeplinkService = App.Container.Resolve<IAppDeeplinkService>();
            _appDeeplinkService.AppDeeplinkUri = url;
            return true;
        }

        private void RegisterForRemoteNotifications()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound,
                (granted, error) =>
                {
                    if (granted)
                    {
                        InvokeOnMainThread(UIApplication.SharedApplication.RegisterForRemoteNotifications);
                    }
                });
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                UIUserNotificationSettings pushSettings = UIUserNotificationSettings.GetSettingsForTypes(
                        UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound,
                        new NSSet());

                UIApplication.SharedApplication.RegisterUserNotificationSettings(pushSettings);
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            }
            else
            {
                UIRemoteNotificationType notificationTypes = UIRemoteNotificationType.Alert | UIRemoteNotificationType.Badge | UIRemoteNotificationType.Sound;
                UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(notificationTypes);
            }
        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            try
            {
                Hub = new SBNotificationHub(WalletParams.ListenConnectionString, WalletParams.NotificationHubName);

                Hub.UnregisterAll(deviceToken, (error) =>
                {
                    if (error != null)
                    {
                        return;
                    }

                    App.SetPnsHandle(App.NativeStorageService, UIDevice.CurrentDevice.IdentifierForVendor.AsString());

                    NSSet tags = new NSSet(UIDevice.CurrentDevice.IdentifierForVendor.AsString());
                    Hub.RegisterNative(deviceToken, tags, (errorCallback) => { });
                });
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            ProcessNotification(userInfo, false);
        }

        public override void DidReceiveRemoteNotification(UIApplication application, NSDictionary userInfo, Action<UIBackgroundFetchResult> completionHandler)
        {
            ProcessNotification(userInfo, false);
        }

        void ProcessNotification(NSDictionary options, bool fromFinishedLaunching)
        {
            if (options != null && options.ContainsKey(new NSString("aps")))
            {
                NSDictionary aps = options.ObjectForKey(new NSString("aps")) as NSDictionary;
                string payload = IDWallet.Resources.Lang.Native_Notification_Message;

                if (!string.IsNullOrWhiteSpace(payload))
                {
                    (App.Current.MainPage as CustomTabbedPage)?.AddMessage(payload);
                }
            }
        }
    }
}
