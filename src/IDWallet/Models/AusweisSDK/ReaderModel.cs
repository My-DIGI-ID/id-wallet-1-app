using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class ReaderModel
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("attached")]
        public bool Attached { get; set; }

        [JsonProperty("keypad")]
        public bool Keypad { get; set; }

        [JsonProperty("card")]
        public CardModel Card { get; set; }
    }
}