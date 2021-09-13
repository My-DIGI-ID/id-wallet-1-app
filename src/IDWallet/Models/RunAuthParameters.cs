using IDWallet.Models.AusweisSDK;

namespace IDWallet.Models
{
    public class RunAuthParameters
    {
        public TcTokenModel TcToken;
        public string KeyAttestationCerts;
        public string KeyChallengeSignature;
    }
}
