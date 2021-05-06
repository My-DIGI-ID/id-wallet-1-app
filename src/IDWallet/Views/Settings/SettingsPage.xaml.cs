using Autofac;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.ViewModels;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Inbox;
using IDWallet.Views.Settings.Backup;
using IDWallet.Views.Settings.ChangePin;
using IDWallet.Views.Settings.Connections;
using IDWallet.Views.Settings.Imprint;
using IDWallet.Views.Settings.Ledger;
using IDWallet.Views.Settings.Liability;
using IDWallet.Views.Settings.License;
using IDWallet.Views.Settings.SwitchLanguage;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : ContentPage
    {
        private readonly ICustomSecureStorageService _storageService =
            App.Container.Resolve<ICustomSecureStorageService>();

        private Command _notificationsClickedCommand;

        public SettingsPage()
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingOpen_Icon.svg");

            InitializeComponent();

            CustomViewModel viewModel = new CustomViewModel();
            BindingContext = viewModel;
            viewModel.DisableNotificationAlert();

            //bool isBiometricActivated = App.GetBiometricsInfo(_storageService);
            //BiometricsToggle.IsToggled = isBiometricActivated;
            ShowMediatorToggle.IsToggled = App.ShowMediatorConnection;

            if (Device.RuntimePlatform == Device.Android)
            {
                string osVersion = DependencyService.Get<INativeHelper>().GetOsVersion();

                if (!string.IsNullOrEmpty(osVersion) && int.Parse(osVersion) < 28)
                {
                    ForceFocusStack.IsVisible = false;
                }
            }

            ForceFocusToggle.IsToggled = App.ForceFocus;
        }

        public Command NotificationsClickedCommand =>
                    _notificationsClickedCommand ??= new Command(Notifications_Clicked);

        public ImageSource SettingsIconImage { get; set; }

        //public async void OnBiometricsToggled(object sender, ToggledEventArgs e)
        //{
        //    bool isBiometricActivated = App.GetBiometricsInfo(_storageService);

        //    if (isBiometricActivated == BiometricsToggle.IsToggled)
        //    {
        //        return;
        //    }

        //    if (BiometricsToggle.IsToggled)
        //    {
        //        bool isBiometricsAvailable = await CrossFingerprint.Current.IsAvailableAsync(true);
        //        if (isBiometricsAvailable == false)
        //        {
        //            BiometricsToggle.IsToggled = false;

        //            BasicPopUp popUp = new BasicPopUp(
        //                IDWallet.Resources.Lang.PopUp_Biometrics_Not_Available_Title,
        //                IDWallet.Resources.Lang.PopUp_Biometrics_Not_Available_Text,
        //                IDWallet.Resources.Lang.PopUp_Biometrics_Not_Available_Button);
        //            PopUpResult _ = await popUp.ShowPopUp();
        //            return;
        //        }

        //        AuthenticationRequestConfiguration authReqConf =
        //            new AuthenticationRequestConfiguration(IDWallet.Resources.Lang.LoginPage_Biometrics_Title,
        //                IDWallet.Resources.Lang.LoginPage_Biometrics_Label);
        //        FingerprintAuthenticationResult auth = await CrossFingerprint.Current.AuthenticateAsync(authReqConf);

        //        if (auth.Status == FingerprintAuthenticationResultStatus.Canceled)
        //        {
        //            BiometricsToggle.IsToggled = false;
        //        }
        //        else if (!auth.Authenticated)
        //        {
        //            BiometricsToggle.IsToggled = false;

        //            BasicPopUp popUp = Device.RuntimePlatform == Device.Android
        //                ? new BasicPopUp(
        //                    IDWallet.Resources.Lang.PopUp_Biometrics_Too_Many_Attempts_Title,
        //                    IDWallet.Resources.Lang.PopUp_Biometrics_Too_Many_Attempts_Android_Text,
        //                    IDWallet.Resources.Lang.PopUp_Biometrics_Too_Many_Attempts_Button)
        //                : new BasicPopUp(
        //                    IDWallet.Resources.Lang.PopUp_Biometrics_Too_Many_Attempts_Title,
        //                    IDWallet.Resources.Lang.PopUp_Biometrics_Too_Many_Attempts_iOS_Text,
        //                    IDWallet.Resources.Lang.PopUp_Biometrics_Too_Many_Attempts_Button);
        //            await popUp.ShowPopUp();
        //        }
        //        else
        //        {
        //            await _storageService.SetAsync(WalletParams.KeyBiometricActivated, true);
        //        }
        //    }
        //    else
        //    {
        //        await _storageService.SetAsync(WalletParams.KeyBiometricActivated, false);
        //    }
        //}

        public async void OnForceFocusToggled(object sender, ToggledEventArgs e)
        {
            App.ForceFocus = ForceFocusToggle.IsToggled;
            await _storageService.SetAsync(WalletParams.KeyForceFocusActivated, ForceFocusToggle.IsToggled);
        }

        public async void OnShowMediatorToggled(object sender, ToggledEventArgs e)
        {
            if (App.ShowMediatorConnection == ShowMediatorToggle.IsToggled)
            {
                return;
            }

            if (ShowMediatorToggle.IsToggled)
            {
                ShowMediatorToggle.IsToggled = true;
                App.ShowMediatorConnection = true;
                await _storageService.SetAsync(WalletParams.ShowMediatorConnection, true);
            }
            else
            {
                ShowMediatorToggle.IsToggled = false;
                App.ShowMediatorConnection = false;
                await _storageService.SetAsync(WalletParams.ShowMediatorConnection, false);
            }

            MessagingCenter.Send(this, WalletEvents.ToggleShowMediator);
        }

        //public void Tapped_BiometricsToggle(object sender, EventArgs eventArgs)
        //{
        //    BiometricsToggle.IsToggled = !BiometricsToggle.IsToggled;
        //}

        public async void Tapped_ChangeLanguage(object sender, EventArgs eventArgs)
        {
            DisableAll();
            await Navigation.PushAsync(new SwitchLanguagePage());
        }

        public async void Tapped_ChangeLedger(object sender, EventArgs eventArgs)
        {
            DisableAll();
            await Navigation.PushAsync(new SwitchLedgerPage());
        }

        public async void Tapped_ChangePasscode(object sender, EventArgs eventArgs)
        {
            DisableAll();
            await Navigation.PushAsync(new ChangePinPage());
        }

        public async void Tapped_Connections(object send, EventArgs eventArgs)
        {
            DisableAll();
            await Navigation.PushAsync(new ConnectionsPage());
        }

        public async void Tapped_CreateBackup(object sender, EventArgs eventArgs)
        {
            DisableAll();
            await Navigation.PushAsync(new NewBackupPage());
        }

        public void Tapped_ForceFocusToggle(object sender, EventArgs eventArgs)
        {
            ForceFocusToggle.IsToggled = !ForceFocusToggle.IsToggled;
        }

        public async void Tapped_LegalNotice(object sender, EventArgs eventArgs)
        {
            DisableAll();
            await Navigation.PushAsync(new ImprintPage());
        }

        public async void Tapped_Licenses(object sender, EventArgs eventArgs)
        {
            DisableAll();
            await Navigation.PushAsync(new LicensePage());
        }

        public async void Tapped_PrivacyPolicy(object sender, EventArgs eventArgs)
        {
            DisableAll();
            await Launcher.OpenAsync(new Uri("https://digital-enabling.com/datenschutzerklaerung"));
            EnableAll();
        }

        public async void Tapped_RecoverAccount(object sender, EventArgs eventArgs)
        {
            DisableAll();
            await Navigation.PushAsync(new BackupRecoveryPage());
        }

        public async void Tapped_RenewPush(object sender, EventArgs eventArgs)
        {
            DisableAll();

            MigrationPopUp migrationPopUp = new MigrationPopUp(
                IDWallet.Resources.Lang.PopUp_RenewPush_Title,
                IDWallet.Resources.Lang.PopUp_RenewPush_Message,
                IDWallet.Resources.Lang.PopUp_RenewPush_Cancel,
                IDWallet.Resources.Lang.PopUp_RenewPush_Accept);
            PopUpResult popUpResult = await migrationPopUp.ShowPopUp();

            if (PopUpResult.Accepted == popUpResult)
            {
                if (Device.RuntimePlatform == Device.Android)
                {
                    DependencyService.Get<IAndroidPns>().Renew();

                    while (!App.FinishedNewPnsHandle)
                    {
                        await Task.Delay(100);
                    }

                    BasicPopUp popUp = new BasicPopUp(
                        IDWallet.Resources.Lang.PopUp_Finished_Title,
                        IDWallet.Resources.Lang.PopUp_Finished_Message,
                        IDWallet.Resources.Lang.PopUp_Finished_Button);
                    await popUp.ShowPopUp();
                }
                else if (Device.RuntimePlatform == Device.iOS)
                {
                    App.SetNewPnsHandle(App.NativeStorageService, true);

                    BasicPopUp popUp = new BasicPopUp(
                        IDWallet.Resources.Lang.PopUp_Finished_Title,
                        IDWallet.Resources.Lang.PopUp_Finished_Close_Message,
                        IDWallet.Resources.Lang.PopUp_Finished_Button);
                    await popUp.ShowPopUp();
                }
            }

            EnableAll();
        }

        public void Tapped_ShowMediatorToggle(object sender, EventArgs eventArgs)
        {
            ShowMediatorToggle.IsToggled = !ShowMediatorToggle.IsToggled;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            EnableAll();
        }
        protected override bool OnBackButtonPressed()
        {
            DisableAll();
            return base.OnBackButtonPressed();
        }

        private void DisableAll()
        {
            NotificationsToolBarItem.IsEnabled = false;
            SettingsToolBarItem.IsEnabled = false;
            //CreateBackupStack.IsEnabled = false;
            //RecoverBackupStack.IsEnabled = false;
            ChangePasscodeStack.IsEnabled = false;
            ChangeLedgerStack.IsEnabled = false;
            //BiometricsStack.IsEnabled = false;
            ShowMediatorStack.IsEnabled = false;
            ForceFocusStack.IsEnabled = false;
            ChangeLanguageStack.IsEnabled = false;
            LegalNoticeStack.IsEnabled = false;
            PrivacyPolicyStack.IsEnabled = false;
            LicensesStack.IsEnabled = false;
            ConnectionsStack.IsEnabled = false;
            RenewPushStack.IsEnabled = false;
        }

        private void EnableAll()
        {
            NotificationsToolBarItem.IsEnabled = true;
            SettingsToolBarItem.IsEnabled = true;
            //CreateBackupStack.IsEnabled = true;
            //RecoverBackupStack.IsEnabled = true;
            ChangePasscodeStack.IsEnabled = true;
            ChangeLedgerStack.IsEnabled = true;
            //BiometricsStack.IsEnabled = true;
            ShowMediatorStack.IsEnabled = true;
            ForceFocusStack.IsEnabled = true;
            ChangeLanguageStack.IsEnabled = true;
            LegalNoticeStack.IsEnabled = true;
            PrivacyPolicyStack.IsEnabled = true;
            LicensesStack.IsEnabled = true;
            ConnectionsStack.IsEnabled = true;
            RenewPushStack.IsEnabled = true;
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
		
		private async void Tapped_Liability(object sender, EventArgs e)
        {
            DisableAll();
            await Navigation.PushAsync(new LiabilityPage());
        }
    }
}