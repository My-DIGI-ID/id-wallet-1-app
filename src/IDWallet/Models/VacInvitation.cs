using Newtonsoft.Json;

namespace IDWallet.Models
{
    public class VacInvitation
    {
        [JsonProperty("url")]
        public string InvitationUrl { get; set; }
    }
}
