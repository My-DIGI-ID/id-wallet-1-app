using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Svg;

namespace IDWallet.ViewModels
{
    public class CustomViewModel : INotifyPropertyChanged
    {
        private bool _hasNotifications;

        private bool _isConnected = false;

        private bool _isLoading = false;

        private ImageSource _notificationIconImage;

        private string _title = string.Empty;

        public CustomViewModel()
        {
            Connectivity.ConnectivityChanged += CheckConnectivity;
            IsConnected = Connectivity.NetworkAccess == NetworkAccess.Internet
                          || Connectivity.NetworkAccess == NetworkAccess.ConstrainedInternet;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Command DisableNotificationsCommand { get; set; }

        public bool HasNotifications
        {
            get => _hasNotifications;
            private set => SetProperty(ref _hasNotifications, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        public ImageSource NotificationIconImage
        {
            get => _notificationIconImage;
            private set => SetProperty(ref _notificationIconImage, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }
        public void DisableNotificationAlert()
        {
            HasNotifications = false;
            NotificationIconImage = SvgImageSource.FromSvgResource("imagesources.NotificationOff_Icon.svg");
        }

        protected void CheckConnectivity(object sender, EventArgs e)
        {
            IsConnected = Connectivity.NetworkAccess == NetworkAccess.Internet
                          || Connectivity.NetworkAccess == NetworkAccess.ConstrainedInternet;
            if (IsConnected)
            {
                Reload();
            }
        }

        protected void EnableNotificationAlert()
        {
            HasNotifications = true;
            NotificationIconImage = SvgImageSource.FromSvgResource("imagesources.NotificationOn_Icon.svg");
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler changed = PropertyChanged;
            if (changed == null)
            {
                return;
            }

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void Reload()
        {
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "",
                                                    Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
            {
                return false;
            }

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}