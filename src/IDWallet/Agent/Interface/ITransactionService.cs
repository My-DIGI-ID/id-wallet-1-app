using IDWallet.Agent.Messages;
using IDWallet.Agent.Models;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IDWallet.Agent.Interface
{
    public interface ITransactionService
    {
        Task<(TransactionRecord transaction, ConnectionRecord connection, ConnectionInvitationMessage message)>
            CreateOrUpadteTransactionAsync(IAgentContext agentContext, string transactionId = null,
                string connectionId = null, OfferConfiguration credentialOfferJson = null,
                ProofRequest proofRequest = null, InviteConfiguration connectionConfig = null);

        Task DeleteAsync(IAgentContext agentContext, string transactionId);

        Task<TransactionRecord> GetAsync(IAgentContext agentContext, string transactionId);

        Task<List<TransactionRecord>> ListAsync(IAgentContext agentContext, ISearchQuery query = null, int count = 100,
            int skip = 0);

        Task ProcessTransactionAsync(IAgentContext agentContext,
            TransactionResponseMessage connectionTransactionMessage, ConnectionRecord connection);

        (string sessionId, CustomConnectionInvitationMessage connectionInvitationMessage, bool awaitableConnection, bool
            awaitableProof) ReadTransactionUrl(string invitationUrl);

        Task SendTransactionResponse(IAgentContext agentContext, string transactionId, ConnectionRecord connection);
    }
}