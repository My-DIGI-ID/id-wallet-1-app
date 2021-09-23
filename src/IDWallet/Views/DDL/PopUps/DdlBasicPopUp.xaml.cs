using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.DDL.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DdlBasicPopUp : CustomPopUp
    {
        public DdlBasicPopUp(string title, string message, string buttontext)
        {
            InitializeComponent();
            TitleLabel.Text = title;
            TextLabel.Text = message;
            ButtonLabel.Text = buttontext;
        }
    }
}