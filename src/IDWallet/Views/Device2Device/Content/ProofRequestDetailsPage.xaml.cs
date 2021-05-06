using IDWallet.Models;
using IDWallet.ViewModels;
using IDWallet.Views.Proofs.Device2Device.PopUps;
using Hyperledger.Aries.Features.PresentProof;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Proofs.Device2Device.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProofRequestDetailsPage : ContentPage
    {
        public readonly ProofElementsViewModel ViewModel;

        private Command<ProofAttributeInfo> _infoTappedCommand;

        public ProofRequestDetailsPage(SendableRequest sendableRequest)
        {
            BindingContext = ViewModel = new ProofElementsViewModel(sendableRequest);
            InitializeComponent();
        }

        public Command<ProofAttributeInfo> InfoTappedCommand =>
                    _infoTappedCommand ??= new Command<ProofAttributeInfo>(OnInfoTapped);
        protected override void OnAppearing()
        {
            base.OnAppearing();

            ViewModel.LoadItemsCommand.Execute(null);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            ViewModel.Pending = false;
        }

        private void Create_Button_Clicked(object sender, EventArgs e)
        {
            QRButton.IsVisible = false;
            QRGrid.IsVisible = true;

            ViewModel.RevealProof();
        }

        private async void OnInfoTapped(ProofAttributeInfo proofAttributeInfo)
        {
            if (proofAttributeInfo is ProofPredicateInfo)
            {
                RestrictionsPopUp popUp = new RestrictionsPopUp(proofAttributeInfo, true);
                await popUp.ShowPopUp();
            }
            else
            {
                RestrictionsPopUp popUp = new RestrictionsPopUp(proofAttributeInfo);
                await popUp.ShowPopUp();
            }
        }
    }
}