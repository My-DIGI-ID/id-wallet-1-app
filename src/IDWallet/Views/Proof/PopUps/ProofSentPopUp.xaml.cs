using IDWallet.Views.Customs.PopUps;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Proof.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProofSentPopUp : CustomPopUp
    {
        private Command _checkBoxTappedCommand;
        public ProofSentPopUp()
        {
            InitializeComponent();
        }

        public Command CheckBoxTappedCommand => _checkBoxTappedCommand ??= new Command(CheckBoxTapped);

        private void Button_Clicked(object sender, EventArgs e)
        {
            if (checkBox.IsChecked)
            {
                OnPopUpDeleted(sender, e);
            }
            else
            {
                OnPopUpAccepted(sender, e);
            }
        }

        private void CheckBoxTapped(object obj)
        {
            checkBox.IsChecked = !checkBox.IsChecked;
        }
    }
}