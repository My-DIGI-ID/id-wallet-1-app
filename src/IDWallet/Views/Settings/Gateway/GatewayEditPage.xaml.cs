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
    public partial class GatewayEditPage : CustomPage
    {
        private readonly string _oldAddress;
        private readonly string _oldKey;
        private readonly string _oldName;
        private readonly GatewaysViewModel _viewModel;
        private Command _addressBackCommand;
        private Command _keyBackCommand;
        private Command _nameBackCommand;
        private Command _notificationsClickedCommand;
        public GatewayEditPage(Models.Gateway gateway, GatewaysViewModel gatewaysViewModel)
        {
            _oldName = gateway.Name;
            _oldAddress = gateway.Address;
            _oldKey = gateway.Key;
            _viewModel = gatewaysViewModel;
            NewName = _oldName;
            NewAddress = _oldAddress;
            NewKey = _oldKey;
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingOpen_Icon.svg");

            InitializeComponent();

            CustomViewModel viewModel = new CustomViewModel();
            BindingContext = viewModel;
            viewModel.DisableNotificationAlert();
        }

        public Command AddressBackCommand => _addressBackCommand ??= new Command(OnAddressBackClicked);
        public Command KeyBackCommand => _keyBackCommand ??= new Command(OnKeyBackClicked);
        public Command NameBackCommand => _nameBackCommand ??= new Command(OnNameBackClicked);
        public string NewAddress { get; set; }
        public string NewKey { get; set; }
        public string NewName { get; set; }
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
            await _viewModel.EditGateway(NewName, NewAddress, NewKey, _oldName, _oldAddress, _oldKey);
            await Navigation.PopAsync();
            EnableAll();
        }

        private void GatewayAddressEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NewAddress != _oldAddress)
            {
                AddressBackButton.IsVisible = true;
            }
        }

        private void GatewayKeyEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NewKey != _oldKey)
            {
                KeyBackButton.IsVisible = true;
            }
        }

        private void GatewayNameEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NewName != _oldName)
            {
                NameBackButton.IsVisible = true;
            }
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
        private void OnAddressBackClicked()
        {
            NewAddress = _oldAddress;
            GatewayAddressEntry.Text = _oldAddress;
            AddressBackButton.IsVisible = false;
        }

        private void OnKeyBackClicked()
        {
            NewKey = _oldKey;
            GatewayKeyEntry.Text = _oldKey;
            KeyBackButton.IsVisible = false;
        }

        private void OnNameBackClicked()
        {
            NewName = _oldName;
            GatewayNameEntry.Text = _oldName;
            NameBackButton.IsVisible = false;
        }
    }
}