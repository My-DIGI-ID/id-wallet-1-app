using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using IDWallet.Views;
using Firebase.Messaging;
using System;
using WindowsAzure.Messaging;

namespace IDWallet.Droid.Push
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class FirebaseService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);
            string messageTitle = IDWallet.Resources.Lang.Native_Notification_Title;
            string messageBody = IDWallet.Resources.Lang.Native_Notification_Message;

            if (!App.IsInForeground)
            {
                SendLocalNotification(messageTitle, messageBody);
            }
            SendMessageToMainPage(messageBody);
        }

        public override void OnNewToken(string token)
        {
            SendRegistrationToServer(token);
        }

        private void SendLocalNotification(string title, string body)
        {
            Intent intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            intent.PutExtra("message", body);
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);
            NotificationCompat.Builder notificationBuilder = new NotificationCompat.Builder(this, WalletParams.NotificationChannelName)
                .SetContentTitle(title)
                .SetSmallIcon(Resource.Drawable.ic_notification)
                .SetBadgeIconType(Resource.Drawable.ic_notification)
                .SetLargeIcon(BitmapFactory.DecodeResource(Resources, Resource.Drawable.ic_notification))
                .SetStyle(new NotificationCompat.BigTextStyle().BigText(body))
                .SetContentText(body)
                .SetAutoCancel(true)
                .SetShowWhen(false)
                .SetContentIntent(pendingIntent);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                notificationBuilder.SetChannelId(WalletParams.NotificationChannelName);
            }
            NotificationManager notificationManager = NotificationManager.FromContext(this);
            notificationManager.Notify(0, notificationBuilder.Build());
        }

        private void SendMessageToMainPage(string body)
        {
            (App.Current.MainPage as CustomTabbedPage)?.AddMessage(body);
        }

        private void SendRegistrationToServer(string token)
        {
            try
            {
                NotificationHub hub = new NotificationHub(WalletParams.NotificationHubName, WalletParams.ListenConnectionString, this);
                Registration registration = hub.Register(token);
                string pnsHandle = registration.PNSHandle;
                App.SetPnsHandle(App.NativeStorageService, pnsHandle);
                TemplateRegistration templateReg = hub.RegisterTemplate(pnsHandle, "defaultTemplate", WalletParams.FCMTemplateBody);
            }
            catch (Exception)
            {
                //Ignore
            }
        }
    }
}