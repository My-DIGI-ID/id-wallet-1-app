using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EnterCANPopUp : CustomPopUp
    {
        public EnterCANPopUp(string bodyText)
        {
            InitializeComponent();

            bodySpan.Text = bodyText;
        }
    }
}