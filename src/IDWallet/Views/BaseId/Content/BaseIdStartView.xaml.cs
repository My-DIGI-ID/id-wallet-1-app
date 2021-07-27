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
            bsiClick.CommandParameter = WalletParams.BsiUrl;
            privacyConsentClick.CommandParameter = $"https://{WalletParams.AusweisHost}/ssi/privacy.html#privacy-consent";
            privacyClick.CommandParameter = $"https://{WalletParams.AusweisHost}/ssi/privacy.html#privacy-info";
            termsClick.CommandParameter = $"https://{WalletParams.AusweisHost}/ssi/terms.html";
            GoToNextButton.IsEnabled = false;
        }

        private Command<string> _linkTappedCommand;
        public Command LinkTappedCommand =>
            _linkTappedCommand ??= new Command<string>(LinkTapped);

        private async void LinkTapped(string url)
        {
            await Launcher.OpenAsync(new Uri(url));
        }

        private Command _linkClickedCommand;
        public Command LinkClickedCommand => _linkClickedCommand ??= new Command(OnLinkClicked);
        private async void OnLinkClicked(object obj)
        {
            await Launcher.OpenAsync(new Uri("https://www.bsi.bund.de/DE/Themen/Oeffentliche-Verwaltung/Elektronische-Identitaeten/Online-Ausweisfunktion/Testinfrastruktur/eID-Karte/eID-Karte_node.html"));
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