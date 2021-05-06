using IDWallet.Agent.Handler;
using IDWallet.Agent.Handlers;
using Hyperledger.Aries.Agents;
using System;

namespace IDWallet.Agent
{
    public class CustomWalletAgent : AgentBase
    {
        public CustomWalletAgent(IServiceProvider provider) : base(provider)
        {
        }

        protected override void ConfigureHandlers()
        {
            AddHandler<CustomConnectionHandler>();
            AddHandler<CustomProofHandler>();
            AddHandler<TransactionHandler>();
            AddHandler<PrivacyHandler>();
            AddHandler<CustomCredentialHandler>();
            AddHandler<CustomForwardHandler>();
            AddBasicMessageHandler();
            AddTrustPingHandler();
        }
    }
}