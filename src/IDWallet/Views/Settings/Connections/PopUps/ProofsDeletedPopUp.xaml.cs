using IDWallet.Views.Customs.PopUps;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Connections.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProofsDeletedPopUp : CustomPopUp
    {
        public ProofsDeletedPopUp(string connectionAlias, int deletionsCount)
        {
            InitializeComponent();

            ConnectionNameSpan.Text = connectionAlias;
            DeletedCountSpan.Text = deletionsCount.ToString();
        }
    }
}