using IDWallet.Events;
using IDWallet.ViewModels;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Inbox
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InboxPage : ContentPage
    {
        public InboxPage()
        {
            InitializeComponent();

            BindingContext = ViewModel = new InboxViewModel();

            Subscribe();
        }

        private void Subscribe()
        {
            MessagingCenter.Subscribe<InboxViewModel>(this, WalletEvents.NoMoreNotifications, OnNoNotifications);
        }

        public InboxViewModel ViewModel { get; private set; }
        public void DisableAll()
        {
            InBoxStack.IsEnabled = false;
        }

        public void EnableAll()
        {
            InBoxStack.IsEnabled = true;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            EnableAll();

            if (!ViewModel.InboxMessages.Any())
            {
                ViewModel.LoadItemsCommand.Execute(null);
            }
        }

        protected override bool OnBackButtonPressed()
        {
            DisableAll();
            return base.OnBackButtonPressed();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            MessagingCenter.Send(this, WalletEvents.DisableNotifications);
        }

        private void NotificationsListView_ItemTapped(object sender, ItemTappedEventArgs e)
        {
        }

        private async void OnNoNotifications(InboxViewModel obj)
        {
            try
            {
                await Navigation.PopAsync();
            }
            catch (System.Exception)
            {
                //ignore
            }
        }
    }
}