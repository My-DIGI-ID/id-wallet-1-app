using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class ChatModel
    {
        [JsonProperty("effective")]
        public string[] Effective { get; set; }

        [JsonProperty("optional")]
        public string[] Optional { get; set; }

        [JsonProperty("required")]
        public string[] Required { get; set; }
    }
}