using Hyperledger.Aries.Features.DidExchange;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace IDWallet.Models
{
    public class HistoryProofElement : HistorySubElement
    {
        public ConnectionRecord ConnectionRecord { get; set; }
        public List<string> CredentialRecordIds { get; set; }
        public ObservableCollection<CredentialClaim> NonRevealedClaims { get; set; }
        public ObservableCollection<CredentialClaim> PredicateClaims { get; set; }
        public ObservableCollection<CredentialClaim> RevealedClaims { get; set; }
        public ObservableCollection<CredentialClaim> SelfAttestedClaims { get; set; }
        public string ProofRecordId { get; set; }
        public string State { get; set; }
    }
}
