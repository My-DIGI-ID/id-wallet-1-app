using Autofac;
using IDWallet.Interfaces;
using IDWallet.Resources;
using IDWallet.Views.Customs.Pages;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Onboarding
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class OnboardingPage : CustomPage
    {
        private readonly ICustomSecureStorageService _storageService = App.Container.Resolve<ICustomSecureStorageService>();
        private int _progress;
        private bool _arrowIsVisible;

        public int Progress 
        { 
            get => _progress; 
            set => SetProperty(ref _progress, value); 
        }

        public bool ArrowIsVisible
        {
            get => _arrowIsVisible;
            set => SetProperty(ref _arrowIsVisible, value);
        }

        private Command _nextButtonTappedCommand;
        public Command NextButtonTappedCommand => _nextButtonTappedCommand ??= new Command(NextButtonTapped);

        private Command _finishedButtonTappedCommand;
        public Command FinishedButtonTappedCommand => _finishedButtonTappedCommand ??= new Command(FinishedButtonTapped);

        public OnboardingPage()
        {            
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            EnableAll();
            Progress = 0;
            PageImage.Source = ImageSource.FromFile("Onboarding01.png");
            PageImage.Margin = new Thickness(0, 35, 0, 10);
            Title.Text = Lang.OnboardingPage_Title_First;
            Text.Text = Lang.OnboardingPage_Text_First;
            ArrowIsVisible = true;
        }

        private void NextButtonTapped()
        {
            Progress += 1;
            switch (Progress)
            {
                case 1:
                    PageImage.Source = ImageSource.FromFile("Onboarding02_new.png");
                    PageImage.Margin = new Thickness(0, 0, 0, 0);
                    PageImage.HeightRequest = 400;
                    Title.Text = Lang.OnboardingPage_Title_Second;
                    Text.Text = Lang.OnboardingPage_Text_Second;
                    ArrowIsVisible = true;                    
                    break;
                case 2:
                    PageImage.Source = ImageSource.FromFile("Onboarding03.png");
                    PageImage.Margin = new Thickness(0, 50, 0, 0);
                    PageImage.HeightRequest = 300;
                    Title.Text = Lang.OnboardingPage_Title_Third;
                    Text.Text = Lang.OnboardingPage_Text_Third;
                    ArrowIsVisible = false;                   
                    break;
            }
        }

        private async void FinishedButtonTapped()
        {
            App.IntroCompleted = true;
            await _storageService.SetAsync(WalletParams.IntroCompletedTag, true);
            await Navigation.PopModalAsync();
        }
    }
}