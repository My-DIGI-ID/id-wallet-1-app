using IDWallet.ViewModels;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Inbox;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Gateway
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class GatewaysPage : ContentPage
    {
        private readonly GatewaysViewModel _viewModel;

        private Command<Models.Gateway> _deleteClickedCommand;
        private Command<Models.Gateway> _editClickedCommand;

        private Command _notificationsClickedCommand;

        public GatewaysPage()
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingOpen_Icon.svg");

            InitializeComponent();

            BindingContext = _viewModel = new GatewaysViewModel();
            _viewModel.DisableNotificationAlert();
        }

        public Command<Models.Gateway> DeleteClickedCommand =>
            _deleteClickedCommand ??= new Command<Models.Gateway>(OnDeleteClicked);

        public Command<Models.Gateway> EditClickedCommand =>
                                    _editClickedCommand ??= new Command<Models.Gateway>(OnEditClicked);
        public Command NotificationsClickedCommand =>
            _notificationsClickedCommand ??= new Command(Notifications_Clicked);

        public ImageSource SettingsIconImage { get; set; }
        private void Add_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new AddGatewayPage(_viewModel));
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
        private async void OnDeleteClicked(Models.Gateway obj)
        {
            GatewayDeletionPopUp popUp = new GatewayDeletionPopUp();
            PopUpResult popUpResult = await popUp.ShowPopUp();

            if (popUpResult == PopUpResult.Accepted)
            {
                await _viewModel.DeleteGateway(obj);
            }
        }

        private async void OnEditClicked(Models.Gateway obj)
        {
            await Navigation.PushAsync(new GatewayEditPage(obj, _viewModel));
        }
    }
}