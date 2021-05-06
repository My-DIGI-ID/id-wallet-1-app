using IDWallet.ViewModels;
using IDWallet.Views.Inbox;
using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Imprint
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ImprintPage : ContentPage
    {
        private Command _linkClickedCommand;
        private Command _notificationsClickedCommand;
        public ImprintPage()
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingOpen_Icon.svg");

            InitializeComponent();

            CustomViewModel viewModel = new CustomViewModel();
            BindingContext = viewModel;
            viewModel.DisableNotificationAlert();

            AppVersionLabel.Text = WalletParams.AppVersion;
            BuildVersionLabel.Text = WalletParams.BuildVersion;
        }

        public Command LinkClickedCommand => _linkClickedCommand ??= new Command(OnLinkClicked);
        public Command NotificationsClickedCommand =>
            _notificationsClickedCommand ??= new Command(Notifications_Clicked);

        public ImageSource SettingsIconImage { get; set; }

        protected override bool OnBackButtonPressed()
        {
            DisableAll();
            return base.OnBackButtonPressed();
        }

        private void DisableAll()
        {
            NotificationsToolBarItem.IsEnabled = false;
            SettingsToolBarItem.IsEnabled = false;
            LinkLabel.IsEnabled = false;
        }

        private void EnableAll()
        {
            NotificationsToolBarItem.IsEnabled = true;
            SettingsToolBarItem.IsEnabled = true;
            LinkLabel.IsEnabled = true;
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

        private async void OnLinkClicked(object obj)
        {
            try
            {
                DisableAll();
                await Launcher.OpenAsync(new Uri("https://digital-enabling.com"));
            }
            catch (Exception)
            {
                //ignore
            }
            finally
            {
                EnableAll();
            }
        }
    }
}