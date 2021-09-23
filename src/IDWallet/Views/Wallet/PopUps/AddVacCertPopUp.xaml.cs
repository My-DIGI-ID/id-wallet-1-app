using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Wallet.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddVacCertPopUp : CustomPopUp
    {
        public AddVacCertPopUp()
        {
            InitializeComponent();
        }

        private async void ScanNowTapped(object sender, System.EventArgs e)
        {
            try
            {
                CustomTabbedPage mainPage = Application.Current.MainPage as CustomTabbedPage;
                mainPage.CurrentPage = mainPage.Children[1];
                OnPopUpCanceled(sender, e);
            }
            catch
            {
                //...
            }
        }
    }
}