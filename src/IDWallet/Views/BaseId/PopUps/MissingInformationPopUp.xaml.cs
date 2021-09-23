using IDWallet.Views.Customs.PopUps;
using System;
using Xamarin.Essentials;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MissingInformationPopUp : CustomPopUp
    {
        public MissingInformationPopUp()
        {
            InitializeComponent();
        }

        private async void Burgeramt_Tapped(object sender, EventArgs e)
        {
            try
            {
                await Launcher.OpenAsync(new Uri(WalletParams.BaseIDBehoerdenFinder));
            }
            catch
            {

            }
        }

        private async void EMail_Tapped(object sender, EventArgs e)
        {
            try
            {
                await Launcher.OpenAsync(new Uri(WalletParams.BaseIDSupportMail));
            }
            catch
            {

            }
        }
    }
}