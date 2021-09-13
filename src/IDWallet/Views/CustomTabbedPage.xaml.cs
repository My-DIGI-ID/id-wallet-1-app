using Autofac;
using IDWallet.Agent.Services;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Services;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.History;
using IDWallet.Views.Inbox;
using IDWallet.Views.QRScanner;
using IDWallet.Views.QRScanner.Content;
using IDWallet.Views.Wallet;
using Hyperledger.Aries.Contracts;
using System;
using System.ComponentModel;
using Xamarin.Forms;
using PlatformConfiguration = Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;
using TabbedPage = Xamarin.Forms.TabbedPage;

namespace IDWallet.Views
{
    [DesignTimeVisible(false)]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomTabbedPage : TabbedPage, IAppContext
    {
        private readonly IEventAggregator _eventAggregator = App.Container.Resolve<IEventAggregator>();

        private Page _lastKnownPage;

        public CustomTabbedPage()
        {
            InitializeComponent();
            On<PlatformConfiguration.Android>().SetToolbarPlacement(ToolbarPlacement.Bottom);
            On<PlatformConfiguration.Android>().DisableSwipePaging();

            Children.Add(new NavigationPage(new WalletPage())
            {
                Title = IDWallet.Resources.Lang.Tabbar_Wallet,
                IconImageSource = SvgImageSource.FromSvgResource("imagesources.WalletNotOpen_Icon.svg")
            });
            Children.Add(new ScannerLoadPage(new ScanButtonPage())
            {
                Title = IDWallet.Resources.Lang.Tabbar_Scan,
                IconImageSource = SvgImageSource.FromSvgResource("imagesources.ScanNotOpen_Icon.svg")
            });
            Children.Add(new NavigationPage(new HistoryPage())
            {
                Title = IDWallet.Resources.Lang.Tabbar_History,
                IconImageSource = SvgImageSource.FromSvgResource("imagesources.HistoryNotOpen_Icon.svg")
            });

            Services.AppContext.Register(this);

            Subscribe();
        }

        private void Subscribe()
        {
            MessagingCenter.Subscribe<ConnectService>(this, WalletEvents.NetworkError, OnNetworkError);
            MessagingCenter.Subscribe<TransactionService>(this, WalletEvents.NetworkError, OnNetworkError);
        }

        public void AddMessage(string message)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                InboxEvent inboxEvent = new InboxEvent
                {
                    Message = IDWallet.Resources.Lang.Native_Notification_Message,
                    Title = IDWallet.Resources.Lang.Native_Notification_Title
                };
                _eventAggregator.Publish(inboxEvent);

                if (App.IsLoggedIn)
                {
                    if (!App.IsInForeground)
                    {
                        try
                        {
                            await CurrentPage.Navigation.PopToRootAsync();
                        }
                        catch (Exception)
                        {
                            //ignore
                        }

                        try
                        {
                            await CurrentPage.Navigation.PopModalAsync();
                        }
                        catch (Exception)
                        {
                            //ignore
                        }

                        InboxPage inboxPage = new InboxPage();
                        await CurrentPage.Navigation.PushAsync(inboxPage);
                    }
                }
                else
                {

                }
            });
        }

        public void Restore()
        {
        }

        public void Save()
        {
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        protected override void OnCurrentPageChanged()
        {
            base.OnCurrentPageChanged();

            if (CurrentPage.IsEnabled)
            {
                _lastKnownPage = CurrentPage;
            }
            else
            {
                CurrentPage = _lastKnownPage;
            }

            if (CurrentPage is ScannerLoadPage scannerLoadPage && !App.ScanActive && CurrentPage.IsEnabled)
            {
                scannerLoadPage.OnSelection();
            }
            else if (CurrentPage.Navigation.NavigationStack.Count > 1 && !App.ScanActive && CurrentPage.IsEnabled)
            {
                CurrentPage.Navigation.PopToRootAsync();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        private async void OnNetworkError(object obj)
        {
            BasicPopUp popUp = new BasicPopUp(
                IDWallet.Resources.Lang.PopUp_Network_Error_Title,
                IDWallet.Resources.Lang.PopUp_Network_Error_Text,
                IDWallet.Resources.Lang.PopUp_Network_Error_Button);
            await popUp.ShowPopUp();
        }
    }
}