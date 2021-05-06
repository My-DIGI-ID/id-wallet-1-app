using IDWallet.ViewModels;
using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BaseIdStartView : ContentView
    {
        public BaseIdStartView()
        {
            InitializeComponent();
            privacyClick.CommandParameter = $"https://{WalletParams.AusweisHost}/ssi/privacy.html";
            termsClick.CommandParameter = $"https://{WalletParams.AusweisHost}/ssi/terms.html";
            GoToNextButton.IsEnabled = false;
        }

        private Command<string> _linkTappedCommand;
        public Command LinkTappedCommand =>
            _linkTappedCommand ??= new Command<string>(LinkTapped);

        private async void LinkTapped(string url)
        {
            await Launcher.OpenAsync(url);
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            ((BaseIdViewModel)BindingContext).GoToNext();
            PrivacyCheckbox.IsChecked = false;
            UserAgreementCheckbox.IsChecked = false;
        }

        private void CheckboxChecked(object sender, EventArgs e)
        {
            if (PrivacyCheckbox.IsChecked && UserAgreementCheckbox.IsChecked)
            {
                GoToNextButton.IsEnabled = true;
            }
            else
            {
                GoToNextButton.IsEnabled = false;
            }
        }
    }
}