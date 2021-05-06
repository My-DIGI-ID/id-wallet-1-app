using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Customs.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BasicPopUp : CustomPopUp
    {
        public BasicPopUp(string title, string message, string buttontext)
        {
            InitializeComponent();

            TitleLabel.Text = title;
            TextLabel.Text = message;
            ButtonLabel.Text = buttontext;
        }
    }
}