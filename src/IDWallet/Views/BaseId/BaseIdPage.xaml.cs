using Autofac;
using IDWallet.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BaseIdPage : ContentPage
    {
        private readonly BaseIdViewModel _baseIdViewModel = App.Container.Resolve<BaseIdViewModel>();

        public BaseIdPage()
        {
            InitializeComponent();

            _baseIdViewModel.Navigation = Navigation;

            BindingContext = _baseIdViewModel;

            foreach (ContentView view in BaseIdCarousel.ItemsSource)
            {
                view.BindingContext = _baseIdViewModel;
            }

            ResetViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _baseIdViewModel.Subscribe();
            if (_baseIdViewModel.ViewModelWasResetted)
            {
                BaseIdCarousel.Position = 0;
                _baseIdViewModel.ViewModelWasResetted = false;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _baseIdViewModel.Unsubscribe();
        }

        protected override bool OnBackButtonPressed()
        {
            _baseIdViewModel.CancelCurrentProcess();
            return base.OnBackButtonPressed();
        }

        public void ResetViewModel()
        {
            _baseIdViewModel.GoToStart();
        }
    }
}