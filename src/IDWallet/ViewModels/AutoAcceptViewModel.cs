using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Agent.Services;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Models;
using IDWallet.Services;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Inbox;
using IDWallet.Views.Proof;
using IDWallet.Views.Proof.PopUps;
using IDWallet.Views.Settings.Connections.PopUps;
using IDWallet.Views.Wallet;
using IDWallet.Views.Wallet.PopUps;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Decorators;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Newtonsoft.Json;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class AutoAcceptViewModel : CustomViewModel
    {
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();
        private readonly ICustomAgentProvider _customAgentProvider = App.Container.Resolve<ICustomAgentProvider>();

        private readonly IMessageService _messageService = App.Container.Resolve<IMessageService>();

        private readonly CustomProofService _proofService = App.Container.Resolve<CustomProofService>();

        private readonly IProvisioningService _provisioningService = App.Container.Resolve<IProvisioningService>();

        private readonly ICustomSecureStorageService _secureStorageService =
            App.Container.Resolve<ICustomSecureStorageService>();

        private readonly ICustomWalletRecordService _walletRecordService =
                                            App.Container.Resolve<ICustomWalletRecordService>();
        private bool _emptyLayoutVisible;

        public AutoAcceptViewModel()
        {
            Title = "AutoAccept";
            AutoAcceptMessages = new List<InboxMessage>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadNotifications());
            LoadItemsCommand.Execute(null);
        }

        public List<InboxMessage> AutoAcceptMessages { get; set; }

        public bool EmptyLayoutVisible
        {
            get => _emptyLayoutVisible;
            set => SetProperty(ref _emptyLayoutVisible, value);
        }
        public Command LoadItemsCommand { get; set; }
        public static async Task WaitForProcessPopUp()
        {
            while (!App.IsLoggedIn)
            {
                await Task.Delay(10);
            }
        }

        public async Task<bool> CreateAndSendProof(ProofRecord proofRecord, bool onlyKnownProofs, bool takeNewest)
        {
            ProofRequest proofRequest = proofRecord.RequestJson.ToObject<ProofRequest>();
            ProofViewModel viewModel = new ProofViewModel(proofRequest, proofRecord.Id, onlyKnownProofs, takeNewest);
            
            var authPopUp = new ProofAuthenticationPopUp(new AuthViewModel(viewModel))
            {
                ProofSendPopUp = true
            };
#pragma warning disable CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
            authPopUp.ShowPopUp(); // No await.
#pragma warning restore CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.

            while (!viewModel.AuthSuccess)
            {
                if (viewModel.AuthError)
                {
                    return false;
                }
                await Task.Delay(100);
            }
            authPopUp.OnAuthCanceled(authPopUp, null);

            if (!viewModel.AuthError && viewModel.AuthSuccess)
            {
                viewModel.LoadItemsCommand.Execute(null);
                while (!viewModel.LoadCommandFinished)
                {
                    await Task.Delay(1000);
                }

                if (viewModel.ReadyToSend)
                {
                    await viewModel.CreateAndSendProof();
                    return true;
                }

                return false;
            }
            else
            {
                return false;
            }
        }

        public async void HandleProofRequestResult(ConnectionRecord connectionRecord = null)
        {
            try
            {
                bool showPopUp = true;
                try
                {
                    showPopUp = Convert.ToBoolean(
                        await _secureStorageService.GetAsync<string>(WalletParams.ShowSendingResponsePopUp));
                }
                catch
                {
                    await _secureStorageService.SetAsync(WalletParams.ShowSendingResponsePopUp, "True");
                }

                if (showPopUp)
                {
                    ProofSentPopUp popUp = new ProofSentPopUp()
                    {
                        ProofSendPopUp = true
                    };

                    PopUpResult result = await popUp.ShowPopUp();
                    if (result == PopUpResult.Deleted)
                    {
                        await _secureStorageService.SetAsync(WalletParams.ShowSendingResponsePopUp, "False");
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }

            if (connectionRecord != null)
            {
                string neverAskAgain = "";
                try
                {
                    neverAskAgain = connectionRecord.GetTag("NeverAskAgainProof");
                }
                catch (Exception)
                {
                    //ignore
                }

                if (string.IsNullOrEmpty(neverAskAgain))
                {
                    ActivateAutoAcceptPopUp activateAutoAcceptPopUp =
                        new ActivateAutoAcceptPopUp(connectionRecord, true)
                        {
                            ProofSendPopUp = true
                        };
                    PopUpResult accepted = await activateAutoAcceptPopUp.ShowPopUp();

                    if (accepted == PopUpResult.Accepted)
                    {
                        try
                        {
                            MessagingCenter.Send(this, WalletEvents.ReloadConnections);
                        }
                        catch (Exception)
                        {
                            //ignore
                        }
                    }
                }
            }
        }

        public void Sleep()
        {
            try
            {
                MessagingCenter.Unsubscribe<ServiceMessageEventService, string>(this,
                    MessageTypes.IssueCredentialNames.OfferCredential);
                MessagingCenter.Unsubscribe<ServiceMessageEventService, string>(this,
                    MessageTypes.PresentProofNames.RequestPresentation);
                MessagingCenter.Unsubscribe<ServiceMessageEventService, string>(this,
                    MessageTypesHttps.IssueCredentialNames.OfferCredential);
                MessagingCenter.Unsubscribe<ServiceMessageEventService, string>(this,
                    MessageTypesHttps.PresentProofNames.RequestPresentation);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public void Subscribe()
        {
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.IssueCredentialNames.OfferCredential, OnNewCredentialOffer);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypes.PresentProofNames.RequestPresentation, OnPresentationRequest);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.IssueCredentialNames.OfferCredential, OnNewCredentialOffer);
            MessagingCenter.Subscribe<ServiceMessageEventService, string>(this,
                MessageTypesHttps.PresentProofNames.RequestPresentation, OnPresentationRequest);
        }

        protected override async void Reload()
        {
            await ExecuteLoadNotifications();
        }

        private async Task CallPopUp(string proofRecordId, ProofRecord proofRecord, ConnectionRecord connectionRecord)
        {
            string presentationText = Resources.Lang.PopUp_Presentation_Request_Received_Text;
            PresentationRequestPopUp presentationRequestPopUp =
                new PresentationRequestPopUp(presentationText, connectionRecord.Alias.Name);
            PopUpResult presentationRequestPopUpResult = await presentationRequestPopUp.ShowPopUp();

            if (presentationRequestPopUpResult == PopUpResult.Accepted)
            {
                ProofRequest request = new ProofRequest();
                try
                {
                    request = proofRecord.RequestJson.ToObject<ProofRequest>();
                }
                catch (Exception)
                {
                    BasicPopUp alertPopUp2 = new BasicPopUp(
                        Resources.Lang.PopUp_Undefined_Error_Title,
                        Resources.Lang.PopUp_Undefined_Error_Message,
                        Resources.Lang.PopUp_Undefined_Error_Button);
                    await alertPopUp2.ShowPopUp();
                }

                await Task.Run(() => WaitForProcessPopUp());

                ProofViewModel proofViewModel = new ProofViewModel(request, proofRecordId);
                proofViewModel.LoadItemsCommand.Execute(null);
                while (!proofViewModel.LoadCommandFinished)
                {
                    await Task.Delay(100);
                }

                if (proofViewModel.ReadyToSend)
                {
                    ProofPopUp popUp = new ProofPopUp(proofViewModel, proofRecordId, null, connectionRecord.Alias.Name);
                    PopUpResult result = await popUp.ShowPopUp();
                    if (result == PopUpResult.Accepted)
                    {
                        HandleProofRequestResult(connectionRecord);
                    }
                }
                else
                {
                    ProofMissingCredentialsPopUp popUp = new ProofMissingCredentialsPopUp(request, proofViewModel.FailedRequests);
                    await popUp.ShowPopUp();
                }
            }
        }

        private (bool isProofAutoAccept, bool isProofNotification, bool isCredentialAutoAccept, bool
            isCredentialNotification,
            bool onlyKnownProofs, bool takeNewest) CheckForAutoAccept(ConnectionRecord connectionRecord)
        {
            string autoAcceptProof = connectionRecord.GetTag("AutoAcceptProof");
            string autoNotificationProof = connectionRecord.GetTag("AutoProofNotification");
            string autoAcceptCredential = connectionRecord.GetTag("AutoAcceptCredential");
            string autoNotificationCredential = connectionRecord.GetTag("AutoCredentialNotification");
            string onlyKnownProofs = connectionRecord.GetTag("OnlyKnownProofs");
            string takeNewest = connectionRecord.GetTag("TakeNewest");

            bool isProofAutoAccept = !string.IsNullOrEmpty(autoAcceptProof) && Convert.ToBoolean(autoAcceptProof);
            bool isProofNotification =
                !string.IsNullOrEmpty(autoNotificationProof) && Convert.ToBoolean(autoNotificationProof);
            bool isCredentialAutoAccept =
                !string.IsNullOrEmpty(autoAcceptCredential) && Convert.ToBoolean(autoAcceptCredential);
            bool isCredentialNotification = !string.IsNullOrEmpty(autoNotificationCredential) &&
                                            Convert.ToBoolean(autoNotificationCredential);
            bool isOnlyKnownProofs = !string.IsNullOrEmpty(onlyKnownProofs) && Convert.ToBoolean(onlyKnownProofs);
            bool isTakeNewest = !string.IsNullOrEmpty(takeNewest) && Convert.ToBoolean(takeNewest);

            return (isProofAutoAccept, isProofNotification, isCredentialAutoAccept, isCredentialNotification,
                isOnlyKnownProofs, isTakeNewest);
        }

        private async Task CreateAutoNotification(string connectionRecordId, ProofRecord proofRecord)
        {
            IAgentContext context = await _customAgentProvider.GetContextAsync();
            ConnectionRecord connectionRecord =
                await _walletRecordService.GetAsync<ConnectionRecord>(context.Wallet, connectionRecordId, true);
            AutoAcceptMessages.Add(new AutoAcceptMessage(connectionRecord, proofRecord));

            string openNotificationsJson = connectionRecord.GetTag("OpenProofNotifications");

            List<string> openNotifications = new List<string>();
            try
            {
                openNotifications = string.IsNullOrEmpty(openNotificationsJson)
                    ? new List<string>()
                    : JsonConvert.DeserializeObject<List<string>>(openNotificationsJson);
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Undefined_Error_Title,
                    Resources.Lang.PopUp_Undefined_Error_Message,
                    Resources.Lang.PopUp_Undefined_Error_Button);
                await alertPopUp.ShowPopUp();
            }

            openNotifications.Add(proofRecord.Id);
            try
            {
                connectionRecord.SetTag("OpenProofNotifications", JsonConvert.SerializeObject(openNotifications));
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Undefined_Error_Title,
                    Resources.Lang.PopUp_Undefined_Error_Message,
                    Resources.Lang.PopUp_Undefined_Error_Button);
                await alertPopUp.ShowPopUp();
            }

            await _walletRecordService.UpdateAsync(context.Wallet, connectionRecord);

            Timer t1 = new Timer
            {
                Interval = 5000,
                AutoReset = false
            };
            t1.Elapsed += EnableNotification;
            t1.Start();
        }

        private async Task CreateAutoNotification(string connectionRecordId, CredentialRecord credentialRecord)
        {
            IAgentContext context = await _customAgentProvider.GetContextAsync();
            ConnectionRecord connectionRecord =
                await _walletRecordService.GetAsync<ConnectionRecord>(context.Wallet, connectionRecordId, true);
            AutoAcceptMessages.Add(new AutoAcceptMessage(connectionRecord, credentialRecord));

            string openNotificationsJson = connectionRecord.GetTag("OpenCredentialNotifications");

            List<string> openNotifications = new List<string>();

            try
            {
                openNotifications = string.IsNullOrEmpty(openNotificationsJson)
                    ? new List<string>()
                    : JsonConvert.DeserializeObject<List<string>>(openNotificationsJson);
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Undefined_Error_Title,
                    Resources.Lang.PopUp_Undefined_Error_Message,
                    Resources.Lang.PopUp_Undefined_Error_Button);
                await alertPopUp.ShowPopUp();
            }

            openNotifications.Add(credentialRecord.Id);
            try
            {
                connectionRecord.SetTag("OpenCredentialNotifications", JsonConvert.SerializeObject(openNotifications));
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Undefined_Error_Title,
                    Resources.Lang.PopUp_Undefined_Error_Message,
                    Resources.Lang.PopUp_Undefined_Error_Button);
                await alertPopUp.ShowPopUp();
            }

            await _walletRecordService.UpdateAsync(context.Wallet, connectionRecord);

            Timer t1 = new Timer
            {
                Interval = 5000,
                AutoReset = false
            };
            t1.Elapsed += EnableNotification;
            t1.Start();
        }

        private void EnableNotification(object source, ElapsedEventArgs e)
        {
            TabbedPage mainPage = (TabbedPage)Application.Current.MainPage;
            WalletViewModel credentialsViewModel =
                ((WalletPage)((NavigationPage)mainPage.Children[0]).RootPage).ViewModel;
            credentialsViewModel.EnableNotification();
        }

        private async Task ExecuteLoadNotifications()
        {
            try
            {
                if (!IsConnected)
                {
                    return;
                }

                IAgentContext agentContext = await _customAgentProvider.GetContextAsync();

                AutoAcceptMessages.Clear();
                List<ConnectionRecord> allConnections = await _connectionService.ListAsync(agentContext);
                foreach (ConnectionRecord connection in allConnections)
                {
                    if (connection.GetTag("OpenCredentialNotifications") != null)
                    {
                        List<string> openCredentialNotifications = new List<string>();
                        try
                        {
                            openCredentialNotifications =
                                JsonConvert.DeserializeObject<List<string>>(
                                    connection.GetTag("OpenCredentialNotifications"));
                        }
                        catch (Exception)
                        {
                            BasicPopUp alertPopUp = new BasicPopUp(
                                Resources.Lang.PopUp_Undefined_Error_Title,
                                Resources.Lang.PopUp_Undefined_Error_Execute_Notifications_Message,
                                Resources.Lang.PopUp_Undefined_Error_Button);
                            await alertPopUp.ShowPopUp();
                        }

                        foreach (string recordId in openCredentialNotifications)
                        {
                            try
                            {
                                CredentialRecord credentialRecord =
                                    await _credentialService.GetAsync(agentContext, recordId);
                                AutoAcceptMessages.Add(new AutoAcceptMessage(connection, credentialRecord));
                            }
                            catch (Exception)
                            {
                                BasicPopUp alertPopUp = new BasicPopUp(
                                    Resources.Lang.PopUp_Undefined_Error_Title,
                                    Resources.Lang.PopUp_Undefined_Error_Execute_Notifications_Add_Message,
                                    Resources.Lang.PopUp_Undefined_Error_Button);
                                await alertPopUp.ShowPopUp();
                            }
                        }
                    }

                    if (connection.GetTag("OpenProofNotifications") != null)
                    {
                        List<string> openProofNotifications = new List<string>();
                        try
                        {
                            openProofNotifications =
                                JsonConvert.DeserializeObject<List<string>>(
                                    connection.GetTag("OpenProofNotifications"));
                        }
                        catch (Exception)
                        {
                            BasicPopUp alertPopUp = new BasicPopUp(
                                Resources.Lang.PopUp_Undefined_Error_Title,
                                Resources.Lang.PopUp_Undefined_Error_Execute_Notifications_Proof_Message,
                                Resources.Lang.PopUp_Undefined_Error_Button);
                            await alertPopUp.ShowPopUp();
                        }

                        foreach (string recordId in openProofNotifications)
                        {
                            try
                            {
                                ProofRecord proofRecord = await _proofService.GetAsync(agentContext, recordId);
                                AutoAcceptMessages.Add(new AutoAcceptMessage(connection, proofRecord));
                            }
                            catch (Exception)
                            {
                                //ignore
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private async void OnNewCredentialOffer(ServiceMessageEventService arg1, string credentialRecordId)
        {
            try
            {
                IAgentContext context = await _customAgentProvider.GetContextAsync();
                CredentialRecord credentialRecord =
                    await _walletRecordService.GetAsync<CredentialRecord>(context.Wallet, credentialRecordId);
                ConnectionRecord connectionRecord = null;
                try
                {
                    connectionRecord = await _connectionService.GetAsync(context, credentialRecord.ConnectionId);
                }
                catch (Exception)
                {
                    //ignore
                }

                bool autoAcceptOn = false;
                bool autoNotificationOn = false;
                if (connectionRecord != null)
                {
                    (bool isProofAutoAccept, bool isProofNotification, bool isCredentialAutoAccept, bool
                        isCredentialNotification, bool onlyKnownProofs, bool takeNewest) autoAccept =
                            CheckForAutoAccept(connectionRecord);
                    (autoAcceptOn, autoNotificationOn) = (autoAccept.isCredentialAutoAccept,
                        autoAccept.isCredentialNotification);
                }

                if (autoAcceptOn)
                {
                    if (await PermissionCheck())
                    {
                        try
                        {
                            (CredentialRequestMessage request, CredentialRecord record) =
                                await _credentialService.CreateRequestAsync(context, credentialRecordId);
                            await _messageService.SendAsync(context, request, connectionRecord);

                            if (autoNotificationOn)
                            {
                                await CreateAutoNotification(connectionRecord.Id, credentialRecord);
                            }
                        }
                        catch (Exception ex)
                        {
                            credentialRecord =
                                await _walletRecordService.GetAsync<CredentialRecord>(context.Wallet,
                                    credentialRecordId, true);
                            credentialRecord.SetTag("AutoError", "true");
                            await _walletRecordService.UpdateAsync(context.Wallet, credentialRecord);

                            BasicPopUp alertPopUp = new BasicPopUp(
                                Resources.Lang.PopUp_Credential_Error_Title,
                                Resources.Lang.PopUp_Credential_Error_Message,
                                Resources.Lang.PopUp_Credential_Error_Button);
                            await alertPopUp.ShowPopUp();
                        }
                    }
                }
                else if (App.IsLoggedIn)
                {
                    string connectionAlias = !(connectionRecord == null)
                        ? connectionRecord.Alias.Name
                        : credentialRecord.GetTag(DecoratorNames.ServiceDecorator).ToObject<CustomServiceDecorator>()
                            .ServiceEndpoint;

                    NewCredentialOfferPopUp alertPopUp = new NewCredentialOfferPopUp(connectionAlias);
                    PopUpResult alertPopUpResult = await alertPopUp.ShowPopUp();
                    if (alertPopUpResult == PopUpResult.Accepted)
                    {
                        await Task.Run(() => WaitForProcessPopUp());
                        InboxPage notificationsPage = null;
                        InboxViewModel notificationsViewModel = null;
                        try
                        {
                            bool nextPageExists = false;
                            IEnumerator<Page> oldPageEnumerator =
                                Application.Current.MainPage.Navigation.ModalStack.GetEnumerator();
                            do
                            {
                                nextPageExists = oldPageEnumerator.MoveNext();
                            } while (nextPageExists && !(oldPageEnumerator.Current is InboxPage));

                            if (oldPageEnumerator.Current is InboxPage)
                            {
                                notificationsPage = (InboxPage)oldPageEnumerator.Current;
                                notificationsViewModel = notificationsPage.ViewModel;
                                notificationsViewModel.LoadItemsCommand.Execute(null);
                            }
                        }
                        catch (Exception)
                        {
                            notificationsPage = new InboxPage();
                            notificationsViewModel = notificationsPage.ViewModel;
                            notificationsViewModel.LoadItemsCommand.Execute(null);
                        }
                        finally
                        {
                            if (notificationsPage == null)
                            {
                                notificationsPage = new InboxPage();
                                notificationsViewModel = notificationsPage.ViewModel;
                                notificationsViewModel.LoadItemsCommand.Execute(null);
                            }
                        }

                        WalletCredentialOfferMessage credentialOfferNotification =
                            new WalletCredentialOfferMessage(credentialRecord, connectionRecord);
                        OfferCredentialPopUp popUp = new OfferCredentialPopUp(credentialOfferNotification);

                        if (Application.Current.MainPage is TabbedPage tabbedPage)
                        {
                            tabbedPage.CurrentPage = tabbedPage.Children[0];
                        }

                        PopUpResult popUpResult = await popUp.ShowPopUp();

                        PermissionStatus storagePermissionStatus =
                            await CrossPermissions.Current.CheckPermissionStatusAsync<StoragePermission>();
                        if (storagePermissionStatus == PermissionStatus.Granted)
                        {
                            if (popUpResult == PopUpResult.Accepted)
                            {
                                try
                                {
                                    try
                                    {
                                        InboxMessage notification =
                                            notificationsViewModel.InboxMessages.FirstOrDefault(x =>
                                                x.RecordId == credentialRecordId);
                                        try
                                        {
                                            notificationsViewModel.InboxMessages.Remove(notification);
                                        }
                                        catch (Exception)
                                        {
                                            //ignore
                                        }

                                        notificationsViewModel.OnRequestFinished();
                                    }
                                    catch (Exception)
                                    {
                                        //ignore
                                    }

                                    if (!string.IsNullOrEmpty(credentialRecord.ConnectionId))
                                    {
                                        (CredentialRequestMessage request, CredentialRecord record) =
                                            await _credentialService.CreateRequestAsync(context,
                                                credentialOfferNotification.RecordId);

                                        await _messageService.SendAsync(context, request,
                                            credentialOfferNotification.ConnectionRecord);

                                        string neverAskAgain = "";
                                        try
                                        {
                                            neverAskAgain = connectionRecord.GetTag("NeverAskAgainCred");
                                        }
                                        catch (Exception ex)
                                        {
                                            BasicPopUp alertPopUp2 = new BasicPopUp(
                                                Resources.Lang.PopUp_Undefined_Error_Title,
                                                Resources.Lang.PopUp_Undefined_Error_Message,
                                                Resources.Lang.PopUp_Undefined_Error_Button);
                                            await alertPopUp2.ShowPopUp();
                                        }

                                        if (string.IsNullOrEmpty(neverAskAgain))
                                        {
                                            ActivateAutoAcceptPopUp activateAutoAcceptPopUp =
                                                new ActivateAutoAcceptPopUp(connectionRecord);
                                            PopUpResult accepted = await activateAutoAcceptPopUp.ShowPopUp();

                                            if (accepted == PopUpResult.Accepted)
                                            {
                                                try
                                                {
                                                    MessagingCenter.Send(this, WalletEvents.ReloadConnections);
                                                }
                                                catch (Exception)
                                                {
                                                    //ignore
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        (CredentialRequestMessage request, CredentialRecord record) =
                                            await _credentialService.CreateRequestAsync(context, credentialRecord.Id);
                                        ProvisioningRecord provisioning =
                                            await _provisioningService.GetProvisioningAsync(context.Wallet);

                                        CustomServiceDecorator service = record.GetTag(DecoratorNames.ServiceDecorator)
                                            .ToObject<CustomServiceDecorator>();

                                        CredentialIssueMessage credentialIssueMessage =
                                            await _messageService.SendReceiveAsync<CredentialIssueMessage>(
                                                agentContext: context,
                                                message: request,
                                                recipientKey: service.RecipientKeys.First(),
                                                endpointUri: service.ServiceEndpoint,
                                                routingKeys: service.RoutingKeys.ToArray(),
                                                senderKey: provisioning.IssuerVerkey);
                                        string recordId =
                                            await _credentialService.ProcessCredentialAsync(context,
                                                credentialIssueMessage, null);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    BasicPopUp alertPopUp3 = new BasicPopUp(
                                        Resources.Lang.PopUp_Credential_Error_Title,
                                        Resources.Lang.PopUp_Credential_Error_Message,
                                        Resources.Lang.PopUp_Credential_Error_Button);
                                    await alertPopUp3.ShowPopUp();
                                }
                            }

                            else if (popUpResult == PopUpResult.Deleted)
                            {
                                InboxMessage notification =
                                    notificationsViewModel.InboxMessages.First(x => x.RecordId == credentialRecordId);
                                try
                                {
                                    notificationsViewModel.InboxMessages.Remove(notification);
                                }
                                catch (Exception)
                                {
                                    //ignore
                                }

                                notificationsViewModel.OnRequestFinished();

                                await _credentialService.RejectOfferAsync(context,
                                    credentialOfferNotification.RecordId);
                                await _walletRecordService.DeleteAsync<CredentialRecord>(context.Wallet,
                                    credentialOfferNotification.RecordId);
                            }
                        }
                        else
                        {
                            BasicPopUp permissionAlertPopUp = new BasicPopUp(
                                Resources.Lang.PopUp_Storage_Permission_Needed_Credential_Title,
                                Resources.Lang.PopUp_Storage_Permission_Needed_Credential_Text,
                                Resources.Lang.PopUp_Storage_Permission_Needed_Credential_Button);
                            await permissionAlertPopUp.ShowPopUp();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                BasicPopUp alertPopUp4 = new BasicPopUp(
                    Resources.Lang.PopUp_Credential_Error_Title,
                    Resources.Lang.PopUp_Credential_Error_Message,
                    Resources.Lang.PopUp_Credential_Error_Button);
                await alertPopUp4.ShowPopUp();
            }
        }

        private async void OnPresentationRequest(ServiceMessageEventService arg1, string proofRecordId)
        {
            try
            {
                IAgentContext context = await _customAgentProvider.GetContextAsync();
                ProofRecord proofRecord =
                    await _walletRecordService.GetAsync<ProofRecord>(context.Wallet, proofRecordId);

                if (!string.IsNullOrEmpty(proofRecord.ConnectionId))
                {
                    ConnectionRecord connectionRecord =
                        await _connectionService.GetAsync(context, proofRecord.ConnectionId);

                    (bool isProofAutoAccept, bool isProofNotification, bool isCredentialAutoAccept, bool
                        isCredentialNotification, bool onlyKnownProofs, bool takeNewest) autoAccept =
                            CheckForAutoAccept(connectionRecord);
                    (bool autoAcceptOn, bool autoNotificationOn) =
                        (autoAccept.isProofAutoAccept, autoAccept.isProofNotification);

                    ProofRequest proofJsonAsObject = proofRecord.RequestJson.ToObject<ProofRequest>();

                    if (autoAcceptOn)
                    {
                        if (await PermissionCheck())
                        {
                            bool sendComplete = false;
                            (bool onlyKnownProofs, bool takeNewest) =
                                (autoAccept.onlyKnownProofs, autoAccept.takeNewest);
                            try
                            {
                                sendComplete = await CreateAndSendProof(proofRecord, onlyKnownProofs, takeNewest);

                                if (!sendComplete)
                                {
                                    proofRecord =
                                        await _walletRecordService.GetAsync<ProofRecord>(context.Wallet, proofRecordId,
                                            true);
                                    proofRecord.SetTag("AutoError", "true");

                                    await _walletRecordService.UpdateAsync(context.Wallet, proofRecord);

                                    await CallPopUp(proofRecordId, proofRecord, connectionRecord);

                                    if (App.WaitForProof && !string.IsNullOrEmpty(App.AwaitableProofConnectionId) &&
                                        App.AwaitableProofConnectionId.Equals(proofRecord.ConnectionId))
                                    {
                                        App.WaitForProof = false;
                                    }
                                }
                                else
                                {
                                    MessagingCenter.Send(this, WalletEvents.SentProofRequest, proofRecord.Id);

                                    if (App.WaitForProof && !string.IsNullOrEmpty(App.AwaitableProofConnectionId) &&
                                        App.AwaitableProofConnectionId.Equals(proofRecord.ConnectionId))
                                    {
                                        App.WaitForProof = false;
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                proofRecord =
                                    await _walletRecordService.GetAsync<ProofRecord>(context.Wallet, proofRecordId,
                                        true);
                                proofRecord.SetTag("AutoError", "true");

                                await _walletRecordService.UpdateAsync(context.Wallet, proofRecord);

                                if (App.WaitForProof && !string.IsNullOrEmpty(App.AwaitableProofConnectionId) &&
                                    App.AwaitableProofConnectionId.Equals(proofRecord.ConnectionId))
                                {
                                    App.WaitForProof = false;
                                }

                                BasicPopUp alertPopUp = new BasicPopUp(
                                    Resources.Lang.PopUp_Proof_Sending_Error_Title,
                                    Resources.Lang.PopUp_Proof_Sending_Error_Message,
                                    Resources.Lang.PopUp_Proof_Sending_Error_Button);
                                await alertPopUp.ShowPopUp();
                            }

                            if (autoNotificationOn && sendComplete)
                            {
                                await CreateAutoNotification(connectionRecord.Id, proofRecord);
                            }
                        }
                        else
                        {
                            if (App.WaitForProof)
                            {
                                App.WaitForProof = false;
                            }
                        }
                    }
                    else if (App.IsLoggedIn)
                    {
                        await CallPopUp(proofRecordId, proofRecord, connectionRecord);

                        if (App.WaitForProof && !string.IsNullOrEmpty(App.AwaitableProofConnectionId) &&
                            App.AwaitableProofConnectionId.Equals(proofRecord.ConnectionId))
                        {
                            App.WaitForProof = false;
                        }
                    }
                }
                else
                {
                    if (App.IsLoggedIn)
                    {
                        CustomServiceDecorator service = proofRecord.GetTag(DecoratorNames.ServiceDecorator)
                            .ToObject<CustomServiceDecorator>();

                        string presentationText = Resources.Lang.PopUp_Presentation_Request_Received_Text;
                        var endpointUri = new Uri(service.ServiceEndpoint);
                        string serviceAlias = !string.IsNullOrEmpty(service.EndpointName) ? service.EndpointName + " - " + endpointUri.Host : service.ServiceEndpoint;
                        PresentationRequestPopUp presentationRequestPopUp =
                            new PresentationRequestPopUp(presentationText, serviceAlias);
                        PopUpResult presentationRequestPopUpResult = await presentationRequestPopUp.ShowPopUp();

                        if (presentationRequestPopUpResult == PopUpResult.Accepted)
                        {
                            ProofRequest request = new ProofRequest();
                            try
                            {
                                request = proofRecord.RequestJson.ToObject<ProofRequest>();
                            }
                            catch (Exception)
                            {
                                BasicPopUp alertPopUp2 = new BasicPopUp(
                                    Resources.Lang.PopUp_Undefined_Error_Title,
                                    Resources.Lang.PopUp_Undefined_Error_Message,
                                    Resources.Lang.PopUp_Undefined_Error_Button);
                                await alertPopUp2.ShowPopUp();
                            }

                            await Task.Run(() => WaitForProcessPopUp());

                            ProofViewModel proofViewModel = new ProofViewModel(request, proofRecordId);
                            proofViewModel.LoadItemsCommand.Execute(null);
                            while (!proofViewModel.LoadCommandFinished)
                            {
                                await Task.Delay(100);
                            }

                            if (proofViewModel.ReadyToSend)
                            {
                                var endpointUri2 = new Uri(service.ServiceEndpoint);
                                string serviceName = !string.IsNullOrEmpty(service.EndpointName) ? service.EndpointName + " - " + endpointUri.Host : service.ServiceEndpoint;
                                ProofPopUp popUp = new ProofPopUp(proofViewModel, proofRecordId, service, serviceName);
                                PopUpResult result = await popUp.ShowPopUp();
                                if (result == PopUpResult.Accepted)
                                {
                                    HandleProofRequestResult();
                                }
                            }
                            else
                            {
                                ProofMissingCredentialsPopUp popUp = new ProofMissingCredentialsPopUp(request, proofViewModel.FailedRequests);
                                await popUp.ShowPopUp();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Proof_Sending_Error_Title,
                    Resources.Lang.PopUp_Proof_Sending_Error_Message,
                    Resources.Lang.PopUp_Proof_Sending_Error_Button);
                await alertPopUp.ShowPopUp();
            }
        }
        private async Task<bool> PermissionCheck()
        {
            PermissionStatus storagePermissionStatus =
                await CrossPermissions.Current.CheckPermissionStatusAsync<StoragePermission>();
            if (storagePermissionStatus == PermissionStatus.Granted)
            {
                return true;
            }
            else
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Storage_Permission_Needed_Proof_Title,
                    Resources.Lang.PopUp_Storage_Permission_Needed_Proof_Text,
                    Resources.Lang.PopUp_Storage_Permission_Needed_Proof_Button);
                await alertPopUp.ShowPopUp();
                return false;
            }
        }
    }
}