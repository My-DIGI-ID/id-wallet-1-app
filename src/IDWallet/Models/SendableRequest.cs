using Hyperledger.Aries.Features.PresentProof;
using System.Collections.Generic;

namespace IDWallet.Models
{
    public class SendableRequest
    {
        public List<ProofAttributeInfo> Attributes { get; set; }
        public string Name { get; set; }
        public List<ProofPredicateInfo> Predicates { get; set; }
        public ProofRequest ProofRequest { get; set; }
    }
}