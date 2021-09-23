using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Agent.Services;
using IDWallet.Events;
using IDWallet.Resources;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Settings.Connections.PopUps;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IDWallet.Services
{
    public class ConnectService
    {
        private readonly ICustomAgentProvider _agentProvider;
        private readonly CustomConnectionService _connectionService;
        private readonly IMessageService _messageService;
        private readonly ICustomWalletRecordService _walletRecordService;

        public ConnectService(CustomConnectionService connectionService, DefaultMessageService messageService,
            ICustomAgentProvider agentContextProvider, ICustomWalletRecordService walletRecordService)
        {
            _connectionService = connectionService;
            _messageService = messageService;
            _agentProvider = agentContextProvider;
            _walletRecordService = walletRecordService;
        }

        public async Task<ConnectionRecord> AcceptInvitationAsync(CustomConnectionInvitationMessage invitationMessage)
        {
            NetworkAccess connectivity = Connectivity.NetworkAccess;
            if (connectivity != NetworkAccess.ConstrainedInternet && connectivity != NetworkAccess.Internet)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Lang.PopUp_Network_Error_Title,
                    Lang.PopUp_Network_Error_Text,
                    Lang.PopUp_Network_Error_Button);
                await alertPopUp.ShowPopUp();

                return null;
            }

            IAgentContext context = await _agentProvider.GetContextAsync();
            (ConnectionRequestMessage connectionRequest, ConnectionRecord connectionRecord) =
                await _connectionService.CreateRequestAsync(context, invitationMessage);

            MessagingCenter.Send(this, WalletEvents.NewConnection.ToString(), connectionRecord);

            ConnectionResponseMessage responseMessage = null;
            try
            {
                MessageContext responseUnpacked =
                    await _messageService.SendReceiveAsync(context, connectionRequest, connectionRecord);

                if (responseUnpacked is UnpackedMessageContext unpackedContext)
                {
                    responseMessage = unpackedContext.GetMessage<ConnectionResponseMessage>();
                }
            }
            catch (Exception)
            {
                InvalidInvitationPopUp errorPopUp = new InvalidInvitationPopUp();
                PopUpResult result = await errorPopUp.ShowPopUp();

                if (result == PopUpResult.Deleted)
                {
                    await _connectionService.DeleteAsync(context, connectionRecord.Id);
                    MessagingCenter.Send(this, WalletEvents.ReloadConnections);
                }
            }

            if (responseMessage != null)
            {
                await _connectionService.ProcessResponseAsync(context, responseMessage, connectionRecord);

                return connectionRecord;
            }

            return null;
        }

        public async Task<ConnectionRecord> AcceptInvitationAsync(ConnectionInvitationMessage invitationMessage,
            bool awaitableConnection = false)
        {
            IAgentContext context = await _agentProvider.GetContextAsync();
            (ConnectionRequestMessage connectionRequest, ConnectionRecord connectionRecord) =
                await _connectionService.CreateRequestAsync(context, invitationMessage);

            if (awaitableConnection)
            {
                connectionRecord =
                    await _walletRecordService.GetAsync<ConnectionRecord>(context.Wallet, connectionRecord.Id, true);
                connectionRecord.SetTag(WalletParams.RecipientKeys, invitationMessage.RecipientKeys.ToJson());
                await _walletRecordService.UpdateAsync(context.Wallet, connectionRecord);
            }

            MessagingCenter.Send(this, WalletEvents.NewConnection, connectionRecord);

            ConnectionResponseMessage responseMessage = null;
            try
            {
                responseMessage =
                    await _messageService.SendReceiveAsync<ConnectionResponseMessage>(context, connectionRequest,
                        connectionRecord);
            }
            catch (Exception)
            {
                if (!awaitableConnection)
                {
                    InvalidInvitationPopUp errorPopUp = new InvalidInvitationPopUp();
                    PopUpResult result = await errorPopUp.ShowPopUp();

                    if (result == PopUpResult.Deleted)
                    {
                        await _connectionService.DeleteAsync(context, connectionRecord.Id);
                        MessagingCenter.Send(this, WalletEvents.ReloadConnections);
                    }
                }
            }

            if (responseMessage != null)
            {
                await _connectionService.ProcessResponseAsync(context, responseMessage, connectionRecord);

                return connectionRecord;
            }

            return null;
        }

        public CustomConnectionInvitationMessage ReadInvitationUrl(string invitationUrl)
        {
            CustomConnectionInvitationMessage invitationMessage = null;
            Uri uri = null;
            try
            {
                uri = new Uri(invitationUrl);
            }
            catch (Exception)
            {
                try
                {
                    invitationMessage = invitationUrl
                        .ToObject<CustomConnectionInvitationMessage>();

                    if (!string.IsNullOrEmpty(invitationMessage.ServiceEndpoint))
                    {
                        return invitationMessage;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception)
                {
                    try
                    {
                        invitationMessage = invitationUrl
                            .FromBase64()
                            .ToObject<CustomConnectionInvitationMessage>();

                        if (!string.IsNullOrEmpty(invitationMessage.ServiceEndpoint))
                        {
                            return invitationMessage;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }

            try
            {
                if (uri.Query.StartsWith("?c_i="))
                {
                    invitationMessage = uri.Query
                        .Remove(0, 5)
                        .FromBase64()
                        .ToObject<CustomConnectionInvitationMessage>();

                    return invitationMessage;
                }
            }
            catch (Exception)
            {
                //ignore
            }

            return invitationMessage;
        }
    }
}