using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ReadyToScanPopUp : CustomPopUp
    {
        public bool IsOpen { get; set; }
        public ReadyToScanPopUp()
        {
            InitializeComponent();
            IsOpen = false;
        }

        public void CancelScan()
        {
            IsOpen = false;
            OnPopUpCanceled(this, new System.EventArgs());
        }
    }
}