using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class AusweisSdkInvitation
    {
        [JsonProperty("invitationUrl")]
        public string InvitationUrl { get; set; }


        [JsonProperty("revocationPassphrase")]
        public string RevocationPassphrase { get; set; }
    }
}
