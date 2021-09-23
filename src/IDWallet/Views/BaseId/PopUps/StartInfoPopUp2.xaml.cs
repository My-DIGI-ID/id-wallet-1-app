using IDWallet.Views.Customs.PopUps;
using System;
using Xamarin.Essentials;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StartInfoPopUp2 : CustomPopUp
    {
        public StartInfoPopUp2()
        {
            InitializeComponent();
        }

        private async void PINInfo_Tapped(object sender, EventArgs e)
        {
            try
            {
                await Launcher.OpenAsync(new Uri(WalletParams.BaseIDPINInfo));
            }
            catch
            {

            }
        }
    }
}