using IDWallet.Models;
using IDWallet.Views.Customs.PopUps;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BaseIdOfferPopUp : CustomPopUp
    {
        private readonly BaseIdOfferMessage _offerMessage;
        public BaseIdOfferPopUp(BaseIdOfferMessage baseIdOfferMessage)
        {
            InitializeComponent();

            BindingContext = _offerMessage = baseIdOfferMessage;
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