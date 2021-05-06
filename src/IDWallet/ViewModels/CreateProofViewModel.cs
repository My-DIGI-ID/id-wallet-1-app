using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Models;
using IDWallet.Resources;
using IDWallet.Services;
using IDWallet.Views.Inbox;
using IDWallet.Views.Proof;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class CreateProofViewModel : CustomViewModel
    {
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();

        private readonly ProofListService _proofListService = new ProofListService();

        private readonly IProofService _proofService = App.Container.Resolve<IProofService>();

        private readonly ICustomSecureStorageService _secureStorageService =
                            App.Container.Resolve<ICustomSecureStorageService>();
        private string _emptyPageText;
        private string _emptyPageTitle;
        private bool _isEmptyPageVisible;
        public CreateProofViewModel()
        {
            LoadItemsCommand = new Command(async () => await ExecuteLoadRequestsCommand());
            Requests = new ObservableCollection<SendableRequest>();

            IsEmptyPageVisible = true;
            EmptyPageTitle = Lang.CreateProofPage_Loading_Title;
            EmptyPageText = Lang.CreateProofPage_Loading_Text;

            DisableNotificationAlert();
            DisableNotificationsCommand = new Command(DisableNotificationAlert);

            Subscribe();
        }

        private void Subscribe()
        {
            MessagingCenter.Subscribe<InboxPage>(this, WalletEvents.DisableNotifications, NotificationsClosed);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.IssueCredentialNames.OfferCredential, OnCredentialOffer);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.PresentProofNames.RequestPresentation, OnNewProofRequest);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.IssueCredentialNames.OfferCredential, OnCredentialOffer);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.PresentProofNames.RequestPresentation, OnNewProofRequest);
            MessagingCenter.Subscribe<ProofPopUp, string>(this, WalletEvents.SentProofRequest, OnProofSent);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.IssueCredentialNames.IssueCredential, OnCredentialIssued);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.IssueCredentialNames.IssueCredential, OnCredentialIssued);
            MessagingCenter.Subscribe<LoginViewModel>(this, WalletEvents.AppStarted, OnAppStart);
            MessagingCenter.Subscribe<GatewaysViewModel>(this, WalletEvents.NewGateway, OnNewGateway);
            MessagingCenter.Subscribe<AddGatewayService>(this, WalletEvents.NewGateway, OnNewGateway);
        }

        public string EmptyPageText
        {
            get => _emptyPageText;
            set => SetProperty(ref _emptyPageText, value);
        }

        public string EmptyPageTitle
        {
            get => _emptyPageTitle;
            set => SetProperty(ref _emptyPageTitle, value);
        }

        public bool IsEmptyPageVisible
        {
            get => _isEmptyPageVisible;
            set => SetProperty(ref _isEmptyPageVisible, value);
        }

        public Command LoadItemsCommand { get; set; }
        public ObservableCollection<SendableRequest> Requests { get; set; }
        public void EnableNotification()
        {
            EnableNotificationAlert();
        }

        public async void ReloadRequests()
        {
            await InitReload();
        }

        protected override async void Reload()
        {
            await ExecuteLoadRequestsCommand();
        }
        private async Task DiaableNotifications()
        {
            try
            {
                await Task.Run(() => { DisableNotificationAlert(); });
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private async Task ExecuteLoadRequestsCommand()
        {
            try
            {
                IsLoading = true;

                Requests.Clear();
                EmptyPageTitle = Lang.CreateProofPage_Loading_Title;
                EmptyPageText = Lang.CreateProofPage_Loading_Text;
                IsEmptyPageVisible = true;

                if (!IsConnected)
                {
                    EmptyPageTitle = Lang.CreateProofPage_Network_Error_Title;
                    EmptyPageText = Lang.CreateProofPage_Network_Error_Text;

                    IsLoading = false;
                    return;
                }

                ObservableCollection<Gateway> endpointList = null;
                try
                {
                    endpointList =
                        await _secureStorageService.GetAsync<ObservableCollection<Gateway>>(WalletParams.AllGatewaysTag);
                }
                catch (Exception)
                {
                    //ignore
                }

                if (endpointList == null || !endpointList.Any())
                {
                    EmptyPageTitle = Lang.CreateProofPage_No_Gateways_Title;
                    EmptyPageText = Lang.CreateProofPage_No_Gateways_Text;

                    IsLoading = false;
                    return;
                }

                List<ProofRequest> allRequests = null;
                try
                {
                    allRequests = await _proofListService.GetProofListAsync();
                }
                catch (Exception)
                {
                    //ignore
                }

                if (allRequests == null || !allRequests.Any())
                {
                    EmptyPageTitle = Lang.CreateProofPage_No_Proofs_Title;
                    EmptyPageText = Lang.CreateProofPage_No_Proofs_Text;

                    IsLoading = false;
                    return;
                }

                IsEmptyPageVisible = false;

                foreach (ProofRequest proofRequest in allRequests)
                {
                    SendableRequest newRequest = new SendableRequest
                    {
                        ProofRequest = proofRequest,
                        Name = proofRequest.Name,
                        Attributes = new List<ProofAttributeInfo>(),
                        Predicates = new List<ProofPredicateInfo>()
                    };
                    foreach (KeyValuePair<string, ProofAttributeInfo> requestedAttribute in proofRequest
                        .RequestedAttributes)
                    {
                        newRequest.Attributes.Add(requestedAttribute.Value);
                    }

                    foreach (KeyValuePair<string, ProofPredicateInfo> requestedPredicate in proofRequest
                        .RequestedPredicates)
                    {
                        newRequest.Predicates.Add(requestedPredicate.Value);
                    }

                    Requests.Add(newRequest);
                }
            }
            catch (Exception)
            {
                IsEmptyPageVisible = true;
                EmptyPageTitle = Lang.CreateProofPage_No_Proofs_Title;
                EmptyPageText = Lang.CreateProofPage_No_Proofs_Text;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task InitReload()
        {
            while (IsLoading)
            {
                await Task.Delay(100);
            }

            App.ProofsLoaded = false;
            await ExecuteLoadRequestsCommand();
            App.ProofsLoaded = true;
        }

        private void NotificationsClosed(InboxPage obj)
        {
            DisableNotificationsCommand.Execute(null);
        }

        private async void OnAppStart(LoginViewModel sender)
        {
            await InitReload();
        }

        private async void OnCredentialIssued(ServiceMessageEventService msg, string recordId)
        {
            await DiaableNotifications();
        }

        private async void OnCredentialOffer(ServiceMessageEventService arg1, string recordId)
        {
            IAgentContext context = await _agentProvider.GetContextAsync();
            CredentialRecord credentialRecord = await _credentialService.GetAsync(context, recordId);
            ConnectionRecord connectionRecord = credentialRecord.ConnectionId != null
                ? await _connectionService.GetAsync(context, credentialRecord.ConnectionId)
                : null;

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

        private async void OnNewGateway(GatewaysViewModel sender)
        {
            await InitReload();
        }

        private async void OnNewGateway(AddGatewayService sender)
        {
            await InitReload();
        }
        private async void OnNewProofRequest(ServiceMessageEventService arg1, string recordId)
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

        private async void OnProofSent(ProofPopUp sender, string recordId)
        {
            await DiaableNotifications();
        }
    }
}