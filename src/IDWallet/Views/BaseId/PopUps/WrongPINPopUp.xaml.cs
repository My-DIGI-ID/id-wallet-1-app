using IDWallet.Resources;
using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WrongPINPopUp : CustomPopUp
    {
        public WrongPINPopUp(int triesLeft, string bodyText)
        {
            InitializeComponent();

            switch (triesLeft)
            {
                case 1:
                    BoldTextLabel.Text = Lang.PopUp_BaseID_Wrong_PIN_Bold_Text_1;
                    break;
                case 2:
                    BoldTextLabel.Text = Lang.PopUp_BaseID_Wrong_PIN_Bold_Text_2;
                    break;
                case 3:
                    BoldTextLabel.Text = Lang.PopUp_BaseID_Wrong_PIN_Bold_Text_3;
                    break;
                case 4:
                    BoldTextLabel.Text = Lang.PopUp_BaseID_Wrong_PIN_Bold_Text_4;
                    break;
                default:
                    BoldTextLabel.Text = Lang.PopUp_BaseID_Wrong_PIN_Bold_Text_1;
                    break;
            }

            bodySpan1.Text = bodyText1;
        }
    }
}