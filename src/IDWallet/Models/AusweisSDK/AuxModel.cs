using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class AuxModel
    {
        [JsonProperty("ageVerificationDate")]
        public string AgeVerificationDate { get; set; }

        [JsonProperty("requiredAge")]
        public string RequiredAge { get; set; }

        [JsonProperty("validityDate")]
        public string ValidityDate { get; set; }

        [JsonProperty("communityId")]
        public string CommunityId { get; set; }
    }
}