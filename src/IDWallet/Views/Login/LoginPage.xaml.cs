using IDWallet.Resources;
using IDWallet.ViewModels;
using IDWallet.Views.Customs.PopUps;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Login
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LoginPage : ContentPage
    {
        private readonly LoginViewModel _loginViewModel = new LoginViewModel();
        private bool _sizeChanged { get; set; } = false;

        public LoginPage()
        {
            InitializeComponent();
            BindingContext = _loginViewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            NetworkAccess connectivity = Connectivity.NetworkAccess;

            if (connectivity != NetworkAccess.ConstrainedInternet && connectivity != NetworkAccess.Internet)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Lang.PopUp_No_Network_Title,
                    Lang.PopUp_No_Network_Text,
                    Lang.PopUp_No_Network_Button);
                alertPopUp.PreLoginPopUp = true;
                await alertPopUp.ShowPopUp();
            }

            if (!App.BiometricLoginActive && !App.IsLoggedIn)
            {
                _loginViewModel.BiometricsLogin();
            }
        }
    }
}