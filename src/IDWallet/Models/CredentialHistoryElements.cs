using Hyperledger.Aries.Features.DidExchange;
using System.Collections.Generic;

namespace IDWallet.Models
{
    public class CredentialHistoryElements
    {
        public ConnectionRecord ConnectionRecord { get; set; }
        public List<string> CredentialRecordIds { get; set; }
        public List<CredentialClaim> NonRevealedClaims { get; set; }
        public List<CredentialClaim> PredicateClaims { get; set; }
        public string ProofRecordId { get; set; }
        public List<CredentialClaim> RevealedClaims { get; set; }
        public List<CredentialClaim> SelfAttestedClaims { get; set; }
    }
}