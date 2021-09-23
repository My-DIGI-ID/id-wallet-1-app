using IDWallet.ViewModels;
using IDWallet.Views.Customs.PopUps;
using System.ComponentModel;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.DDL.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ReadyToScanPopUp : CustomPopUp, INotifyPropertyChanged
    {
        public bool IsOpen { get; set; }
        public ReadyToScanPopUp(DdlViewModel viewModel)
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