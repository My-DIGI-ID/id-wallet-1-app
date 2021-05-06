using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class ResultModel
    {
        [JsonProperty("major")]
        public string Major { get; set; }

        [JsonProperty("minor")]
        public string Minor { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}