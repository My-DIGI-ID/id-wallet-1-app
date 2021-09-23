using Autofac;
using IDWallet.ViewModels;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.DDL
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DdlPage : ContentPage
    {
        private readonly DdlViewModel _ddlViewModel = App.Container.Resolve<DdlViewModel>();

        public DdlPage()
        {
            BindingContext = _ddlViewModel;

            InitializeComponent();

            foreach (ContentView view in BaseIdCarousel.ItemsSource)
            {
                view.BindingContext = _ddlViewModel;
            }

            _ddlViewModel.GoToStart();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ddlViewModel.Subscribe();
            if (_ddlViewModel.ViewModelWasResetted)
            {
                BaseIdCarousel.Position = 0;
                _ddlViewModel.ViewModelWasResetted = false;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _ddlViewModel.Unsubscribe();
        }

        protected override bool OnBackButtonPressed()
        {
            _ddlViewModel.CancelCurrentProcess();
            return base.OnBackButtonPressed();
        }
    }
}