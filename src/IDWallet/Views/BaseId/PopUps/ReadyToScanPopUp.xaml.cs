using IDWallet.ViewModels;
using IDWallet.Views.Customs.PopUps;
using System.ComponentModel;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ReadyToScanPopUp : CustomPopUp, INotifyPropertyChanged
    {
        public bool IsOpen { get; set; }

        public ReadyToScanPopUp(BaseIdViewModel viewModel)
        {
            InitializeComponent();
            IsOpen = false;
            BindingContext = viewModel;
        }

        public void CancelScan()
        {
            IsOpen = false;
            OnPopUpCanceled(this, new System.EventArgs());
        }
    }
}