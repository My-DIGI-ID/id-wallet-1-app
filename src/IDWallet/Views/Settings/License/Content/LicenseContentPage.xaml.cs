using IDWallet.ViewModels;
using IDWallet.Views.Inbox;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.License.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LicenseContentPage : ContentPage
    {
        private Command _notificationsClickedCommand;

        public LicenseContentPage(string sourceString)
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingOpen_Icon.svg");

            InitializeComponent();

            CustomViewModel viewModel = new CustomViewModel();
            BindingContext = viewModel;
            viewModel.DisableNotificationAlert();

            label.Text = sourceString;
        }

        public Command NotificationsClickedCommand =>
                    _notificationsClickedCommand ??= new Command(Notifications_Clicked);

        public ImageSource SettingsIconImage { get; set; }
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
    }
}