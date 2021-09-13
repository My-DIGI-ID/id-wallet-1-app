using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class TcTokenModel
    {
        [JsonProperty("tcTokenUrl")]
        public string TcTokenUrl { get; set; }

        [JsonProperty("issuerNonce")]
        public string IssuerNonce { get; set; }

        [JsonProperty("walletValidationChallenge")]
        public string Challenge { get; set; }
    }
}
