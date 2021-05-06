using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class VersionInfoModel
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Implementation-Title")]
        public string ImplementationTitle { get; set; }

        [JsonProperty("Implementation-Vendor")]
        public string ImplementationVendor { get; set; }

        [JsonProperty("Implementation-Version")]
        public string ImplementationVersion { get; set; }

        [JsonProperty("Specification-Title")]
        public string SpecificationTitle { get; set; }

        [JsonProperty("Specification-Vendor")]
        public string SpecificationVendor { get; set; }

        [JsonProperty("Specification-Version")]
        public string SpecificationVersion { get; set; }
    }
}