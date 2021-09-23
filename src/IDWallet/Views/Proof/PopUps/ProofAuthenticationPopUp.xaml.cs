using IDWallet.ViewModels;
using IDWallet.Views.Customs.PopUps;
using System;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Proof.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProofAuthenticationPopUp : CustomPopUp
    {
        public ProofAuthenticationPopUp(AuthViewModel authViewModel)
        {
            InitializeComponent();
            BindingContext = authViewModel;
        }

        public void OnAuthCanceled(object sender, EventArgs e)
        {
            OnPopUpCanceled(sender, e);
        }
    }
}