using IDWallet.Models;
using IDWallet.Views.History.PopUps;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Wallet.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CredentialCarouselElement : ContentView
    {
        private Command<WalletElement> _deleteTappedCommand;
        private Command<WalletElement> _historyTappedCommand;
        private Command<WalletElement> _infoTappedCommand;
        private Command<HistoryProofElement> _historyProofElementTappedCommand;
        private Command<string> _openPdfButtonTappedCommand;

        public CredentialCarouselElement()
        {
            InitializeComponent();
        }

        public Command<WalletElement> DeleteTappedCommand =>
            _deleteTappedCommand ?? (_deleteTappedCommand = new Command<WalletElement>(DeleteTapped));

        public Command<WalletElement> HistoryTappedCommand =>
                                    _historyTappedCommand ??= new Command<WalletElement>(HistoryTapped);

        public Command<HistoryProofElement> HistoryProofElementTappedCommand =>
            _historyProofElementTappedCommand ??= new Command<HistoryProofElement>(HistoryProofElementTapped);

        public Command<WalletElement> InfoTappedCommand =>
            _infoTappedCommand ??= new Command<WalletElement>(InfoTapped);

        public Command<string> OpenPdfButtonTappedCommand =>
            _openPdfButtonTappedCommand ??= new Command<string>(OpenPdfButtonTapped);

        private void DeleteTapped(WalletElement walletElement)
        {
            TabbedPage mainPage = (TabbedPage)Application.Current.MainPage;
            WalletPage credentialsPage = (WalletPage)((NavigationPage)mainPage.Children[0]).RootPage;
            credentialsPage.Delete_Credential(walletElement);
        }

        private async void HistoryTapped(WalletElement walletElement)
        {
            TabbedPage mainPage = (TabbedPage)Application.Current.MainPage;
            ViewModels.WalletViewModel credentialsViewModel =
                ((WalletPage)((NavigationPage)mainPage.Children[0]).RootPage).ViewModel;

            if (!walletElement.IsHistoryOpen)
            {
                if (!walletElement.IsHistorySet)
                {
                    credentialsViewModel.SetHistory(walletElement);
                }

                walletElement.IsHistoryOpen = true;
                if (walletElement.HistoryItems.Count > 0)
                {
                    HistoryEndSeparator.IsVisible = false;
                }
                else
                {
                    HistoryEndSeparator.IsVisible = true;
                }
            }
            else
            {
                walletElement.IsHistoryOpen = false;
                HistoryEndSeparator.IsVisible = true;
            }
        }
        private async void InfoTapped(WalletElement walletElement)
        {
            if (!walletElement.IsInfoOpen)
            {
                walletElement.IsInfoOpen = true;
            }
            else
            {
                walletElement.IsInfoOpen = false;
            }
        }

        private void OpenPdfButtonTapped(string documentString)
        {
            App.ViewFile(documentString);
        }

        private async void HistoryProofElementTapped(HistoryProofElement historyProofElement)
        {
            HistoryProofPopUp details = new HistoryProofPopUp(historyProofElement);
            await details.ShowPopUp();
        }
    }
}