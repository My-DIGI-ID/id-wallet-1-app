using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Connections.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConnectionsListElement : Grid
    {
        public ImageSource ConnectionStateIcon;

        public ConnectionsListElement()
        {
            InitializeComponent();
        }
    }
}