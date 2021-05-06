using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class SdkMessage
    {
        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("aux")]
        public AuxModel Aux { get; set; }

        [JsonProperty("chat")]
        public ChatModel Chat { get; set; }

        [JsonProperty("transactionInfo")]
        public string TransactionInfo { get; set; }

        [JsonProperty("canAllowed")]
        public bool CanAllowed { get; set; }

        [JsonProperty("available")]
        public int[] Available { get; set; }

        [JsonProperty("current")]
        public int Current { get; set; }

        [JsonProperty("result")]
        public ResultModel Result { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("description")]
        public DescriptionModel Description { get; set; }

        [JsonProperty("validity")]
        public ValidityModel Validity { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("VersionInfo")]
        public VersionInfoModel VersionInfo { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("attached")]
        public bool Attached { get; set; }

        [JsonProperty("keypad")]
        public bool Keypad { get; set; }

        [JsonProperty("card")]
        public CardModel Card { get; set; }

        [JsonProperty("reader")]
        public ReaderModel Reader { get; set; }
    }
}