using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Agent.Utils;
using IDWallet.Models;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Decorators;
using Hyperledger.Aries.Decorators.Attachments;
using Hyperledger.Aries.Decorators.Threading;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Models.Events;
using Hyperledger.Aries.Storage;
using Hyperledger.Aries.Utils;
using Hyperledger.Indy.AnonCredsApi;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RequestedCredentials = IDWallet.Agent.Models.CustomRequestedCredentials;

namespace IDWallet.Agent.Services
{
    public class CustomProofService : DefaultProofService
    {
        private readonly ICustomWalletRecordService _walletRecordService =
            App.Container.Resolve<ICustomWalletRecordService>();

        public CustomProofService(IEventAggregator eventAggregator, IConnectionService connectionService,
            ICustomWalletRecordService recordService, IProvisioningService provisioningService,
            ILedgerService ledgerService, ITailsService tailsService, IMessageService messageService,
            ILogger<DefaultProofService> logger) : base(eventAggregator, connectionService,
            (IWalletRecordService)recordService, provisioningService, ledgerService, tailsService, messageService,
            logger)
        {
        }

        public virtual async Task<(PresentationMessage, ProofRecord)> CreateProofAsync(IAgentContext agentContext,
            string proofRequestId, RequestedCredentials requestedCredentials)
        {
            ProofRecord proofRecord =
                await _walletRecordService.GetAsync<ProofRecord>(agentContext.Wallet, proofRequestId);

            ConnectionRecord connectionRecord = await CheckConnectionForHistoryElements(agentContext, proofRecord);

            ProofRequest proofRequest = proofRecord.RequestJson.ToObject<ProofRequest>();

            if (proofRecord.State != ProofState.Requested)
            {
                throw new AriesFrameworkException(ErrorCode.RecordInInvalidState,
                    $"Proof state was invalid. Expected '{ProofState.Requested}', found '{proofRecord.State}'");
            }

            ProvisioningRecord provisioningRecord = await ProvisioningService.GetProvisioningAsync(agentContext.Wallet);

            List<CredentialInfo> credentialObjects = new List<CredentialInfo>();
            foreach (string credId in requestedCredentials.GetCredentialIdentifiers())
            {
                try
                {
                    credentialObjects.Add(
                        JsonConvert.DeserializeObject<CredentialInfo>(
                            await AnonCreds.ProverGetCredentialAsync(agentContext.Wallet, credId)));
                }
                catch (Exception)
                {
                    BasicPopUp alertPopUp = new BasicPopUp(
                        Resources.Lang.PopUp_Undefined_Error_Title,
                        Resources.Lang.PopUp_Undefined_Error_Message,
                        Resources.Lang.PopUp_Undefined_Error_Button);
                    await alertPopUp.ShowPopUp();
                }
            }

            string schemas = await BuildSchemasAsync(agentContext,
                credentialObjects
                    .Select(x => x.SchemaId)
                    .Distinct());

            string definitions = await BuildCredentialDefinitionsAsync(agentContext,
                credentialObjects
                    .Select(x => x.CredentialDefinitionId)
                    .Distinct());

            string revocationStates = await BuildRevocationStatesAsync(agentContext,
                credentialObjects,
                requestedCredentials);

            string proofJson = await AnonCreds.ProverCreateProofAsync(agentContext.Wallet, proofRecord.RequestJson,
                requestedCredentials.ToJson(), provisioningRecord.MasterSecretId, schemas, definitions,
                revocationStates);

            proofRecord.ProofJson = proofJson;

            proofRecord = await CreateProofHistoryElements(agentContext, proofRequestId, requestedCredentials, proofRecord, connectionRecord, proofRequest, credentialObjects);

            try
            {
                await proofRecord.TriggerAsync(ProofTrigger.Accept);
            }
            catch (Exception)
            {
                //ignore
            }
            finally
            {
                await _walletRecordService.UpdateAsync(agentContext.Wallet, proofRecord);
            }

            string threadId = proofRecord.GetTag(TagConstants.LastThreadId);

            PresentationMessage presentationMessage = CreatePresentationMessage(proofJson);

            presentationMessage.ThreadFrom(threadId);

            return (presentationMessage, proofRecord);
        }

        private async Task<ConnectionRecord> CheckConnectionForHistoryElements(IAgentContext agentContext, ProofRecord proofRecord)
        {
            ConnectionRecord connectionRecord = null;
            try
            {
                if (proofRecord.ConnectionId != null)
                {
                    connectionRecord =
                        await RecordService.GetAsync<ConnectionRecord>(agentContext.Wallet, proofRecord.ConnectionId);
                }
            }
            catch (Exception)
            {
                //ignore
            }

            return connectionRecord;
        }

        private static PresentationMessage CreatePresentationMessage(string proofJson)
        {
            return new PresentationMessage
            {
                Id = Guid.NewGuid().ToString(),
                Presentations = new[]
                {
                    new Attachment
                    {
                        Id = "libindy-presentation-0",
                        MimeType = CredentialMimeTypes.ApplicationJsonMimeType,
                        Data = new AttachmentContent
                        {
                            Base64 = Convert.ToBase64String(proofJson.GetUTF8Bytes())
                        }
                    }
                }
            };
        }

        private async Task<ProofRecord> CreateProofHistoryElements(IAgentContext agentContext, string proofRequestId, RequestedCredentials requestedCredentials, ProofRecord proofRecord, ConnectionRecord connectionRecord, ProofRequest proofRequest, List<CredentialInfo> credentialObjects)
        {
            CredentialHistoryElements presentedCredentials = new CredentialHistoryElements
            {
                ConnectionRecord = connectionRecord,
                ProofRecordId = proofRecord.Id,
                CredentialRecordIds = requestedCredentials.GetCredentialIdentifiers().ToList(),
                RevealedClaims = new List<CredentialClaim>(),
                NonRevealedClaims = new List<CredentialClaim>(),
                PredicateClaims = new List<CredentialClaim>(),
                SelfAttestedClaims = new List<CredentialClaim>()
            };

            RequestedCredentialHistoryElements(requestedCredentials, proofRequest, credentialObjects, presentedCredentials);
            RequestedPredicatsHistoryElements(requestedCredentials, proofRequest, presentedCredentials);
            SelfAttestedHistoryElements(requestedCredentials, presentedCredentials);

            presentedCredentials.RevealedClaims = presentedCredentials.RevealedClaims.OrderBy(x => x.Name).ToList();
            presentedCredentials.NonRevealedClaims =
                presentedCredentials.NonRevealedClaims.OrderBy(x => x.Name).ToList();
            presentedCredentials.PredicateClaims = presentedCredentials.PredicateClaims.OrderBy(x => x.Name).ToList();
            presentedCredentials.SelfAttestedClaims =
                presentedCredentials.SelfAttestedClaims.OrderBy(x => x.Name).ToList();

            proofRecord = await _walletRecordService.GetAsync<ProofRecord>(agentContext.Wallet, proofRequestId, true);

            try
            {
                proofRecord.SetTag(WalletParams.HistoryCredentialsTag, JsonConvert.SerializeObject(presentedCredentials));
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Undefined_Error_Title,
                    Resources.Lang.PopUp_Undefined_Error_Message,
                    Resources.Lang.PopUp_Undefined_Error_Button);
                await alertPopUp.ShowPopUp();
            }

            return proofRecord;
        }

        private static void SelfAttestedHistoryElements(RequestedCredentials requestedCredentials, CredentialHistoryElements presentedCredentials)
        {
            foreach (KeyValuePair<string, string> selfAttestedAttribute in requestedCredentials.SelfAttestedAttributes)
            {
                presentedCredentials.SelfAttestedClaims.Add(new CredentialClaim
                {
                    Name = selfAttestedAttribute.Key,
                    Value = selfAttestedAttribute.Value
                });
            }
        }

        private static void RequestedPredicatsHistoryElements(RequestedCredentials requestedCredentials, ProofRequest proofRequest, CredentialHistoryElements presentedCredentials)
        {
            foreach (KeyValuePair<string, RequestedAttribute> requestedPredicate in requestedCredentials
                            .RequestedPredicates)
            {
                if (!string.IsNullOrEmpty(proofRequest.RequestedPredicates[requestedPredicate.Key].Name))
                {
                    string referent = proofRequest.RequestedPredicates[requestedPredicate.Key].Name;
                    presentedCredentials.PredicateClaims.Add(new CredentialClaim
                    {
                        CredentialRecordId = requestedPredicate.Value.CredentialId,
                        Name = referent,
                        PredicateType = proofRequest.RequestedPredicates[requestedPredicate.Key].PredicateType,
                        Value = proofRequest.RequestedPredicates[requestedPredicate.Key].PredicateValue.ToString()
                    });
                }
                else if (proofRequest.RequestedPredicates[requestedPredicate.Key].Names.Count() > 0)
                {
                    foreach (string thisArrt in proofRequest.RequestedPredicates[requestedPredicate.Key].Names)
                    {
                        string referent = thisArrt;
                        presentedCredentials.PredicateClaims.Add(new CredentialClaim
                        {
                            CredentialRecordId = requestedPredicate.Value.CredentialId,
                            Name = referent,
                            PredicateType = proofRequest.RequestedPredicates[requestedPredicate.Key].PredicateType,
                            Value = proofRequest.RequestedPredicates[requestedPredicate.Key].PredicateValue.ToString()
                        });
                    }
                }
            }
        }

        private static void RequestedCredentialHistoryElements(RequestedCredentials requestedCredentials, ProofRequest proofRequest, List<CredentialInfo> credentialObjects, CredentialHistoryElements presentedCredentials)
        {
            foreach (KeyValuePair<string, RequestedAttribute> requestedAttribute in requestedCredentials
                            .RequestedAttributes)
            {
                if (!string.IsNullOrEmpty(proofRequest.RequestedAttributes[requestedAttribute.Key].Name))
                {
                    string referent = proofRequest.RequestedAttributes[requestedAttribute.Key].Name;
                    string rawValue = credentialObjects
                        .Find(x => x.Referent == requestedAttribute.Value.CredentialId)
                        .Attributes[referent];

                    CredentialClaim claim = new CredentialClaim
                    {
                        CredentialRecordId = requestedAttribute.Value.CredentialId,
                        Name = referent,
                        Value = rawValue
                    };

                    if (requestedAttribute.Value.Revealed == true)
                    {
                        presentedCredentials.RevealedClaims.Add(claim);
                    }
                    else
                    {
                        presentedCredentials.NonRevealedClaims.Add(claim);
                    }
                }
                else if (proofRequest.RequestedAttributes[requestedAttribute.Key].Names.Count() > 0)
                {
                    foreach (string thisArrt in proofRequest.RequestedAttributes[requestedAttribute.Key].Names)
                    {
                        string referent = thisArrt;
                        string rawValue = credentialObjects
                            .Find(x => x.Referent == requestedAttribute.Value.CredentialId)
                            .Attributes[referent];

                        CredentialClaim claim = new CredentialClaim
                        {
                            CredentialRecordId = requestedAttribute.Value.CredentialId,
                            Name = referent,
                            Value = rawValue
                        };

                        if (requestedAttribute.Value.Revealed == true)
                        {
                            presentedCredentials.RevealedClaims.Add(claim);
                        }
                        else
                        {
                            presentedCredentials.NonRevealedClaims.Add(claim);
                        }
                    }
                }
            }
        }

        public override async Task<List<Credential>> ListCredentialsForProofRequestAsync(IAgentContext agentContext,
            ProofRequest proofRequest, string attributeReferent)
        {
            using (CredentialSearchForProofRequest search =
                await AnonCreds.ProverSearchCredentialsForProofRequestAsync(agentContext.Wallet, proofRequest.ToJson()))
            {
                string searchResult = await search.NextAsync(attributeReferent, 100);
                return JsonConvert.DeserializeObject<List<Credential>>(searchResult);
            }
        }

        public async Task<ProofRecord> ProcessRequestAsync(IAgentContext agentContext,
                    RequestPresentationMessage requestPresentationMessage, CustomServiceDecorator service)
        {
            Attachment requestAttachment =
                requestPresentationMessage.Requests.FirstOrDefault(x => x.Id == "libindy-request-presentation-0")
                ?? throw new ArgumentException("Presentation request attachment not found.");

            string requestJson = requestAttachment.Data.Base64.GetBytesFromBase64().GetUTF8String();

            ProofRecord proofRecord = new ProofRecord
            {
                Id = Guid.NewGuid().ToString(),
                RequestJson = requestJson,
                ConnectionId = null,
                State = ProofState.Requested
            };
            proofRecord.SetTag(TagConstants.LastThreadId, requestPresentationMessage.GetThreadId());
            proofRecord.SetTag(TagConstants.Role, TagConstants.Holder);
            proofRecord.SetTag(DecoratorNames.ServiceDecorator, service.ToJson());

            await RecordService.AddAsync(agentContext.Wallet, proofRecord);

            EventAggregator.Publish(new ServiceMessageProcessingEvent
            {
                RecordId = proofRecord.Id,
                MessageType = requestPresentationMessage.Type,
                ThreadId = requestPresentationMessage.GetThreadId()
            });

            return proofRecord;
        }
        public override async Task<bool> VerifyProofAsync(IAgentContext agentContext, string proofRequestJson,
            string proofJson, bool validateEncoding = true)
        {
            PartialProof proof = JsonConvert.DeserializeObject<PartialProof>(proofJson);

            if (validateEncoding && proof.RequestedProof.RevealedAttributes != null)
            {
                foreach (KeyValuePair<string, ProofAttribute> attribute in proof.RequestedProof.RevealedAttributes)
                {
                    if (!CredentialUtils.CheckValidEncoding(attribute.Value.Raw, attribute.Value.Encoded) &&
                        !attribute.Value.Encoded.Equals("1234567890"))
                    {
                        throw new AriesFrameworkException(ErrorCode.InvalidProofEncoding,
                            $"The encoded value for '{attribute.Key}' is invalid. " +
                            $"Expected '{CustomCredentialUtils.GetEncoded(attribute.Value.Raw)}'. " +
                            $"Actual '{attribute.Value.Encoded}'");
                    }
                }
            }

            int nonRevocCount = 0;
            int withRevocCount = 0;

            ProofRequest proofRequest = proofRequestJson.ToObject<ProofRequest>();

            try
            {
                JToken revealedAttrs = JObject.Parse(proofJson)["requested_proof"]["revealed_attrs"];
                Dictionary<string, ProofAttribute> attrDict = new Dictionary<string, ProofAttribute>();
                attrDict = JsonConvert.DeserializeObject<Dictionary<string, ProofAttribute>>(revealedAttrs.ToString());
                foreach (KeyValuePair<string, ProofAttribute> attr in attrDict)
                {
                    if (proof.Identifiers[attr.Value.SubProofIndex].RevocationRegistryId == null)
                    {
                        nonRevocCount++;
                        proofRequest.RequestedAttributes.Where(x => x.Key.Equals(attr.Key)).FirstOrDefault().Value
                            .NonRevoked = null;
                    }
                    else
                    {
                        withRevocCount++;
                    }
                }
            }
            catch (NullReferenceException)
            {
                //ignore
            }


            try
            {
                JToken revealedAttrGrps = JObject.Parse(proofJson)["requested_proof"]["revealed_attr_groups"];
                Dictionary<string, ProofAttribute> grpDict = new Dictionary<string, ProofAttribute>();
                grpDict = JsonConvert
                    .DeserializeObject<Dictionary<string, ProofAttribute>>(revealedAttrGrps.ToString());
                foreach (KeyValuePair<string, ProofAttribute> grp in grpDict)
                {
                    if (proof.Identifiers[grp.Value.SubProofIndex].RevocationRegistryId == null)
                    {
                        nonRevocCount++;
                        proofRequest.RequestedAttributes.Where(x => x.Key.Equals(grp.Key)).FirstOrDefault().Value
                            .NonRevoked = null;
                    }
                    else
                    {
                        withRevocCount++;
                    }
                }
            }
            catch (NullReferenceException)
            {
                //ignore
            }

            try
            {
                JToken predicates = JObject.Parse(proofJson)["requested_proof"]["predicates"];
                Dictionary<string, ProofAttribute> predicatesDict = new Dictionary<string, ProofAttribute>();
                predicatesDict =
                    JsonConvert.DeserializeObject<Dictionary<string, ProofAttribute>>(predicates.ToString());
                foreach (KeyValuePair<string, ProofAttribute> predicat in predicatesDict)
                {
                    if (proof.Identifiers[predicat.Value.SubProofIndex].RevocationRegistryId == null)
                    {
                        nonRevocCount++;
                        proofRequest.RequestedPredicates.Where(x => x.Key.Equals(predicat.Key)).FirstOrDefault().Value
                            .NonRevoked = null;
                    }
                    else
                    {
                        withRevocCount++;
                    }
                }
            }
            catch (NullReferenceException)
            {
                //ignore
            }

            proofRequestJson = proofRequest.ToJson();

            string schemas = await BuildSchemasAsync(agentContext,
                proof.Identifiers
                    .Select(x => x.SchemaId)
                    .Where(x => x != null)
                    .Distinct());

            string definitions = await BuildCredentialDefinitionsAsync(agentContext,
                proof.Identifiers
                    .Select(x => x.CredentialDefintionId)
                    .Where(x => x != null)
                    .Distinct());

            string revocationDefinitions = await BuildRevocationRegistryDefinitionsAsync(agentContext,
                proof.Identifiers
                    .Select(x => x.RevocationRegistryId)
                    .Where(x => x != null)
                    .Distinct());

            string revocationRegistries = await BuildRevocationRegistryDetlasAsync(agentContext,
                proof.Identifiers
                    .Where(x => x.RevocationRegistryId != null));

            return await AnonCreds.VerifierVerifyProofAsync(proofRequestJson, proofJson, schemas,
                definitions, revocationDefinitions, revocationRegistries);
        }

        private async Task<string> BuildCredentialDefinitionsAsync(IAgentContext agentContext,
            IEnumerable<string> credentialDefIds)
        {
            Dictionary<string, JObject> result = new Dictionary<string, JObject>();

            foreach (string schemaId in credentialDefIds)
            {
                Hyperledger.Indy.LedgerApi.ParseResponseResult ledgerDefinition =
                    await LedgerService.LookupDefinitionAsync(agentContext, schemaId);
                result.Add(schemaId, JObject.Parse(ledgerDefinition.ObjectJson));
            }

            return result.ToJson();
        }

        private async Task<string> BuildRevocationRegistryDefinitionsAsync(IAgentContext context,
            IEnumerable<string> revocationRegistryIds)
        {
            Dictionary<string, JObject> result = new Dictionary<string, JObject>();

            foreach (string revocationRegistryId in revocationRegistryIds)
            {
                Hyperledger.Indy.LedgerApi.ParseResponseResult ledgerSchema =
                    await LedgerService.LookupRevocationRegistryDefinitionAsync(context, revocationRegistryId);
                result.Add(revocationRegistryId, JObject.Parse(ledgerSchema.ObjectJson));
            }

            return result.ToJson();
        }

        private async Task<string> BuildRevocationRegistryDetlasAsync(IAgentContext context,
            IEnumerable<ProofIdentifier> proofIdentifiers)
        {
            Dictionary<string, Dictionary<string, JObject>> result =
                new Dictionary<string, Dictionary<string, JObject>>();

            foreach (ProofIdentifier identifier in proofIdentifiers)
            {
                Hyperledger.Indy.LedgerApi.ParseRegistryResponseResult delta =
                    await LedgerService.LookupRevocationRegistryDeltaAsync(context,
                        identifier.RevocationRegistryId,
                        -1,
                        long.Parse(identifier.Timestamp));

                result.Add(identifier.RevocationRegistryId,
                    new Dictionary<string, JObject>
                    {
                        {identifier.Timestamp, JObject.Parse(delta.ObjectJson)}
                    });
            }

            return result.ToJson();
        }

        private async Task<string> BuildRevocationStatesAsync(IAgentContext agentContext,
            IEnumerable<CredentialInfo> credentialObjects, RequestedCredentials requestedCredentials)
        {
            List<RequestedAttribute> allCredentials = new List<RequestedAttribute>();
            allCredentials.AddRange(requestedCredentials.RequestedAttributes.Values);
            allCredentials.AddRange(requestedCredentials.RequestedPredicates.Values);

            Dictionary<string, Dictionary<string, JObject>> result =
                new Dictionary<string, Dictionary<string, JObject>>();

            foreach (RequestedAttribute requestedCredential in allCredentials)
            {
                CredentialInfo credential =
                    credentialObjects.First(x => x.Referent == requestedCredential.CredentialId);

                if (credential.RevocationRegistryId == null)
                {
                    continue;
                }

                long timestamp = requestedCredential.Timestamp ??
                                 throw new Exception(
                                     "Timestamp must be provided for credential that supports revocation");

                if (result.ContainsKey(credential.RevocationRegistryId) &&
                    result[credential.RevocationRegistryId].ContainsKey($"{timestamp}"))
                {
                    continue;
                }

                Hyperledger.Indy.LedgerApi.ParseResponseResult registryDefinition =
                    await LedgerService.LookupRevocationRegistryDefinitionAsync(agentContext,
                        credential.RevocationRegistryId);

                Hyperledger.Indy.LedgerApi.ParseRegistryResponseResult delta =
                    await LedgerService.LookupRevocationRegistryDeltaAsync(agentContext,
                        credential.RevocationRegistryId, -1, timestamp);

                string tailsfile =
                    await TailsService.EnsureTailsExistsAsync(agentContext, credential.RevocationRegistryId);
                Hyperledger.Indy.BlobStorageApi.BlobStorageReader tailsReader =
                    await TailsService.OpenTailsAsync(tailsfile);

                string state = await AnonCreds.CreateRevocationStateAsync(tailsReader, registryDefinition.ObjectJson,
                    delta.ObjectJson, (long)delta.Timestamp, credential.CredentialRevocationId);

                if (!result.ContainsKey(credential.RevocationRegistryId))
                {
                    result.Add(credential.RevocationRegistryId, new Dictionary<string, JObject>());
                }

                result[credential.RevocationRegistryId].Add($"{timestamp}", JObject.Parse(state));
            }

            return result.ToJson();
        }

        private async Task<string> BuildSchemasAsync(IAgentContext agentContext, IEnumerable<string> schemaIds)
        {
            Dictionary<string, JObject> result = new Dictionary<string, JObject>();

            foreach (string schemaId in schemaIds)
            {
                Hyperledger.Indy.LedgerApi.ParseResponseResult ledgerSchema =
                    await LedgerService.LookupSchemaAsync(agentContext, schemaId);
                result.Add(schemaId, JObject.Parse(ledgerSchema.ObjectJson));
            }

            return result.ToJson();
        }
    }
}