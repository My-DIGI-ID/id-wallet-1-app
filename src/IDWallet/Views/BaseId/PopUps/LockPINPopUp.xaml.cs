using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LockPINPopUp : CustomPopUp
    {
        public LockPINPopUp(string lockPIN)
        {
            InitializeComponent();
            PINLabel.Text = lockPIN;
        }
    }
}