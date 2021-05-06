using IDWallet.ViewModels;
using IDWallet.Views.Customs.Pages;
using IDWallet.Views.Inbox;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Gateway
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddGatewayPage : CustomPage
    {
        private readonly GatewaysViewModel _viewModel;
        private Command _notificationsClickedCommand;
        public AddGatewayPage(GatewaysViewModel gatewaysViewModel)
        {
            _viewModel = gatewaysViewModel;
            Name = "";
            Address = "";
            Key = "";
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingOpen_Icon.svg");

            InitializeComponent();

            CustomViewModel viewModel = new CustomViewModel();
            BindingContext = viewModel;
            viewModel.DisableNotificationAlert();
        }

        public string Address { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public Command NotificationsClickedCommand =>
            _notificationsClickedCommand ??= new Command(Notifications_Clicked);

        public ImageSource SettingsIconImage { get; set; }
        public async void Cancel_Button_Clicked(object sender, EventArgs e)
        {
            DisableAll();
            await Navigation.PopAsync();
            EnableAll();
        }

        public async void Save_Button_Clicked(object sender, EventArgs e)
        {
            DisableAll();
            bool ready = !(CheckEntryFields());
            if (ready)
            {
                Name = GatewayNameEntry.Text;
                Address = GatewayAddressEntry.Text;
                Key = GatewayKeyEntry.Text;
                await _viewModel.AddGateway(Name, Address, Key);

                await Navigation.PopAsync();
            }

            EnableAll();
        }

        private bool CheckEntryFields()
        {
            bool isNameEmpty = string.IsNullOrEmpty(Name);
            bool isAddressEmpty = string.IsNullOrEmpty(Address);
            bool isKeyEmpty = string.IsNullOrEmpty(Key);

            return (isNameEmpty || isAddressEmpty || isKeyEmpty);
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
    }
}