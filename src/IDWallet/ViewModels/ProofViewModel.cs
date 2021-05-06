using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Agent.Services;
using IDWallet.Models;
using IDWallet.Services;
using IDWallet.Views.Wallet;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class ProofViewModel : CustomViewModel
    {
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly bool _onlyKnownProofs;
        private readonly string _proofRecordId;
        private readonly ProofRequest _proofRequest;
        private readonly ProofRequestService _proofRequestService = App.Container.Resolve<ProofRequestService>();
        private readonly CustomProofService _proofService = App.Container.Resolve<CustomProofService>();
        private readonly bool _takeNewest;
        private string _connectionAlias;
        private int _currentlyOpen;
        private bool _readyToSend;
        public ProofViewModel(ProofRequest proofRequest, string proofRecordId, bool onlyKnownProofs = false,
            bool takeNewest = true)
        {
            _proofRequest = proofRequest;
            _proofRecordId = proofRecordId;
            _onlyKnownProofs = onlyKnownProofs;
            _takeNewest = takeNewest;
            _readyToSend = false;
            RequestTitle = proofRequest.Name ?? "";
            IsSelfOpen = false;
            SelfAttestation = "";

            ConnectionAlias = "";
            if (string.IsNullOrEmpty(RequestTitle))
            {
                TitleVisibility = false;
            }
            else
            {
                TitleVisibility = true;
            }

            Requests = new ObservableCollection<ProofModel>();
            FailedRequests = new List<ProofModel>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadRequestsCommand());
        }

        public ConnectionRecord Connection { get; set; }
        public string ConnectionAlias
        {
            get => _connectionAlias;
            set => SetProperty(ref _connectionAlias, value);
        }

        public bool IsSelfOpen { get; set; }
        public bool LoadCommandFinished { get; set; }
        public Command LoadItemsCommand { get; set; }
        public bool ReadyToSend
        {
            get => _readyToSend;
            set => SetProperty(ref _readyToSend, value);
        }

        public ObservableCollection<ProofModel> Requests { get; set; }
        public List<ProofModel> FailedRequests { get; set; }
        public string RequestTitle { get; set; }
        public string SelfAttestation { get; set; }
        public bool TitleVisibility { get; set; }
        private Dictionary<string, Tuple<List<AttributeFilter>, string>> _knownProofAttributes { get; set; }
        public bool AuthSuccess { get; set; } = false;
        public bool AuthError { get; set; } = false;

        public async Task<PresentationMessage> CreateAndSendProof(CustomServiceDecorator service = null)
        {
            Hyperledger.Aries.Agents.IAgentContext agentContext = await _agentProvider.GetContextAsync();

            return await _proofRequestService.CreateAndSendProof(Requests, agentContext, _proofRecordId,
                _knownProofAttributes, service);
        }

        public async Task LoadAttributes(ProofModel request, ProofElementOption listViewOption)
        {
            foreach (ProofModel currentRequest in Requests)
            {
                if (currentRequest.DictionaryKey == request.DictionaryKey)
                {
                    foreach (ProofElementOption currentOption in currentRequest.ProofElementOptions)
                    {
                        if (currentOption.WalletElement.CredentialRecord.CredentialId ==
                            listViewOption.WalletElement.CredentialRecord.CredentialId)
                        {
                            listViewOption.Attributes.Add(new CredentialClaim
                            { Name = currentRequest.RequestedValue, Value = currentOption.Value });
                        }
                    }
                }
            }
        }

        public void SelectCredential(ProofElementOption listViewOption)
        {
            ProofModel currentRequest = Requests[_currentlyOpen];
            string oldCredDefId = "";
            IEnumerable<ProofModel> keyGroup = from request in Requests
                                               where request.DictionaryKey == currentRequest.DictionaryKey
                                               select request;

            if (currentRequest.SelectedOption != null)
            {
                oldCredDefId = currentRequest.SelectedOption.WalletElement.CredentialRecord
                    .CredentialDefinitionId;
            }

            string newCredDefId = listViewOption.WalletElement.CredentialRecord.CredentialDefinitionId;
            foreach (ProofModel request in keyGroup)
            {
                request.SelectedOption.IconSource = "mdi-checkbox-blank-circle-outline";
                request.SelectedOption = listViewOption;
                request.SelectedOption.IconSource = "mdi-circle-slice-8";

                string valueType = "";
                if (request.RequestedValue.Contains("<"))
                {
                    valueType = request.RequestedValue.Split('<')[0];
                    valueType = valueType.TrimEnd();
                }
                else
                {
                    if (request.RequestedValue.Contains(">"))
                    {
                        valueType = request.RequestedValue.Split('>')[0];
                        valueType = valueType.TrimEnd();
                    }
                    else
                    {
                        if (request.RequestedValue.Contains("="))
                        {
                            valueType = request.RequestedValue.Split('=')[0];
                            valueType = valueType.TrimEnd();
                        }
                        else
                        {
                            valueType = request.RequestedValue;
                        }
                    }
                }

                if (request.IsRegular)
                {
                    request.SelectedValue = (from attribute in listViewOption.WalletElement.Claims
                                             where attribute.Name == valueType
                                             select attribute.Value).First();
                }
                else if (request.IsEmbeddedImage)
                {
                    request.PortraitByteArray = listViewOption.WalletElement.PortraitByteArray;
                }
                else if (request.HasEmbeddedDocument)
                {
                    request.DocumentString = listViewOption.WalletElement.DocumentString;
                }

                request.IsSelected = true;

                request.Revoked = listViewOption.WalletElement.Revoked;
            }

            ReadyToSend = IsAvailable();
        }

        public void SetIndex(int index)
        {
            _currentlyOpen = index;
        }

        private async Task ExecuteLoadRequestsCommand()
        {
            TabbedPage mainPage = (TabbedPage)Application.Current.MainPage;
            WalletViewModel credentialsViewModel =
                ((WalletPage)((NavigationPage)mainPage.Children[0]).RootPage).ViewModel;

            Hyperledger.Aries.Agents.IAgentContext agentContext = await _agentProvider.GetContextAsync();
            ProofRecord proofRecord = await _proofService.GetAsync(agentContext, _proofRecordId);

            ConnectionRecord connectionRecord = null;
            if (proofRecord.ConnectionId != null)
            {
                try
                {
                    connectionRecord = await _connectionService.GetAsync(agentContext, proofRecord.ConnectionId);
                }
                catch (Exception)
                {
                    connectionRecord = null;
                }
            }

            Connection = connectionRecord;
            if (Connection == null)
            {
                ConnectionAlias = Resources.Lang.ProofRequestPage_Unknown_Connection_Alias;
            }
            else
            {
                try
                {
                    ConnectionAlias = Connection.Alias.Name;
                }
                catch (Exception)
                {
                    ConnectionAlias = Resources.Lang.ProofRequestPage_Unknown_Connection_Alias;
                }
            }

            _knownProofAttributes = new Dictionary<string, Tuple<List<AttributeFilter>, string>>();

            string knownProofAttributes = null;
            if (connectionRecord != null)
            {
                try
                {
                    knownProofAttributes = connectionRecord.GetTag("KnownProofAttributes");
                }
                catch (Exception)
                {
                    //ignore
                }
            }

            if (!string.IsNullOrEmpty(knownProofAttributes))
            {
                try
                {
                    _knownProofAttributes =
                        JsonConvert.DeserializeObject<Dictionary<string,
                            Tuple<List<AttributeFilter>, string>>>(knownProofAttributes);
                }
                catch (Exception)
                {
                    //ignore
                }
            }

            bool needToLoad = true;
            if (_onlyKnownProofs)
            {
                if (_knownProofAttributes.Any())
                {
                    try
                    {
                        needToLoad =
                            _proofRequestService.CheckKnownProofAttributes(_knownProofAttributes, _proofRequest);
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
                else
                {
                    needToLoad = false;
                    ReadyToSend = false;
                }
            }

            if (needToLoad)
            {
                List<WalletElement> allIssuedCredentials = new List<WalletElement>();
                foreach (WalletElement credential in credentialsViewModel.WalletElements)
                {
                    if (credential.CredentialRecord.State == CredentialState.Issued)
                    {
                        allIssuedCredentials.Add(credential);
                    }
                }

                foreach (KeyValuePair<string, ProofAttributeInfo> attribute in _proofRequest.RequestedAttributes)
                {
                    string name = "";
                    if (!string.IsNullOrEmpty(attribute.Value.Name))
                    {
                        name = attribute.Value.Name;

                        await _proofRequestService.AddCredentialItem(Requests, agentContext, attribute.Value,
                            attribute.Key, allIssuedCredentials, name, _proofRequest, _proofRecordId, _connectionAlias,
                            _knownProofAttributes, _takeNewest);
                    }
                    else if (attribute.Value.Names != null && attribute.Value.Names.Count() > 0)
                    {
                        int length = attribute.Value.Names.Count();
                        for (int i = 0; i < length; i++)
                        {
                            await _proofRequestService.AddCredentialItem(Requests, agentContext, attribute.Value,
                                attribute.Key, allIssuedCredentials, attribute.Value.Names[i], _proofRequest,
                                _proofRecordId, _connectionAlias, _knownProofAttributes, _takeNewest);
                        }
                    }
                }

                foreach (KeyValuePair<string, ProofPredicateInfo> predicate in _proofRequest.RequestedPredicates)
                {
                    string name = "";
                    if (!string.IsNullOrEmpty(predicate.Value.Name))
                    {
                        name = predicate.Value.Name + " "
                                                    + predicate.Value.PredicateType + " "
                                                    + predicate.Value.PredicateValue;

                        await _proofRequestService.AddCredentialItem(Requests, agentContext, predicate.Value,
                            predicate.Key, allIssuedCredentials, name, _proofRequest, _proofRecordId, _connectionAlias,
                            _knownProofAttributes, _takeNewest);
                    }
                    else if (predicate.Value.Names != null && predicate.Value.Names.Count() > 0)
                    {
                        int length = predicate.Value.Names.Count();
                        for (int i = 0; i < length; i++)
                        {
                            name = predicate.Value.Names[i] + " "
                                                            + predicate.Value.PredicateType + " "
                                                            + predicate.Value.PredicateValue;

                            await _proofRequestService.AddCredentialItem(Requests, agentContext, predicate.Value,
                                predicate.Key, allIssuedCredentials, predicate.Value.Names[i], _proofRequest,
                                _proofRecordId, _connectionAlias, _knownProofAttributes, _takeNewest);
                        }
                    }
                }

                ReadyToSend = IsAvailable();
            }

            LoadCommandFinished = true;
        }

        public bool IsAvailable()
        {
            foreach (ProofModel proofModel in Requests)
            {
                if (!proofModel.IsSelected)
                {
                    FailedRequests.Add(proofModel);
                }
            }

            return !FailedRequests.Any();
        }
    }
}