using IDWallet.Utils.Converter;
using Hyperledger.Aries.Features.IssueCredential;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Customs.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CredentialAddedPopUp : CustomPopUp
    {
        public CredentialAddedPopUp(CredentialRecord credentialRecord)
        {
            InitializeComponent();

            TitleLabel.Text = IDWallet.Resources.Lang.PopUp_Credential_Stored_Response_Title;
            string credentialName = credentialRecord.CredentialDefinitionId.Split(':')[4];
            CredDefNameConverter credDefNameConverter = new CredDefNameConverter();
            TextLabel.Text = credDefNameConverter.Convert(credentialName, null, null, null) + " " + IDWallet.Resources.Lang.PopUp_Credential_Stored_Response_Text;
        }
    }
}