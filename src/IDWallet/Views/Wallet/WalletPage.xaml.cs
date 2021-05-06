using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Models;
using IDWallet.Resources;
using IDWallet.ViewModels;
using IDWallet.Views.BaseId;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Inbox;
using IDWallet.Views.Settings;
using IDWallet.Views.Wallet.PopUps;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Svg;

namespace IDWallet.Views.Wallet
{
    [DesignTimeVisible(false)]
    public partial class WalletPage : ContentPage
    {
        private bool _arrowIsBouncing { get; set; } = false;

        public readonly WalletViewModel ViewModel;
        public ImageSource SettingsIconImage { get; set; }

        private Command _notificationsClickedCommand;
        public Command NotificationsClickedCommand =>
            _notificationsClickedCommand ??= new Command(Notifications_Clicked);

        private Command _settingsClickedCommand;
        public Command SettingsClickedCommand =>
            _settingsClickedCommand ??= new Command(Settings_Clicked);

        private Command _baseIdTappedCommand;
        public Command BaseIdTappedCommand =>
            _baseIdTappedCommand ??= new Command(BaseIdTapped);

        public WalletPage()
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingNotOpen_Icon.svg");

            InitializeComponent();

            BindingContext = ViewModel = new WalletViewModel();

            if (DependencyService.Get<IAusweisSdk>().DeviceHasNfc())
            {
                Device.StartTimer(new TimeSpan(0, 0, 2), () =>
                {
                    BaseID_Button.IsEnabled = false;
                    BaseIdFrame.IsEnabled = false;
                    if (!DependencyService.Get<IAusweisSdk>().IsConnected())
                    {
                        DependencyService.Get<IAusweisSdk>().BindService();
                        return true;
                    }
                    else
                    {
                        BaseIdFrame.IsEnabled = true;
                        BaseID_Button.IsEnabled = true;
                        return false;
                    }
                });
            }

            Subscribe();
        }

        private void Subscribe()
        {
            MessagingCenter.Subscribe<InboxPage>(this, WalletEvents.DisableNotifications, NotificationsClosed);
        }


        public async void Delete_Credential(WalletElement walletElement)
        {
            DisableAll();
            DeleteCredentialPopUp popUp = new DeleteCredentialPopUp();
            PopUpResult popResult = await popUp.ShowPopUp();
            if (PopUpResult.Accepted == popResult)
            {
                await ViewModel.DeleteWalletElement(walletElement.CredentialRecord.Id);
            }

            EnableAll();
        }

        public void ReloadCredentials()
        {
            ViewModel.LoadItemsCommand.Execute(null);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            EnableAll();

            if (ViewModel.WalletElements.Count == 0)
            {
                ViewModel.LoadItemsCommand.Execute(null);
            }

            App.ScanActive = false;
        }

        private void DisableAll()
        {
            NotificationsToolBarItem.IsEnabled = false;
            SettingsToolBarItem.IsEnabled = false;
        }

        private void EmptyStackLayout_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            StackLayout emptyStack = sender as StackLayout;
            if (emptyStack.IsVisible && !_arrowIsBouncing)
            {
                _arrowIsBouncing = true;
                new Animation
                {
                    {0, 0.2, new Animation(v => ArrowFrame.TranslationY = v, 0, -30)},
                    {0.2, 0.4, new Animation(v => ArrowFrame.TranslationY = v, -30, 0, Easing.BounceOut)}
                }.Commit(this, "BouncingArrow", length: 5000, repeat: () => true);
            }
            else
            {
                ArrowFrame.CancelAnimations();
            }
        }

        private void EnableAll()
        {
            NotificationsToolBarItem.IsEnabled = true;
            SettingsToolBarItem.IsEnabled = true;
        }

        private async void Notifications_Clicked()
        {
            DisableAll();
            InboxPage notificationsPage = null;
            try
            {
                bool nextPageExists = false;
                System.Collections.Generic.IEnumerator<Page> oldPageEnumerator =
                    Application.Current.MainPage.Navigation.ModalStack.GetEnumerator();
                do
                {
                    nextPageExists = oldPageEnumerator.MoveNext();
                } while (nextPageExists && !(oldPageEnumerator.Current is InboxPage));

                if (oldPageEnumerator.Current is InboxPage)
                {
                    notificationsPage = (InboxPage)oldPageEnumerator.Current;
                }
            }
            catch (Exception)
            {
                notificationsPage = new InboxPage();
            }
            finally
            {
                if (notificationsPage == null)
                {
                    notificationsPage = new InboxPage();
                }
            }

            await Navigation.PushAsync(notificationsPage);
            EnableAll();
        }

        private void NotificationsClosed(InboxPage obj)
        {
            ViewModel.DisableNotificationsCommand.Execute(null);
        }
        private async void Settings_Clicked()
        {
            DisableAll();
            await Navigation.PushAsync(new SettingsPage());
            EnableAll();
        }

        private async void BaseIdTapped(object obj)
        {
            DisableAll();
            if (DependencyService.Get<IAusweisSdk>().NfcEnabled())
            {
                DependencyService.Get<IAusweisSdk>().StartSdkIos();
                DependencyService.Get<IAusweisSdk>().EnableNfcDispatcher();
                await Navigation.PushAsync(new BaseIdPage());
            }
            else
            {
                BasicPopUp popUp = new BasicPopUp(
                    Lang.PopUp_NFC_Not_Enabled_Title,
                    Lang.PopUp_NFC_Not_Enabled_Text,
                    Lang.PopUp_NFC_Not_Enabled_Button
                    );
                await popUp.ShowPopUp();
            }
            EnableAll();
        }
    }
}