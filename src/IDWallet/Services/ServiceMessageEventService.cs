using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Events;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Settings.Connections.PopUps;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Models.Events;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.Services
{
    public class ServiceMessageEventService : IObserver<ServiceMessageProcessingEvent>
    {
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();
        private IDisposable _unsubscriber;

        public void OnCompleted()
        {
            Unsubscribe();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public async void OnNext(ServiceMessageProcessingEvent msg)
        {
            IAgentContext agentContext = await _agentProvider.GetContextAsync();

            switch (msg.MessageType)
            {
                case MessageTypes.ConnectionResponse:
                case MessageTypesHttps.ConnectionResponse:
                    ConnectionRecord connectionRecord = await _connectionService.GetAsync(agentContext, msg.RecordId);
                    if (!(connectionRecord.Id.Equals(App.BaseIdConnectionId) || connectionRecord.Id.Equals(App.DdlConnectionId) || connectionRecord.Id.Equals(App.VacConnectionId)))
                    {
                        await ShowConnectionAddedPopUp(connectionRecord);
                    }
                    break;

                case MessageTypes.IssueCredentialNames.OfferCredential:
                case MessageTypesHttps.IssueCredentialNames.OfferCredential:
                    CredentialRecord credentialRecordOffer =
                        await _credentialService.GetAsync(agentContext, msg.RecordId);

                    if (!string.IsNullOrEmpty(credentialRecordOffer.ConnectionId) && credentialRecordOffer.ConnectionId.Equals(App.BaseIdConnectionId))
                    {
                        MessagingCenter.Send(this, WalletEvents.BaseIdCredentialOffer, msg.RecordId);
                    }
                    else if (!string.IsNullOrEmpty(credentialRecordOffer.ConnectionId) && credentialRecordOffer.ConnectionId.Equals(App.VacConnectionId))
                    {
                        MessagingCenter.Send(this, WalletEvents.VacCredentialOffer, msg.RecordId);
                    }
                    else if (!string.IsNullOrEmpty(credentialRecordOffer.ConnectionId) && credentialRecordOffer.ConnectionId.Equals(App.DdlConnectionId))
                    {
                        MessagingCenter.Send(this, WalletEvents.DdlCredentialOffer, msg.RecordId);
                    }
                    else
                    {
                        MessagingCenter.Send(this, MessageTypes.IssueCredentialNames.OfferCredential, msg.RecordId);
                    }
                    break;

                case MessageTypes.IssueCredentialNames.IssueCredential:
                case MessageTypesHttps.IssueCredentialNames.IssueCredential:
                    CredentialRecord credentialRecordIssue =
                        await _credentialService.GetAsync(agentContext, msg.RecordId);

                    if (credentialRecordIssue.ConnectionId.Equals(App.BaseIdConnectionId))
                    {
                        App.BaseIdConnectionId = "";
                        MessagingCenter.Send(this, WalletEvents.BaseIdCredentialIssue, msg.RecordId);
                    }
                    else if (credentialRecordIssue.ConnectionId.Equals(App.VacConnectionId))
                    {
                        MessagingCenter.Send(this, WalletEvents.VacCredentialIssue, msg.RecordId);
                    }
                    else if (credentialRecordIssue.ConnectionId.Equals(App.DdlConnectionId))
                    {
                        App.DdlConnectionId = "";
                        MessagingCenter.Send(this, WalletEvents.DdlCredentialIssue, msg.RecordId);
                    }
                    else
                    {
                        await ShowCredentialAddedPopUp(credentialRecordIssue, agentContext);
                        MessagingCenter.Send(this, MessageTypes.IssueCredentialNames.IssueCredential, msg.RecordId);
                    }
                    break;

                case MessageTypes.PresentProofNames.RequestPresentation:
                case MessageTypesHttps.PresentProofNames.RequestPresentation:
                    MessagingCenter.Send(this, MessageTypes.PresentProofNames.RequestPresentation, msg.RecordId);
                    break;
            }

            ;
        }

        public virtual void Subscribe(IObservable<ServiceMessageProcessingEvent> provider)
        {
            if (provider != null)
            {
                _unsubscriber = provider.Subscribe(this);
            }
        }

        public virtual void Unsubscribe()
        {
            _unsubscriber.Dispose();
        }

        private async
            Task<(bool isProofAutoAccept, bool isProofNotification, bool isCredentialAutoAccept, bool
                isCredentialNotification)> CheckForAutoAccept(IAgentContext agentContext, string connectionId)
        {
            ConnectionRecord connectionRecord = await _connectionService.GetAsync(agentContext, connectionId);
            string autoAcceptProof = connectionRecord.GetTag("AutoAcceptProof");
            string autoNotificationProof = connectionRecord.GetTag("AutoProofNotification");
            string autoAcceptCredential = connectionRecord.GetTag("AutoAcceptCredential");
            string autoNotificationCredential = connectionRecord.GetTag("AutoCredentialNotification");

            bool isProofAutoAccept = !string.IsNullOrEmpty(autoAcceptProof) && Convert.ToBoolean(autoAcceptProof);
            bool isProofNotification =
                !string.IsNullOrEmpty(autoNotificationProof) && Convert.ToBoolean(autoNotificationProof);
            bool isCredentialAutoAccept =
                !string.IsNullOrEmpty(autoAcceptCredential) && Convert.ToBoolean(autoAcceptCredential);
            bool isCredentialNotification = !string.IsNullOrEmpty(autoNotificationCredential) &&
                                            Convert.ToBoolean(autoNotificationCredential);

            return (isProofAutoAccept, isProofNotification, isCredentialAutoAccept, isCredentialNotification);
        }

        private async Task ShowConnectionAddedPopUp(ConnectionRecord connectionRecord)
        {
            if (connectionRecord.State == ConnectionState.Connected &&
                connectionRecord.Alias.Name != WalletParams.MediatorConnectionAliasName)
            {
                ConnectionAddedPopUp popUp = new ConnectionAddedPopUp(connectionRecord);
                await popUp.ShowPopUp();
            }

            ;
        }

        private async Task ShowCredentialAddedPopUp(CredentialRecord credentialRecord, IAgentContext agentContext)
        {
            if (credentialRecord.State == CredentialState.Issued)
            {
                if (string.IsNullOrEmpty(credentialRecord.ConnectionId) ||
                    !(await CheckForAutoAccept(agentContext, credentialRecord.ConnectionId)).isCredentialAutoAccept)
                {
                    CredentialAddedPopUp popUp = new CredentialAddedPopUp(credentialRecord);
                    await popUp.ShowPopUp();
                }
            }
        }
    }
}