using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class TcTokenModel
    {
        [JsonProperty("tcTokenUrl")]
        public string TcTokenUrl { get; set; }

        [JsonProperty("walletValidationChallenge")]
        public string Challenge { get; set; }
    }
}
