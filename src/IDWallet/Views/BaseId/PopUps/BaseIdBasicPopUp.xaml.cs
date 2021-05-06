using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BaseIdBasicPopUp : CustomPopUp
    {
        public BaseIdBasicPopUp(string title, string message, string buttontext)
        {
            InitializeComponent();

            TitleLabel.Text = title;
            TextLabel.Text = message;
            ButtonLabel.Text = buttontext;
        }
    }
}