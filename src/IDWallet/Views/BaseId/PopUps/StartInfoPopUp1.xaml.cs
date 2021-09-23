using IDWallet.Views.Customs.PopUps;
using System;
using Xamarin.Essentials;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StartInfoPopUp1 : CustomPopUp
    {
        public StartInfoPopUp1()
        {
            InitializeComponent();
        }

        private async void AusweisApp2_Tapped(object sender, EventArgs e)
        {
            try
            {
                await Launcher.OpenAsync(new Uri(WalletParams.BaseIDAusweisApp2Link));
            }
            catch
            {

            }
        }

        private async void BehoerdenFinder_Tapped(object sender, EventArgs e)
        {
            try
            {
                await Launcher.OpenAsync(new Uri(WalletParams.BaseIDBehoerdenFinder));
            }
            catch
            {

            }
        }
    }
}