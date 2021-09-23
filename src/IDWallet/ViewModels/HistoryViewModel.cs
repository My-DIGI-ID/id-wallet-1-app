using Autofac;
using IDWallet.Agent;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Events;
using IDWallet.Models;
using IDWallet.Services;
using IDWallet.Views.Proof;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Decorators;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class HistoryViewModel : CustomViewModel
    {
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly IProofService _proofService = App.Container.Resolve<IProofService>();
        private readonly ICustomWalletRecordService _walletRecordService =
            App.Container.Resolve<ICustomWalletRecordService>();

        public HistoryViewModel()
        {
            HistoryElements = new ObservableCollection<HistoryElement>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadHistory());

            DisableNotificationAlert();

            Subscribe();
        }

        private void Subscribe()
        {
            MessagingCenter.Subscribe<ProofPopUp, string>(this, WalletEvents.SentProofRequest, OnProofSent);
            MessagingCenter.Subscribe<AutoAcceptViewModel, string>(this, WalletEvents.SentProofRequest, OnProofSent);
            MessagingCenter.Subscribe<LoginViewModel>(this, WalletEvents.AppStarted, OnAppStart);
            MessagingCenter.Subscribe<BaseIdViewModel>(this, WalletEvents.ReloadHistory, ReloadHistory);
            MessagingCenter.Subscribe<DdlViewModel>(this, WalletEvents.ReloadHistory, ReloadHistory);
            MessagingCenter.Subscribe<AddVacService>(this, WalletEvents.ReloadHistory, ReloadHistory);
            MessagingCenter.Subscribe<CustomAgentProvider>(this, WalletEvents.AgentSwitched, ReloadHistory);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.IssueCredentialNames.IssueCredential, OnCredentialIssued);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.IssueCredentialNames.IssueCredential, callback: OnCredentialIssued);
            MessagingCenter.Subscribe<AddVacService, string>(this, WalletEvents.VacAdded, OnVacQrCredentialAdded);
        }

        private void ReloadHistory(object obj)
        {
            LoadItemsCommand.Execute(null);
        }

        public ObservableCollection<HistoryElement> HistoryElements { get; set; }

        public Command LoadItemsCommand { get; set; }

        private async Task ExecuteLoadHistory()
        {
            if (App.IsLoggedIn)
            {
                App.HistoryLoaded = false;
                try
                {
                    HistoryElements.Clear();

                    IAgentContext agentContext = await _agentProvider.GetContextAsync();

                    List<HistoryCredentialElement> allIssuedCredentials = await GetAllIssuedCredentials();
                    List<HistoryCredentialElement> allIssuedVacQrCredentials = await GetAllIssuedVacQrCredentials();
                    List<(HistoryProofElement, ProofRecord)> allPresentedCredentials = await GetAllPresentedCredentials();
                    List<HistorySubElement> allHistorySubElements = await GetAllCredentialHistoryItems(agentContext, allIssuedCredentials, allPresentedCredentials, allIssuedVacQrCredentials);
                    allHistorySubElements = allHistorySubElements.OrderByDescending(x => x.CreatedAtUtc).ToList();

                    HistoryElement newHistoryPageItem = new HistoryElement
                    {
                        Date = DateTime.Now.Date,
                        HistorySubElements = new ObservableCollection<HistorySubElement>()
                    };
                    foreach (HistorySubElement historySubElement in allHistorySubElements)
                    {
                        try
                        {
                            if (historySubElement.CreatedAtUtc.Value.Date == newHistoryPageItem.Date)
                            {
                                newHistoryPageItem.HistorySubElements.Add(historySubElement);
                            }
                            else
                            {
                                if (newHistoryPageItem.HistorySubElements.Any())
                                {
                                    if (newHistoryPageItem.Date == DateTime.Now.Date)
                                    {
                                        newHistoryPageItem.Name = Resources.Lang.HistoryPage_Today_Label;
                                    }
                                    else if (newHistoryPageItem.Date == DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0)).Date)
                                    {
                                        newHistoryPageItem.Name = Resources.Lang.HistoryPage_Yesterday_Label;
                                    }
                                    else
                                    {
                                        CultureInfo currentUICulture = CultureInfo.CurrentUICulture ?? new CultureInfo("en-GB", false);
                                        newHistoryPageItem.Name = newHistoryPageItem.Date.ToString(currentUICulture.DateTimeFormat.ShortDatePattern);
                                    }

                                    HistoryElements.Add(newHistoryPageItem);
                                }

                                newHistoryPageItem = new HistoryElement
                                {
                                    Date = historySubElement.CreatedAtUtc.Value.Date,
                                    HistorySubElements = new ObservableCollection<HistorySubElement>()
                                };
                                newHistoryPageItem.HistorySubElements.Add(historySubElement);
                            }
                        }
                        catch (Exception)
                        {
                            //ignore
                        }
                    }

                    if (newHistoryPageItem.HistorySubElements.Any())
                    {
                        if (newHistoryPageItem.Date == DateTime.Now.Date)
                        {
                            newHistoryPageItem.Name = Resources.Lang.HistoryPage_Today_Label;
                        }
                        else if (newHistoryPageItem.Date == DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0)).Date)
                        {
                            newHistoryPageItem.Name = Resources.Lang.HistoryPage_Yesterday_Label;
                        }
                        else
                        {
                            CultureInfo currentUICulture = CultureInfo.CurrentUICulture ?? new CultureInfo("en-GB", false);
                            newHistoryPageItem.Name = newHistoryPageItem.Date.ToString(currentUICulture.DateTimeFormat.ShortDatePattern);
                        }

                        HistoryElements.Add(newHistoryPageItem);
                    }
                }
                catch (Exception)
                {
                    //ignore
                }
                finally
                {
                    App.HistoryLoaded = true;
                }
            }
        }

        private async Task<List<HistorySubElement>> GetAllCredentialHistoryItems(IAgentContext agentContext, List<HistoryCredentialElement> allIssuedCredentials, List<(HistoryProofElement, ProofRecord)> allPresentedCredentials, List<HistoryCredentialElement> allIssuedVacQrCredentials)
        {
            List<HistorySubElement> allCredentialHistoryItems = new List<HistorySubElement>();
            try
            {
                foreach (HistoryCredentialElement issuedCredential in allIssuedCredentials)
                {
                    try
                    {
                        allCredentialHistoryItems.Add(issuedCredential);
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }

                foreach (HistoryCredentialElement issuedVacQrCredential in allIssuedVacQrCredentials)
                {
                    try
                    {
                        allCredentialHistoryItems.Add(issuedVacQrCredential);
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }

                foreach ((HistoryProofElement historyProofElement, ProofRecord proofRecord) in allPresentedCredentials)
                {
                    try
                    {
                        foreach (string credentialRecordId in historyProofElement.CredentialRecordIds)
                        {
                            CredentialRecord credentialRecord = await _credentialService.GetAsync(agentContext, credentialRecordId);

                            List<CredentialClaim> revealedList = (from claim in historyProofElement.RevealedClaims
                                                                  where claim.CredentialRecordId == credentialRecordId
                                                                  select claim).ToList().OrderBy(x => x.Name).ToList();
                            ObservableCollection<CredentialClaim> revealed = new ObservableCollection<CredentialClaim>();
                            foreach (CredentialClaim claim in revealedList)
                            {
                                revealed.Add(claim);
                            }

                            List<CredentialClaim> nonrevealedList = (from claim in historyProofElement.NonRevealedClaims
                                                                     where claim.CredentialRecordId == credentialRecordId
                                                                     select claim).ToList().OrderBy(x => x.Name).ToList();
                            ObservableCollection<CredentialClaim> nonrevealed = new ObservableCollection<CredentialClaim>();
                            foreach (CredentialClaim claim in nonrevealedList)
                            {
                                nonrevealed.Add(claim);
                            }

                            List<CredentialClaim> selfAttestedList = (from claim in historyProofElement.SelfAttestedClaims
                                                                      where claim.CredentialRecordId == credentialRecordId
                                                                      select claim).ToList().OrderBy(x => x.Name).ToList();
                            ObservableCollection<CredentialClaim> selfAttested = new ObservableCollection<CredentialClaim>();
                            foreach (CredentialClaim claim in selfAttestedList)
                            {
                                selfAttested.Add(claim);
                            }

                            List<CredentialClaim> predicatesList = (from claim in historyProofElement.PredicateClaims
                                                                    where claim.CredentialRecordId == credentialRecordId
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
                            if (string.IsNullOrEmpty(historyProofElement.ConnectionRecord?.Alias.Name))
                            {
                                CustomServiceDecorator service = proofRecord.GetTag(DecoratorNames.ServiceDecorator)
                                    .ToObject<CustomServiceDecorator>();
                                Uri endpointUri = new Uri(service.ServiceEndpoint);
                                serviceAlias = !string.IsNullOrEmpty(service.EndpointName) ? service.EndpointName + " - " + endpointUri.Host : service.ServiceEndpoint;
                            }

                            HistoryProofElement newHistoryProofElement = new HistoryProofElement
                            {
                                CredentialName = credentialRecord.CredentialDefinitionId.Split(':')[4],
                                ConnectionAlias = !string.IsNullOrEmpty(historyProofElement.ConnectionRecord?.Alias.Name) ? historyProofElement.ConnectionRecord.Alias.Name : serviceAlias,
                                State = Resources.Lang.WalletPage_History_Panel_Status_Shared,
                                ImageUri = string.IsNullOrEmpty(historyProofElement.ConnectionRecord?.Alias.ImageUrl)
                                    ? ImageSource.FromFile("default_logo.png")
                                    : new Uri(historyProofElement.ConnectionRecord.Alias.ImageUrl),
                                CreatedAtUtc = proofRecord.CreatedAtUtc,
                                RevealedClaims = revealed,
                                NonRevealedClaims = nonrevealed,
                                SelfAttestedClaims = selfAttested,
                                PredicateClaims = predicates
                            };
                            allCredentialHistoryItems.Add(newHistoryProofElement);
                        }
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
            return allCredentialHistoryItems;
        }

        private async Task<List<HistoryCredentialElement>> GetAllIssuedCredentials(List<CredentialRecord> allCredentialRecords = null)
        {
            List<HistoryCredentialElement> issuedCredentialHistoryElements = new List<HistoryCredentialElement>();

            IAgentContext agentContext = await _agentProvider.GetContextAsync();

            List<CredentialRecord> allIssuedCredentials;
            if (allCredentialRecords == null)
            {
                ISearchQuery[] credentialQuery = new[]
                {
                SearchQuery.Equal(nameof(CredentialRecord.State), CredentialState.Issued.ToString())
                };
                allIssuedCredentials = await _credentialService.ListAsync(agentContext, credentialQuery[0], count: 2147483647);
            }
            else
            {
                allIssuedCredentials = allCredentialRecords;
            }

            foreach (CredentialRecord record in allIssuedCredentials)
            {
                ConnectionRecord connectionRecord = await _connectionService.GetAsync(agentContext, record.ConnectionId);
                HistoryCredentialElement historyCredentialElement = new HistoryCredentialElement
                {
                    ConnectionAlias = connectionRecord.Alias.Name,
                    CredentialName = record.CredentialDefinitionId.Split(':')[4],
                    CreatedAtUtc = record.CreatedAtUtc,
                    Claims = new ObservableCollection<CredentialPreviewAttribute>()
                };

                switch (record.CredentialDefinitionId.Split(':')[0])
                {
                    case "JiVLsA5wxVnbHQ5s7pDN58":
                        historyCredentialElement.ImageUri = ImageSource.FromFile("ibm_logo.png");
                        break;
                    case "Vq2C7Wfc44Q1cSroPuXaw2":
                    case "MGfd8JjWRoiXMm2YGL4SGj":
                        historyCredentialElement.ImageUri = ImageSource.FromFile("bdr_logo.png");
                        break;
                    case "9hsRe5jdzAbbyLAStV6sPc":
                    case "KqtBRiQSyWqnzaxN3u2d7G":
                        historyCredentialElement.ImageUri = ImageSource.FromFile("kraftfahrtbundesamt_logo.png");
                        break;
                    case "En38baYaTqVYSB8SFwguhT":
                    case "9HX4bs8pdH2uJB7sjeWPtU":
                        historyCredentialElement.ImageUri = ImageSource.FromFile("bosch_logo.png");
                        break;
                    case "VkVqDPzeDCQe31H3RsMzbf":
                    case "EKtoaKk2ifgmY4cQxYAwcE":
                        historyCredentialElement.ImageUri = ImageSource.FromFile("dbahn_logo.png");
                        break;
                    case "7vyyugPwC3ArWtRWbz6LCm":
                    case "PsDLsaget7L9duoaxzC2DZ":
                        historyCredentialElement.ImageUri = ImageSource.FromFile("bwi_logo.png");
                        break;
                    case "X2p16G1BeEceJauzqofjQW":
                        historyCredentialElement.ImageUri = ImageSource.FromFile("dlufthansa_logo.png");
                        break;
                    case "XnGEZ7gJxDNfxwnZpkkVcs":
                        historyCredentialElement.ImageUri = ImageSource.FromFile("kba_logo.png");
                        break;
                    default:
                        historyCredentialElement.ImageUri = ImageSource.FromFile("default_logo.png");
                        break;
                }

                foreach (CredentialPreviewAttribute credentialPreviewAttribute in record.CredentialAttributesValues)
                {
                    historyCredentialElement.Claims.Add(credentialPreviewAttribute);
                }

                issuedCredentialHistoryElements.Add(historyCredentialElement);
            }

            return issuedCredentialHistoryElements;
        }
        private async Task<List<HistoryCredentialElement>> GetAllIssuedVacQrCredentials(List<VacQrCredential> allVacQrCredentialRecords = null)
        {
            List<HistoryCredentialElement> issuedCredentialHistoryElements = new List<HistoryCredentialElement>();

            IAgentContext agentContext = await _agentProvider.GetContextAsync();

            List<VacQrCredential> allVacQrCredentials;
            if (allVacQrCredentialRecords == null)
            {
                allVacQrCredentials = await _walletRecordService.SearchAsync<VacQrCredential>(agentContext.Wallet, null, null, 2147483647, false);
            }
            else
            {
                allVacQrCredentials = allVacQrCredentialRecords;
            }

            foreach (VacQrCredential record in allVacQrCredentials)
            {
                HistoryCredentialElement historyCredentialElement = new HistoryCredentialElement
                {
                    ConnectionAlias = "QR-Scan",
                    CredentialName = record.Name,
                    CreatedAtUtc = record.CreatedAtUtc,
                    Claims = new ObservableCollection<CredentialPreviewAttribute>()
                };

                historyCredentialElement.ImageUri = ImageSource.FromFile("qr_code.png");

                historyCredentialElement.Claims.Add(new CredentialPreviewAttribute() { Name = "COVID-Zertifikat", Value = record.QrContent });

                issuedCredentialHistoryElements.Add(historyCredentialElement);
            }

            return issuedCredentialHistoryElements;
        }


        private async Task<List<(HistoryProofElement, ProofRecord)>> GetAllPresentedCredentials(List<ProofRecord> listOfRecords = null)
        {
            IAgentContext agentContext = await _agentProvider.GetContextAsync();
            List<ProofRecord> allProofRecords;
            if (listOfRecords == null)
            {
                allProofRecords = await _proofService.ListAsync(agentContext, SearchQuery.Equal(nameof(ProofRecord.State), ProofState.Accepted.ToString()), count: 2147483647);
            }
            else
            {
                allProofRecords = listOfRecords;
            }

            List<(HistoryProofElement, ProofRecord)> allPresentedCredentials = new List<(HistoryProofElement, ProofRecord)>();
            foreach (ProofRecord proof in allProofRecords)
            {
                HistoryProofElement proofCredentials = new HistoryProofElement();
                try
                {
                    proofCredentials = JsonConvert.DeserializeObject<HistoryProofElement>(proof.GetTag(WalletParams.HistoryCredentialsTag));
                }
                catch (Exception)
                {
                    //ignore
                }

                allPresentedCredentials.Add((proofCredentials, proof));
            }

            return allPresentedCredentials;
        }

        private void OnAppStart(LoginViewModel arg1)
        {
            LoadItemsCommand.Execute(null);
        }

        private void OnProofSent(AutoAcceptViewModel arg1, string proofRecordId)
        {
            LoadItemsCommand.Execute(null);
        }

        private void OnProofSent(ProofPopUp arg1, string proofRecordId)
        {
            LoadItemsCommand.Execute(null);
        }

        private void OnCredentialIssued(ServiceMessageEventService arg1, string credentialRecordId)
        {
            LoadItemsCommand.Execute(null);
        }

        private void OnVacQrCredentialAdded(AddVacService arg1, string vacQrRecordId)
        {
            LoadItemsCommand.Execute(null);
        }
    }
}