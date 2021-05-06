using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Connections.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeleteSharedInfoPopUp : CustomPopUp
    {
        public DeleteSharedInfoPopUp()
        {
            InitializeComponent();
        }

        private void Button_Clicked(object sender, System.EventArgs e)
        {
            if (CheckBox.IsChecked)
            {
                CheckBox.IsChecked = false;
            }
            else
            {
                CheckBox.IsChecked = true;
            }
        }

        private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            DeleteButton.IsVisible = CheckBox.IsChecked;
        }
    }
}