using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Newtonsoft.Json;

namespace IDWallet.Agent.Models
{
    public class TransactionRecord : RecordBase
    {
        [JsonProperty("connection")]
        public string ConnectionId
        {
            get => Get();
            set => Set(value);
        }

        [JsonProperty("connectionRecord")] public ConnectionRecord ConnectionRecord { get; set; }
        public string CredentialRecordId { get; set; }
        public OfferConfiguration OfferConfiguration { get; set; }
        public string ProofRecordId { get; set; }
        public ProofRequest ProofRequest { get; set; }
        public override string TypeName => "AF.TransactionRecord";
    }
}