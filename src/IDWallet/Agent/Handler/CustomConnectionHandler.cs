using IDWallet.Events;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.Agent.Handler
{
    public class CustomConnectionHandler : IMessageHandler
    {
        private readonly IConnectionService _connectionService;
        private readonly IMessageService _messageService;

        public CustomConnectionHandler(
            IConnectionService connectionService,
            IMessageService messageService)
        {
            _connectionService = connectionService;
            _messageService = messageService;
        }

        public IEnumerable<MessageType> SupportedMessageTypes => new[]
        {
            new MessageType(MessageTypes.ConnectionRequest),
            new MessageType(MessageTypes.ConnectionResponse),
            new MessageType(MessageTypesHttps.ConnectionRequest),
            new MessageType(MessageTypesHttps.ConnectionResponse)
        };

        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            switch (messageContext.GetMessageType())
            {
                case MessageTypesHttps.ConnectionInvitation:
                case MessageTypes.ConnectionInvitation:
                    {
                        ConnectionInvitationMessage invitation = messageContext.GetMessage<ConnectionInvitationMessage>();
                        await _connectionService.CreateRequestAsync(agentContext, invitation);
                        return null;
                    }

                case MessageTypes.ConnectionRequest:
                case MessageTypesHttps.ConnectionRequest:
                    {
                        ConnectionRequestMessage request = messageContext.GetMessage<ConnectionRequestMessage>();
                        string connectionId =
                            await _connectionService.ProcessRequestAsync(agentContext, request, messageContext.Connection);
                        messageContext.ContextRecord = messageContext.Connection;

                        if (messageContext.Connection.GetTag(TagConstants.AutoAcceptConnection) == "true")
                        {
                            (ConnectionResponseMessage message, ConnectionRecord record) =
                                await _connectionService.CreateResponseAsync(agentContext, connectionId);
                            messageContext.ContextRecord = record;
                            await _messageService.SendAsync(agentContext, message, messageContext.Connection);

                            MessagingCenter.Send(this, WalletEvents.UpdateConnections);
                        }

                        return null;
                    }

                case MessageTypes.ConnectionResponse:
                case MessageTypesHttps.ConnectionResponse:
                    {
                        ConnectionResponseMessage response = messageContext.GetMessage<ConnectionResponseMessage>();
                        string connectionId =
                            await _connectionService.ProcessResponseAsync(agentContext, response,
                                messageContext.Connection);
                        messageContext.ContextRecord = messageContext.Connection;

                        try
                        {
                            if (App.WaitForConnection)
                            {
                                ConnectionRecord connection = await _connectionService.GetAsync(agentContext, connectionId);
                                if (connection.Endpoint.Uri.Equals(App.AwaitableInvitation.ServiceEndpoint)
                                    && connection.Endpoint.Verkey.SequenceEqual(App.AwaitableInvitation.RoutingKeys)
                                    && connection.GetTag(WalletParams.RecipientKeys)
                                        .Equals(App.AwaitableInvitation.RecipientKeys.ToJson()))
                                {
                                    App.AwaitableConnection = connection;

                                    App.WaitForConnection = false;
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            // ignore
                        }

                        MessagingCenter.Send(this, WalletEvents.UpdateConnections);

                        return null;
                    }
                default:
                    throw new AriesFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageContext.GetMessageType()}");
            }
        }
    }
}