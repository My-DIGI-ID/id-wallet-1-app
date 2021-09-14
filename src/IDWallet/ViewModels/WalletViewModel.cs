using Autofac;
using IDWallet.Agent;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Events;
using IDWallet.Models;
using IDWallet.Services;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Inbox.Content;
using IDWallet.Views.Login;
using IDWallet.Views.Proof;
using IDWallet.Views.Settings;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Decorators;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Hyperledger.Indy.AnonCredsApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class WalletViewModel : CustomViewModel
    {
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly CheckRevocationService _checkRevocationService =
            App.Container.Resolve<CheckRevocationService>();

        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();
        private readonly IProofService _proofService = App.Container.Resolve<IProofService>();

        private readonly ICustomWalletRecordService _walletRecordService =
            App.Container.Resolve<ICustomWalletRecordService>();

        private ObservableCollection<WalletElement> _walletElements;
        private int _walletElementPosition;

        private bool _emptyLayoutVisible;
        private bool _addBaseIdIsVisible;

        private double _historyHeight;
        private bool _isCheckingRevocationStatus = false;
        private Command _openPdfButtonClickedCommand;
        private ImageSource _portrait;
        public WalletViewModel()
        {
            Title = "Credentials";
            WalletElements = new ObservableCollection<WalletElement>();
            EmptyLayoutVisible = true;
            AddBaseIdIsVisible = true;
            LoadItemsCommand = new Command(async () => await ExecuteLoadWalletElements());
            _allProofs = new List<(CredentialHistoryElements, ProofRecord)>();

            DisableNotificationAlert();
            DisableNotificationsCommand = new Command(DisableNotificationAlert);

            Subscribe();
        }

        private void Subscribe()
        {
            MessagingCenter.Subscribe<AutoAcceptViewModel>(this, WalletEvents.OfferRejected, OnOfferRejected);
            MessagingCenter.Subscribe<AutoAcceptViewModel, string>(this, WalletEvents.SentProofRequest, OnProofSent);
            MessagingCenter.Subscribe<CustomAgentProvider>(this, WalletEvents.AgentSwitched, ReloadWalletElements);
            MessagingCenter.Subscribe<BaseIdViewModel>(this, WalletEvents.ReloadCredentials, ReloadWalletElements);
            MessagingCenter.Subscribe<AddVacService>(this, WalletEvents.ReloadCredentials, ReloadWalletElements);
            MessagingCenter.Subscribe<AddVacService, string>(this, WalletEvents.VacAdded, OnVacQrAdded);
            MessagingCenter.Subscribe<InboxListElement, string>(this, WalletEvents.CredentialDeleted,
                OnWalletElementDeleted);
            MessagingCenter.Subscribe<LoginPage>(this, WalletEvents.AppStarted, ReloadWalletElements);
            MessagingCenter.Subscribe<LoginViewModel>(this, WalletEvents.AppStarted, ReloadWalletElements);
            MessagingCenter.Subscribe<ProofPopUp, string>(this, WalletEvents.SentProofRequest, OnProofSent);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.IssueCredentialNames.IssueCredential, OnCredentialIssued);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.IssueCredentialNames.IssueCredential, OnCredentialIssued);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.IssueCredentialNames.OfferCredential, OnCredentialOffer);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.IssueCredentialNames.OfferCredential, OnCredentialOffer);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.PresentProofNames.RequestPresentation, OnNewProof);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.PresentProofNames.RequestPresentation, OnNewProof);
            MessagingCenter.Subscribe<SettingsPage>(this, WalletEvents.ToggleUseMediatorImages, ReloadWalletElements);
        }

        public ObservableCollection<WalletElement> WalletElements
        {
            get => _walletElements;
            set => SetProperty(ref _walletElements, value);
        }

        public int WalletElementPosition
        {
            get => _walletElementPosition;
            set => SetProperty(ref _walletElementPosition, value);
        }

        public bool EmptyLayoutVisible
        {
            get => _emptyLayoutVisible;
            set => SetProperty(ref _emptyLayoutVisible, value);
        }

        public bool AddBaseIdIsVisible
        {
            get => _addBaseIdIsVisible;
            set => SetProperty(ref _addBaseIdIsVisible, value);
        }

        public double HistoryHeight
        {
            get => _historyHeight;
            set => SetProperty(ref _historyHeight, value);
        }

        public Command LoadItemsCommand { get; set; }
        public Command OpenPdfButtonClickedCommand =>
            _openPdfButtonClickedCommand ??= new Command(OpenPdfButtonClicked);

        public ImageSource Portrait
        {
            get => _portrait;
            set => SetProperty(ref _portrait, value);
        }

        private List<(CredentialHistoryElements, ProofRecord)> _allProofs { get; set; }

        public async void CheckAllRevocations()
        {
            if (_isCheckingRevocationStatus)
            {
                return;
            }

            _isCheckingRevocationStatus = true;

            try
            {
                NetworkAccess connectivity = Connectivity.NetworkAccess;
                if (connectivity != NetworkAccess.ConstrainedInternet && connectivity != NetworkAccess.Internet)
                {
                    return;
                }

                bool updateRecord = false;
                IAgentContext agentContext = await _agentProvider.GetContextAsync();

                foreach (WalletElement credential in WalletElements.ToList().Where(x => x.CredentialRecord != null))
                {
                    CredentialInfo credentialInfo = new CredentialInfo();
                    try
                    {
                        credentialInfo = JsonConvert.DeserializeObject<CredentialInfo>(
                            await AnonCreds.ProverGetCredentialAsync(agentContext.Wallet,
                                credential.CredentialRecord.Id));
                    }
                    catch (Exception)
                    {
                        //ignore
                    }

                    credential.Revoked =
                        !await _checkRevocationService.NonRevoked(credentialInfo, credential.CredentialRecord);

                    bool oldStatus = false;
                    try
                    {
                        oldStatus = bool.Parse(
                            credential.CredentialRecord.GetTag(WalletParams.IsRevokedOnLedgerTag));
                    }
                    catch (Exception)
                    {
                        updateRecord = true;
                    }

                    if (oldStatus != credential.Revoked)
                    {
                        updateRecord = true;
                    }

                    if (updateRecord)
                    {
                        credential.CredentialRecord.SetTag(WalletParams.IsRevokedOnLedgerTag,
                            credential.Revoked.ToString());

                        await _walletRecordService.UpdateAsync(agentContext.Wallet, credential.CredentialRecord);
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
            finally
            {
                _isCheckingRevocationStatus = false;
            }
        }

        private void CheckForBaseId()
        {
            foreach (WalletElement walletElement in WalletElements.Where(x => x.CredentialRecord != null))
            {
                if (walletElement.CredentialRecord.CredentialDefinitionId == "Vq2C7Wfc44Q1cSroPuXaw2:3:CL:1128:Basis-ID" || walletElement.CredentialRecord.CredentialDefinitionId == "MGfd8JjWRoiXMm2YGL4SGj:3:CL:43:Basis-ID Testnetzwerk")
                {
                    AddBaseIdIsVisible = false;
                    return;
                }
                else
                {
                    AddBaseIdIsVisible = true;
                }
            }
        }

        public async Task DeleteWalletElement(string recordId)
        {
            try
            {
                IAgentContext agentContext = await _agentProvider.GetContextAsync();
                await _credentialService.DeleteCredentialAsync(agentContext, recordId);

                WalletElement walletElement = null;
                try
                {
                    walletElement = WalletElements.First(x => x.CredentialRecord != null && x.CredentialRecord.Id == recordId);
                }
                catch
                {
                }

                if (walletElement != null)
                {
                    if (WalletElements.Count == 1)
                    {
                        EmptyLayoutVisible = true;
                    }

                    WalletElements.Remove(walletElement);
                }
            }
            catch
            {
            }

            CheckForBaseId();
        }

        public async Task DeleteWalletQrElement(string recordId)
        {
            IAgentContext agentContext = await _agentProvider.GetContextAsync();
            await _walletRecordService.DeleteAsync<VacQrCredential>(agentContext.Wallet, recordId);

            WalletElement walletElement = null;
            try
            {
                walletElement = WalletElements.First(x => x.VacQrRecordId == recordId);
            }
            catch
            {
            }

            if (walletElement != null)
            {
                if (WalletElements.Count == 1)
                {
                    EmptyLayoutVisible = true;
                }

                WalletElements.Remove(walletElement);
            }
        }

        public void EnableNotification()
        {
            EnableNotificationAlert();
        }

        public async Task ExecuteLoadWalletElements()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            App.CredentialsLoaded = false;

            try
            {
                await LoadHistoryElements();
            }
            catch (Exception)
            {
                //ignore
            }

            try
            {
                WalletElements.Clear();

                IAgentContext agentContext = await _agentProvider.GetContextAsync();

                ISearchQuery[] credentialQuery = new[]
                {
                    SearchQuery.Equal(nameof(CredentialRecord.State), CredentialState.Issued.ToString())
                };
                List<CredentialRecord> allIssuedCredentials =
                    await _credentialService.ListAsync(agentContext, credentialQuery[0], count: 2147483647);

                foreach (CredentialRecord credential in allIssuedCredentials)
                {
                    try
                    {
                        await AddWalletElement(credential);
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }

                List<VacQrCredential> allVacQrCredentials =
                    await _walletRecordService.SearchAsync<VacQrCredential>(agentContext.Wallet, null, null, 2147483647, false);

                foreach (VacQrCredential vacQrCredential in allVacQrCredentials)
                {
                    try
                    {
                        await AddVacQrElement(vacQrCredential);
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }

                CheckAllRevocations();
                CheckForBaseId();
            }
            catch (Exception)
            {
                //ignore
            }
            finally
            {
                EmptyLayoutVisible = !WalletElements.Any();
                IsLoading = false;
                App.CredentialsLoaded = true;
            }
        }

        public void SetHistory(WalletElement credentialsPageItem)
        {
            if (credentialsPageItem.IsHistorySet)
            {
                return;
            }

            try
            {
                ObservableCollection<HistoryProofElement> history = new ObservableCollection<HistoryProofElement>();
                ObservableCollection<(CredentialHistoryElements, ProofRecord)> relevantProofs =
                    new ObservableCollection<(CredentialHistoryElements, ProofRecord)>();

                foreach ((CredentialHistoryElements presentedCredential, ProofRecord proofRecord) in _allProofs)
                {
                    if (presentedCredential.CredentialRecordIds != null &&
                        presentedCredential.CredentialRecordIds.Contains(credentialsPageItem.CredentialRecord
                            .CredentialId))
                    {
                        relevantProofs.Add((presentedCredential, proofRecord));
                    }
                }

                foreach ((CredentialHistoryElements presentedCredentials, ProofRecord proofRecord) in relevantProofs)
                {
                    List<CredentialClaim> revealedList = (from claim in presentedCredentials.RevealedClaims
                                                          where claim.CredentialRecordId == credentialsPageItem.CredentialRecord.CredentialId
                                                          select claim).ToList().OrderBy(x => x.Name).ToList();
                    ObservableCollection<CredentialClaim> revealed = new ObservableCollection<CredentialClaim>();
                    foreach (CredentialClaim claim in revealedList)
                    {
                        revealed.Add(claim);
                    }

                    List<CredentialClaim> nonrevealedList = (from claim in presentedCredentials.NonRevealedClaims
                                                             where claim.CredentialRecordId == credentialsPageItem.CredentialRecord.CredentialId
                                                             select claim).ToList().OrderBy(x => x.Name).ToList();
                    ObservableCollection<CredentialClaim> nonrevealed = new ObservableCollection<CredentialClaim>();
                    foreach (CredentialClaim claim in nonrevealedList)
                    {
                        nonrevealed.Add(claim);
                    }

                    List<CredentialClaim> predicatesList = (from claim in presentedCredentials.PredicateClaims
                                                            where claim.CredentialRecordId == credentialsPageItem.CredentialRecord.CredentialId
                                                            select claim).ToList().OrderBy(x => x.Name).ToList();
                    ObservableCollection<CredentialClaim> predicates = new ObservableCollection<CredentialClaim>();
                    foreach (CredentialClaim claim in predicatesList)
                    {
                        predicates.Add(new CredentialClaim
                        {
                            Name = claim.Name,
                            CredentialRecordId = claim.CredentialRecordId,
                            Value = claim.PredicateType + " " + claim.Value,
                            PredicateType = claim.PredicateType
                        });
                    }

                    string serviceAlias = "";
                    if (string.IsNullOrEmpty(presentedCredentials.ConnectionRecord?.Alias.Name))
                    {
                        CustomServiceDecorator service = proofRecord.GetTag(DecoratorNames.ServiceDecorator)
                            .ToObject<CustomServiceDecorator>();
                        Uri endpointUri = new Uri(service.ServiceEndpoint);
                        serviceAlias = !string.IsNullOrEmpty(service.EndpointName) ? service.EndpointName + " - " + endpointUri.Host : service.ServiceEndpoint;
                    }
                    else
                    {
                        serviceAlias = presentedCredentials.ConnectionRecord?.Alias.Name;
                    }

                    HistoryProofElement historyItem = new HistoryProofElement
                    {
                        CredentialName = credentialsPageItem.Name,
                        ConnectionAlias = serviceAlias,
                        ImageUri = string.IsNullOrEmpty(presentedCredentials.ConnectionRecord?.Alias.ImageUrl)
                            ? ImageSource.FromFile("default_logo.png")
                            : new Uri(presentedCredentials.ConnectionRecord.Alias.ImageUrl),
                        CreatedAtUtc = proofRecord.CreatedAtUtc,
                        State = Resources.Lang.WalletPage_History_Panel_Status_Shared,
                        RevealedClaims = revealed,
                        NonRevealedClaims = nonrevealed,
                        SelfAttestedClaims = new ObservableCollection<CredentialClaim>(),
                        PredicateClaims = predicates
                    };
                    history.Add(historyItem);
                }

                history = new ObservableCollection<HistoryProofElement>(
                    history.OrderByDescending(x => x.CreatedAtUtc.Value));
                foreach (HistoryProofElement credentialHistoryItem in history)
                {
                    credentialsPageItem.HistoryItems.Add(credentialHistoryItem);
                    HistoryHeight += 63.6;
                }

                credentialsPageItem.IsHistorySet = true;
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private async Task AddWalletElement(CredentialRecord credentialRecord)
        {
            IAgentContext agentContext = await _agentProvider.GetContextAsync();

            WalletElement result = new WalletElement
            {
                CredentialRecord = credentialRecord
            };

            string[] credDefId = credentialRecord.CredentialDefinitionId.Split(':');

            result.Name = credDefId[4];

            result.Claims = new ObservableCollection<CredentialClaim>();
            result.QrCodes = new ObservableCollection<CredentialClaim>();
            List<CredentialClaim> temporaryAttributes = new List<CredentialClaim>();

            CredentialInfo credentialInfo = new CredentialInfo();
            try
            {
                credentialInfo = JsonConvert.DeserializeObject<CredentialInfo>(
                    await AnonCreds.ProverGetCredentialAsync(agentContext.Wallet, credentialRecord.CredentialId));
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Undefined_Error_Title,
                    Resources.Lang.PopUp_Undefined_Error_Add_Credential_Page_Item_Message,
                    Resources.Lang.PopUp_Undefined_Error_Button);
                await alertPopUp.ShowPopUp();
            }

            foreach (KeyValuePair<string, string> credentialAttribute in credentialInfo.Attributes)
            {
                if (credentialAttribute.Key == "embeddedImage")
                {
                    try
                    {
                        string portraitString = credentialAttribute.Value;
                        result.PortraitByteArray = Convert.FromBase64String(portraitString);
                        result.HasImage = true;
                    }
                    catch (Exception)
                    {
                        result.PortraitByteArray = null;
                        result.HasImage = false;
                    }
                }
                else if (credentialAttribute.Key == "embeddedDocument")
                {
                    result.HasDocument = true;
                    result.DocumentString = credentialAttribute.Value;
                }
                else
                {
                    temporaryAttributes.Add(new CredentialClaim
                    {
                        Name = credentialAttribute.Key,
                        Value = credentialAttribute.Value
                    });
                }
            }

            string connectionAlias = "";
            bool recordNotFound = false;
            try
            {
                connectionAlias = credentialRecord.GetTag("ConnectionAlias");
                if (string.IsNullOrEmpty(connectionAlias))
                {
                    ConnectionRecord connection =
                        await _connectionService.GetAsync(agentContext, credentialRecord.ConnectionId);
                    connectionAlias = connection.Alias.Name;
                    credentialRecord.SetTag("ConnectionAlias", connection.Alias.Name);
                    await _walletRecordService.UpdateAsync(agentContext.Wallet, credentialRecord);
                }
                else
                {
                    ConnectionRecord connection =
                        await _connectionService.GetAsync(agentContext, credentialRecord.ConnectionId);
                    if (connectionAlias != connection.Alias.Name)
                    {
                        connectionAlias = connection.Alias.Name;
                        credentialRecord.SetTag("ConnectionAlias", connection.Alias.Name);
                        await _walletRecordService.UpdateAsync(agentContext.Wallet, credentialRecord);
                    }
                }
            }
            catch (AriesFrameworkException ex) when (ex.ErrorCode == ErrorCode.RecordNotFound)
            {
                recordNotFound = true;
            }
            catch (Exception)
            {
                //ignore
            }

            if (recordNotFound && string.IsNullOrEmpty(connectionAlias))
            {
                try
                {
                    connectionAlias = credentialRecord.ConnectionId;
                    credentialRecord.SetTag("ConnectionAlias", credentialRecord.ConnectionId);
                    await _walletRecordService.UpdateAsync(agentContext.Wallet, credentialRecord);
                }
                catch (Exception)
                {
                    //ignore
                }
            }

            string issuedByValue = "";
            if (recordNotFound)
            {
                issuedByValue = connectionAlias + " (" + Resources.Lang.WalletPage_Info_Panel_Deleted + ")";
            }
            else
            {
                issuedByValue = connectionAlias;
            }

            if (IsBaseIdCredential(credentialRecord))
            {
                ObservableCollection<string> orderedAttributeNames = new ObservableCollection<string> { "firstname", "familyname", "birthname", "academictitle", "addressstreet", "addresszipcode", "addresscity", "addresscountry", "dateofbirth", "placeofbirth", "dateofexpiry", "documenttype", "pseudonym" };
                foreach (string attributeName in orderedAttributeNames)
                {
                    CredentialClaim tmpAttribute = temporaryAttributes.FirstOrDefault(x => x.Name.ToLower().Equals(attributeName));
                    if (tmpAttribute != null)
                    {
                        result.Claims.Add(tmpAttribute);
                        temporaryAttributes.Remove(tmpAttribute);
                    }
                }
            }

            if (IsArbeitgeberCredential(credentialRecord))
            {
                ObservableCollection<string> orderedAttributeNames = new ObservableCollection<string> { "firstname", "lastname", "firmname", "firmsubject", "firmstreet", "firmpostalcode", "firmcity" };
                foreach (string attributeName in orderedAttributeNames)
                {
                    CredentialClaim tmpAttribute = temporaryAttributes.FirstOrDefault(x => x.Name.ToLower().Equals(attributeName));
                    if (tmpAttribute != null)
                    {
                        result.Claims.Add(tmpAttribute);
                        temporaryAttributes.Remove(tmpAttribute);
                    }
                }
            }

            if (IsFuehrerscheinCredential(credentialRecord))
            {
                ObservableCollection<string> orderedAttributeNames = new ObservableCollection<string> { "name", "geburtsname", "vorname", "geburtsdatum", "geburtsort", "ausstellungsdatum", "ablaufdatum", "aussteller", "führerscheinnummer", "führerscheinklassen", "gültigab", "gültigbis", "beschränkungen" };
                foreach (string attributeName in orderedAttributeNames)
                {
                    CredentialClaim tmpAttribute = temporaryAttributes.FirstOrDefault(x => x.Name.ToLower().Equals(attributeName));
                    if (tmpAttribute != null)
                    {
                        result.Claims.Add(tmpAttribute);
                        temporaryAttributes.Remove(tmpAttribute);
                    }
                }
            }

            temporaryAttributes = temporaryAttributes.OrderBy(x => x.Name).ToList();
            foreach (CredentialClaim attribute in temporaryAttributes)
            {
                if (!attribute.Name.ToLower().Equals("dgc"))
                {
                    result.Claims.Add(attribute);
                }
                else
                {
                    result.QrCodes.Add(attribute);
                }
            }
            if (!result.QrCodes.Count.Equals(0))
            {
                result.QrCodesVisible = true;
            }

            if (IsBaseIdCredential(credentialRecord))
            {
                try
                {
                    ConnectionRecord connection =
                        await _connectionService.GetAsync(agentContext, credentialRecord.ConnectionId);
                    string revocationPassphrase = connection.GetTag(WalletParams.KeyRevocationPassphrase);
                    if (!string.IsNullOrEmpty(revocationPassphrase))
                    {
                        result.Claims.Add(new CredentialClaim
                        {
                            Name = Resources.Lang.WalletPage_Info_Panel_LockPIN,
                            Value = revocationPassphrase
                        });
                    }
                }
                catch
                {
                    //ignore
                }
            }

            result.Claims.Add(new CredentialClaim
            {
                Name = Resources.Lang.WalletPage_Info_Panel_Origin,
                Value = issuedByValue
            });
            result.Claims.Add(new CredentialClaim
            {
                Name = Resources.Lang.WalletPage_Info_Panel_Issue_Time,
                Value = $"{credentialRecord.CreatedAtUtc:dd/MM/yyyy}"
            });

            foreach (CredentialClaim claim in result.Claims)
            {
                claim.SchemaId = credentialRecord.SchemaId;
            }

            result.HistoryItems = new ObservableCollection<HistoryProofElement>();
            result.IsHistoryOpen = false;
            result.IsHistorySet = false;
            SetHistory(result);

            result.IsInfoOpen = false;

            try
            {
                result.Revoked = bool.Parse(credentialRecord.GetTag(WalletParams.IsRevokedOnLedgerTag));
            }
            catch (Exception)
            {
                result.Revoked = false;
            }

            switch (credentialRecord.CredentialDefinitionId.Split(':')[0])
            {
                case "JiVLsA5wxVnbHQ5s7pDN58":
                    result.CredentialImageSource = ImageSource.FromFile("ibm_logo.png");
                    result.ImageUri = ImageSource.FromFile("ibm_logo.png");
                    result.CredentialBarColor = Color.FromHex("#0F62FE");
                    break;
                case "Vq2C7Wfc44Q1cSroPuXaw2":
                case "MGfd8JjWRoiXMm2YGL4SGj":
                    result.CredentialImageSource = ImageSource.FromFile("bdr_logo.png");
                    result.ImageUri = ImageSource.FromFile("bdr_logo.png");
                    result.CredentialBarColor = Color.FromHex("#f9f9e0");
                    break;
                case "En38baYaTqVYSB8SFwguhT":
                case "9HX4bs8pdH2uJB7sjeWPtU":
                    result.CredentialImageSource = ImageSource.FromFile("bosch_logo.png");
                    result.ImageUri = ImageSource.FromFile("bosch_logo.png");
                    result.CredentialBarColor = Color.FromHex("#ffffff");
                    break;
                case "VkVqDPzeDCQe31H3RsMzbf":
                case "EKtoaKk2ifgmY4cQxYAwcE":
                    result.CredentialImageSource = ImageSource.FromFile("dbahn_white_logo.png");
                    result.ImageUri = ImageSource.FromFile("dbahn_white_logo.png");
                    result.CredentialBarColor = Color.FromHex("#ec0016");
                    break;
                case "7vyyugPwC3ArWtRWbz6LCm":
                case "PsDLsaget7L9duoaxzC2DZ":
                    result.CredentialImageSource = ImageSource.FromFile("bwi_logo.png");
                    result.ImageUri = ImageSource.FromFile("bwi_logo.png");
                    result.CredentialBarColor = Color.FromHex("#ffffff");
                    break;
                case "X2p16G1BeEceJauzqofjQW":
                    result.CredentialImageSource = ImageSource.FromFile("dlufthansa_logo.png");
                    result.ImageUri = ImageSource.FromFile("dlufthansa_logo.png");
                    result.CredentialBarColor = Color.FromHex("#ffffff");
                    break;
                default:
                    if (credentialRecord.CredentialDefinitionId.Equals("XnGEZ7gJxDNfxwnZpkkVcs:3:CL:988:Digitaler Führerschein"))
                    {
                        result.CredentialImageSource = ImageSource.FromFile("kba_logo.png");
                        result.ImageUri = ImageSource.FromFile("kba_logo.png");
                        result.CredentialBarColor = Color.FromHex("#eaeaea");
                    }
                    else
                    {
                        result.CredentialImageSource = ImageSource.FromFile("default_logo.png");
                        result.ImageUri = ImageSource.FromFile("default_logo.png");
                        result.CredentialBarColor = Color.FromHex("#cfcfcf");
                    }
                    break;
            }

            result.IssuedBy = issuedByValue;

            WalletElements.Add(result);
        }

        private async Task AddVacQrElement(VacQrCredential vacQrCredential)
        {
            IAgentContext agentContext = await _agentProvider.GetContextAsync();

            WalletElement result = new WalletElement();

            result.Name = vacQrCredential.Name;
            result.VacQrRecordId = vacQrCredential.Id;

            result.Claims = new ObservableCollection<CredentialClaim>();
            result.QrCodes = new ObservableCollection<CredentialClaim>();

            //if (IsVaccinePassCredential(credentialRecord))
            //{
            //    ObservableCollection<string> orderedAttributeNames = new ObservableCollection<string> { "nam_gn", "nam_fn", "dob", "vac_tg", "vac_vp", "vac_ma", "vac_dn", "vac_sd", "vac_dt", "vac_co", "vac_is", "vac_ci", "nam_fnt", "nam_gnt", "ver", "vac_mp" };
            //    foreach (string attributeName in orderedAttributeNames)
            //    {
            //        CredentialClaim tmpAttribute = temporaryAttributes.FirstOrDefault(x => x.Name.ToLower().Equals(attributeName));
            //        if (tmpAttribute != null)
            //        {
            //            result.Claims.Add(tmpAttribute);
            //            temporaryAttributes.Remove(tmpAttribute);
            //        }
            //    }
            //}

            CredentialClaim vacQrAttribute = new CredentialClaim();
            vacQrAttribute.Value = vacQrCredential.QrContent;
            result.QrCodes.Add(vacQrAttribute);

            result.QrCodesVisible = true;

            result.HistoryItems = new ObservableCollection<HistoryProofElement>();
            result.IsHistoryOpen = false;
            result.IsHistorySet = false;

            result.IsInfoOpen = true;

            result.ImageUri = ImageSource.FromFile("qr_code.png");
            result.CredentialImageSource = ImageSource.FromFile("qr_code.png");
            result.CredentialBarColor = Color.FromHex("#cfcfcf");

            result.IssuedBy = "";

            WalletElements.Add(result);
        }

        private async Task LoadHistoryElements()
        {
            _allProofs.Clear();
            IAgentContext agentContext = await _agentProvider.GetContextAsync();
            List<ProofRecord> allProofRecords = await _proofService.ListAsync(agentContext,
                SearchQuery.Equal(nameof(ProofRecord.State), ProofState.Accepted.ToString()), count: 2147483647);
            foreach (ProofRecord proof in allProofRecords)
            {
                CredentialHistoryElements credentialHistoryElements = new CredentialHistoryElements();
                try
                {
                    credentialHistoryElements =
                        JsonConvert.DeserializeObject<CredentialHistoryElements>(proof.GetTag(WalletParams.HistoryCredentialsTag));
                }
                catch (Exception)
                {
                    //ignore
                }

                _allProofs.Add((credentialHistoryElements, proof));
            }
        }

        private async void OnCredentialIssued(ServiceMessageEventService msg, string recordId)
        {
            try
            {
                DisableNotificationAlert();
                IAgentContext context = await _agentProvider.GetContextAsync();
                CredentialRecord credential =
                    await _walletRecordService.GetAsync<CredentialRecord>(context.Wallet, recordId);
                if (credential.State == CredentialState.Issued)
                {
                    IEnumerable<WalletElement> contains = WalletElements.ToList().Where(x => x.CredentialRecord != null && x.CredentialRecord.Id == credential.Id);


                    if (contains != null && contains.Any())
                    {
                        WalletElements.Remove(contains.First());
                    }

                    await AddWalletElement(credential);
                    EmptyLayoutVisible = false;

                    CheckAllRevocations();
                }
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

        private async void OnVacQrAdded(AddVacService msg, string recordId)
        {
            try
            {
                IAgentContext context = await _agentProvider.GetContextAsync();
                VacQrCredential credential = await _walletRecordService.GetAsync<VacQrCredential>(context.Wallet, recordId);

                await AddVacQrElement(credential);
                EmptyLayoutVisible = false;
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private async void OnCredentialOffer(ServiceMessageEventService arg1, string recordId)
        {
            IAgentContext context = await _agentProvider.GetContextAsync();
            CredentialRecord credentialRecord = await _credentialService.GetAsync(context, recordId);
            ConnectionRecord connectionRecord = null;
            if (credentialRecord.ConnectionId != null)
            {
                connectionRecord = await _connectionService.GetAsync(context, credentialRecord.ConnectionId);
            }

            if (connectionRecord != null && Convert.ToBoolean(connectionRecord.GetTag("AutoAcceptCredential")))
            {
                if (Convert.ToBoolean(connectionRecord.GetTag("AutoCredentialNotification")))
                {
                    EnableNotificationAlert();
                }
            }
            else
            {
                EnableNotificationAlert();
            }
        }

        private async void OnWalletElementDeleted(object arg1, string recordId)
        {
            await DeleteWalletElement(recordId);
        }

        private async void OnNewProof(ServiceMessageEventService arg1, string recordId)
        {
            IAgentContext context = await _agentProvider.GetContextAsync();
            ProofRecord proofRecord = await _proofService.GetAsync(context, recordId);
            ConnectionRecord connectionRecord = proofRecord.ConnectionId != null
                ? await _connectionService.GetAsync(context, proofRecord.ConnectionId)
                : null;

            if (connectionRecord != null)
            {
                if (Convert.ToBoolean(connectionRecord.GetTag("AutoAcceptProof")))
                {
                    if (Convert.ToBoolean(connectionRecord.GetTag("AutoProofNotification")))
                    {
                        EnableNotificationAlert();
                    }
                }
                else
                {
                    EnableNotificationAlert();
                }
            }
            else
            {
                EnableNotificationAlert();
            }
        }

        private void OnOfferRejected(AutoAcceptViewModel obj)
        {
            DisableNotificationAlert();
        }

        private async void OnProofSent(ProofPopUp sender, string recordId)
        {
            try
            {
                DisableNotificationAlert();
                IAgentContext context = await _agentProvider.GetContextAsync();
                ProofRecord proofRecord = await _proofService.GetAsync(context, recordId);

                CredentialHistoryElements presentedCredentials = new CredentialHistoryElements();
                try
                {
                    presentedCredentials =
                        JsonConvert.DeserializeObject<CredentialHistoryElements>(proofRecord.GetTag(WalletParams.HistoryCredentialsTag));
                }
                catch (Exception)
                {
                    BasicPopUp alertPopUp = new BasicPopUp(
                        Resources.Lang.PopUp_Undefined_Error_Title,
                        Resources.Lang.PopUp_Undefined_Error_Message,
                        Resources.Lang.PopUp_Undefined_Error_Button);
                    await alertPopUp.ShowPopUp();
                }

                _allProofs.Add((presentedCredentials, proofRecord));
                IEnumerable<WalletElement> credentialsPageItems =
                    from credentialPageItem in WalletElements
                    where presentedCredentials.CredentialRecordIds.Contains(credentialPageItem.CredentialRecord
                        .Id)
                    select credentialPageItem;
                foreach (WalletElement credentialsPageItem in credentialsPageItems)
                {
                    if (!credentialsPageItem.IsHistorySet)
                    {
                        SetHistory(credentialsPageItem);
                    }
                    else
                    {
                        List<CredentialClaim> revealedList = (from claim in presentedCredentials.RevealedClaims
                                                              where claim.CredentialRecordId == credentialsPageItem.CredentialRecord.Id
                                                              select claim).ToList().OrderBy(x => x.Name).ToList();
                        ObservableCollection<CredentialClaim> revealed = new ObservableCollection<CredentialClaim>();
                        foreach (CredentialClaim claim in revealedList)
                        {
                            revealed.Add(claim);
                        }

                        List<CredentialClaim> nonrevealedList = (from claim in presentedCredentials.NonRevealedClaims
                                                                 where claim.CredentialRecordId == credentialsPageItem.CredentialRecord.Id
                                                                 select claim).ToList().OrderBy(x => x.Name).ToList();
                        ObservableCollection<CredentialClaim> nonrevealed = new ObservableCollection<CredentialClaim>();
                        foreach (CredentialClaim claim in nonrevealedList)
                        {
                            nonrevealed.Add(claim);
                        }

                        List<CredentialClaim> selfAttestedList = (from claim in presentedCredentials.SelfAttestedClaims
                                                                  where claim.CredentialRecordId == credentialsPageItem.CredentialRecord.Id
                                                                  select claim).ToList().OrderBy(x => x.Name).ToList();
                        ObservableCollection<CredentialClaim>
                            selfAttested = new ObservableCollection<CredentialClaim>();
                        foreach (CredentialClaim claim in selfAttestedList)
                        {
                            selfAttested.Add(claim);
                        }

                        List<CredentialClaim> predicatesList = (from claim in presentedCredentials.PredicateClaims
                                                                where claim.CredentialRecordId == credentialsPageItem.CredentialRecord.Id
                                                                select claim).ToList().OrderBy(x => x.Name).ToList();
                        ObservableCollection<CredentialClaim> predicates = new ObservableCollection<CredentialClaim>();
                        foreach (CredentialClaim claim in predicatesList)
                        {
                            predicates.Add(new CredentialClaim
                            {
                                Name = claim.Name,
                                CredentialRecordId = claim.CredentialRecordId,
                                Value = claim.PredicateType + " " + claim.Value,
                                PredicateType = claim.PredicateType
                            });
                        }

                        string serviceAlias = "";
                        if (string.IsNullOrEmpty(presentedCredentials.ConnectionRecord?.Alias.Name))
                        {
                            CustomServiceDecorator service = proofRecord.GetTag(DecoratorNames.ServiceDecorator)
                                .ToObject<CustomServiceDecorator>();
                            Uri endpointUri = new Uri(service.ServiceEndpoint);
                            serviceAlias = !string.IsNullOrEmpty(service.EndpointName) ? service.EndpointName + " - " + endpointUri.Host : service.ServiceEndpoint;
                        }
                        else
                        {
                            serviceAlias = presentedCredentials.ConnectionRecord.Alias.Name;
                        }

                        HistoryProofElement historyItem = new HistoryProofElement
                        {
                            CredentialName = credentialsPageItem.Name,
                            ConnectionAlias = serviceAlias,
                            ImageUri = string.IsNullOrEmpty(presentedCredentials.ConnectionRecord?.Alias.ImageUrl)
                                ? ImageSource.FromFile("default_logo.png")
                                : new Uri(presentedCredentials.ConnectionRecord.Alias.ImageUrl),
                            CreatedAtUtc = proofRecord.CreatedAtUtc,
                            State = Resources.Lang.WalletPage_History_Panel_Status_Shared,
                            RevealedClaims = revealed,
                            NonRevealedClaims = nonrevealed,
                            SelfAttestedClaims = selfAttested,
                            PredicateClaims = predicates
                        };

                        HistoryHeight += 63.6;
                        credentialsPageItem.HistoryItems.Insert(0, historyItem);
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private async void OnProofSent(AutoAcceptViewModel sender, string recordId)
        {
            try
            {
                DisableNotificationAlert();
                IAgentContext context = await _agentProvider.GetContextAsync();
                ProofRecord proofRecord = await _proofService.GetAsync(context, recordId);

                CredentialHistoryElements presentedCredentials = new CredentialHistoryElements();
                try
                {
                    presentedCredentials =
                        JsonConvert.DeserializeObject<CredentialHistoryElements>(proofRecord.GetTag(WalletParams.HistoryCredentialsTag));
                }
                catch (Exception)
                {
                    BasicPopUp alertPopUp = new BasicPopUp(
                        Resources.Lang.PopUp_Undefined_Error_Title,
                        Resources.Lang.PopUp_Undefined_Error_Message,
                        Resources.Lang.PopUp_Undefined_Error_Button);
                    await alertPopUp.ShowPopUp();
                }

                _allProofs.Add((presentedCredentials, proofRecord));
                IEnumerable<WalletElement> credentialsPageItems =
                    from credentialPageItem in WalletElements
                    where presentedCredentials.CredentialRecordIds.Contains(credentialPageItem.CredentialRecord
                        .CredentialId)
                    select credentialPageItem;
                foreach (WalletElement credentialsPageItem in credentialsPageItems)
                {
                    if (!credentialsPageItem.IsHistorySet)
                    {
                        SetHistory(credentialsPageItem);
                    }
                    else
                    {
                        List<CredentialClaim> revealedList = (from claim in presentedCredentials.RevealedClaims
                                                              where claim.CredentialRecordId == credentialsPageItem.CredentialRecord.CredentialId
                                                              select claim).ToList().OrderBy(x => x.Name).ToList();
                        ObservableCollection<CredentialClaim> revealed = new ObservableCollection<CredentialClaim>();
                        foreach (CredentialClaim claim in revealedList)
                        {
                            revealed.Add(claim);
                        }

                        List<CredentialClaim> nonrevealedList = (from claim in presentedCredentials.NonRevealedClaims
                                                                 where claim.CredentialRecordId == credentialsPageItem.CredentialRecord.CredentialId
                                                                 select claim).ToList().OrderBy(x => x.Name).ToList();
                        ObservableCollection<CredentialClaim> nonrevealed = new ObservableCollection<CredentialClaim>();
                        foreach (CredentialClaim claim in nonrevealedList)
                        {
                            nonrevealed.Add(claim);
                        }

                        List<CredentialClaim> selfAttestedList = (from claim in presentedCredentials.SelfAttestedClaims
                                                                  where claim.CredentialRecordId == credentialsPageItem.CredentialRecord.CredentialId
                                                                  select claim).ToList().OrderBy(x => x.Name).ToList();
                        ObservableCollection<CredentialClaim>
                            selfAttested = new ObservableCollection<CredentialClaim>();
                        foreach (CredentialClaim claim in selfAttestedList)
                        {
                            selfAttested.Add(claim);
                        }

                        List<CredentialClaim> predicatesList = (from claim in presentedCredentials.PredicateClaims
                                                                where claim.CredentialRecordId == credentialsPageItem.CredentialRecord.CredentialId
                                                                select claim).ToList().OrderBy(x => x.Name).ToList();
                        ObservableCollection<CredentialClaim> predicates = new ObservableCollection<CredentialClaim>();
                        foreach (CredentialClaim claim in predicatesList)
                        {
                            predicates.Add(new CredentialClaim
                            {
                                Name = claim.Name,
                                CredentialRecordId = claim.CredentialRecordId,
                                Value = claim.PredicateType + " " + claim.Value,
                                PredicateType = claim.PredicateType
                            });
                        }

                        string serviceAlias = "";
                        if (string.IsNullOrEmpty(presentedCredentials.ConnectionRecord?.Alias.Name))
                        {
                            CustomServiceDecorator service = proofRecord.GetTag(DecoratorNames.ServiceDecorator)
                                .ToObject<CustomServiceDecorator>();
                            Uri endpointUri = new Uri(service.ServiceEndpoint);
                            serviceAlias = !string.IsNullOrEmpty(service.EndpointName) ? service.EndpointName + " - " + endpointUri.Host : service.ServiceEndpoint;
                        }
                        else
                        {
                            serviceAlias = presentedCredentials.ConnectionRecord.Alias.Name;
                        }

                        HistoryProofElement historyItem = new HistoryProofElement
                        {
                            CredentialName = credentialsPageItem.Name,
                            ConnectionAlias = serviceAlias,
                            ImageUri = string.IsNullOrEmpty(presentedCredentials.ConnectionRecord?.Alias.ImageUrl)
                                ? ImageSource.FromFile("default_logo.png")
                                : new Uri(presentedCredentials.ConnectionRecord.Alias.ImageUrl),
                            CreatedAtUtc = proofRecord.CreatedAtUtc,
                            State = Resources.Lang.WalletPage_History_Panel_Status_Shared,
                            RevealedClaims = revealed,
                            NonRevealedClaims = nonrevealed,
                            SelfAttestedClaims = selfAttested,
                            PredicateClaims = predicates
                        };

                        HistoryHeight += 63.6;
                        credentialsPageItem.HistoryItems.Insert(0, historyItem);
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private void OpenPdfButtonClicked()
        {
            string documentBase64 = WalletElements[WalletElementPosition].DocumentString;

            App.ViewFile(documentBase64);
        }

        private void ReloadWalletElements(object obj)
        {
            Reload();
        }

        protected override async void Reload()
        {
            WalletElementPosition = 0;

            await ExecuteLoadWalletElements();
        }

        private bool IsBaseIdCredential(CredentialRecord credential)
        {
            string credentialIssuerDID = credential.CredentialDefinitionId.Split(":")[0];
            if (credentialIssuerDID == "Vq2C7Wfc44Q1cSroPuXaw2" || credentialIssuerDID == "MGfd8JjWRoiXMm2YGL4SGj")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsArbeitgeberCredential(CredentialRecord credential)
        {
            string credentialIssuerDID = credential.CredentialDefinitionId.Split(":")[0];
            if (credential.SchemaId.Equals("VkVqDPzeDCQe31H3RsMzbf:2:corporateID:1.0"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsVaccinePassCredential(CredentialRecord credential)
        {
            if (credential.CredentialDefinitionId.Equals("REPLACE"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsFuehrerscheinCredential(CredentialRecord credential)
        {
            if (credential.CredentialDefinitionId.Equals("XnGEZ7gJxDNfxwnZpkkVcs:3:CL:988:Digitaler Führerschein"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}