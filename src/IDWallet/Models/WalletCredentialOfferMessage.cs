using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Forms;

namespace IDWallet.Models
{
    public class WalletCredentialOfferMessage : InboxMessage
    {
        private readonly string _documentString;
        private ImageSource _embeddedImage;
        private Command _openPdfButtonClickedCommand;

        public WalletCredentialOfferMessage(CredentialRecord credentialRecord, ConnectionRecord connectionRecord)
        {
            RecordId = credentialRecord.Id;
            CredentialRecord = credentialRecord;
            Description = Resources.Lang.NotificationsPage_Credential_Offer_Text;
            CreatedAtUtc = credentialRecord.CreatedAtUtc;
            CredentialTitle = credentialRecord.CredentialDefinitionId?.Split(':')[4] ?? "PlaceholderTitle";
            IsDocumentVisible = false;
            _documentString = "";

            ConnectionAlias = connectionRecord?.Alias.Name ?? Resources.Lang.WalletPage_Info_Panel_No_Origin;
            ConnectionRecord = connectionRecord;
            MessageImageSource = string.IsNullOrEmpty(connectionRecord?.Alias.ImageUrl)
                ? ImageSource.FromFile("default_logo.png")
                : ImageSource.FromUri(new Uri(connectionRecord.Alias.ImageUrl));

            CredentialPreviewAttributes = new List<CredentialPreviewAttribute>();
            List<CredentialPreviewAttribute> attributes =
                credentialRecord.CredentialAttributesValues?.ToList().OrderBy(x => x.Name).ToList() ??
                new List<CredentialPreviewAttribute>();
            foreach (CredentialPreviewAttribute attribute in attributes)
            {
                if (attribute.Name == "embeddedImage")
                {
                    try
                    {
                        string imageString = attribute.Value.ToString();
                        EmbeddedByteArray = Convert.FromBase64String(imageString);
                        Stream embeddedImageStream = new MemoryStream(EmbeddedByteArray);
                        EmbeddedImage = ImageSource.FromStream(() => embeddedImageStream);
                    }
                    catch (Exception)
                    {
                        EmbeddedByteArray = null;
                        EmbeddedImage = null;
                    }
                }
                else if (attribute.Name == "embeddedDocument")
                {
                    IsDocumentVisible = true;
                    _documentString = attribute.Value.ToString();
                }
                else
                {
                    CredentialPreviewAttributes.Add(attribute);
                }
            }
        }

        public List<CredentialPreviewAttribute> CredentialPreviewAttributes { get; }

        public CredentialRecord CredentialRecord { get; }

        public string CredentialTitle { get; }

        public byte[] EmbeddedByteArray { get; }

        public ImageSource EmbeddedImage
        {
            get => _embeddedImage;
            set => SetProperty(ref _embeddedImage, value);
        }

        public bool IsDocumentVisible { get; private set; }

        public Command OpenPdfButtonClickedCommand =>
                                                                    _openPdfButtonClickedCommand ??= new Command(OpenPdfButtonClicked);
        private void OpenPdfButtonClicked()
        {
            App.ViewFile(_documentString);
        }
    }
}