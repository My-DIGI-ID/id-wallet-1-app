using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries.Features.DidExchange;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Connections.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NewConnectionPopUp : CustomPopUp
    {
        public NewConnectionPopUp(ConnectionInvitationMessage invitation)
        {
            InitializeComponent();

            ConnectionCompany.Text = invitation.Label ?? IDWallet.Resources.Lang.Connection_Not_Known_Label;

            ConnectionImage.Source = string.IsNullOrEmpty(invitation.ImageUrl)
                ? ImageSource.FromFile("default_logo.png")
                : ImageSource.FromUri(new Uri(invitation.ImageUrl));
        }
    }
}