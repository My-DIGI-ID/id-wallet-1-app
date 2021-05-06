using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Agent.Services;
using IDWallet.Interfaces;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.PresentProof;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace IDWallet.Services
{
    public class TransactionOfferService
    {
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly ICustomAgentProvider _customAgentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly CustomProofService _proofService = App.Container.Resolve<CustomProofService>();
        private readonly ICustomWalletRecordService
            _recordService = App.Container.Resolve<ICustomWalletRecordService>();

        private readonly ICustomSecureStorageService _storageService =
            App.Container.Resolve<ICustomSecureStorageService>();

        private readonly ITransactionService _transactionService = App.Container.Resolve<ITransactionService>();
        public enum State
        {
            NoProof,
            Pending,
            True,
            False
        };

        public async
            Task<(CustomConnectionInvitationMessage connectionInvitationMessage, ConnectionRecord connectionRecord,
                TransactionRecord transactionRecord)> CreateTransaction(ProofRequest proofRequest)
        {
            AgentOptions agentOption = _customAgentProvider.GetActiveAgentOptions();
            Hyperledger.Aries.Agents.IAgentContext agentContext = await _customAgentProvider.GetContextAsync();
            await App.GetTransactionConnectionId(_storageService, _recordService, agentContext, _connectionService,
                agentOption.WalletConfiguration.Id);
            (TransactionRecord transaction, ConnectionRecord connection, ConnectionInvitationMessage message) =
                await _transactionService.CreateOrUpadteTransactionAsync(agentContext, null,
                    App.TransactionConnectionId, null, proofRequest);

            AgentOptions activeAgent = _customAgentProvider.GetActiveAgentOptions();

            string ledger = GetPoolName(activeAgent);

            CustomConnectionInvitationMessage customConnectionInvitation = new CustomConnectionInvitationMessage()
            {
                Id = message.Id,
                ImageUrl = message.ImageUrl,
                Label = message.Label,
                Ledger = ledger,
                RecipientKeys = message.RecipientKeys,
                RoutingKeys = message.RoutingKeys,
                ServiceEndpoint = message.ServiceEndpoint,
                Type = message.Type
            };

            return (customConnectionInvitation, connection, transaction);
        }

        public async Task<(JToken, string)> GetPresentedCredentials(string transactionId)
        {
            Hyperledger.Aries.Agents.IAgentContext agentContext = await _customAgentProvider.GetContextAsync();
            TransactionRecord transaction = await _transactionService.GetAsync(agentContext, transactionId);

            if (transaction.ProofRequest == null)
            {
                return (null, null);
            }

            if (!string.IsNullOrEmpty(transaction.ProofRecordId))
            {
                ProofRecord proofRecord = await _proofService.GetAsync(agentContext, transaction.ProofRecordId);

                if (proofRecord.State == ProofState.Accepted)
                {
                    try
                    {
                        JObject proofJson = JObject.Parse(proofRecord.ProofJson);
                        JToken requestedProof = proofJson["requested_proof"];

                        return (requestedProof, transaction.ProofRecordId);
                    }
                    catch (Exception)
                    {
                        return (null, null);
                    }
                }
            }

            return (null, null);
        }

        public async Task<State> Verify(string transactionId)
        {
            Hyperledger.Aries.Agents.IAgentContext agentContext = await _customAgentProvider.GetContextAsync();
            TransactionRecord transaction = await _transactionService.GetAsync(agentContext, transactionId);

            if (transaction.ProofRequest == null)
            {
                return State.NoProof;
            }

            if (!string.IsNullOrEmpty(transaction.ProofRecordId))
            {
                ProofRecord proofRecord = await _proofService.GetAsync(agentContext, transaction.ProofRecordId);

                if (proofRecord.State == ProofState.Accepted)
                {
                    if (await _proofService.VerifyProofAsync(agentContext, transaction.ProofRecordId))
                    {
                        return State.True;
                    }
                    else
                    {
                        return State.False;
                    }
                }
            }

            return State.Pending;
        }

        private string GetPoolName(AgentOptions options)
        {
            switch (options.PoolName)
            {
                case "idw_live":
                    return "live";
                case "idw_staging":
                    return "staging";
                case "idw_builder":
                    return "builder";
                case "idw_esatus":
                    return "esatus";
                case "idw_bcgov":
                    return "bcgov";
                case "idw_iduniontest":
                    return "iduniontest";
                case "idw_eesdi":
                    return "eesdi";
                case "idw_devledger":
                    return "devledger";
                default:
                    return "live";
            }
        }
    }
}