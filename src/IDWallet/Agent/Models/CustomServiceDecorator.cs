using Hyperledger.Aries.Decorators.Service;
using Newtonsoft.Json;

namespace IDWallet.Agent.Models
{
    public class CustomServiceDecorator : ServiceDecorator
    {
        //
        // Zusammenfassung:
        //     Service endpoint name
        [JsonProperty("endpointName")]
        public string EndpointName { get; set; }
    }
}
