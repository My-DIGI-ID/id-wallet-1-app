using IDWallet.Utils.Converter;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using System;
using Xamarin.Forms;

namespace IDWallet.Models
{
    public class AutoAcceptMessage : InboxMessage
    {
        public AutoAcceptMessage(ConnectionRecord connectionRecord, ProofRecord proofRecord)
        {
            RecordId = proofRecord.Id;
            Description = Resources.Lang.NotificationsPage_Proof_Auto_Accept_Text;
            CreatedAtUtc = proofRecord.CreatedAtUtc;
            ProofRequest request = proofRecord.RequestJson.ToObject<ProofRequest>();
            Title = request.Name;
            CredentialId = null;
            ConnectionAlias = connectionRecord?.Alias.Name ?? Resources.Lang.WalletPage_Info_Panel_No_Origin;
            ConnectionRecord = connectionRecord;
            MessageImageSource = string.IsNullOrEmpty(connectionRecord?.Alias.ImageUrl)
                ? ImageSource.FromFile("default_logo.png")
                : ImageSource.FromUri(new Uri(connectionRecord.Alias.ImageUrl));
        }

        public AutoAcceptMessage(ConnectionRecord connectionRecord, CredentialRecord credentialRecord)
        {
            RecordId = credentialRecord.Id;
            Description = Resources.Lang.NotificationsPage_Credential_Auto_Accept_Text;
            CreatedAtUtc = credentialRecord.CreatedAtUtc;
            CredDefNameConverter credDefNameConverter = new CredDefNameConverter();
            Title = credDefNameConverter.Convert(credentialRecord.CredentialDefinitionId?.Split(':')[4] ?? "UnknownTitle", null, null, null).ToString();
            CredentialId = credentialRecord.Id;
        }

        public string CredentialId { get; }
        public string Title { get; }
    }
}