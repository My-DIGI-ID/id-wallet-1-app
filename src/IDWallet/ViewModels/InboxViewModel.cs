using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Events;
using IDWallet.Models;
using IDWallet.Services;
using IDWallet.Views.Proof;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class InboxViewModel : CustomViewModel
    {
        public readonly Command LoadItemsCommand;
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();
        private readonly IProofService _proofService = App.Container.Resolve<IProofService>();
        private bool _isLoading;

        public InboxViewModel()
        {
            InboxMessages = new ObservableCollection<InboxMessage>();
            LoadItemsCommand = new Command(async () => await LoadMessages());

            Subscribe();
        }

        private void Subscribe()
        {
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.IssueCredentialNames.OfferCredential, NewCredentialOffer);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.PresentProofNames.RequestPresentation, NewProofRequest);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.IssueCredentialNames.OfferCredential, NewCredentialOffer);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.PresentProofNames.RequestPresentation, NewProofRequest);
            MessagingCenter.Subscribe<ProofPopUp, string>(this, WalletEvents.SendProofRequest, ProofSent);
        }

        public ObservableCollection<InboxMessage> InboxMessages { get; set; }

        public new bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public void OnRequestFinished()
        {
            if (!InboxMessages.Any())
            {
                MessagingCenter.Send(this, WalletEvents.NoMoreNotifications);
            }
        }

        public void PopNotificationsPage()
        {
            MessagingCenter.Send(this, WalletEvents.NoMoreNotifications);
        }

        private async Task LoadMessages()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            InboxMessages.Clear();

            List<InboxMessage> inboxMessages = new List<InboxMessage>();

            IAgentContext context = await _agentProvider.GetContextAsync();
            List<CredentialRecord> credentials =
                await _credentialService.ListAsync(context, SearchQuery.Equal(nameof(CredentialRecord.State), CredentialState.Offered.ToString()), 2147483647);
            foreach (CredentialRecord credential in credentials)
            {
                try
                {
                    if (credential.ConnectionId != null)
                    {
                        ConnectionRecord connectionRecord =
                            await _connectionService.GetAsync(context, credential.ConnectionId);

                        bool autoAccept;
                        try
                        {
                            autoAccept = Convert.ToBoolean(connectionRecord.GetTag("AutoAcceptCredential"));
                        }
                        catch (Exception)
                        {
                            autoAccept = false;
                        }

                        bool autoError;
                        try
                        {
                            autoError = Convert.ToBoolean(credential.GetTag("AutoError"));
                        }
                        catch (Exception)
                        {
                            autoError = false;
                        }

                        if (!autoAccept || autoError)
                        {
                            inboxMessages.Add(new WalletCredentialOfferMessage(credential, connectionRecord));
                        }
                    }
                }
                catch (AriesFrameworkException)
                {
                    //ignore
                }
            }

            List<ProofRecord> proofRequests =
                await _proofService.ListAsync(context, SearchQuery.Equal(nameof(ProofRecord.State), ProofState.Requested.ToString()), 2147483647);
            foreach (ProofRecord request in proofRequests)
            {
                try
                {
                    ConnectionRecord connectionRecord = null;
                    bool isConnectionless = false;
                    if (string.IsNullOrEmpty(request.ConnectionId))
                    {
                        isConnectionless = true;
                    }
                    else
                    {
                        try
                        {
                            connectionRecord = await _connectionService.GetAsync(context, request.ConnectionId);
                        }
                        catch (Exception)
                        {
                            await _proofService.RejectProofRequestAsync(context, request.Id);
                        }
                    }

                    bool autoAccept;
                    try
                    {
                        autoAccept = Convert.ToBoolean(connectionRecord.GetTag("AutoAcceptProof"));
                    }
                    catch (Exception)
                    {
                        autoAccept = false;
                    }

                    bool autoError;
                    try
                    {
                        autoError = Convert.ToBoolean(request.GetTag("AutoError"));
                    }
                    catch (Exception)
                    {
                        autoError = false;
                    }

                    if (!autoAccept || autoError)
                    {
                        if (connectionRecord != null || isConnectionless)
                        {
                            inboxMessages.Add(new NewProofRequestMessage(request, connectionRecord));
                        }
                    }
                }
                catch (AriesFrameworkException)
                {
                    //ignore
                }
            }

            foreach (InboxMessage autoAcceptNotification in App.AutoAcceptViewModel.AutoAcceptMessages)
            {
                inboxMessages.Add(autoAcceptNotification);
            }

            inboxMessages = new List<InboxMessage>(inboxMessages.OrderByDescending(x => x.CreatedAtUtc.Value));
            foreach (InboxMessage notification in inboxMessages)
            {
                InboxMessages.Add(notification);
            }

            IsLoading = false;
        }

        private async void NewCredentialOffer(ServiceMessageEventService sender, string recordId)
        {
            IsLoading = true;
            IAgentContext context = await _agentProvider.GetContextAsync();
            try
            {
                CredentialRecord credentialRecord = await _credentialService.GetAsync(context, recordId);
                ConnectionRecord connectionRecord = credentialRecord.ConnectionId != null
                    ? await _connectionService.GetAsync(context, credentialRecord.ConnectionId)
                    : null;

                if (connectionRecord != null)
                {
                    bool autoAccept;
                    try
                    {
                        autoAccept = Convert.ToBoolean(connectionRecord.GetTag("AutoAcceptCredential"));
                    }
                    catch (Exception)
                    {
                        autoAccept = false;
                    }

                    bool autoNotification;
                    try
                    {
                        autoNotification = Convert.ToBoolean(connectionRecord.GetTag("AutoCredentialNotification"));
                    }
                    catch (Exception)
                    {
                        autoNotification = false;
                    }

                    if (autoAccept)
                    {
                        if (autoNotification)
                        {
                            InboxMessages.Add(new AutoAcceptMessage(connectionRecord, credentialRecord));
                        }
                    }
                    else
                    {
                        InboxMessages.Add(new Models.WalletCredentialOfferMessage(credentialRecord, connectionRecord));
                    }
                }
            }
            catch (AriesFrameworkException)
            {
                //ignore
            }

            IsLoading = false;
        }

        private async void NewProofRequest(ServiceMessageEventService sender, string recordId)
        {
            IsLoading = true;

            IAgentContext context = await _agentProvider.GetContextAsync();
            try
            {
                ProofRecord proofRecord = await _proofService.GetAsync(context, recordId);
                ConnectionRecord connectionRecord = null;
                try
                {
                    if (proofRecord.ConnectionId != null)
                    {
                        connectionRecord = await _connectionService.GetAsync(context, proofRecord.ConnectionId);
                    }
                }
                catch (Exception)
                {
                    //ignore
                }

                bool autoAccept;
                try
                {
                    autoAccept = Convert.ToBoolean(connectionRecord.GetTag("AutoAcceptProof"));
                }
                catch (Exception)
                {
                    autoAccept = false;
                }

                bool autoNotification;
                try
                {
                    autoNotification = Convert.ToBoolean(connectionRecord.GetTag("AutoProofNotification"));
                }
                catch (Exception)
                {
                    autoNotification = false;
                }

                if (autoAccept)
                {
                    if (autoNotification)
                    {
                        InboxMessages.Add(new AutoAcceptMessage(connectionRecord, proofRecord));
                    }
                }
                else
                {
                    InboxMessages.Add(new NewProofRequestMessage(proofRecord, connectionRecord));
                }
            }
            catch (AriesFrameworkException)
            {
                //ignore
            }

            IsLoading = false;
        }

        private void ProofSent(ProofPopUp proofRequestPopUp, string proofRecordId)
        {
            try
            {
                InboxMessage notification = InboxMessages.First(x => x.RecordId == proofRecordId);
                InboxMessages.Remove(notification);
                OnRequestFinished();
            }
            catch (Exception)
            {
                //ignore
            }
        }
    }
}