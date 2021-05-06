using IDWallet.Models;
using IDWallet.Views.Customs.PopUps;
using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Wallet.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CredentialInfoPopUp : CustomPopUp
    {
        private readonly ConnectionDetailsCredential _viewModel;
        private Command _openPdfButtonClickedCommand;

        public CredentialInfoPopUp(ConnectionDetailsCredential connectionDetailsCredential)
        {
            InitializeComponent();

            BindingContext = _viewModel = connectionDetailsCredential;

            CredentialInfoListView.HeightRequest =
                (_viewModel.Attributes == null
                 || !_viewModel.Attributes.Any())
                    ? 40
                    : Math.Min(200, _viewModel.Attributes.Count * CredentialInfoListView.RowHeight);
            if (_viewModel.Attributes?.Count > 5)
            {
                CredentialInfoListView.VerticalScrollBarVisibility = ScrollBarVisibility.Always;
            }
            else
            {
                CredentialInfoListView.VerticalScrollBarVisibility = ScrollBarVisibility.Never;
            }

            if (_viewModel.EmbeddedByteArray != null)
            {
                EmbeddedImageFrame.HeightRequest = 100;
            }
        }

        public Command OpenPdfButtonClickedCommand =>
                    _openPdfButtonClickedCommand ??= new Command(OpenPdfButtonClicked);
        private void OpenPdfButtonClicked()
        {
            App.ViewFile(_viewModel.DocumentString);
        }
    }
}