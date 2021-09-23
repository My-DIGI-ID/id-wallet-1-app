using Newtonsoft.Json;

namespace IDWallet.Models.AusweisSDK
{
    public class SdkInvitation
    {
        [JsonProperty("invitationUrl")]
        public string InvitationUrl { get; set; }


        [JsonProperty("revocationPassphrase")]
        public string RevocationPassphrase { get; set; }
    }
}
