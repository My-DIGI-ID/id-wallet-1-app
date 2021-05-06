using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries.Features.DidExchange;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Connections.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConnectionAddedPopUp : CustomPopUp
    {
        public ConnectionAddedPopUp(ConnectionRecord connection)
        {
            InitializeComponent();
            ConnectionCompany.Text = connection.Alias.Name ?? IDWallet.Resources.Lang.Connection_Not_Known_Label;

            ConnectionImage.Source = string.IsNullOrEmpty(connection.Alias.ImageUrl)
                ? ImageSource.FromFile("default_logo.png")
                : ImageSource.FromUri(new Uri(connection.Alias.ImageUrl));
        }
    }
}