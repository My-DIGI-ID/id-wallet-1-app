using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Services;
using IDWallet.Interfaces;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Settings.Connections.PopUps;
using System;
using System.Threading.Tasks;

namespace IDWallet.Services
{
    public class AppDeeplinkService : IAppDeeplinkService
    {
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly ConnectService _connectionService = App.Container.Resolve<ConnectService>();
        private readonly TransactionService _transactionService = App.Container.Resolve<TransactionService>();
        private readonly UrlShortenerService _urlShortenerService = App.Container.Resolve<UrlShortenerService>();

        private Uri _appDeeplinkUri;
        public Uri AppDeeplinkUri
        {
            get => _appDeeplinkUri;
            set
            {
                if (value != null)
                {
                    _appDeeplinkUri = value;
                    CalledFromAppDeeplink = true;
                }
            }
        }

        public bool CalledFromAppDeeplink { get; private set; }
        public async Task ProcessAppDeeplink()
        {
            if (CalledFromAppDeeplink)
            {
                try
                {
                    CalledFromAppDeeplink = false;

                    string deepLinkUri = AppDeeplinkUri.ToString();
                    Uri uri = new Uri(deepLinkUri);
                    if (uri.Query.StartsWith("?c_i="))
                    {
                        await ProcessConnectionInvitationLinkAsync(deepLinkUri);
                    }
                    else if (uri.Query.StartsWith("?t_o="))
                    {
                        await ProcessTransactionOfferLinkAsync(deepLinkUri);
                    }
                    else
                    {
                        if (uri.Scheme.Equals(WalletParams.AppLinkDidcommTag))
                        {
                            uri = new Uri(uri.ToString()
                                .Replace(WalletParams.AppLinkDidcommTag, "https"));
                        }
                        else if (uri.Scheme.Equals(WalletParams.AppLinkIdTag))
                        {
                            uri = new Uri(uri.ToString()
                                .Replace(WalletParams.AppLinkIdTag, "https"));
                        }

                        await ProcessShortenedUrl(uri.ToString());
                    }
                }
                catch (Exception)
                {
                    //ignore
                }
            }
        }

        public void SetAppDeeplink(Uri deepLinkUri)
        {
            if (deepLinkUri != null)
            {
                AppDeeplinkUri = deepLinkUri;
                CalledFromAppDeeplink = true;
            }
        }
        protected async Task ProcessConnectionInvitationLinkAsync(string deepLinkUri)
        {
            Agent.Models.CustomConnectionInvitationMessage invitation =
                _connectionService.ReadInvitationUrl(deepLinkUri);

            if (invitation != null)
            {
                NewConnectionPopUp popUp = new NewConnectionPopUp(invitation);
                PopUpResult popUpResult = await popUp.ShowPopUp();

                if (PopUpResult.Accepted == popUpResult)
                {
                    await _connectionService.AcceptInvitationAsync(invitation);
                }
            }
            else
            {
                BasicPopUp popUp = new BasicPopUp(
                    Resources.Lang.PopUp_DeepLink_Connection_Fail_Title,
                    Resources.Lang.PopUp_DeepLink_Connection_Fail_Text,
                    Resources.Lang.PopUp_DeepLink_Connection_Fail_Button);
                await popUp.ShowPopUp();
            }
        }

        protected async Task ProcessShortenedUrl(string deepLinkUri)
        {
            await _urlShortenerService.ProcessShortenedUrl(deepLinkUri);
        }

        protected async Task ProcessTransactionOfferLinkAsync(string deepLinkUri)
        {
            (string transactionId, Agent.Models.CustomConnectionInvitationMessage connectionInvitationMessage,
                bool awaitableConnection, bool awaitableProof) = _transactionService.ReadTransactionUrl(deepLinkUri);

            Hyperledger.Aries.Agents.IAgentContext agentContext = await _agentProvider.GetContextAsync();
            Hyperledger.Aries.Features.DidExchange.ConnectionRecord transactionConnectionRecord =
                await _transactionService.CheckForExistingConnection(agentContext, connectionInvitationMessage);

            if (transactionConnectionRecord == null)
            {
                NewConnectionPopUp popUpTransactionConnectionOffer =
                    new NewConnectionPopUp(connectionInvitationMessage);
                PopUpResult popUpTransactionConnectionOfferResult = await popUpTransactionConnectionOffer.ShowPopUp();

                if (PopUpResult.Accepted == popUpTransactionConnectionOfferResult)
                {
                    transactionConnectionRecord =
                        await _connectionService.AcceptInvitationAsync(connectionInvitationMessage);

                    if (awaitableConnection)
                    {
                        App.WaitForConnection = true;
                        App.AwaitableInvitation = connectionInvitationMessage;
                    }
                }
                else
                {
                    BasicPopUp popNoConnection = new BasicPopUp(
                        Resources.Lang.PopUp_Connection_Needed_Title,
                        Resources.Lang.PopUp_Connection_Needed_Text,
                        Resources.Lang.PopUp_Connection_Needed_Button);
                    await popNoConnection.ShowPopUp();
                }
            }

            if (transactionConnectionRecord != null)
            {
                await _transactionService.SendTransactionResponse(agentContext, transactionId,
                    transactionConnectionRecord);
            }
            else if (App.WaitForConnection)
            {
                int counter = 0;
                while (App.WaitForConnection)
                {
                    await Task.Delay(100);

                    counter++;

                    if (counter == 1000)
                    {
                        break;
                    }
                }

                if (App.AwaitableConnection != null)
                {
                    await _transactionService.SendTransactionResponse(agentContext, transactionId,
                        App.AwaitableConnection);
                }

                App.AwaitableInvitation = null;
                App.AwaitableConnection = null;
            }

            App.WaitForProof = awaitableProof;

            if (App.WaitForProof)
            {
                App.AwaitableProofConnectionId = transactionConnectionRecord.Id;

                while (App.WaitForProof)
                {
                    int counter = 0;
                    await Task.Delay(100);

                    counter++;

                    if (counter == 1000)
                    {
                        break;
                    }
                }
            }

            App.AwaitableProofConnectionId = null;
        }
    }
}