using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Models;
using IDWallet.Resources;
using IDWallet.ViewModels;
using IDWallet.Views.BaseId;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.DDL;
using IDWallet.Views.Inbox;
using IDWallet.Views.Settings;
using IDWallet.Views.Wallet.PopUps;
using System;
using System.ComponentModel;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Svg;

namespace IDWallet.Views.Wallet
{
    [DesignTimeVisible(false)]
    public partial class WalletPage : ContentPage
    {
        private bool _clicked = false;
        public readonly WalletViewModel ViewModel;
        public ImageSource SettingsIconImage { get; set; }
        public ImageSource AddCredentialsImage { get; set; }

        private Command _notificationsClickedCommand;
        public Command NotificationsClickedCommand =>
            _notificationsClickedCommand ??= new Command(Notifications_Clicked);

        private Command _settingsClickedCommand;
        public Command SettingsClickedCommand =>
            _settingsClickedCommand ??= new Command(Settings_Clicked);

        private Command _addBaseIdTappedCommand;
        public Command AddBaseIdTappedCommand =>
            _addBaseIdTappedCommand ??= new Command(AddBaseIdTapped);

        private Command _addDdlTappedCommand;
        public Command AddDdlTappedCommand =>
            _addDdlTappedCommand ??= new Command(AddDdlTapped);

        private Command _addVacCertTappedCommand;
        public Command AddVacCertTappedCommand =>
            _addVacCertTappedCommand ??= new Command(AddVacCertTapped);

        private Command _addDocumentTappedCommand;
        public Command AddDocumentTappedCommand => _addDocumentTappedCommand ??= new Command(AddDocumentTapped);

        public WalletPage()
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingNotOpen_Icon.svg");

            if (CultureInfo.CurrentUICulture.Name.Equals("de-DE"))
            {
                AddCredentialsImage = SvgImageSource.FromSvgResource("imagesources.WalletPage.AddCredential.svg");
            }
            else
            {
                AddCredentialsImage = SvgImageSource.FromSvgResource("imagesources.WalletPage.AddCredential_en.svg");
            }

            InitializeComponent();

            BindingContext = ViewModel = new WalletViewModel();

            if (DependencyService.Get<IAusweisSdk>().DeviceHasNfc())
            {
                Device.StartTimer(new TimeSpan(0, 0, 2), () =>
                {
                    if (!DependencyService.Get<IAusweisSdk>().IsConnected())
                    {
                        DependencyService.Get<IAusweisSdk>().BindService();
                        return true;
                    }
                    else
                    {
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
            if (_clicked)
            {
                return;
            }
            DisableAll();

            _clicked = true;
            try
            {

                DeleteCredentialPopUp popUp = new DeleteCredentialPopUp();
                PopUpResult popResult = await popUp.ShowPopUp();
                if (PopUpResult.Accepted == popResult)
                {
                    if (string.IsNullOrEmpty(walletElement.VacQrRecordId))
                    {
                        await ViewModel.DeleteWalletElement(walletElement.CredentialRecord.Id);
                    }
                    else
                    {
                        await ViewModel.DeleteWalletQrElement(walletElement.VacQrRecordId);
                    }
                }
            }
            finally
            {
                _clicked = false;
                EnableAll();
            }
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

        private async void AddBaseIdTapped(object obj)
        {
            if (!DependencyService.Get<IAusweisSdk>().IsConnected() || _clicked)
            {
                if (!DependencyService.Get<IAusweisSdk>().DeviceHasNfc())
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Lang.PopUp_NFC_No_NFC_Title,
                        Lang.PopUp_NFC_No_NFC_Text,
                        Lang.PopUp_NFC_No_NFC_Button
                    );
                    await popUp.ShowPopUp();

                    return;
                }

                if (!DependencyService.Get<IAusweisSdk>().IsConnected())
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Lang.PopUp_SDK_Not_Connected_Title,
                        Lang.PopUp_SDK_Not_Connected_Text,
                        Lang.PopUp_SDK_Not_Connected_Button
                    );
                    await popUp.ShowPopUp();

                    return;
                }

                return;
            }
            DisableAll();

            _clicked = true;
            try
            {
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
            }
            finally
            {
                _clicked = false;
                EnableAll();
            }
        }

        private async void AddDdlTapped(object obj)
        {
            if (!DependencyService.Get<IAusweisSdk>().IsConnected() || _clicked)
            {
                if (!DependencyService.Get<IAusweisSdk>().DeviceHasNfc())
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Lang.PopUp_NFC_No_NFC_Title,
                        Lang.PopUp_NFC_No_NFC_Text,
                        Lang.PopUp_NFC_No_NFC_Button
                    );
                    await popUp.ShowPopUp();

                    return;
                }

                if (!DependencyService.Get<IAusweisSdk>().IsConnected())
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Lang.PopUp_SDK_Not_Connected_Title,
                        Lang.PopUp_SDK_Not_Connected_Text,
                        Lang.PopUp_SDK_Not_Connected_Button
                    );
                    await popUp.ShowPopUp();

                    return;
                }

                return;
            }
            DisableAll();

            _clicked = true;
            try
            {
                if (DependencyService.Get<IAusweisSdk>().NfcEnabled())
                {
                    DependencyService.Get<IAusweisSdk>().StartSdkIos();
                    DependencyService.Get<IAusweisSdk>().EnableNfcDispatcher();
                    await Navigation.PushAsync(new DdlPage());
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

            }
            finally
            {
                _clicked = false;
                EnableAll();
            }
        }

        private async void AddVacCertTapped(object obj)
        {
            if (_clicked)
            {
                return;
            }
            DisableAll();

            _clicked = true;
            try
            {
                AddVacCertPopUpSoon addVacCertPopUp = new AddVacCertPopUpSoon();
                await addVacCertPopUp.ShowPopUp();
            }
            finally
            {
                _clicked = false;
                EnableAll();
            }
        }

        private async void AddDocumentTapped(object obj)
        {
            if (_clicked)
            {
                return;
            }
            DisableAll();

            _clicked = true;
            try
            {
                AddDocumentPopUp addDocumentPopUp = new AddDocumentPopUp(ViewModel);
                await addDocumentPopUp.ShowPopUp();
            }
            finally
            {
                _clicked = false;
                EnableAll();
            }
        }

        private void EmptyStack_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Width")
            {
                ddlImage.WidthRequest = (Application.Current.MainPage.Width - EmptyStack.Spacing - EmptyStack.Margin.HorizontalThickness - EmptyStack.Padding.HorizontalThickness) / 2;
                vacImage.WidthRequest = (Application.Current.MainPage.Width - EmptyStack.Spacing - EmptyStack.Margin.HorizontalThickness - EmptyStack.Padding.HorizontalThickness) / 2;
                addImage.WidthRequest = (Application.Current.MainPage.Width - EmptyStack.Spacing - EmptyStack.Margin.HorizontalThickness - EmptyStack.Padding.HorizontalThickness) / 2;
            }
        }
    }
}