using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Storage;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IDWallet.Agent.Handler
{
    internal class CustomCredentialHandler : IMessageHandler
    {
        private readonly AgentOptions _agentOptions;
        private readonly ICredentialService _credentialService;
        private readonly IMessageService _messageService;
        private readonly IWalletRecordService _recordService;
        public CustomCredentialHandler(
            IOptions<AgentOptions> agentOptions,
            ICredentialService credentialService,
            IWalletRecordService recordService,
            IMessageService messageService)
        {
            _agentOptions = agentOptions.Value;
            _credentialService = credentialService;
            _recordService = recordService;
            _messageService = messageService;
        }

        public IEnumerable<MessageType> SupportedMessageTypes => new MessageType[]
        {
            MessageTypes.IssueCredentialNames.OfferCredential,
            MessageTypes.IssueCredentialNames.RequestCredential,
            MessageTypes.IssueCredentialNames.IssueCredential,
            MessageTypesHttps.IssueCredentialNames.OfferCredential,
            MessageTypesHttps.IssueCredentialNames.RequestCredential,
            MessageTypesHttps.IssueCredentialNames.IssueCredential
        };

        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            switch (messageContext.GetMessageType())
            {
                case MessageTypesHttps.IssueCredentialNames.OfferCredential:
                case MessageTypes.IssueCredentialNames.OfferCredential:
                    {
                        CredentialOfferMessage offer = messageContext.GetMessage<CredentialOfferMessage>();
                        string recordId = await _credentialService.ProcessOfferAsync(
                            agentContext, offer, messageContext.Connection);

                        messageContext.ContextRecord = await _credentialService.GetAsync(agentContext, recordId);

                        if (_agentOptions.AutoRespondCredentialOffer == true)
                        {
                            (CredentialRequestMessage message, CredentialRecord record) =
                                await _credentialService.CreateRequestAsync(agentContext, recordId);
                            messageContext.ContextRecord = record;
                            return message;
                        }

                        return null;
                    }

                case MessageTypesHttps.IssueCredentialNames.RequestCredential:
                case MessageTypes.IssueCredentialNames.RequestCredential:
                    {
                        CredentialRequestMessage request = messageContext.GetMessage<CredentialRequestMessage>();
                        string recordId = await _credentialService.ProcessCredentialRequestAsync(
                            agentContext: agentContext,
                            credentialRequest: request,
                            connection: messageContext.Connection);
                        if (request.ReturnRoutingRequested() && messageContext.Connection == null)
                        {
                            (CredentialIssueMessage message, CredentialRecord record) =
                                await _credentialService.CreateCredentialAsync(agentContext, recordId);
                            messageContext.ContextRecord = record;
                            return message;
                        }
                        else
                        {
                            if (_agentOptions.AutoRespondCredentialRequest == true)
                            {
                                (CredentialIssueMessage message, CredentialRecord record) =
                                    await _credentialService.CreateCredentialAsync(agentContext, recordId);
                                messageContext.ContextRecord = record;
                                return message;
                            }

                            messageContext.ContextRecord = await _credentialService.GetAsync(agentContext, recordId);
                            return null;
                        }
                    }

                case MessageTypesHttps.IssueCredentialNames.IssueCredential:
                case MessageTypes.IssueCredentialNames.IssueCredential:
                    {
                        CredentialIssueMessage credential = messageContext.GetMessage<CredentialIssueMessage>();
                        string recordId = await _credentialService.ProcessCredentialAsync(
                            agentContext, credential, messageContext.Connection);

                        messageContext.ContextRecord = await UpdateValuesAsync(
                            credentialId: recordId,
                            credentialIssue: messageContext.GetMessage<CredentialIssueMessage>(),
                            agentContext: agentContext);

                        return null;
                    }
                default:
                    throw new AriesFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageContext.GetMessageType()}");
            }
        }

        private async Task<CredentialRecord> UpdateValuesAsync(string credentialId,
            CredentialIssueMessage credentialIssue, IAgentContext agentContext)
        {
            Hyperledger.Aries.Decorators.Attachments.Attachment credentialAttachment =
                credentialIssue.Credentials.FirstOrDefault(x => x.Id == "libindy-cred-0")
                ?? throw new ArgumentException("Credential attachment not found");

            string credentialJson = credentialAttachment.Data.Base64.GetBytesFromBase64().GetUTF8String();

            JObject jcred = JObject.Parse(credentialJson);
            Dictionary<string, AttributeValue> values = jcred["values"].ToObject<Dictionary<string, AttributeValue>>();

            CredentialRecord credential = await _credentialService.GetAsync(agentContext, credentialId);
            credential.CredentialAttributesValues = values.Select(x => new CredentialPreviewAttribute
            { Name = x.Key, Value = x.Value.Raw, MimeType = CredentialMimeTypes.TextMimeType }).ToList();
            await _recordService.UpdateAsync(agentContext.Wallet, credential);

            return credential;
        }

        private class AttributeValue
        {
            [JsonProperty("encoded")] public string Encoded { get; set; }
            [JsonProperty("raw")] public string Raw { get; set; }
        }
    }
}