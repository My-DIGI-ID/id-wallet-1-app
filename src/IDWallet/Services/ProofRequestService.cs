using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Agent.Services;
using IDWallet.Interfaces;
using IDWallet.Models;
using IDWallet.Utils;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Indy;
using Hyperledger.Indy.AnonCredsApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.Services
{
    public class ProofRequestService
    {
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly IMessageService _messageService = App.Container.Resolve<IMessageService>();
        private readonly CustomProofService _proofService = App.Container.Resolve<CustomProofService>();

        private readonly ICustomWalletRecordService _walletRecordService =
            App.Container.Resolve<ICustomWalletRecordService>();
        public async Task AddCredentialItem(ObservableCollection<ProofModel> requestCollection,
            IAgentContext agentContext,
            ProofAttributeInfo attribute,
            string key,
            List<WalletElement> walletElements,
            string name,
            ProofRequest proofRequest,
            string proofRecordId,
            string connectionAlias,
            Dictionary<string, Tuple<List<AttributeFilter>, string>> knownProofAttributes,
            bool takeNewest)
        {
            List<WalletElement> matchingCredentials = new List<WalletElement>();
            List<AttributeFilter> restrictions = attribute.Restrictions;
            bool needToShow = true;
            foreach (ProofModel request in requestCollection)
            {
                if (request.DictionaryKey == key)
                {
                    needToShow = false;
                }
            }

            List<Credential> credentials = new List<Credential>();
            try
            {
                credentials = await _proofService.ListCredentialsForProofRequestAsync(agentContext, proofRequest, key);
            }
            catch (Exception ex) when (ex.GetType() == typeof(InvalidStructureException) ||
                                       ex.InnerException?.GetType() == typeof(InvalidStructureException))
            {
                proofRequest.Nonce = "0000000000";

                ProofRecord proofRecord =
                    await _walletRecordService.GetAsync<ProofRecord>(agentContext.Wallet, proofRecordId, true);

                proofRecord.RequestJson = proofRequest.ToJson();

                await _walletRecordService.UpdateAsync(agentContext.Wallet, proofRecord);

                credentials = await _proofService.ListCredentialsForProofRequestAsync(agentContext, proofRequest, key);
            }

            foreach (Credential credential in credentials)
            {
                WalletElement foundCredential = walletElements.Find(x =>
                   x.CredentialRecord.CredentialId == credential.CredentialInfo.Referent);
                if (foundCredential != null)
                {
                    matchingCredentials.Add(foundCredential);
                }
            }

            // Check if its a self attested attribute for hardware signature
            if (!string.IsNullOrEmpty(attribute.Name) && (attribute.Name.Equals(WalletParams.HardwareSignature) || attribute.Name.Equals(WalletParams.HardwareSignatureDdl)))
            {
                AddHwSignatureRequest(requestCollection, name, key, attribute.Name);
                return;
            }

            if (restrictions == null || !restrictions.Any())
            {
                AddSelfAttestedRequest(requestCollection, name, key, walletElements, connectionAlias, attribute);
            }
            else if (takeNewest)
            {
                AddTakeNewestRequest(requestCollection, name, key, matchingCredentials, connectionAlias, attribute, needToShow, restrictions);
            }
            else
            {
                AddRequest(requestCollection, name, key, matchingCredentials, connectionAlias, attribute, knownProofAttributes, restrictions);
            }
        }

        private void AddHwSignatureRequest(ObservableCollection<ProofModel> requestCollection, string name, string key, string hardwareType)
        {
            CredentialClaim credentialClaim = new CredentialClaim();
            credentialClaim.Name = hardwareType;
            credentialClaim.Value = "Signatur";
            ProofElementOption proofElementOption = new ProofElementOption();
            proofElementOption.Attributes.Add(credentialClaim);

            WalletElement walletElement = new WalletElement();
            walletElement.Name = "Hardware-Signatur";
            walletElement.IssuedBy = "Selbsterstellte Signatur";
            walletElement.Revoked = false;
            walletElement.CredentialImageSource = ImageSource.FromFile("hardware_signatur.png");
            proofElementOption.WalletElement = walletElement;

            List<ProofElementOption> proofElementOptions = new List<ProofElementOption>(new ProofElementOption[] { proofElementOption });

            requestCollection.Add(new ProofModel
            {
                RequestedValue = name,
                SelectedValue = "",
                DictionaryKey = key,
                IsSelfAttested = true,
                ProofElementOptions = proofElementOptions,
                ImageVisibility = false,
                SelectedOption = null,
                IsSelected = true,
                Restrictions = null,
                IsEmbeddedImage = false,
                IsRegular = true,
                HasEmbeddedDocument = false,
                PortraitByteArray = null,
                OnlyOneOption = true,
                NeedToShow = true
            });
        }

        private void AddSelfAttestedRequest(ObservableCollection<ProofModel> requestCollection, string name, string key, List<WalletElement> walletElements, string connectionAlias, ProofAttributeInfo attribute)
        {
            List<ProofElementOption> proofElementOptions = new List<ProofElementOption>();
            foreach (WalletElement walletElement in walletElements)
            {
                string value = "";
                if (!string.IsNullOrEmpty(attribute.Name) && !attribute.Name.Equals("embeddedImage") &&
                    !attribute.Name.Equals("embeddedDocument"))
                {
                    if (walletElement.Claims.ToList().Exists(x => x.Name == name))
                    {
                        value = walletElement.Claims.First(x => x.Name == attribute.Name).Value;
                    }
                }
                else if (attribute.Names != null && attribute.Names.Count() > 0 && name != "embeddedImage" &&
                         name != "embeddedDocument")
                {
                    if (walletElement.Claims.ToList().Exists(x => x.Name == name))
                    {
                        value = walletElement.Claims.First(x =>
                            x.Name == attribute.Names.Where(y => y.Equals(name)).FirstOrDefault()).Value;
                    }
                }

                if (!string.IsNullOrEmpty(value))
                {
                    proofElementOptions.Add(new ProofElementOption
                    {
                        WalletElement = walletElement,
                        Value = value,
                        ConnectionAlias = connectionAlias
                    });
                }
            }

            proofElementOptions = new List<ProofElementOption>(proofElementOptions
                .OrderBy(x => x.WalletElement.CredentialRecord.CredentialDefinitionId)
                .ThenByDescending(x => x.WalletElement.CredentialRecord.CreatedAtUtc.Value));
            for (int i = 1; i < proofElementOptions.Count; i++)
            {
                ProofElementOption currentOption = proofElementOptions[i];
                ProofElementOption previousOption = proofElementOptions[i - 1];
                if (currentOption.WalletElement.CredentialRecord.CredentialDefinitionId ==
                    previousOption.WalletElement.CredentialRecord.CredentialDefinitionId)
                {
                    if (previousOption.CopyCounter == null)
                    {
                        currentOption.CopyCounter = "#2";
                        previousOption.CopyCounter = "#1";
                    }
                    else
                    {
                        currentOption.CopyCounter =
                            "#" + (int.Parse(previousOption.CopyCounter.Substring(1)) + 1).ToString();
                    }
                }
            }

            requestCollection.Add(new ProofModel
            {
                RequestedValue = name,
                SelectedValue = "",
                DictionaryKey = key,
                OnlyOneOption = false,
                IsSelfAttested = true,
                ProofElementOptions = proofElementOptions,
                ImageVisibility = false,
                SelectedOption = null,
                IsSelected = false,
                Restrictions = null,
                IsEmbeddedImage = false,
                IsRegular = true,
                HasEmbeddedDocument = false,
                PortraitByteArray = null,
                NeedToShow = true
            });
        }

        private void AddTakeNewestRequest(ObservableCollection<ProofModel> requestCollection, string name, string key, List<WalletElement> matchingCredentials, string connectionAlias, ProofAttributeInfo attribute, bool needToShow, List<AttributeFilter> restrictions)
        {
            bool optionsEmpty = true;
            List<ProofElementOption> proofElementOptions = new List<ProofElementOption>();
            bool onlyOneOption = true;
            string selectedValue = null;
            string copyCounter = null;
            ProofElementOption selectedOption = null;
            bool isSelected = false;
            bool revoked = false;
            bool isRegular = true;
            bool isEmbedded = false;
            byte[] portraitByteArray = null;
            bool hasDocument = false;
            string documentString = "";

            if (matchingCredentials.Any())
            {
                optionsEmpty = false;
                foreach (WalletElement credential in matchingCredentials)
                {
                    string value = "";
                    if (!string.IsNullOrEmpty(attribute.Name) && !attribute.Name.Equals("embeddedImage") &&
                        !attribute.Name.Equals("embeddedDocument"))
                    {
                        value = credential.Claims.First(x => x.Name == attribute.Name).Value;
                    }
                    else if (attribute.Names != null && attribute.Names.Count() > 0 && name != "embeddedImage" &&
                             name != "embeddedDocument")
                    {
                        value = credential.Claims.First(x =>
                            x.Name == attribute.Names.Where(y => y.Equals(name)).FirstOrDefault()).Value;
                    }

                    proofElementOptions.Add(new ProofElementOption
                    {
                        WalletElement = credential,
                        Value = value,
                        ConnectionAlias = connectionAlias
                    });
                }

                proofElementOptions = new List<ProofElementOption>(proofElementOptions
                    .OrderBy(x => x.WalletElement.CredentialRecord.CredentialDefinitionId)
                    .ThenByDescending(x => x.WalletElement.CredentialRecord.CreatedAtUtc.Value));
                for (int i = 1; i < proofElementOptions.Count; i++)
                {
                    ProofElementOption currentOption = proofElementOptions[i];
                    ProofElementOption previousOption = proofElementOptions[i - 1];
                    if (currentOption.WalletElement.CredentialRecord.CredentialDefinitionId ==
                        previousOption.WalletElement.CredentialRecord.CredentialDefinitionId)
                    {
                        if (previousOption.CopyCounter == null)
                        {
                            currentOption.CopyCounter = "#2";
                            previousOption.CopyCounter = "#1";
                        }
                        else
                        {
                            currentOption.CopyCounter =
                                "#" + (int.Parse(previousOption.CopyCounter.Substring(1)) + 1).ToString();
                        }
                    }
                }

                proofElementOptions = new List<ProofElementOption>(proofElementOptions.OrderByDescending(x =>
                    x.WalletElement.CredentialRecord.CreatedAtUtc.Value));
                if (proofElementOptions.Count > 1)
                {
                    onlyOneOption = false;
                    proofElementOptions.First().IconSource = "mdi-circle-slice-8";
                }

                if (proofElementOptions.Any())
                {
                    proofElementOptions[proofElementOptions.Count - 1].ShowSeparator = false;
                }

                selectedValue = proofElementOptions.First().Value;
                copyCounter = proofElementOptions.First().CopyCounter;
                selectedOption = proofElementOptions.First();
                isSelected = true;
                revoked = proofElementOptions.First().WalletElement.Revoked;

                if ((attribute.Name != null && attribute.Name == "embeddedImage") || (attribute.Names != null &&
                    attribute.Names.Count() > 0 && attribute.Names.Contains("embeddedImage") &&
                    name == "embeddedImage"))
                {
                    isEmbedded = true;
                    isRegular = false;
                    portraitByteArray = proofElementOptions.First().WalletElement.PortraitByteArray;
                }

                else if ((attribute.Name != null && attribute.Name == "embeddedDocument") ||
                         (attribute.Names != null && attribute.Names.Count() > 0 &&
                          attribute.Names.Contains("embeddedDocument") && name == "embeddedDocument"))
                {
                    hasDocument = true;
                    isRegular = false;
                    documentString = proofElementOptions.First().WalletElement.DocumentString;
                }
            }

            requestCollection.Add(new ProofModel
            {
                RequestedValue = name,
                SelectedValue = selectedValue,
                DictionaryKey = key,
                NeedToShow = needToShow,
                IsSelfAttested = false,
                Revoked = revoked,
                ProofElementOptions = proofElementOptions,
                OnlyOneOption = onlyOneOption,
                ImageVisibility = optionsEmpty,
                SelectedOption = selectedOption,
                IsSelected = isSelected,
                Restrictions = restrictions,
                IsEmbeddedImage = isEmbedded,
                IsRegular = isRegular,
                PortraitByteArray = portraitByteArray,
                HasEmbeddedDocument = hasDocument,
                DocumentString = documentString
            });
        }

        private void AddRequest(ObservableCollection<ProofModel> requestCollection, string name, string key, List<WalletElement> matchingCredentials, string connectionAlias, ProofAttributeInfo attribute, Dictionary<string, Tuple<List<AttributeFilter>, string>> knownProofAttributes, List<AttributeFilter> restrictions)
        {
            bool isSelected = false;
            string selectedValue = null;
            bool isEmbedded = false;
            bool isRegular = true;
            bool hasDocument = false;
            byte[] portraitByteArray = null;
            WalletElement lastUsedCredential = null;
            ProofElementOption selectedOption = null;
            if (knownProofAttributes.ContainsKey(name))
            {
                string lastUsedCredentialId = knownProofAttributes[name].Item2;
                lastUsedCredential =
                    matchingCredentials.Find(x => x.CredentialRecord.CredentialId == lastUsedCredentialId);
                if (lastUsedCredential != null)
                {
                    isSelected = true;
                    if ((attribute.Name != null && attribute.Name == "embeddedImage") || (attribute.Names != null &&
                        attribute.Names.Count() > 0 && attribute.Names.Contains("embeddedImage") &&
                        name == "embeddedImage"))
                    {
                        selectedValue = lastUsedCredential.PortraitByteArray.ToBase64String();
                        isEmbedded = true;
                        isRegular = false;
                    }
                    else if ((attribute.Name != null && attribute.Name == "embeddedDocument") ||
                             (attribute.Names != null && attribute.Names.Count() > 0 &&
                              attribute.Names.Contains("embeddedDocument") && name == "embeddedDocument"))
                    {
                        selectedValue = lastUsedCredential.DocumentString;
                        hasDocument = true;
                        isRegular = false;
                    }
                    else
                    {
                        if (attribute.Name != null)
                        {
                            selectedValue = lastUsedCredential.Claims.First(x => x.Name == attribute.Name).Value;
                        }
                        else if (attribute.Names != null && attribute.Names.Count() > 0)
                        {
                            selectedValue = lastUsedCredential.Claims.First(x =>
                                x.Name == attribute.Names.Where(y => y.Equals(name)).FirstOrDefault()).Value;
                        }
                    }

                    selectedOption = new ProofElementOption
                    {
                        WalletElement = lastUsedCredential,
                        Value = selectedValue,
                        ConnectionAlias = connectionAlias
                    };
                }
            }

            requestCollection.Add(new ProofModel
            {
                RequestedValue = name,
                SelectedValue = selectedValue,
                DictionaryKey = key,
                IsSelfAttested = false,
                ProofElementOptions = null,
                ImageVisibility = false,
                SelectedOption = selectedOption,
                IsSelected = isSelected,
                Restrictions = restrictions,
                IsEmbeddedImage = isEmbedded,
                IsRegular = isRegular,
                HasEmbeddedDocument = hasDocument,
                PortraitByteArray = portraitByteArray
            });
        }

        public async Task AddKnownProofAttributes(ObservableCollection<ProofModel> requestCollection,
            IAgentContext agentContext, Dictionary<string, Tuple<List<AttributeFilter>, string>> knownProofAttributes,
            string connectionId)
        {
            try
            {
                Dictionary<string, Tuple<List<AttributeFilter>, string>> alreadyKnown =
                    new Dictionary<string, Tuple<List<AttributeFilter>, string>>();
                if (knownProofAttributes != null && knownProofAttributes.Any())
                {
                    alreadyKnown = knownProofAttributes;
                }

                foreach (ProofModel request in requestCollection)
                {
                    if (!request.IsSelfAttested)
                    {
                        if (alreadyKnown.ContainsKey(request.RequestedValue))
                        {
                            List<AttributeFilter> currentRestrictions = alreadyKnown[request.RequestedValue].Item1;
                            List<AttributeFilter> newRestrictions = new List<AttributeFilter>();
                            foreach (AttributeFilter attributeFilter1 in request.Restrictions)
                            {
                                bool filterAlreadyExists = false;
                                foreach (AttributeFilter attributeFilter2 in currentRestrictions)
                                {
                                    if (EqualFilter(attributeFilter1, attributeFilter2))
                                    {
                                        filterAlreadyExists = true;
                                    }
                                }

                                if (!filterAlreadyExists)
                                {
                                    newRestrictions.Add(attributeFilter1);
                                }

                                currentRestrictions.Concat(newRestrictions);
                            }

                            alreadyKnown[request.RequestedValue] = new Tuple<List<AttributeFilter>, string>(
                                currentRestrictions,
                                request.SelectedOption.WalletElement.CredentialRecord.CredentialId);
                        }
                        else
                        {
                            alreadyKnown.Add(request.RequestedValue,
                                new Tuple<List<AttributeFilter>, string>(request.Restrictions,
                                    request.SelectedOption.WalletElement.CredentialRecord.CredentialId));
                        }
                    }
                }

                ConnectionRecord connectionRecord =
                    await _walletRecordService.GetAsync<ConnectionRecord>(agentContext.Wallet, connectionId, true);
                connectionRecord.SetTag("KnownProofAttributes", alreadyKnown.ToJson());
                await _walletRecordService.UpdateAsync(agentContext.Wallet, connectionRecord);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public bool CheckKnownProofAttributes(
            Dictionary<string, Tuple<List<AttributeFilter>, string>> knownProofAttributes, ProofRequest proofRequest)
        {
            Dictionary<string, Tuple<List<AttributeFilter>, string>> allKnown = knownProofAttributes;
            foreach (KeyValuePair<string, ProofAttributeInfo> attribute in proofRequest.RequestedAttributes)
            {
                string attributeName = "";
                if (attribute.Value.Name != null)
                {
                    attributeName = attribute.Value.Name;

                    List<AttributeFilter> attributeRestrictions = attribute.Value.Restrictions;
                    if (allKnown.ContainsKey(attributeName))
                    {
                        List<AttributeFilter> knownRestrictions = allKnown[attributeName].Item1;
                        foreach (AttributeFilter requestedFilter in attributeRestrictions)
                        {
                            bool filterIsKnown = false;
                            foreach (AttributeFilter knownFilter in knownRestrictions)
                            {
                                if (EqualFilter(requestedFilter, knownFilter))
                                {
                                    filterIsKnown = true;
                                }
                            }

                            if (!filterIsKnown)
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (attribute.Value.Names != null && attribute.Value.Names.Count() > 0)
                {
                    foreach (string name in attribute.Value.Names)
                    {
                        List<AttributeFilter> attributeRestrictions = attribute.Value.Restrictions;
                        if (allKnown.ContainsKey(name))
                        {
                            List<AttributeFilter> knownRestrictions = allKnown[name].Item1;
                            foreach (AttributeFilter requestedFilter in attributeRestrictions)
                            {
                                bool filterIsKnown = false;
                                foreach (AttributeFilter knownFilter in knownRestrictions)
                                {
                                    if (EqualFilter(requestedFilter, knownFilter))
                                    {
                                        filterIsKnown = true;
                                    }
                                }

                                if (!filterIsKnown)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, ProofPredicateInfo> predicate in proofRequest.RequestedPredicates)
            {
                if (predicate.Value.Name != null)
                {
                    string predicateName = predicate.Value.Name + " "
                                                                + predicate.Value.PredicateType + " "
                                                                + predicate.Value.PredicateValue;
                    List<AttributeFilter> predicateRestrictions = predicate.Value.Restrictions;
                    if (allKnown.ContainsKey(predicateName))
                    {
                        List<AttributeFilter> knownRestrictions = allKnown[predicateName].Item1;
                        foreach (AttributeFilter requestedFilter in predicateRestrictions)
                        {
                            bool filterIsKnown = false;
                            foreach (AttributeFilter knownFilter in knownRestrictions)
                            {
                                if (EqualFilter(requestedFilter, knownFilter))
                                {
                                    filterIsKnown = true;
                                }
                            }

                            if (!filterIsKnown)
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (predicate.Value.Names != null && predicate.Value.Names.Count() > 0)
                {
                    foreach (string name in predicate.Value.Names)
                    {
                        string predicateName = name + " "
                                                    + predicate.Value.PredicateType + " "
                                                    + predicate.Value.PredicateValue;

                        List<AttributeFilter> predicateRestrictions = predicate.Value.Restrictions;
                        if (allKnown.ContainsKey(predicateName))
                        {
                            List<AttributeFilter> knownRestrictions = allKnown[predicateName].Item1;
                            foreach (AttributeFilter requestedFilter in predicateRestrictions)
                            {
                                bool filterIsKnown = false;
                                foreach (AttributeFilter knownFilter in knownRestrictions)
                                {
                                    if (EqualFilter(requestedFilter, knownFilter))
                                    {
                                        filterIsKnown = true;
                                    }
                                }

                                if (!filterIsKnown)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public async Task<PresentationMessage> CreateAndSendProof(ObservableCollection<ProofModel> requestCollection,
            IAgentContext agentContext,
            string proofRecordId,
            Dictionary<string, Tuple<List<AttributeFilter>, string>> knownProofAttributes,
            CustomServiceDecorator service = null,
            ProofRequest proofRequest = null)
        {
            Dictionary<string, RequestedAttribute> attributes = new Dictionary<string, RequestedAttribute>();
            Dictionary<string, string> selfs = new Dictionary<string, string>();
            Dictionary<string, RequestedAttribute> predicates = new Dictionary<string, RequestedAttribute>();
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            try
            {
                await GetCallCredentialsForProof(requestCollection, agentContext, attributes, selfs, predicates,
                    timestamp, proofRequest);
            }
            catch (Exception)
            {
                //ignore
            }

            CustomRequestedCredentials requestedCredentials = new CustomRequestedCredentials
            { RequestedAttributes = attributes, RequestedPredicates = predicates, SelfAttestedAttributes = selfs };
            (PresentationMessage presentation, ProofRecord proofRecord) =
                await _proofService.CreateProofAsync(agentContext, proofRecordId, requestedCredentials);

            if (service == null)
            {
                ConnectionRecord connectionRecord =
                    await _connectionService.GetAsync(agentContext, proofRecord.ConnectionId);
                try
                {
                    await _messageService.SendAsync(agentContext, presentation, connectionRecord);
                }
                catch (AriesFrameworkException)
                {
                    BasicPopUp alert = new BasicPopUp(
                        Resources.Lang.PopUp_Proof_Sending_Failed_Title,
                        Resources.Lang.PopUp_Proof_Sending_Failed_Text,
                        Resources.Lang.PopUp_Proof_Sending_Failed_Button)
                    {
                        AlwaysDisplay = true
                    };
                    await alert.ShowPopUp();
                }
                catch (HttpRequestException ex) when (ex.Message == "No such host is known")
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Resources.Lang.PopUp_Network_Error_Title,
                        Resources.Lang.PopUp_Network_Error_Text,
                        Resources.Lang.PopUp_Network_Error_Button)
                    {
                        AlwaysDisplay = true
                    };
                    await popUp.ShowPopUp();
                }

                await AddKnownProofAttributes(requestCollection, agentContext, knownProofAttributes,
                    proofRecord.ConnectionId);
            }
            else
            {
                try
                {
                    await _messageService.SendAsync(agentContext, presentation, service.RecipientKeys.First(),
                        service.ServiceEndpoint, service.RoutingKeys?.ToArray());
                }
                catch (AriesFrameworkException)
                {
                    BasicPopUp alert = new BasicPopUp(
                        Resources.Lang.PopUp_Proof_Sending_Failed_Title,
                        Resources.Lang.PopUp_Proof_Sending_Failed_Text,
                        Resources.Lang.PopUp_Proof_Sending_Failed_Button)
                    {
                        AlwaysDisplay = true
                    };
                    await alert.ShowPopUp();
                }
                catch (HttpRequestException ex) when (ex.Message == "No such host is known")
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Resources.Lang.PopUp_Network_Error_Title,
                        Resources.Lang.PopUp_Network_Error_Text,
                        Resources.Lang.PopUp_Network_Error_Button)
                    {
                        AlwaysDisplay = true
                    };
                    await popUp.ShowPopUp();
                }
            }

            return presentation;
        }

        public async Task GetCallCredentialsForProof(ObservableCollection<ProofModel> requestCollection,
            IAgentContext agentContext, Dictionary<string, RequestedAttribute> attributes,
            Dictionary<string, string> selfs, Dictionary<string, RequestedAttribute> predicates, long timestamp, ProofRequest proofRequest = null)
        {
            foreach (ProofModel currentRequest in requestCollection)
            {
                if (currentRequest.IsSelfAttested && currentRequest.SelectedOption == null)
                {
                    if (currentRequest.RequestedValue.Equals(WalletParams.HardwareSignature) || currentRequest.RequestedValue.Equals(WalletParams.HardwareSignatureDdl))
                    {
                        string hwAlias;
                        if (currentRequest.RequestedValue.Equals(WalletParams.HardwareSignature))
                        {
                            hwAlias = WalletParams.BaseIdAlias;
                        }
                        else
                        {
                            hwAlias = WalletParams.DdlAlias;
                        }
                        IHardwareKeyService hardwareKeyService = DependencyService.Resolve<IHardwareKeyService>();
                        string signature = hardwareKeyService.Sign(GetNonce(proofRequest.Nonce, 2), hwAlias);

                        selfs.Add(currentRequest.DictionaryKey, signature);
                    }
                    else
                    {
                        selfs.Add(currentRequest.DictionaryKey, currentRequest.SelectedValue);
                    }
                }
                else
                {
                    RequestedAttribute reqAttr = new RequestedAttribute
                    {
                        CredentialId = currentRequest.SelectedOption.WalletElement.CredentialRecord
                            .CredentialId
                    };

                    CredentialInfo credentialObject = new CredentialInfo();
                    try
                    {
                        credentialObject = JsonConvert.DeserializeObject<CredentialInfo>(
                            await AnonCreds.ProverGetCredentialAsync(agentContext.Wallet, reqAttr.CredentialId));
                    }
                    catch (Exception)
                    {
                        BasicPopUp alertPopUp = new BasicPopUp(
                            Resources.Lang.PopUp_Undefined_Error_Title,
                            Resources.Lang.PopUp_Undefined_Error_Message,
                            Resources.Lang.PopUp_Undefined_Error_Button)
                        {
                            AlwaysDisplay = true
                        };
                        await alertPopUp.ShowPopUp();
                    }

                    if (!string.IsNullOrEmpty(credentialObject.RevocationRegistryId))
                    {
                        reqAttr.Timestamp = timestamp;
                    }

                    if (currentRequest.RequestedValue.Contains("<") ||
                        currentRequest.RequestedValue.Contains("=") ||
                        currentRequest.RequestedValue.Contains(">"))
                    {
                        reqAttr.Revealed = false;
                        predicates.Add(currentRequest.DictionaryKey, reqAttr);
                    }
                    else
                    {
                        reqAttr.Revealed = true;
                        if (!attributes.ContainsKey(currentRequest.DictionaryKey))
                        {
                            attributes.Add(currentRequest.DictionaryKey, reqAttr);
                        }
                    }
                }
            }
        }

        private static byte[] GetNonce(string nonce, byte ending)
        {
            byte[] bytesNonce = Convert.FromBase64String(nonce);
            byte[] bytesConst = new byte[] { ending };
            byte[] bytesNonceAndConst = new byte[bytesNonce.Length + bytesConst.Length];
            Buffer.BlockCopy(bytesNonce, 0, bytesNonceAndConst, 0, bytesNonce.Length);
            Buffer.BlockCopy(bytesConst, 0, bytesNonceAndConst, bytesNonce.Length, bytesConst.Length);
            return Sha256.sha256(bytesNonceAndConst);
        }

        private bool EqualFilter(AttributeFilter filter1, AttributeFilter filter2)
        {
            if (string.IsNullOrEmpty(filter1.SchemaId))
            {
                if (!string.IsNullOrEmpty(filter2.SchemaId))
                {
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(filter2.SchemaId) || filter1.SchemaId != filter2.SchemaId)
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(filter1.SchemaIssuerDid))
            {
                if (!string.IsNullOrEmpty(filter2.SchemaIssuerDid))
                {
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(filter2.SchemaIssuerDid) || filter1.SchemaIssuerDid != filter2.SchemaIssuerDid)
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(filter1.SchemaName))
            {
                if (!string.IsNullOrEmpty(filter2.SchemaName))
                {
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(filter2.SchemaName) || filter1.SchemaName != filter2.SchemaName)
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(filter1.SchemaVersion))
            {
                if (!string.IsNullOrEmpty(filter2.SchemaVersion))
                {
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(filter2.SchemaVersion) || filter1.SchemaVersion != filter2.SchemaVersion)
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(filter1.IssuerDid))
            {
                if (!string.IsNullOrEmpty(filter2.IssuerDid))
                {
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(filter2.IssuerDid) || filter1.IssuerDid != filter2.IssuerDid)
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(filter1.CredentialDefinitionId))
            {
                if (!string.IsNullOrEmpty(filter2.CredentialDefinitionId))
                {
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(filter2.CredentialDefinitionId) ||
                    filter1.CredentialDefinitionId != filter2.CredentialDefinitionId)
                {
                    return false;
                }
            }

            if (filter1.AttributeValue == null)
            {
                if (filter2.AttributeValue != null)
                {
                    return false;
                }
            }
            else
            {
                if (filter2.AttributeValue != null)
                {
                    if (string.IsNullOrEmpty(filter1.AttributeValue.Name))
                    {
                        if (!string.IsNullOrEmpty(filter2.AttributeValue.Name))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(filter2.AttributeValue.Name) ||
                            filter1.AttributeValue.Name != filter2.AttributeValue.Name)
                        {
                            return false;
                        }
                    }

                    if (string.IsNullOrEmpty(filter1.AttributeValue.Value))
                    {
                        if (!string.IsNullOrEmpty(filter2.AttributeValue.Value))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(filter2.AttributeValue.Value) ||
                            filter1.AttributeValue.Value != filter2.AttributeValue.Value)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }


            return true;
        }
    }
}