using IDWallet.Agent.Models;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.Routing;
using System.Threading.Tasks;

namespace IDWallet.Agent.Handler
{
    internal class CustomForwardHandler : MessageHandlerBase<ForwardMessage>
    {
        private readonly IConnectionService _connectionService;

        public CustomForwardHandler(IConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        protected override async Task<AgentMessage> ProcessAsync(ForwardMessage message, IAgentContext agentContext,
            UnpackedMessageContext messageContext)
        {
            ConnectionRecord connectionRecord = await _connectionService.ResolveByMyKeyAsync(agentContext, message.To);

            if (agentContext is CustomAgentContext context)
            {
                context.AddNext(new PackedMessageContext(message.Message.ToJson(), connectionRecord));
            }

            return null;
        }
    }
}