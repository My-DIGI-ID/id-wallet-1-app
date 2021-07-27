using Hyperledger.Aries.Decorators;
using Hyperledger.Aries.Features.PresentProof;
using Newtonsoft.Json;

namespace IDWallet.Agent.Models
{
    public class CustomRequestPresentationMessage : RequestPresentationMessage
    {
        [JsonProperty("~service")]
        public CustomServiceDecorator Service { get; set; }

        [JsonProperty("~order")]
        public int Order { get; set; }
    }
}
