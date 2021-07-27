using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Agent.Services;
using IDWallet.Events;
using IDWallet.Models;
using IDWallet.Resources;
using IDWallet.ViewModels;
using IDWallet.Views.BaseId.PopUps;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Proof;
using IDWallet.Views.Proof.PopUps;
using IDWallet.Views.Settings.Connections.PopUps;
using IDWallet.Views.Wallet.PopUps;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Decorators;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Indy.AnonCredsApi;
using Newtonsoft.Json;
using Plugin.Permissions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Inbox.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InboxListElement : ContentView
    {
        private readonly InboxPage _basePage;
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();
        private readonly ICustomAgentProvider _customAgentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IMessageService _messageService = App.Container.Resolve<IMessageService>();
        private readonly CustomProofService _proofService = App.Container.Resolve<CustomProofService>();


        private readonly InboxViewModel _viewModel;
        private readonly ICustomWalletRecordService _walletRecordService =
            App.Container.Resolve<ICustomWalletRecordService>();

        public InboxListElement()
        {
            InitializeComponent();

            InboxPage notificationsPage = null;
            try
            {
                bool nextPageExists = false;
                TabbedPage mainPage = (TabbedPage)Application.Current.MainPage;
                IEnumerator<Page> oldPageEnumerator =
                    ((NavigationPage)mainPage.CurrentPage).Navigation.NavigationStack.GetEnumerator();
                do
                {
                    nextPageExists = oldPageEnumerator.MoveNext();
                } while (nextPageExists && !(oldPageEnumerator.Current is InboxPage));

                if (oldPageEnumerator.Current is InboxPage)
                {
                    notificationsPage = (InboxPage)oldPageEnumerator.Current;
                }
            }
            catch (Exception)
            {
                notificationsPage = new InboxPage();
            }
            finally
            {
                if (notificationsPage == null)
                {
                    notificationsPage = new InboxPage();
                }
            }

            _basePage = notificationsPage;
            _viewModel = notificationsPage.ViewModel;
        }

        private void ConnectionAliasLabel_BindingContextChanged(object sender, EventArgs e)
        {
            try
            {
                object notification = (sender as Label)?.BindingContext;
                if (notification is NewProofRequestMessage proofRequestNotification)
                {
                    ProofRequest request = proofRequestNotification.ProofRecord.RequestJson.ToObject<ProofRequest>();
                    TitleLabel.Text = request.Name;

                    CustomServiceDecorator service = null;
                    if (proofRequestNotification.ProofRecord.ConnectionId == null)
                    {
                        service = proofRequestNotification.ProofRecord.GetTag(DecoratorNames.ServiceDecorator)
                            .ToObject<CustomServiceDecorator>();
                    }
                    var endpointUri = new Uri(service.ServiceEndpoint);
                    ConnectionAliasLabel.Text = !string.IsNullOrEmpty(service.EndpointName) ? service.EndpointName + " - " + endpointUri.Host : service.ServiceEndpoint;
                }
                else if (notification is Models.WalletCredentialOfferMessage credentialOfferNotification)
                {
                    TitleLabel.Text = credentialOfferNotification.CredentialTitle;
                }
                else if (notification is AutoAcceptMessage autoAcceptNotification)
                {
                    TitleLabel.Text = autoAcceptNotification.Title;
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private ConnectionElement CreateConnectionDetailsItem(object detail, ProofRecord proofRecord = null)
        {
            if (detail is CredentialContent credentialDetail)
            {
                Uri baseUri = new Uri(WalletParams.MediatorEndpoint);
                ConnectionDetailsCredential cred = new ConnectionDetailsCredential
                {
                    RecordId = credentialDetail.CredentialRecord.CredentialId,
                    Title = credentialDetail.Title,
                    State = credentialDetail.State,
                    CreatedAtUtc = credentialDetail.CreatedAtUtc,
                    Attributes = credentialDetail.Attributes
                };

                if (App.UseMediatorImages)
                {
                    cred.ImageUri = ImageSource.FromUri(new Uri(baseUri,
                        $"api/Image/{credentialDetail.CredentialRecord.CredentialDefinitionId}"));
                }
                else
                {
                    cred.ImageUri = ImageSource.FromFile("default_logo.png");
                }

                return (cred);
            }

            else
            {
                CredentialHistoryElements presentedCredentials = (CredentialHistoryElements)detail;

                DateTime? createDatetime = proofRecord.CreatedAtUtc;

                string proofTitle = "";
                try
                {
                    proofTitle = JsonConvert.DeserializeObject<ProofRequest>(proofRecord.RequestJson).Name;
                }
                catch (Exception)
                {
                    BasicPopUp alertPopUp = new BasicPopUp(
                        Lang.PopUp_Undefined_Error_Title,
                        Lang.PopUp_Undefined_Error_Message,
                        Lang.PopUp_Undefined_Error_Button);
                    alertPopUp.ShowPopUp().Wait();
                }

                List<CredentialClaim> revealedList = presentedCredentials.RevealedClaims.OrderBy(x => x.Name).ToList();
                ObservableCollection<CredentialClaim> revealed = new ObservableCollection<CredentialClaim>();
                foreach (CredentialClaim claim in revealedList)
                {
                    revealed.Add(claim);
                }

                List<CredentialClaim> nonrevealedList =
                    presentedCredentials.NonRevealedClaims.OrderBy(x => x.Name).ToList();
                ObservableCollection<CredentialClaim> nonrevealed = new ObservableCollection<CredentialClaim>();
                foreach (CredentialClaim claim in nonrevealedList)
                {
                    nonrevealed.Add(claim);
                }

                List<CredentialClaim> predicatesList =
                    presentedCredentials.PredicateClaims.OrderBy(x => x.Name).ToList();
                ObservableCollection<CredentialClaim> predicates = new ObservableCollection<CredentialClaim>();
                foreach (CredentialClaim claim in predicatesList)
                {
                    predicates.Add(new CredentialClaim
                    {
                        CredentialRecordId = claim.CredentialRecordId,
                        Name = claim.Name,
                        PredicateType = claim.PredicateType,
                        Value = claim.PredicateType + " " + claim.Value
                    });
                }

                List<CredentialClaim> selfsList = presentedCredentials.SelfAttestedClaims.OrderBy(x => x.Name).ToList();
                ObservableCollection<CredentialClaim> selfs = new ObservableCollection<CredentialClaim>();
                foreach (CredentialClaim claim in selfsList)
                {
                    selfs.Add(claim);
                }

                ConnectionDetailsPresentation cred = new ConnectionDetailsPresentation
                {
                    Title = proofTitle,
                    CreatedAtUtc = createDatetime,
                    ImageUri = string.IsNullOrEmpty(presentedCredentials.ConnectionRecord.Alias.ImageUrl)
                        ? ImageSource.FromFile("default_logo.png")
                        : new Uri(presentedCredentials.ConnectionRecord.Alias.ImageUrl),
                    State = Lang.WalletPage_History_Panel_Status_Shared,
                    RevealedAttributes = revealed,
                    NonRevealedAttributes = nonrevealed,
                    SelfAttested = selfs,
                    Predicates = predicates,
                    ProofRecord = proofRecord
                };

                return (cred);
            }
        }

        private async Task<CredentialContent> CreateCredentialDetail(CredentialRecord credential,
            ConnectionRecord connectionRecord)
        {
            IAgentContext agentContext = await _customAgentProvider.GetContextAsync();

            string[] credentialIdComponents = credential.CredentialDefinitionId.Split(':');

            CredentialContent credentialDetail = new CredentialContent
            {
                CredentialRecord = credential,
                ConnectionRecord = connectionRecord,
                Title = credentialIdComponents[4],
                State = Lang.ConnectionDetailsPage_Status_Issued
            };

            credentialDetail.CreatedAtUtc = credential.CreatedAtUtc;

            CredentialInfo credentialInfo = new CredentialInfo();
            try
            {
                credentialInfo = JsonConvert.DeserializeObject<CredentialInfo>(
                    await AnonCreds.ProverGetCredentialAsync(agentContext.Wallet, credential.CredentialId));
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Lang.PopUp_Undefined_Error_Title,
                    Lang.PopUp_Undefined_Error_Message,
                    Lang.PopUp_Undefined_Error_Button);
                await alertPopUp.ShowPopUp();
            }

            foreach (KeyValuePair<string, string> credentialAttribute in credentialInfo.Attributes)
            {
                credentialDetail.Attributes.Add(new CredentialContentAttribute
                {
                    Name = credentialAttribute.Key,
                    Value = credentialAttribute.Value
                });
            }

            return credentialDetail;
        }

        private async void OnCredentialOfferPopUpResult(PopUpResult popUpResult,
            Models.WalletCredentialOfferMessage credentialOfferNotification)
        {
            if (PopUpResult.Accepted == popUpResult)
            {
                _viewModel.InboxMessages.Remove(credentialOfferNotification);
                _viewModel.OnRequestFinished();
                IAgentContext context = await _customAgentProvider.GetContextAsync();
                try
                {
                    string baseIdIssuerDid = credentialOfferNotification.CredentialRecord.CredentialDefinitionId.Split(':')[0];
                    if (baseIdIssuerDid == "XwQCiUus8QubFNJPJD2mDi" || baseIdIssuerDid == "Vq2C7Wfc44Q1cSroPuXaw2" || baseIdIssuerDid == "5PmwwGsFhq8NDiRCyqjNXy")
                    {
                        ConnectionRecord baseIdConnection = credentialOfferNotification.ConnectionRecord;
                        string revocationPassphrase = baseIdConnection.GetTag(WalletParams.KeyRevocationPassphrase);

                        (CredentialRequestMessage request, CredentialRecord record) = await _credentialService.CreateRequestAsync(context, credentialOfferNotification.RecordId);
                        await _messageService.SendAsync(context, request, credentialOfferNotification.ConnectionRecord);

                        if (!string.IsNullOrEmpty(revocationPassphrase))
                        {
                            LockPINPopUp lockPinPopUp = new LockPINPopUp(revocationPassphrase);
                            await lockPinPopUp.ShowPopUp();
                        }
                    }
                    else
                    {
                        (CredentialRequestMessage request, CredentialRecord record) = await _credentialService.CreateRequestAsync(context, credentialOfferNotification.RecordId);

                        string[] routingKeys = credentialOfferNotification.ConnectionRecord.Endpoint?.Verkey != null
                        ? credentialOfferNotification.ConnectionRecord.Endpoint.Verkey
                        : new string[0];

                        await _messageService.SendAsync(context, request, credentialOfferNotification.ConnectionRecord);

                        ConnectionRecord connectionRecord = credentialOfferNotification.ConnectionRecord;
                        string neverAskAgain = "";
                        try
                        {
                            neverAskAgain = connectionRecord.GetTag("NeverAskAgainCred");
                        }
                        catch (Exception)
                        {
                            //ignore
                        }

                        if (neverAskAgain == null || neverAskAgain == "")
                        {
                            ActivateAutoAcceptPopUp activateAutoAcceptPopUp = new ActivateAutoAcceptPopUp(connectionRecord);
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
                catch (Exception)
                {
                    BasicPopUp alertPopUp = new BasicPopUp(
                        Lang.PopUp_Credential_Error_Title,
                        Lang.PopUp_Credential_Error_Message,
                        Lang.PopUp_Credential_Error_Button);
                    await alertPopUp.ShowPopUp();
                }
            }
            else if (PopUpResult.Deleted == popUpResult)
            {
                _viewModel.InboxMessages.Remove(credentialOfferNotification);
                _viewModel.OnRequestFinished();
                IAgentContext context = await _customAgentProvider.GetContextAsync();
                await _credentialService.RejectOfferAsync(context, credentialOfferNotification.RecordId);
                await _walletRecordService.DeleteAsync<CredentialRecord>(context.Wallet,
                    credentialOfferNotification.RecordId);
            }
        }

        private async void OnInboxDeleted(object sender, EventArgs e)
        {
            _basePage.DisableAll();
            object notification = (sender as Button)?.BindingContext;
            IAgentContext context = await _customAgentProvider.GetContextAsync();

            _viewModel.InboxMessages.Remove(notification as InboxMessage);
            App.AutoAcceptViewModel.AutoAcceptMessages.Remove(notification as InboxMessage);
            _viewModel.OnRequestFinished();

            if (notification is NewProofRequestMessage proofRequestNotification)
            {
                await _proofService.RejectProofRequestAsync(context, proofRequestNotification.RecordId);
            }

            else if (notification is Models.WalletCredentialOfferMessage credentialOfferNotification)
            {
                await _credentialService.RejectOfferAsync(context, credentialOfferNotification.RecordId);
            }

            else if (notification is AutoAcceptMessage autoAcceptNotification)
            {
                ConnectionRecord connectionRecord =
                    await _walletRecordService.GetAsync<ConnectionRecord>(context.Wallet,
                        autoAcceptNotification.ConnectionRecord.Id, true);
                if (autoAcceptNotification.CredentialId == null)
                {
                    try
                    {
                        List<string> openNotifications =
                            JsonConvert.DeserializeObject<List<string>>(
                                connectionRecord.GetTag("OpenProofNotifications"));
                        openNotifications.Remove(autoAcceptNotification.RecordId);
                        connectionRecord.SetTag("OpenProofNotifications",
                            JsonConvert.SerializeObject(openNotifications));
                    }
                    catch (Exception)
                    {
                        BasicPopUp alertPopUp = new BasicPopUp(
                            Lang.PopUp_Undefined_Error_Title,
                            Lang.PopUp_Undefined_Error_Message,
                            Lang.PopUp_Undefined_Error_Button);
                        await alertPopUp.ShowPopUp();
                    }
                }
                else
                {
                    try
                    {
                        List<string> openNotifications =
                            JsonConvert.DeserializeObject<List<string>>(
                                connectionRecord.GetTag("OpenCredentialNotifications"));
                        openNotifications.Remove(autoAcceptNotification.RecordId);
                        connectionRecord.SetTag("OpenCredentialNotifications",
                            JsonConvert.SerializeObject(openNotifications));
                    }
                    catch (Exception)
                    {
                        BasicPopUp alertPopUp = new BasicPopUp(
                            Lang.PopUp_Undefined_Error_Title,
                            Lang.PopUp_Undefined_Error_Message,
                            Lang.PopUp_Undefined_Error_Button);
                        await alertPopUp.ShowPopUp();
                    }
                }

                await _walletRecordService.UpdateAsync(context.Wallet, connectionRecord);
            }

            _basePage.EnableAll();
        }

        private async void OnViewRequest(object sender, EventArgs e)
        {
            _basePage.DisableAll();
            try
            {
                object notification = (sender as Button)?.BindingContext;
                if (notification is Models.WalletCredentialOfferMessage credentialOfferNotification)
                {
                    OfferCredentialPopUp popUp = new OfferCredentialPopUp(credentialOfferNotification);

                    if (Application.Current.MainPage is TabbedPage tabbedPage)
                    {
                        tabbedPage.CurrentPage = tabbedPage.Children[0];
                    }

                    PopUpResult popupResult = await popUp.ShowPopUp();

                    NetworkAccess connectivity = Connectivity.NetworkAccess;
                    if (connectivity != NetworkAccess.ConstrainedInternet && connectivity != NetworkAccess.Internet)
                    {
                        BasicPopUp alertPopUp = new BasicPopUp(
                            Lang.PopUp_Network_Error_Title,
                            Lang.PopUp_Network_Error_Text,
                            Lang.PopUp_Network_Error_Button);
                        await alertPopUp.ShowPopUp();
                        return;
                    }

                    Plugin.Permissions.Abstractions.PermissionStatus storagePermissionStatus =
                        await CrossPermissions.Current.CheckPermissionStatusAsync<StoragePermission>();
                    if (storagePermissionStatus == Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                    {
                        OnCredentialOfferPopUpResult(popupResult, credentialOfferNotification);
                    }
                    else
                    {
                        BasicPopUp alertPopUp = new BasicPopUp(
                            Lang.PopUp_Storage_Permission_Needed_Credential_Title,
                            Lang.PopUp_Storage_Permission_Needed_Credential_Text,
                            Lang.PopUp_Storage_Permission_Needed_Credential_Button);
                        await alertPopUp.ShowPopUp();
                    }
                }

                else if (notification is NewProofRequestMessage proofRequestNotification)
                {
                    ProofRequest request = proofRequestNotification.ProofRecord.RequestJson.ToObject<ProofRequest>();
                    string recordId = proofRequestNotification.ProofRecord.Id;

                    ProofViewModel proofViewModel = new ProofViewModel(request, recordId);
                    proofViewModel.LoadItemsCommand.Execute(null);
                    while (!proofViewModel.LoadCommandFinished)
                    {
                        await Task.Delay(100);
                    }

                    if (proofViewModel.ReadyToSend)
                    {
                        CustomServiceDecorator service = null;

                        if (proofRequestNotification.ProofRecord.ConnectionId == null)
                        {
                            service = proofRequestNotification.ProofRecord.GetTag(DecoratorNames.ServiceDecorator)
                                .ToObject<CustomServiceDecorator>();

                            var endpointUri = new Uri(service.ServiceEndpoint);
                            string serviceAlias = !string.IsNullOrEmpty(service.EndpointName) ? service.EndpointName + " - " + endpointUri.Host : service.ServiceEndpoint;
                            ProofPopUp popUp = new ProofPopUp(proofViewModel, recordId, service, serviceAlias);
                            PopUpResult result = await popUp.ShowPopUp();
                            if (result == PopUpResult.Accepted)
                            {
                                App.AutoAcceptViewModel.HandleProofRequestResult();
                            }
                        }
                        else
                        {
                            ProofPopUp popUp = new ProofPopUp(proofViewModel, recordId, service,
                                proofRequestNotification.ConnectionAlias);
                            PopUpResult result = await popUp.ShowPopUp();
                            IAgentContext context = await _customAgentProvider.GetContextAsync();
                            ConnectionRecord connectionRecord =
                                await _connectionService.GetAsync(context,
                                    proofRequestNotification.ProofRecord.ConnectionId);
                            if (result == PopUpResult.Accepted)
                            {
                                App.AutoAcceptViewModel.HandleProofRequestResult(connectionRecord);
                            }
                        }
                    }
                    else
                    {
                        ProofMissingCredentialsPopUp popUp = new ProofMissingCredentialsPopUp(request, proofViewModel.FailedRequests);
                        await popUp.ShowPopUp();
                    }
                }

                else if (notification is AutoAcceptMessage autoAcceptNotification)
                {
                    _viewModel.InboxMessages.Remove(notification as InboxMessage);
                    App.AutoAcceptViewModel.AutoAcceptMessages.Remove(notification as InboxMessage);
                    _viewModel.OnRequestFinished();
                    NetworkAccess connectivity = Connectivity.NetworkAccess;
                    if (connectivity != NetworkAccess.ConstrainedInternet && connectivity != NetworkAccess.Internet)
                    {
                        BasicPopUp alertPopUp = new BasicPopUp(
                            Lang.PopUp_Network_Error_Title,
                            Lang.PopUp_Network_Error_Text,
                            Lang.PopUp_Network_Error_Button);
                        await alertPopUp.ShowPopUp();
                        return;
                    }

                    IAgentContext context = await _customAgentProvider.GetContextAsync();

                    ConnectionRecord connectionRecord =
                        await _walletRecordService.GetAsync<ConnectionRecord>(context.Wallet,
                            autoAcceptNotification.ConnectionRecord.Id, true);
                    if (autoAcceptNotification.CredentialId != null)
                    {
                        CredentialRecord credential =
                            await _credentialService.GetAsync(context, autoAcceptNotification.CredentialId);

                        CredentialContent credDetail = await CreateCredentialDetail(credential, connectionRecord);

                        ConnectionElement connectionDetailsItem = CreateConnectionDetailsItem(credDetail);

                        ConnectionDetailsCredential connectionDetailsCredential = new ConnectionDetailsCredential
                        {
                            RecordId = connectionDetailsItem.RecordId,
                            ImageUri = connectionDetailsItem.ImageUri,
                            Title = connectionDetailsItem.Title,
                            State = connectionDetailsItem.State,
                            CreatedAtUtc = connectionDetailsItem.CreatedAtUtc,
                            Attributes = new List<CredentialContentAttribute>()
                        };

                        foreach (CredentialContentAttribute credentialAttribute in credDetail.Attributes
                            .OrderBy(x => x.Name).ToList())
                        {
                            if (credentialAttribute.Name == "embeddedImage")
                            {
                                try
                                {
                                    object portraitString = credentialAttribute.Value;
                                    connectionDetailsCredential.EmbeddedByteArray =
                                        Convert.FromBase64String(portraitString.ToString());
                                }
                                catch (Exception)
                                {
                                    //ignore
                                }
                            }
                            else if (credentialAttribute.Name == "embeddedDocument")
                            {
                                connectionDetailsCredential.HasDocument = true;
                                connectionDetailsCredential.DocumentString = credentialAttribute.Value.ToString();
                            }
                            else
                            {
                                connectionDetailsCredential.Attributes.Add(new CredentialContentAttribute
                                {
                                    Name = credentialAttribute.Name,
                                    Value = credentialAttribute.Value
                                });
                            }
                        }

                        CredentialInfoPopUp popUp = new CredentialInfoPopUp(connectionDetailsCredential);
                        if (await popUp.ShowPopUp() == PopUpResult.Deleted)
                        {
                            try
                            {
                                DeleteCredentialPopUp confirm = new DeleteCredentialPopUp();
                                if (await confirm.ShowPopUp() == PopUpResult.Accepted)
                                {
                                    try
                                    {
                                        await Navigation.PopAsync();
                                    }
                                    catch (System.Exception)
                                    {
                                        //ignore
                                    }

                                    MessagingCenter.Send(this, WalletEvents.CredentialDeleted,
                                        connectionDetailsCredential.RecordId);
                                }
                            }
                            catch (Exception)
                            {
                                BasicPopUp alertPopUp = new BasicPopUp(
                                    Lang.PopUp_Undefined_Error_Title,
                                    Lang.PopUp_Undefined_Error_Message,
                                    Lang.PopUp_Undefined_Error_Button);
                                await alertPopUp.ShowPopUp();
                            }
                        }

                        List<string> openNotifications =
                            JsonConvert.DeserializeObject<List<string>>(
                                connectionRecord.GetTag("OpenCredentialNotifications"));
                        openNotifications.Remove(autoAcceptNotification.RecordId);
                        connectionRecord.SetTag("OpenCredentialNotifications",
                            JsonConvert.SerializeObject(openNotifications));
                    }
                    else
                    {
                        try
                        {
                            ConnectionElement result = new ConnectionElement();
                            ProofRecord acceptedConnectionProof =
                                await _proofService.GetAsync(context, autoAcceptNotification.RecordId);
                            if (acceptedConnectionProof != null)
                            {
                                string usedCredentialsTag = acceptedConnectionProof.GetTag(WalletParams.HistoryCredentialsTag);

                                CredentialHistoryElements proofForConnection = new CredentialHistoryElements();
                                try
                                {
                                    proofForConnection =
                                        JsonConvert.DeserializeObject<CredentialHistoryElements>(usedCredentialsTag);
                                }
                                catch (Exception)
                                {
                                    BasicPopUp alertPopUp = new BasicPopUp(
                                        Lang.PopUp_Undefined_Error_Title,
                                        Lang.PopUp_Undefined_Error_Message,
                                        Lang.PopUp_Undefined_Error_Button);
                                    await alertPopUp.ShowPopUp();
                                }

                                ConnectionElement newItem =
                                    CreateConnectionDetailsItem(proofForConnection, acceptedConnectionProof);

                                List<CredentialClaim> revealedList =
                                    proofForConnection.RevealedClaims.OrderBy(x => x.Name).ToList();
                                ObservableCollection<CredentialClaim> revealed =
                                    new ObservableCollection<CredentialClaim>();
                                foreach (CredentialClaim claim in revealedList)
                                {
                                    revealed.Add(claim);
                                }

                                List<CredentialClaim> nonrevealedList =
                                    proofForConnection.NonRevealedClaims.OrderBy(x => x.Name).ToList();
                                ObservableCollection<CredentialClaim> nonrevealed =
                                    new ObservableCollection<CredentialClaim>();
                                foreach (CredentialClaim claim in nonrevealedList)
                                {
                                    nonrevealed.Add(claim);
                                }

                                List<CredentialClaim> predicatesList =
                                    proofForConnection.PredicateClaims.OrderBy(x => x.Name).ToList();
                                ObservableCollection<CredentialClaim> predicates =
                                    new ObservableCollection<CredentialClaim>();
                                foreach (CredentialClaim claim in predicatesList)
                                {
                                    predicates.Add(new CredentialClaim
                                    {
                                        CredentialRecordId = claim.CredentialRecordId,
                                        Name = claim.Name,
                                        PredicateType = claim.PredicateType,
                                        Value = claim.PredicateType + " " + claim.Value
                                    });
                                }

                                List<CredentialClaim> selfsList =
                                    proofForConnection.SelfAttestedClaims.OrderBy(x => x.Name).ToList();
                                ObservableCollection<CredentialClaim> selfs =
                                    new ObservableCollection<CredentialClaim>();
                                foreach (CredentialClaim claim in selfsList)
                                {
                                    selfs.Add(claim);
                                }

                                HistoryProofElement credentialHistoryItem = new HistoryProofElement
                                {
                                    ConnectionAlias = newItem.Title,
                                    ImageUri = newItem.ImageUri,
                                    State = newItem.State,
                                    CreatedAtUtc = newItem.CreatedAtUtc,
                                    RevealedClaims = revealed,
                                    NonRevealedClaims = nonrevealed,
                                    SelfAttestedClaims = selfs,
                                    PredicateClaims = predicates
                                };

                                List<string> openNotifications =
                                    JsonConvert.DeserializeObject<List<string>>(
                                        connectionRecord.GetTag("OpenProofNotifications"));
                                openNotifications.Remove(autoAcceptNotification.RecordId);
                                connectionRecord.SetTag("OpenProofNotifications",
                                    JsonConvert.SerializeObject(openNotifications));
                            }
                        }
                        catch (Exception)
                        {
                            //ignore
                        }
                    }

                    try
                    {
                        await _walletRecordService.UpdateAsync(context.Wallet, connectionRecord);
                    }
                    catch (Exception)
                    {
                        BasicPopUp alertPopUp = new BasicPopUp(
                            Lang.PopUp_Connection_Does_Not_Exist_Title,
                            Lang.PopUp_Connection_Does_Not_Exist_Text,
                            Lang.PopUp_Connection_Does_Not_Exist_Button);
                        await alertPopUp.ShowPopUp();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                BasicPopUp alertPopUp = new BasicPopUp(
                    Lang.PopUp_Undefined_Error_Title,
                    Lang.PopUp_Undefined_Error_Message,
                    Lang.PopUp_Undefined_Error_Button);
                await alertPopUp.ShowPopUp();
            }
            finally
            {
                _basePage.EnableAll();
            }
        }
    }
}