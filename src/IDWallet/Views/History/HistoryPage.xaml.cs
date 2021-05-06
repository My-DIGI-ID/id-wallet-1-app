using IDWallet.Models;
using IDWallet.ViewModels;
using IDWallet.Views.History.PopUps;
using IDWallet.Views.Inbox;
using IDWallet.Views.Settings;
using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.History
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HistoryPage : ContentPage
    {
        private readonly HistoryViewModel _viewModel;
        private Command _notificationsClickedCommand;
        private Command _settingsClickedCommand;
        private Command<HistorySubElement> _historySubElementTappedCommand;

        public HistoryPage()
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingNotOpen_Icon.svg");

            InitializeComponent();
            BindingContext = _viewModel = new HistoryViewModel();
        }

        public Command NotificationsClickedCommand =>
            _notificationsClickedCommand ??= new Command(Notifications_Clicked);
        public Command SettingsClickedCommand =>
            _settingsClickedCommand ??= new Command(Settings_Clicked);
        public Command<HistorySubElement> HistorySubElementTappedCommand =>
            _historySubElementTappedCommand ??= new Command<HistorySubElement>(HistorySubElementTapped);
        public ImageSource SettingsIconImage { get; set; }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            EnableAll();

            if (!_viewModel.HistoryElements.Any())
            {
                _viewModel.LoadItemsCommand.Execute(null);
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

        private async void Settings_Clicked()
        {
            DisableAll();
            await Navigation.PushAsync(new SettingsPage());
            EnableAll();
        }

        private async void HistorySubElementTapped(HistorySubElement historySubElement)
        {
            DisableAll();
            if (historySubElement is HistoryProofElement)
            {
                HistoryProofPopUp details = new HistoryProofPopUp(historySubElement as HistoryProofElement);
                await details.ShowPopUp();
            }
            else if (historySubElement is HistoryCredentialElement)
            {
                HistoryCredentialPopUp details = new HistoryCredentialPopUp(historySubElement as HistoryCredentialElement);
                await details.ShowPopUp();
            }
            EnableAll();
        }
    }
}