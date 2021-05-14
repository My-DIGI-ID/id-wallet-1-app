using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Agent.Services;
using IDWallet.Interfaces;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Decorators;
using Hyperledger.Aries.Decorators.Attachments;
using Hyperledger.Aries.Decorators.Threading;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Models.Events;
using Hyperledger.Aries.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Xamarin.Forms;

namespace IDWallet.Services
{
    internal class UrlShortenerService
    {
        private readonly ICustomAgentProvider _agentProvider;
        private readonly IEventAggregator _eventAggregator;
        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _httpClientHandler = new HttpClientHandler();
        private readonly CustomProofService _proofService;
        private readonly ICustomWalletRecordService _walletRecordService;
        public UrlShortenerService(
            CustomProofService proofService,
            ICustomAgentProvider agentProvider,
            ICustomWalletRecordService walletRecordService,
            IEventAggregator eventAggregator)
        {
            _httpClientHandler.AllowAutoRedirect = false;
            _httpClientHandler.Proxy = DependencyService.Get<IProxyInfoProvider>().GetProxySettings();
            _proofService = proofService;
            _agentProvider = agentProvider;
            _walletRecordService = walletRecordService;
            _eventAggregator = eventAggregator;
            _httpClient = new HttpClient(_httpClientHandler);
        }

        public async
            Task<(ProofRecord proofRecord, CustomServiceDecorator serviceDecorator, CredentialRecord credentialRecord,
                CustomConnectionInvitationMessage invitation)> ProcessShortenedUrl(string shortenedUrl)
        {
            Uri uri = null;
            try
            {
                uri = new Uri(shortenedUrl);
            }
            catch (Exception)
            {
                return (null, null, null, null);
            }

            HttpResponseMessage responseMessage = null;
            try
            {
                responseMessage = await _httpClient.GetAsync(shortenedUrl);
            }
            catch (Exception)
            {
                //ignore
            }

            if (responseMessage != null)
            {
                if (responseMessage.Headers.Location != null)
                {
                    uri = responseMessage.Headers.Location;
                    try
                    {
                        if (uri.Query.StartsWith("?m="))
                        {
                            try
                            {
                                string queryMessage = uri.Query.Remove(0, 3).FromBase64();

                                CustomRequestPresentationMessage requestPresentationMessage =
                                    queryMessage.ToObject<CustomRequestPresentationMessage>();

                                IAgentContext agentContext = await _agentProvider.GetContextAsync();

                                CustomServiceDecorator service = requestPresentationMessage.Service;

                                ProofRecord proofRecord =
                                    await _proofService.ProcessRequestAsync(agentContext, requestPresentationMessage,
                                        service);

                                return (proofRecord, service, null, null);
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    string queryMessage = uri.Query.Remove(0, 3).FromBase64();

                                    CredentialOfferMessage credentialOfferMessage =
                                        queryMessage.ToObject<CredentialOfferMessage>();

                                    IAgentContext agentContext = await _agentProvider.GetContextAsync();

                                    JToken serviceJToken = JObject.Parse(queryMessage)
                                        .SelectToken($"~{DecoratorNames.ServiceDecorator}");

                                    CustomServiceDecorator service = serviceJToken.ToObject<CustomServiceDecorator>();

                                    string recordId = await ProcessOfferAsync(agentContext, credentialOfferMessage,
                                        service);

                                    CredentialRecord credentialRecord =
                                        await _walletRecordService.GetAsync<CredentialRecord>(agentContext.Wallet,
                                            recordId);

                                    return (null, service, credentialRecord, null);
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        string queryMessage = uri.Query.Remove(0, 3).FromBase64();

                                        CustomConnectionInvitationMessage invitationMessage =
                                            queryMessage.ToObject<CustomConnectionInvitationMessage>();

                                        return (null, null, null, invitationMessage);
                                    }
                                    catch (Exception)
                                    {
                                        return (null, null, null, null);
                                    }
                                }
                            }
                        }
                        else if (uri.Query.StartsWith("?d_m="))
                        {
                            try
                            {
                                System.Collections.Generic.Dictionary<string, string> arguments = uri.Query
                                    .Substring(1)
                                    .Split('&')
                                    .Select(q => q.Split('='))
                                    .ToDictionary(q => q.FirstOrDefault(), q => q.Skip(1).FirstOrDefault());

                                string message = HttpUtility.UrlDecode(arguments["d_m"], Encoding.UTF8);

                                string basedecodedMessage = message.FromBase64();

                                CustomRequestPresentationMessage requestPresentationMessage =
                                    basedecodedMessage.ToObject<CustomRequestPresentationMessage>();

                                IAgentContext agentContext = await _agentProvider.GetContextAsync();

                                CustomServiceDecorator service = requestPresentationMessage.Service;

                                ProofRecord proofRecord =
                                    await _proofService.ProcessRequestAsync(agentContext, requestPresentationMessage,
                                        service);

                                return (proofRecord, service, null, null);
                            }
                            catch (Exception)
                            {
                                try
                                {
                                    System.Collections.Generic.Dictionary<string, string> arguments = uri.Query
                                        .Substring(1)
                                        .Split('&')
                                        .Select(q => q.Split('='))
                                        .ToDictionary(q => q.FirstOrDefault(), q => q.Skip(1).FirstOrDefault());

                                    string message = HttpUtility.UrlDecode(arguments["d_m"], Encoding.UTF8);

                                    string basedecodedMessage = message.FromBase64();

                                    CredentialOfferMessage credentialOfferMessage =
                                        basedecodedMessage.ToObject<CredentialOfferMessage>();

                                    IAgentContext agentContext = await _agentProvider.GetContextAsync();

                                    JToken serviceJToken = JObject.Parse(basedecodedMessage)
                                        .SelectToken($"~{DecoratorNames.ServiceDecorator}");

                                    CustomServiceDecorator service = serviceJToken.ToObject<CustomServiceDecorator>();

                                    string recordId = await ProcessOfferAsync(agentContext, credentialOfferMessage,
                                        service);

                                    CredentialRecord credentialRecord =
                                        await _walletRecordService.GetAsync<CredentialRecord>(agentContext.Wallet,
                                            recordId);

                                    return (null, service, credentialRecord, null);
                                }
                                catch (Exception)
                                {
                                    try
                                    {
                                        System.Collections.Generic.Dictionary<string, string> arguments = uri.Query
                                            .Substring(1)
                                            .Split('&')
                                            .Select(q => q.Split('='))
                                            .ToDictionary(q => q.FirstOrDefault(), q => q.Skip(1).FirstOrDefault());

                                        string message = HttpUtility.UrlDecode(arguments["d_m"], Encoding.UTF8);

                                        string basedecodedMessage = message.FromBase64();

                                        CustomConnectionInvitationMessage invitationMessage =
                                            basedecodedMessage.ToObject<CustomConnectionInvitationMessage>();
                                        return (null, null, null, invitationMessage);
                                    }
                                    catch (Exception)
                                    {
                                        return (null, null, null, null);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        return (null, null, null, null);
                    }
                }
            }

            return (null, null, null, null);
        }

        private async Task<string> ProcessOfferAsync(IAgentContext agentContext, CredentialOfferMessage credentialOffer,
            CustomServiceDecorator service)
        {
            Attachment offerAttachment = credentialOffer.Offers.FirstOrDefault(x => x.Id == "libindy-cred-offer-0") ??
                                         throw new ArgumentNullException(nameof(CredentialOfferMessage.Offers));

            string offerJson = offerAttachment.Data.Base64.GetBytesFromBase64().GetUTF8String();
            JObject offer = JObject.Parse(offerJson);
            string definitionId = offer["cred_def_id"].ToObject<string>();
            string schemaId = offer["schema_id"].ToObject<string>();

            CredentialRecord credentialRecord = new CredentialRecord
            {
                Id = Guid.NewGuid().ToString(),
                OfferJson = offerJson,
                ConnectionId = null,
                CredentialDefinitionId = definitionId,
                CredentialAttributesValues = credentialOffer.CredentialPreview?.Attributes
                    .Select(x => new CredentialPreviewAttribute
                    {
                        Name = x.Name,
                        MimeType = x.MimeType,
                        Value = x.Value
                    }).ToArray(),
                SchemaId = schemaId,
                State = CredentialState.Offered
            };
            credentialRecord.SetTag(TagConstants.Role, TagConstants.Holder);
            credentialRecord.SetTag(TagConstants.LastThreadId, credentialOffer.GetThreadId());
            credentialRecord.SetTag(DecoratorNames.ServiceDecorator, service.ToJson());

            await _walletRecordService.AddAsync(agentContext.Wallet, credentialRecord);

            _eventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = credentialRecord.Id,
                MessageType = credentialOffer.Type,
                ThreadId = credentialOffer.GetThreadId()
            });

            return credentialRecord.Id;
        }
    }
}