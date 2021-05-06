using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Customs.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ClearDataPopUp : CustomPopUp
    {
        public ClearDataPopUp(string title, string message, string cancelButtonLabel, string acceptButtonLabel)
        {
            InitializeComponent();

            TitleLabel.Text = title;
            TextLabel.Text = message;
            CancelButtonLabel.Text = cancelButtonLabel;
            AcceptButtonLabel.Text = acceptButtonLabel;
        }
    }
}