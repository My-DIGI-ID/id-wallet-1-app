using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class LockBaseIdPopUp : CustomPopUp
    {
        private readonly string _lockPIN;
        public LockBaseIdPopUp(string lockPIN)
        {
            _lockPIN = lockPIN;
            InitializeComponent();
        }

        private void Entry_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EntryField.Text.Length < 1)
            {
                DeleteButton.IsEnabled = false;
            }
            else
            {
                DeleteButton.IsEnabled = true;
            }
        }

        private void Delete_Button_Clicked(object sender, System.EventArgs e)
        {
            if (EntryField.Text == _lockPIN)
            {
                OnPopUpAccepted(sender, e);
            }
            else
            {
                EntryField.Text = "";
            }
        }
    }
}