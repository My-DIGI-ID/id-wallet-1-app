using IDWallet.Agent.Messages;
using IDWallet.Agent.Services;
using IDWallet.Resources;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.PresentProof;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IDWallet.Agent.Handler
{
    public class CustomProofHandler : IMessageHandler
    {
        private readonly CustomWalletRecordService _customWalletRecordService;
        private readonly CustomProofService _proofService;
        public CustomProofHandler(CustomProofService proofService, CustomWalletRecordService customWalletRecordService)
        {
            _proofService = proofService;
            _customWalletRecordService = customWalletRecordService;
        }

        public IEnumerable<MessageType> SupportedMessageTypes => new[]
        {
            new MessageType(MessageTypes.PresentProofNames.Presentation),
            new MessageType(MessageTypes.PresentProofNames.RequestPresentation),
            new MessageType(CustomMessageTypes.TransactionError),
            new MessageType(MessageTypesHttps.PresentProofNames.Presentation),
            new MessageType(MessageTypesHttps.PresentProofNames.RequestPresentation),
            new MessageType(CustomMessageTypes.TransactionErrorHttps)
        };

        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            switch (messageContext.GetMessageType())
            {
                case MessageTypes.PresentProofNames.Presentation:
                case MessageTypesHttps.PresentProofNames.Presentation:
                    {
                        PresentationMessage message = messageContext.GetMessage<PresentationMessage>();
                        ProofRecord record = await _proofService.ProcessPresentationAsync(agentContext, message);

                        messageContext.ContextRecord = record;
                        break;
                    }
                case MessageTypes.PresentProofNames.RequestPresentation:
                case MessageTypesHttps.PresentProofNames.RequestPresentation:
                    {
                        RequestPresentationMessage request = messageContext.GetMessage<RequestPresentationMessage>();
                        string messageJson = messageContext.GetMessageJson();
                        ProofRecord proofRecord =
                            await _proofService.ProcessRequestAsync(agentContext, request, messageContext.Connection);

                        try
                        {
                            string deleteId = request.FindDecorator<string>("delete_id");
                            if (!string.IsNullOrEmpty(deleteId))
                            {
                                proofRecord.SetTag("delete_id", deleteId);
                                await _customWalletRecordService.UpdateAsync(agentContext.Wallet, proofRecord);
                            }
                        }
                        catch (Exception)
                        {
                            //ignore
                        }

                        messageContext.ContextRecord = await _proofService.GetAsync(agentContext, proofRecord.Id);
                        break;
                    }
                case CustomMessageTypes.TransactionError:
                case CustomMessageTypes.TransactionErrorHttps:
                    {
                        BasicPopUp alertPopUp = new BasicPopUp(
                            Lang.PopUp_Transaction_Error_Title,
                            Lang.PopUp_Transaction_Error_Message,
                            Lang.PopUp_Transaction_Error_Button);
                        await alertPopUp.ShowPopUp();

                        if (App.WaitForProof && !string.IsNullOrEmpty(App.AwaitableProofConnectionId) &&
                            App.AwaitableProofConnectionId.Equals(messageContext.Connection.Id))
                        {
                            App.WaitForProof = false;
                        }

                        break;
                    }
                default:
                    throw new AriesFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageContext.GetMessageType()}");
            }

            return null;
        }
    }
}