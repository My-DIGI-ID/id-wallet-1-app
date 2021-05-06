using IDWallet.ViewModels;
using IDWallet.Views.Inbox;
using IDWallet.Views.Settings.License.Content;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.License
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LicensePage : ContentPage
    {
        private Command<string> _licenseTappedCommand;
        private Command _notificationsClickedCommand;
        public LicensePage()
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingOpen_Icon.svg");

            InitializeComponent();

            LicenseViewModel viewModel = new LicenseViewModel();
            BindingContext = viewModel;
            viewModel.DisableNotificationAlert();
        }

        public Command<string> LicenseTappedCommand => _licenseTappedCommand ??= new Command<string>(OnLicenseTapped);
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
            LicenseStack.IsEnabled = false;
        }

        private void EnableAll()
        {
            NotificationsToolBarItem.IsEnabled = true;
            SettingsToolBarItem.IsEnabled = true;
            LicenseStack.IsEnabled = true;
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


        private async void OnLicenseTapped(string sourceString)
        {
            try
            {
                DisableAll();
                await Navigation.PushAsync(new LicenseContentPage(sourceString));
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