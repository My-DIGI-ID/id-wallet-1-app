using IDWallet.Models;
using IDWallet.Views.Customs.PopUps;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.DDL.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DdlOfferPopUp : CustomPopUp
    {
        private readonly DdlOfferMessage _offerMessage;
        public DdlOfferPopUp(DdlOfferMessage ddlOfferMessage)
        {
            InitializeComponent();

            BindingContext = _offerMessage = ddlOfferMessage;
        }

        private Command _infoButtonTappedCommand;
        public Command InfoButtonTappedCommand =>
            _infoButtonTappedCommand ??= new Command(InfoButtonTapped);

        private void InfoButtonTapped()
        {
            _offerMessage.InfoStackIsVisible = !_offerMessage.InfoStackIsVisible;
        }
    }
}