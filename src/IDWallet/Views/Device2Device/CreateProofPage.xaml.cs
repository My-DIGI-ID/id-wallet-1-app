using IDWallet.Models;
using IDWallet.ViewModels;
using IDWallet.Views.Customs.Pages;
using IDWallet.Views.Inbox;
using IDWallet.Views.Proofs.Device2Device.Content;
using IDWallet.Views.Settings;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Proofs.Device2Device
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CreateProofPage : CustomPage
    {
        private readonly CreateProofViewModel _viewModel;
        private Command<SendableRequest> _proofRequestTappedCommand;

        public CreateProofPage()
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingNotOpen_Icon.svg", 20, 20);
            BindingContext = _viewModel = new CreateProofViewModel();
            InitializeComponent();
        }

        public Command<SendableRequest> ProofRequestTappedCommand =>
                    _proofRequestTappedCommand ??= new Command<SendableRequest>(OnProofRequestTapped);
        public ImageSource SettingsIconImage { get; set; }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            EnableAll();
            if (!_viewModel.Requests.Any())
            {
                _viewModel.LoadItemsCommand.Execute(null);
            }
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

        private async void OnProofRequestTapped(SendableRequest sendableRequest)
        {
            DisableAll();
            await Navigation.PushAsync(new ProofRequestDetailsPage(sendableRequest));
        }

        private async void Refresh_Clicked(object sender, EventArgs e)
        {
            DisableAll();
            _viewModel.ReloadRequests();
            while (!App.ProofsLoaded)
            {
                await Task.Delay(100);
            }

            EnableAll();
        }
        private async void Settings_Clicked(object sender, EventArgs e)
        {
            DisableAll();
            await Navigation.PushAsync(new SettingsPage());
        }
    }
}