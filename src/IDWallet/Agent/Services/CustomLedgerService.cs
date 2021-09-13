using IDWallet.Agent.Interface;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Ledger;
using Hyperledger.Aries.Payments;
using Hyperledger.Aries.Utils;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PaymentsApi;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace IDWallet.Agent.Services
{
    class CustomLedgerService : ILedgerService
    {
        private readonly ILedgerSigningService _signingService;
        private readonly ICustomAgentProvider _customAgentProvider;

        public object IndyLedger { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomLedgerService" /> class
        /// </summary>
        /// <param name="signingService"></param>
        /// /// <param name="customAgentProvider"></param>
        public CustomLedgerService(ILedgerSigningService signingService, ICustomAgentProvider customAgentProvider)
        {
            _signingService = signingService;
            _customAgentProvider = customAgentProvider;
        }

        /// <inheritdoc />
        public virtual async Task<ParseResponseResult> LookupDefinitionAsync(IAgentContext agentContext, string definitionId)
        {
            List<AgentOptions> allAgents = GetAllAgents();

            foreach (AgentOptions agent in allAgents)
            {
                await _customAgentProvider.SwitchLedger(agent);
                agentContext = await _customAgentProvider.GetContextAutoSwitchAsync();
                if (agentContext != null)
                {
                    try
                    {
                        string req = await Ledger.BuildGetCredDefRequestAsync(null, definitionId);
                        string res = await Ledger.SubmitRequestAsync(await agentContext.Pool, req);

                        if (EnsureSuccessResponse(res))
                        {
                            return await Ledger.ParseGetCredDefResponseAsync(res);
                        }
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }

            throw new AriesFrameworkException(ErrorCode.LedgerItemNotFound, "Ledger item not found");
        }

        /// <inheritdoc />
        public virtual async Task<string> LookupAttributeAsync(IAgentContext agentContext, string targetDid, string attributeName)
        {
            List<AgentOptions> allAgents = GetAllAgents();

            foreach (AgentOptions agent in allAgents)
            {
                await _customAgentProvider.SwitchLedger(agent);
                agentContext = await _customAgentProvider.GetContextAutoSwitchAsync();
                if (agentContext != null)
                {
                    try
                    {
                        string req = await Ledger.BuildGetAttribRequestAsync(null, targetDid, attributeName, null, null);
                        string res = await Ledger.SubmitRequestAsync(await agentContext.Pool, req);

                        if (EnsureSuccessResponse(res))
                        {
                            return res;
                        }
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }

            throw new AriesFrameworkException(ErrorCode.LedgerItemNotFound, "Ledger item not found");
        }

        /// <inheritdoc />
        public async Task<IList<AuthorizationRule>> LookupAuthorizationRulesAsync(IAgentContext agentContext)
        {
            string req = await Ledger.BuildGetAuthRuleRequestAsync(null, null, null, null, null, null);
            string res = await Ledger.SubmitRequestAsync(await agentContext.Pool, req);

            EnsureSuccessResponse(res);

            JObject jobj = JObject.Parse(res);
            return jobj["result"]["data"].ToObject<IList<AuthorizationRule>>();
        }

        /// <inheritdoc />
        public async Task<string> LookupNymAsync(IAgentContext agentContext, string did)
        {
            List<AgentOptions> allAgents = GetAllAgents();

            foreach (AgentOptions agent in allAgents)
            {
                if (agentContext != null)
                {
                    try
                    {
                        string req = await Ledger.BuildGetNymRequestAsync(null, did);
                        string res = await Ledger.SubmitRequestAsync(await agentContext.Pool, req);

                        if (EnsureSuccessResponse(res))
                        {
                            return res;
                        }
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }

            throw new AriesFrameworkException(ErrorCode.LedgerItemNotFound, "Ledger item not found");
        }

        /// <inheritdoc />
        public virtual async Task<ParseRegistryResponseResult> LookupRevocationRegistryAsync(IAgentContext agentContext, string revocationRegistryId, long timestamp)
        {
            List<AgentOptions> allAgents = GetAllAgents();

            foreach (AgentOptions agent in allAgents)
            {
                await _customAgentProvider.SwitchLedger(agent);
                agentContext = await _customAgentProvider.GetContextAutoSwitchAsync();
                if (agentContext != null)
                {
                    try
                    {
                        string req = await Ledger.BuildGetRevocRegRequestAsync(null, revocationRegistryId, timestamp);
                        string res = await Ledger.SubmitRequestAsync(await agentContext.Pool, req);

                        if (EnsureSuccessResponse(res))
                        {
                            return await Ledger.ParseGetRevocRegResponseAsync(res);
                        }
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }

            throw new AriesFrameworkException(ErrorCode.LedgerItemNotFound, "Ledger item not found");
        }

        /// <inheritdoc />
        public virtual async Task<ParseResponseResult> LookupRevocationRegistryDefinitionAsync(IAgentContext agentContext, string registryId)
        {
            List<AgentOptions> allAgents = GetAllAgents();

            foreach (AgentOptions agent in allAgents)
            {
                await _customAgentProvider.SwitchLedger(agent);
                agentContext = await _customAgentProvider.GetContextAutoSwitchAsync();
                if (agentContext != null)
                {
                    try
                    {
                        string req = await Ledger.BuildGetRevocRegDefRequestAsync(null, registryId);
                        string res = await Ledger.SubmitRequestAsync(await agentContext.Pool, req);

                        if (EnsureSuccessResponse(res))
                        {
                            return await Ledger.ParseGetRevocRegDefResponseAsync(res);
                        }
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }

            throw new AriesFrameworkException(ErrorCode.LedgerItemNotFound, "Ledger item not found");
        }

        /// <inheritdoc />
        public virtual async Task<ParseRegistryResponseResult> LookupRevocationRegistryDeltaAsync(IAgentContext agentContext, string revocationRegistryId, long from, long to)
        {
            List<AgentOptions> allAgents = GetAllAgents();

            foreach (AgentOptions agent in allAgents)
            {
                await _customAgentProvider.SwitchLedger(agent);
                agentContext = await _customAgentProvider.GetContextAutoSwitchAsync();
                if (agentContext != null)
                {
                    try
                    {
                        string req = await Ledger.BuildGetRevocRegDeltaRequestAsync(null, revocationRegistryId, from, to);
                        string res = await Ledger.SubmitRequestAsync(await agentContext.Pool, req);

                        if (EnsureSuccessResponse(res))
                        {
                            return await Ledger.ParseGetRevocRegDeltaResponseAsync(res);
                        }
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }

            throw new AriesFrameworkException(ErrorCode.LedgerItemNotFound, "Ledger item not found");
        }

        /// <inheritdoc />
        public virtual async Task<ParseResponseResult> LookupSchemaAsync(IAgentContext agentContext, string schemaId)
        {
            List<AgentOptions> allAgents = GetAllAgents();

            foreach (AgentOptions agent in allAgents)
            {
                await _customAgentProvider.SwitchLedger(agent);
                agentContext = await _customAgentProvider.GetContextAutoSwitchAsync();
                if (agentContext != null)
                {
                    try
                    {
                        string req = await Ledger.BuildGetSchemaRequestAsync(null, schemaId);
                        string res = await Ledger.SubmitRequestAsync(await agentContext.Pool, req);

                        if (EnsureSuccessResponse(res))
                        {
                            return await Ledger.ParseGetSchemaResponseAsync(res);
                        }
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }

            throw new AriesFrameworkException(ErrorCode.LedgerItemNotFound, "Ledger item not found");
        }

        /// <inheritdoc />
        public virtual async Task<string> LookupTransactionAsync(IAgentContext agentContext, string ledgerType, int sequenceId)
        {
            List<AgentOptions> allAgents = GetAllAgents();

            foreach (AgentOptions agent in allAgents)
            {
                await _customAgentProvider.SwitchLedger(agent);
                agentContext = await _customAgentProvider.GetContextAutoSwitchAsync();
                if (agentContext != null)
                {
                    try
                    {
                        string req = await Ledger.BuildGetTxnRequestAsync(null, ledgerType, sequenceId);
                        string res = await Ledger.SubmitRequestAsync(await agentContext.Pool, req);

                        if (EnsureSuccessResponse(res))
                        {
                            return res;
                        }
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }

            throw new AriesFrameworkException(ErrorCode.LedgerItemNotFound, "Ledger item not found");
        }

        /// <inheritdoc />
        public virtual async Task RegisterAttributeAsync(IAgentContext context, string submittedDid, string targetDid, string attributeName, object value, TransactionCost paymentInfo = null)
        {
            string data = $"{{\"{attributeName}\": {value.ToJson()}}}";

            string req = await Ledger.BuildAttribRequestAsync(submittedDid, targetDid, null, data, null);
            string res = await SignAndSubmitAsync(context, submittedDid, req, paymentInfo);

            EnsureSuccessResponse(res);
        }

        /// <inheritdoc />
        public virtual async Task RegisterCredentialDefinitionAsync(IAgentContext context, string submitterDid, string data, TransactionCost paymentInfo = null)
        {
            string req = await Ledger.BuildCredDefRequestAsync(submitterDid, data);
            string res = await SignAndSubmitAsync(context, submitterDid, req, paymentInfo);

            EnsureSuccessResponse(res);
        }

        /// <inheritdoc />
        public virtual async Task RegisterNymAsync(IAgentContext context, string submitterDid, string theirDid, string theirVerkey, string role, TransactionCost paymentInfo = null)
        {
            if (DidUtils.IsFullVerkey(theirVerkey))
            {
                theirVerkey = await Did.AbbreviateVerkeyAsync(theirDid, theirVerkey);
            }

            string req = await Ledger.BuildNymRequestAsync(submitterDid, theirDid, theirVerkey, null, role);
            string res = await SignAndSubmitAsync(context, submitterDid, req, paymentInfo);

            EnsureSuccessResponse(res);
        }

        /// <inheritdoc />
        public virtual async Task RegisterRevocationRegistryDefinitionAsync(IAgentContext context, string submitterDid,
            string data, TransactionCost paymentInfo = null)
        {
            string req = await Ledger.BuildRevocRegDefRequestAsync(submitterDid, data);
            string res = await SignAndSubmitAsync(context, submitterDid, req, paymentInfo);

            EnsureSuccessResponse(res);
        }

        /// <inheritdoc />
        public virtual async Task RegisterSchemaAsync(IAgentContext context, string issuerDid, string schemaJson, TransactionCost paymentInfo = null)
        {
            string req = await Ledger.BuildSchemaRequestAsync(issuerDid, schemaJson);
            string res = await SignAndSubmitAsync(context, issuerDid, req, paymentInfo);

            EnsureSuccessResponse(res);
        }

        /// <inheritdoc />
        public virtual async Task SendRevocationRegistryEntryAsync(IAgentContext context, string issuerDid, string revocationRegistryDefinitionId, string revocationDefinitionType, string value, TransactionCost paymentInfo = null)
        {
            string req = await Ledger.BuildRevocRegEntryRequestAsync(issuerDid, revocationRegistryDefinitionId,
                revocationDefinitionType, value);
            string res = await SignAndSubmitAsync(context, issuerDid, req, paymentInfo);

            EnsureSuccessResponse(res);
        }

        private async Task<string> SignAndSubmitAsync(IAgentContext context, string submitterDid, string request, TransactionCost paymentInfo)
        {
            if (paymentInfo != null)
            {
                PaymentResult requestWithFees = await Payments.AddRequestFeesAsync(
                    wallet: context.Wallet,
                    submitterDid: null,
                    reqJson: request,
                    inputsJson: paymentInfo.PaymentAddress.Sources.Select(x => x.Source).ToJson(),
                    outputsJson: new[]
                    {
                        new IndyPaymentOutputSource
                        {
                            Recipient = paymentInfo.PaymentAddress.Address,
                            Amount = paymentInfo.PaymentAddress.Balance - paymentInfo.Amount
                        }
                    }.ToJson(),
                    extra: null);
                request = requestWithFees.Result;
            }
            string signedRequest = await _signingService.SignRequestAsync(context, submitterDid, request);
            string response = await Ledger.SubmitRequestAsync(await context.Pool, signedRequest);

            EnsureSuccessResponse(response);

            if (paymentInfo != null)
            {
                string responsePayment = await Payments.ParseResponseWithFeesAsync(paymentInfo.PaymentMethod, response);
                IList<IndyPaymentOutputSource> paymentOutputs = responsePayment.ToObject<IList<IndyPaymentOutputSource>>();
                paymentInfo.PaymentAddress.Sources = paymentOutputs
                    .Where(x => x.Recipient == paymentInfo.PaymentAddress.Address)
                    .Select(x => new IndyPaymentInputSource
                    {
                        Amount = x.Amount,
                        PaymentAddress = x.Recipient,
                        Source = x.Receipt
                    })
                    .ToList();
            }
            return response;
        }

        private List<AgentOptions> GetAllAgents()
        {
            List<AgentOptions> agentList = _customAgentProvider.GetAllAgentOptions().ToList();
            AgentOptions activeAgent = _customAgentProvider.GetActiveAgentOptions();

            List<AgentOptions> returnList = new List<AgentOptions>();
            returnList.AddRange(agentList.Where(x => x.PoolName.Equals(activeAgent.PoolName)).ToList());
            returnList.AddRange(agentList.Where(x => !x.PoolName.Equals(activeAgent.PoolName)).ToList());

            return returnList;
        }

        bool EnsureSuccessResponse(string res)
        {
            JObject response = JObject.Parse(res);

            if (!response["op"].ToObject<string>().Equals("reply", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
