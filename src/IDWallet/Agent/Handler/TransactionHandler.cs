using IDWallet.Agent.Interface;
using IDWallet.Agent.Messages;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IDWallet.Agent.Handler
{
    public class TransactionHandler : IMessageHandler
    {
        private readonly IMessageService _messageService;
        private readonly ITransactionService _transactionService;
        public TransactionHandler(
            IMessageService messageService,
            ITransactionService transactionService)
        {
            _messageService = messageService;
            _transactionService = transactionService;
        }

        public IEnumerable<MessageType> SupportedMessageTypes => new[]
        {
            new MessageType(CustomMessageTypes.TransactionResponse),
            new MessageType(CustomMessageTypes.TransactionResponseHttps)
        };

        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            switch (messageContext.GetMessageType())
            {
                case CustomMessageTypes.TransactionResponse:
                case CustomMessageTypes.TransactionResponseHttps:
                    {
                        TransactionResponseMessage transaction = messageContext.GetMessage<TransactionResponseMessage>();
                        await _transactionService.ProcessTransactionAsync(agentContext, transaction,
                            messageContext.Connection);
                        messageContext.ContextRecord = messageContext.Connection;
                        return null;
                    }

                default:
                    throw new AriesFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageContext.GetMessageType()}");
            }
        }
    }
}