using IDWallet.Agent.Interface;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Decorators.Signature;
using Hyperledger.Aries.Decorators.Threading;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Models.Events;
using Hyperledger.Aries.Routing;
using Hyperledger.Aries.Storage;
using Hyperledger.Aries.Utils;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PairwiseApi;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace IDWallet.Agent.Services
{
    public class CustomConnectionService : DefaultConnectionService
    {
        private readonly IEdgeClientService _edgeClientService;
        private readonly ICustomWalletRecordService _recordService;
        public CustomConnectionService(
            IEdgeClientService edgeClientService,
            IEventAggregator eventAggregator,
            ICustomWalletRecordService recordService,
            IProvisioningService provisioningService,
            ILogger<DefaultConnectionService> logger
        ) : base(eventAggregator, (IWalletRecordService)recordService, provisioningService, logger)
        {
            _recordService = recordService;
            _edgeClientService = edgeClientService;
        }

        public override async Task<(ConnectionInvitationMessage, ConnectionRecord)> CreateInvitationAsync(
            IAgentContext agentContext, InviteConfiguration config = null)
        {
            (ConnectionInvitationMessage message, ConnectionRecord record) =
                await base.CreateInvitationAsync(agentContext, config);

            await _edgeClientService.AddRouteAsync(agentContext, message.RecipientKeys.First());

            return (message, record);
        }

        public override async Task<(ConnectionRequestMessage, ConnectionRecord)> CreateRequestAsync(
                    IAgentContext agentContext, ConnectionInvitationMessage invitation)
        {
            (ConnectionRequestMessage message, ConnectionRecord record) =
                await base.CreateRequestAsync(agentContext, invitation);

            await _edgeClientService.AddRouteAsync(agentContext, record.MyVk);

            return (message, record);
        }

        public override async Task<(ConnectionResponseMessage, ConnectionRecord)> CreateResponseAsync(
            IAgentContext agentContext, string connectionId)
        {
            (ConnectionResponseMessage message, ConnectionRecord record) =
                await base.CreateResponseAsync(agentContext, connectionId);

            await _edgeClientService.AddRouteAsync(agentContext, record.MyVk);

            return (message, record);
        }
        public override async Task<ConnectionRecord> GetAsync(IAgentContext agentContext, string connectionId)
        {
            Logger.LogInformation(LoggingEvents.GetConnection, "ConnectionId {0}", connectionId);

            ConnectionRecord record =
                await _recordService.GetAsync<ConnectionRecord>(agentContext.Wallet, connectionId);

            if (record == null)
            {
                throw new AriesFrameworkException(ErrorCode.RecordNotFound, "Connection record not found");
            }

            return record;
        }

        public override async Task<string> ProcessResponseAsync(IAgentContext agentContext,
            ConnectionResponseMessage response, ConnectionRecord connection)
        {
            Logger.LogInformation(LoggingEvents.AcceptConnectionResponse, "To {1}", connection.MyDid);

            Connection connectionObj = await SignatureUtils.UnpackAndVerifyAsync<Connection>(response.ConnectionSig);

            await Did.StoreTheirDidAsync(agentContext.Wallet,
                new { did = connectionObj.Did, verkey = connectionObj.DidDoc.Keys[0].PublicKeyBase58 }.ToJson());

            await Pairwise.CreateAsync(agentContext.Wallet, connectionObj.Did, connection.MyDid,
                connectionObj.DidDoc.Services[0].ServiceEndpoint);

            connection.TheirDid = connectionObj.Did;
            connection.TheirVk = connectionObj.DidDoc.Keys[0].PublicKeyBase58;

            connection.SetTag(TagConstants.LastThreadId, response.GetThreadId());

            if (connectionObj.DidDoc.Services[0] is IndyAgentDidDocService service)
            {
                connection.Endpoint = new AgentEndpoint(service.ServiceEndpoint, null,
                    service.RoutingKeys != null && service.RoutingKeys.Count > 0
                        ? service.RoutingKeys.ToArray()
                        : null);
            }

            await connection.TriggerAsync(ConnectionTrigger.Response);
            await RecordService.UpdateAsync(agentContext.Wallet, connection);

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = connection.Id,
                MessageType = response.Type,
                ThreadId = response.GetThreadId()
            });

            return connection.Id;
        }
    }
}