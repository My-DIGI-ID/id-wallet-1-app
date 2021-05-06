using Autofac;
using IDWallet.Models;
using IDWallet.ViewModels;
using IDWallet.Views.Customs.Pages;
using IDWallet.Views.Inbox;
using IDWallet.Views.Settings.Connections.Content;
using Hyperledger.Aries.Features.DidExchange;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Svg;

namespace IDWallet.Views.Settings.Connections
{
    [DesignTimeVisible(false)]
    public partial class ConnectionsPage : CustomPage
    {
        public readonly ConnectionsViewModel ViewModel;
        private readonly ConnectionsViewModel _viewModel;
        private bool _initialLoadingDone;
        private Command _notificationsClickedCommand;

        public ConnectionsPage()
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingOpen_Icon.svg");

            InitializeComponent();

            _viewModel = ViewModel = App.Container.Resolve<ConnectionsViewModel>();

            BindingContext = _viewModel;

            _viewModel.DisableNotificationAlert();
        }

        public Command NotificationsClickedCommand =>
                    _notificationsClickedCommand ??= new Command(Notifications_Clicked);

        public ImageSource SettingsIconImage { get; set; }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            EnableAll();

            if (_initialLoadingDone == false)
            {
                _viewModel.ReloadConnections();
                _initialLoadingDone = true;
            }

            App.ScanActive = false;
        }

        private new void DisableAll()
        {
            NotificationsToolBarItem.IsEnabled = false;
            SettingsToolBarItem.IsEnabled = false;
            base.DisableAll();
        }

        private new void EnableAll()
        {
            NotificationsToolBarItem.IsEnabled = true;
            SettingsToolBarItem.IsEnabled = true;
            base.EnableAll();
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
        private async void Notifications_Clicked(object sender, EventArgs e)
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
        }

        private async void OnItemTapped(object sender, ItemTappedEventArgs args)
        {
            DisableAll();
            ConnectionRecord connectionRecord = ((ConnectionsPageItem)args.Item).ConnectionRecord;
            if (!await _viewModel.IsMediatorConnection(connectionRecord))
            {
                App.BlockedRecordTypes.Add(connectionRecord.GetType().ToString());

                await Navigation.PushAsync(new ConnectionEditPage(connectionRecord, _viewModel));
            }
            else
            {
                EnableAll();
            }
        }
        private async void Settings_Clicked(object sender, EventArgs e)
        {
            DisableAll();
            await Navigation.PushAsync(new SettingsPage());
        }
    }
}