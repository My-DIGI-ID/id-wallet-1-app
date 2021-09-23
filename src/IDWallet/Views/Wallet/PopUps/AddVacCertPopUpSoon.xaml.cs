using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Wallet.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddVacCertPopUpSoon : CustomPopUp
    {
        public AddVacCertPopUpSoon()
        {
            InitializeComponent();
        }

        private void Button_Clicked(object sender, System.EventArgs e)
        {
            OnPopUpCanceled(sender, e);
        }
    }
}