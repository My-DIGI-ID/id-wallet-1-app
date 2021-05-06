using IDWallet.Views.Customs.PopUps;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Ledger.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SwitchLedgerPopUp : CustomPopUp
    {
        public SwitchLedgerPopUp(string ledgerName)
        {
            InitializeComponent();
            DescriptionLabel.Text = IDWallet.Resources.Lang.PopUp_Change_Ledger_Text + " " + ledgerName + "?";
        }

        private void Button_Clicked(object sender, EventArgs e)
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
            AcceptButton.IsVisible = CheckBox.IsChecked;
        }
    }
}