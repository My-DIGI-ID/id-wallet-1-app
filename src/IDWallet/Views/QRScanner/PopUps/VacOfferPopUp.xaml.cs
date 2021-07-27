using IDWallet.Models;
using IDWallet.Views.Customs.PopUps;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.QRScanner.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class VacOfferPopUp : CustomPopUp
    {
        private VacOfferMessage _offerMessage;
        public VacOfferPopUp(VacOfferMessage vacOfferMessage)
        {
            InitializeComponent();

            BindingContext = _offerMessage = vacOfferMessage;
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