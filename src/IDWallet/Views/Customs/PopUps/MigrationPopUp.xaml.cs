using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Customs.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MigrationPopUp : CustomPopUp
    {
        public MigrationPopUp(string title, string message, string cancelButtonLabel, string acceptButtonLabel)
        {
            InitializeComponent();

            TitleLabel.Text = title;
            TextLabel.Text = message;
            CancelButtonLabel.Text = cancelButtonLabel;
            AcceptButtonLabel.Text = acceptButtonLabel;
        }
    }
}