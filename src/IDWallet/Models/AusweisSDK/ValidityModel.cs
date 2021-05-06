using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class ValidityModel
    {
        [JsonProperty("effectiveDate")]
        public string EffectiveDate { get; set; }

        [JsonProperty("expirationDate")]
        public string ExpirationDate { get; set; }
    }
}