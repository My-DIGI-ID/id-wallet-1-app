using Autofac;
using IDWallet.Interfaces;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Intro
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class IntroPage : ContentPage
    {
        private readonly ICustomSecureStorageService _storageService =
            App.Container.Resolve<ICustomSecureStorageService>();

        private Command _finishButtonTappedCommand;
        public IntroPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        public Command FinishButtonTappedCommand => _finishButtonTappedCommand ??= new Command(FinishButtonTapped);
        private async void FinishButtonTapped(object obj)
        {
            App.IntroCompleted = true;
            await _storageService.SetAsync(WalletParams.IntroCompletedTag, true);
            await Navigation.PopModalAsync();
        }
    }
}