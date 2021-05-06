using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Proof.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PresentationRequestPopUp : CustomPopUp
    {
        public PresentationRequestPopUp(string popUpText, string connectionRecordAlias = "")
        {
            InitializeComponent();
            PresentationRequestText.Text = popUpText;
            Alias.Text = connectionRecordAlias;
        }
    }
}