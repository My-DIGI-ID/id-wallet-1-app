using IDWallet.Agent.Events;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Messages;
using IDWallet.Agent.Models;
using IDWallet.Events;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using ConnectionState = Hyperledger.Aries.Features.DidExchange.ConnectionState;

namespace IDWallet.Agent.Services
{
    public class TransactionService : ITransactionService
    {
        protected readonly IConnectionService ConnectionService;
        protected readonly ICredentialService CredentialService;
        protected readonly ILogger<TransactionService> Logger;
        protected readonly IMessageService MessageService;
        protected readonly CustomProofService ProofService;
        protected readonly IProvisioningService ProvisioningService;
        protected readonly CustomWalletRecordService RecordService;
        public TransactionService(
            CustomWalletRecordService recordService,
            IProvisioningService provisioningService,
            IMessageService messageService,
            IConnectionService connectionService,
            ICredentialService credentialService,
            CustomProofService proofService,
            ILogger<TransactionService> logger)
        {
            ProvisioningService = provisioningService;
            MessageService = messageService;
            ConnectionService = connectionService;
            CredentialService = credentialService;
            ProofService = proofService;
            Logger = logger;
            RecordService = recordService;
        }

        public virtual async Task<ConnectionRecord> CheckForExistingConnection(IAgentContext agentContext,
            ConnectionInvitationMessage connectionInvitationMessage, bool awaitableConnection = false)
        {
            List<ConnectionRecord> transactionConnections =
                await ConnectionService.ListAsync(agentContext, null, 2147483647);

            if (!awaitableConnection)
            {
                IEnumerable<ConnectionRecord> transactionConnectionsEndpoints = transactionConnections.Where(x =>
                    x.Endpoint != null && x.Endpoint.Uri == connectionInvitationMessage.ServiceEndpoint &&
                    x.State == ConnectionState.Connected && x.Endpoint.Verkey != null);
                transactionConnections.Where(x =>
                    x.Endpoint.Uri == connectionInvitationMessage.ServiceEndpoint &&
                    x.State == ConnectionState.Connected && x.Endpoint.Verkey != null);

                return transactionConnectionsEndpoints
                    .Where(x => x.Endpoint.Verkey.SequenceEqual(connectionInvitationMessage.RoutingKeys))
                    .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
            }
            else
            {
                IEnumerable<ConnectionRecord> transactionConnectionsEndpoints = transactionConnections.Where(x =>
                    x.Endpoint != null && x.Endpoint.Uri == connectionInvitationMessage.ServiceEndpoint &&
                    x.State == ConnectionState.Connected && x.Endpoint.Verkey != null &&
                    x.GetTag(WalletParams.RecipientKeys) != null);
                transactionConnections.Where(x =>
                    x.Endpoint.Uri == connectionInvitationMessage.ServiceEndpoint &&
                    x.State == ConnectionState.Connected && x.Endpoint.Verkey != null);

                return transactionConnectionsEndpoints
                    .Where(x => x.Endpoint.Verkey.SequenceEqual(connectionInvitationMessage.RoutingKeys) &&
                                x.GetTag(WalletParams.RecipientKeys)
                                    .Equals(connectionInvitationMessage.RecipientKeys.ToJson()))
                    .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
            }
        }

        public virtual async
            Task<(TransactionRecord transaction, ConnectionRecord connection, ConnectionInvitationMessage message)>
            CreateOrUpadteTransactionAsync(
                IAgentContext agentContext,
                string transactionId = null,
                string connectionId = null,
                OfferConfiguration offerConfiguration = null,
                ProofRequest proofRequest = null,
                InviteConfiguration connectionConfig = null)
        {
            Logger.LogInformation(CustomAgentEvents.CreateTransaction, "For {1}", transactionId);

            (ConnectionInvitationMessage message, ConnectionRecord record) connection;
            TransactionRecord transactionRecord;

            if (string.IsNullOrEmpty(connectionId))
            {
                connectionConfig = connectionConfig ?? new InviteConfiguration()
                {
                    AutoAcceptConnection = true,
                    MultiPartyInvitation = true
                };

                connection = (await ConnectionService.CreateInvitationAsync(agentContext, connectionConfig));
                connection.record.SetTag("InvitationMessage", JObject.FromObject(connection.message).ToString());
                await RecordService.UpdateAsync(agentContext.Wallet, connection.record);
            }
            else
            {
                if ((await ConnectionService.GetAsync(agentContext, connectionId)) != null)
                {
                    connection.record =
                        await RecordService.GetAsync<ConnectionRecord>(agentContext.Wallet, connectionId);
                    string message = connection.record.GetTag("InvitationMessage");
                    connection.message = JObject.Parse(message).ToObject<ConnectionInvitationMessage>();
                }
                else
                {
                    throw new AriesFrameworkException(ErrorCode.RecordNotFound,
                        $"Connection '{connectionId}' not found.");
                }
            }

            transactionId = !string.IsNullOrEmpty(transactionId)
                ? transactionId
                : Guid.NewGuid().ToString();

            if ((await RecordService.GetAsync<TransactionRecord>(agentContext.Wallet, transactionId)) == null)
            {
                transactionRecord = new TransactionRecord
                {
                    Id = transactionId,
                    ConnectionId = connection.record.Id,
                    OfferConfiguration = offerConfiguration,
                    ProofRequest = proofRequest
                };

                await RecordService.AddAsync(agentContext.Wallet, transactionRecord);
            }
            else
            {
                transactionRecord = await RecordService.GetAsync<TransactionRecord>(agentContext.Wallet, transactionId);

                transactionRecord.ConnectionId = connection.record.Id;
                transactionRecord.OfferConfiguration = offerConfiguration;
                transactionRecord.ProofRequest = proofRequest;

                await RecordService.UpdateAsync(agentContext.Wallet, transactionRecord);
            }

            return (transactionRecord, connection.record, connection.message);
        }

        public virtual async Task DeleteAsync(IAgentContext agentContext, string transactionId)
        {
            await RecordService.DeleteAsync<TransactionRecord>(agentContext.Wallet, transactionId);
        }

        public virtual async Task<TransactionRecord> GetAsync(IAgentContext agentContext, string transactionId)
        {
            TransactionRecord record =
                await RecordService.GetAsync<TransactionRecord>(agentContext.Wallet, transactionId);

            if (record == null)
            {
                throw new AriesFrameworkException(ErrorCode.RecordNotFound, "Transaction record not found");
            }

            return record;
        }

        public virtual Task<List<TransactionRecord>> ListAsync(IAgentContext agentContext, ISearchQuery query = null,
            int count = 100, int skip = 0)
        {
            return RecordService.SearchAsync<TransactionRecord>(agentContext.Wallet, query, null, count, skip);
        }
        public virtual async Task ProcessTransactionAsync(IAgentContext agentContext,
            TransactionResponseMessage connectionTransactionMessage, ConnectionRecord connection)
        {
            Logger.LogInformation(CustomAgentEvents.ProcessTransaction, "To {1}", connection.TheirDid);

            TransactionRecord transactionRecord =
                await RecordService.GetAsync<TransactionRecord>(agentContext.Wallet,
                    connectionTransactionMessage.Transaction, true);

            transactionRecord.ConnectionRecord = connection;

            if (transactionRecord.ProofRequest != null)
            {
                (RequestPresentationMessage message, ProofRecord record) =
                    await ProofService.CreateRequestAsync(agentContext, transactionRecord.ProofRequest);

                transactionRecord.ProofRecordId = record.Id;
                string deleteId = Guid.NewGuid().ToString();
                message.AddDecorator(deleteId, "delete_id");
                record.SetTag("delete_id", deleteId);
                record.ConnectionId = connection.Id;

                await RecordService.UpdateAsync(agentContext.Wallet, record);

                await MessageService.SendAsync(agentContext, message, connection);
            }

            if (transactionRecord.OfferConfiguration != null)
            {
                (CredentialOfferMessage credentialOfferMessage, CredentialRecord credentialRecord) =
                    await CredentialService.CreateOfferAsync(agentContext, transactionRecord.OfferConfiguration,
                        connection.Id);

                transactionRecord.CredentialRecordId = credentialRecord.Id;

                await MessageService.SendAsync(agentContext, credentialOfferMessage, connection);
            }

            await RecordService.UpdateAsync(agentContext.Wallet, transactionRecord);
        }

        public (string sessionId, CustomConnectionInvitationMessage connectionInvitationMessage, bool
            awaitableConnection, bool awaitableProof) ReadTransactionUrl(string invitationUrl)
        {
            string sessionId = null;
            CustomConnectionInvitationMessage invitationMessage = null;
            bool awaitableConnection = false;
            bool awaitableProof = false;

            Uri uri = null;
            try
            {
                uri = new Uri(invitationUrl);
            }
            catch (Exception)
            {
                return (null, null, false, false);
            }

            try
            {
                if (uri.Query.StartsWith("?t_o="))
                {
                    Dictionary<string, string> arguments = uri.Query
                        .Substring(1)
                        .Split('&')
                        .Select(q => q.Split('='))
                        .ToDictionary(q => q.FirstOrDefault(), q => q.Skip(1).FirstOrDefault());

                    sessionId = arguments["t_o"];

                    invitationMessage = arguments["c_i"].FromBase64().ToObject<CustomConnectionInvitationMessage>();

                    try
                    {
                        awaitableConnection = bool.Parse(arguments["waitconnection"]);
                    }
                    catch (Exception)
                    {
                        awaitableConnection = false;
                    }

                    try
                    {
                        awaitableProof = bool.Parse(arguments["waitproof"]);
                    }
                    catch (Exception)
                    {
                        awaitableProof = false;
                    }

                    return (sessionId, invitationMessage, awaitableConnection, awaitableProof);
                }
            }
            catch (Exception)
            {
                //ignore
            }

            return (sessionId, invitationMessage, awaitableConnection, awaitableProof);
        }

        public virtual async Task SendTransactionResponse(IAgentContext agentContext, string transactionId,
                            ConnectionRecord connection)
        {
            TransactionResponseMessage message = new TransactionResponseMessage()
            {
                Transaction = transactionId
            };
            try
            {
                await MessageService.SendAsync(agentContext, message, connection);
            }
            catch (HttpRequestException ex) when (ex.Message == "No such host is known")
            {
                MessagingCenter.Send(this, WalletEvents.NetworkError);
            }
            catch (Exception)
            {
                //Ignore
            }
        }
    }
}