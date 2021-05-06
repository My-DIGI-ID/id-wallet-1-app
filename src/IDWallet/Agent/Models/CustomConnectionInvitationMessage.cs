using Hyperledger.Aries.Features.DidExchange;

namespace IDWallet.Agent.Models
{
    public class CustomConnectionInvitationMessage : ConnectionInvitationMessage
    {
        public string Ledger { get; set; }
    }
}