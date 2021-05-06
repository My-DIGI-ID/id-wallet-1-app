using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class CardModel
    {
        [JsonProperty("inoperative")]
        public bool Inoperative { get; set; }

        [JsonProperty("deactivated")]
        public bool Deactivated { get; set; }

        [JsonProperty("retryCounter")]
        public int RetryCounter { get; set; }
    }
}