using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class DescriptionModel
    {
        [JsonProperty("issuerName")]
        public string IssuerName { get; set; }

        [JsonProperty("issuerUrl")]
        public string IssuerUrl { get; set; }

        [JsonProperty("subjectName")]
        public string SubjectName { get; set; }

        [JsonProperty("subjectUrl")]
        public string SubjectUrl { get; set; }

        [JsonProperty("termsOfUsage")]
        public string TermsOfUsage { get; set; }

        [JsonProperty("purpose")]
        public string Purpose { get; set; }
    }
}