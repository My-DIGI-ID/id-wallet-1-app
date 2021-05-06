using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.PresentProof;
using System;
using Xamarin.Forms;

namespace IDWallet.Models
{
    public class NewProofRequestMessage : InboxMessage
    {
        public NewProofRequestMessage(ProofRecord proofRecord, ConnectionRecord connectionRecord)
        {
            RecordId = proofRecord.Id;
            ProofRecord = proofRecord;
            Description = Resources.Lang.NotificationsPage_Proof_Request_Text;
            CreatedAtUtc = proofRecord.CreatedAtUtc;

            ConnectionAlias = connectionRecord?.Alias.Name ?? Resources.Lang.WalletPage_Info_Panel_No_Origin;
            ConnectionRecord = connectionRecord;
            MessageImageSource = string.IsNullOrEmpty(connectionRecord?.Alias.ImageUrl)
                ? ImageSource.FromFile("default_logo.png")
                : ImageSource.FromUri(new Uri(connectionRecord.Alias.ImageUrl));
        }
    }
}