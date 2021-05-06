using Autofac;
using IDWallet.Agent;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Services;
using IDWallet.Events;
using IDWallet.Models;
using IDWallet.Services;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Inbox;
using IDWallet.Views.Login;
using IDWallet.Views.Proof;
using IDWallet.Views.Settings;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Models.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class ConnectionsViewModel : CustomViewModel
    {
        private const string _mediatorConnectionIdTagName = "MediatorConnectionId";
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();
        private readonly IEventAggregator _eventAggregator = App.Container.Resolve<IEventAggregator>();
        private readonly IProofService _proofService = App.Container.Resolve<IProofService>();

        private readonly IProvisioningService _provisioningService = App.Container.Resolve<IProvisioningService>();

        private readonly ICustomWalletRecordService
                    _walletRecordService = App.Container.Resolve<ICustomWalletRecordService>();
        private bool _emptyLayoutVisible;

        public ConnectionsViewModel()
        {
            Title = "Connections";
            Connections = new ObservableCollection<ConnectionsPageItem>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadConnectionsCommand());

            DisableNotificationAlert();
            DisableNotificationsCommand = new Command(DisableNotificationAlert);

            Subscribe();
        }

        private void Subscribe()
        {
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.IssueCredentialNames.OfferCredential, OnCredentialOffer);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.PresentProofNames.RequestPresentation, OnNewProofRequest);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.IssueCredentialNames.OfferCredential, OnCredentialOffer);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.PresentProofNames.RequestPresentation, OnNewProofRequest);
            MessagingCenter.Subscribe<ConnectService, ConnectionRecord>(this, WalletEvents.NewConnection,
                OnConnectionAdded);
            MessagingCenter.Subscribe<LoginPage>(this, WalletEvents.AppStarted, OnAppStart);
            MessagingCenter.Subscribe<AutoAcceptViewModel>(this, WalletEvents.ReloadConnections, ReloadConnections);
            MessagingCenter.Subscribe<ConnectService>(this, WalletEvents.ReloadConnections, ReloadConnections);
            MessagingCenter.Subscribe<ProofElementsViewModel>(this, WalletEvents.ReloadConnections, ReloadConnections);
            MessagingCenter.Subscribe<CustomWalletRecordService>(this, WalletEvents.ReloadConnections,
                ReloadConnections);
            MessagingCenter.Subscribe<BaseIdViewModel>(this, WalletEvents.ReloadConnections,
                ReloadConnections);
            MessagingCenter.Subscribe<SettingsPage>(this, WalletEvents.ToggleShowMediator, ReloadConnections);
            MessagingCenter.Subscribe<SettingsPage>(this, WalletEvents.ToggleUseMediatorImages, ReloadConnections);
            MessagingCenter.Subscribe<LoginViewModel>(this, WalletEvents.AppStarted, OnAppStart);
            MessagingCenter.Subscribe<CustomAgentProvider>(this, WalletEvents.AgentSwitched, OnAgentChanged);
            MessagingCenter.Subscribe<ProofPopUp, string>(this, WalletEvents.SentProofRequest, OnProofSent);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.IssueCredentialNames.IssueCredential, OnCredentialIssued);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.IssueCredentialNames.IssueCredential, OnCredentialIssued);
            MessagingCenter.Subscribe<InboxPage>(this, WalletEvents.DisableNotifications, NotificationsClosed);

            _eventAggregator.GetEventByType<ServiceMessageProcessingEvent>().Subscribe(OnProcessedMessage);
        }

        public ObservableCollection<ConnectionsPageItem> Connections { get; set; }

        public bool EmptyLayoutVisible
        {
            get => _emptyLayoutVisible;
            set => SetProperty(ref _emptyLayoutVisible, value);
        }
        public Command LoadItemsCommand { get; set; }

        public void EnableNotification()
        {
            EnableNotificationAlert();
        }
        public async Task<bool> IsMediatorConnection(ConnectionRecord connectionRecord)
        {
            IAgentContext agentContext = await _agentProvider.GetContextAsync();
            ProvisioningRecord provisioning = await _provisioningService.GetProvisioningAsync(agentContext.Wallet);

            if (connectionRecord.Id != provisioning.GetTag(_mediatorConnectionIdTagName))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public async void ReloadConnections()
        {
            while (IsLoading)
            {
                await Task.Delay(100);
            }

            App.ConnectionsLoaded = false;
            await ExecuteLoadConnectionsCommand();
            App.ConnectionsLoaded = true;
        }

        protected override async void Reload()
        {
            while (IsLoading)
            {
                await Task.Delay(100);
            }

            App.ConnectionsLoaded = false;
            await ExecuteLoadConnectionsCommand();
            App.ConnectionsLoaded = true;
        }

        private async Task ExecuteLoadConnectionsCommand()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;

            try
            {
                IAgentContext agentContext = await _agentProvider.GetContextAsync();

                Connections.Clear();
                List<ConnectionRecord> items = await _connectionService.ListAsync(agentContext, count: 2147483647);

                ProvisioningRecord provisioning = await _provisioningService.GetProvisioningAsync(agentContext.Wallet);

                List<ConnectionRecord> displayables = new List<ConnectionRecord>();
                foreach (ConnectionRecord item in items)
                {
                    if (item.Alias != null)
                    {
                        if ((item.Alias.Name != null && item.Id != provisioning.GetTag(_mediatorConnectionIdTagName)) ||
                            (item.Alias.Name != null && item.Id == provisioning.GetTag(_mediatorConnectionIdTagName) &&
                             App.ShowMediatorConnection))
                        {
                            displayables.Add(item);

                            bool autoAcceptProof = !string.IsNullOrEmpty(item.GetTag("AutoAcceptProof")) &&
                                                   Convert.ToBoolean(item.GetTag("AutoAcceptProof"));
                            string takeNewestString = item.GetTag("TakeNewest");

                            if (autoAcceptProof)
                            {
                                if (string.IsNullOrEmpty(takeNewestString))
                                {
                                    item.SetTag("TakeNewest", "true");
                                    await _walletRecordService.UpdateAsync(agentContext.Wallet, item);
                                }
                            }
                        }
                    }
                }

                displayables =
                    new List<ConnectionRecord>(
                        displayables.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc.Value));
                foreach (ConnectionRecord item in displayables)
                {
                    if (item.Id == provisioning.GetTag(_mediatorConnectionIdTagName))
                    {
                        Connections.Add(new ConnectionsPageItem(item, false));
                    }
                    else
                    {
                        Connections.Add(new ConnectionsPageItem(item, true));
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
            finally
            {
                EmptyLayoutVisible = !Connections.Any();
                IsLoading = false;
            }
        }

        private void NotificationsClosed(InboxPage obj)
        {
            DisableNotificationsCommand.Execute(null);
        }

        private async void OnAgentChanged(ICustomAgentProvider obj)
        {
            while (IsLoading)
            {
                await Task.Delay(100);
            }

            App.ConnectionsLoaded = false;
            await ExecuteLoadConnectionsCommand();
            App.ConnectionsLoaded = true;
        }

        private async void OnAppStart(LoginPage sender)
        {
            while (IsLoading)
            {
                await Task.Delay(100);
            }

            App.ConnectionsLoaded = false;
            await ExecuteLoadConnectionsCommand();
            App.ConnectionsLoaded = true;
        }

        private async void OnAppStart(LoginViewModel sender)
        {
            while (IsLoading)
            {
                await Task.Delay(100);
            }

            App.ConnectionsLoaded = false;
            await ExecuteLoadConnectionsCommand();
            App.ConnectionsLoaded = true;
        }

        private async void OnConnectionAdded(object obj, ConnectionRecord record)
        {
            try
            {
                IEnumerable<ConnectionsPageItem> contains = from connection in Connections.ToList()
                                                            where connection.ConnectionRecord.Id == record.Id
                                                            select connection;

                IAgentContext agentContext = await _agentProvider.GetContextAsync();
                ProvisioningRecord provisioning = await _provisioningService.GetProvisioningAsync(agentContext.Wallet);
                ConnectionRecord mediatorConnection =
                    await _walletRecordService.GetAsync<ConnectionRecord>(agentContext.Wallet,
                        provisioning.GetTag(_mediatorConnectionIdTagName));

                if (record.Id != mediatorConnection.Id &&
                    !contains.Any())
                {
                    Connections.Insert(0, new ConnectionsPageItem(record, true));
                    EmptyLayoutVisible = false;
                }
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Undefined_Error_Title,
                    Resources.Lang.PopUp_Undefined_Error_Connections_Added_Message,
                    Resources.Lang.PopUp_Undefined_Error_Button);
                alertPopUp.ShowPopUp().Wait();
            }
        }

        private async void OnCredentialIssued(ServiceMessageEventService msg, string recordId)
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

        private async void OnProcessedMessage(ServiceMessageProcessingEvent msg)
        {
            try
            {
                IAgentContext agentContext = await _agentProvider.GetContextAsync();
                ProvisioningRecord provisioning = await _provisioningService.GetProvisioningAsync(agentContext.Wallet);

                if (msg.MessageType == MessageTypes.ConnectionResponse ||
                    msg.MessageType == MessageTypesHttps.ConnectionResponse)
                {
                    IAgentContext context = await _agentProvider.GetContextAsync();
                    ConnectionRecord connection = await _connectionService.GetAsync(context, msg.RecordId);
                    if (connection.State == ConnectionState.Connected &&
                        connection.Id != provisioning.GetTag(_mediatorConnectionIdTagName))
                    {
                        IEnumerable<ConnectionsPageItem> contains = from con in Connections.ToList()
                                                                    where con.ConnectionRecord.Id == connection.Id
                                                                    select con;
                        if (contains.Any())
                        {
                            Connections.Remove(contains.First());
                        }

                        Connections.Insert(0, new ConnectionsPageItem(connection, true));
                    }
                }
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Undefined_Error_Title,
                    Resources.Lang.PopUp_Undefined_Error_Connections_Processed_Message,
                    Resources.Lang.PopUp_Undefined_Error_Button);
                await alertPopUp.ShowPopUp();
            }
        }

        private async void OnProofSent(ProofPopUp sender, string recordId)
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

        private async void ReloadConnections(object obj)
        {
            while (IsLoading)
            {
                await Task.Delay(100);
            }

            App.ConnectionsLoaded = false;
            await ExecuteLoadConnectionsCommand();
            App.ConnectionsLoaded = true;
        }
    }
}