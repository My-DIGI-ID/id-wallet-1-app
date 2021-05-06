using IDWallet.Views.Customs.PopUps;

namespace IDWallet.Views.Wallet.PopUps
{
    public partial class NewCredentialOfferPopUp : CustomPopUp
    {
        public NewCredentialOfferPopUp(string connectionAlias)
        {
            InitializeComponent();

            Alias.Text = " " + connectionAlias + " ";
        }
    }
}