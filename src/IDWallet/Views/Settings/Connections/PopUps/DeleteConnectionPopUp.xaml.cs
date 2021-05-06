using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Connections.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DeleteConnectionPopUp : CustomPopUp
    {
        private Command _checkBoxTappedCommand;
        public DeleteConnectionPopUp(string connectionAlias)
        {
            InitializeComponent();
            ConnectionAliasSpan.Text = " " + connectionAlias + " ";
        }

        public Command CheckBoxTappedCommand => _checkBoxTappedCommand ??= new Command(CheckBoxTapped);

        private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            DeleteButton.IsEnabled = checkBox.IsChecked;
        }

        private void CheckBoxTapped(object obj)
        {
            checkBox.IsChecked = !checkBox.IsChecked;
        }
    }
}