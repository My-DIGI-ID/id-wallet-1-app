using Hyperledger.Aries.Features.PresentProof;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace IDWallet.Agent.Models
{
    public class CustomRequestedCredentials : RequestedCredentials
    {
        [JsonProperty("requested_attributes")]
        public new Dictionary<string, RequestedAttribute> RequestedAttributes { get; set; } =
            new Dictionary<string, RequestedAttribute>();

        [JsonProperty("requested_predicates")]
        public new Dictionary<string, RequestedAttribute> RequestedPredicates { get; set; }
            = new Dictionary<string, RequestedAttribute>();

        [JsonProperty("self_attested_attributes")]
        public new Dictionary<string, string> SelfAttestedAttributes { get; set; }
            = new Dictionary<string, string>();
        public override string ToString()
        {
            return $"{GetType().Name}: " +
                   $"RequestedAttributes={string.Join(",", RequestedAttributes ?? new Dictionary<string, RequestedAttribute>())}, " +
                   $"SelfAttestedAttributes={string.Join(",", SelfAttestedAttributes ?? new Dictionary<string, string>())}, " +
                   $"RequestedPredicates={string.Join(",", RequestedPredicates ?? new Dictionary<string, RequestedAttribute>())}, " +
                   $"CredentialIdentifiers={string.Join(",", GetCredentialIdentifiers())}";
        }

        internal IEnumerable<string> GetCredentialIdentifiers()
        {
            List<string> credIds = new List<string>();
            credIds.AddRange(RequestedAttributes.Values.Select(x => x.CredentialId));
            credIds.AddRange(RequestedPredicates.Values.Select(x => x.CredentialId));
            return credIds.Distinct();
        }
    }
}