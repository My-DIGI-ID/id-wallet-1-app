using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class SdkHttpError
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("date")]
        public string Date { get; set; }
    }
}
