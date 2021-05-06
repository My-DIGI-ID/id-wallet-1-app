using IDWallet.Agent.Messages;
using IDWallet.Agent.Messages.Privacy;
using IDWallet.Views.Settings.Connections.PopUps;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IDWallet.Agent.Handlers
{
    public class PrivacyHandler : IMessageHandler
    {
        private readonly IMessageService _messageService;
        private readonly IWalletRecordService _recordService;
        public PrivacyHandler(
            IMessageService messageService,
            IWalletRecordService recordService)
        {
            _messageService = messageService;
            _recordService = recordService;
        }


        public IEnumerable<MessageType> SupportedMessageTypes => new[]
        {
            new MessageType(CustomMessageTypes.DeleteProofs),
            new MessageType(CustomMessageTypes.DeleteProofsHttps),
            new MessageType(CustomMessageTypes.ProofsDeleted),
            new MessageType(CustomMessageTypes.ProofsDeletedHttps)
        };

        public async Task<AgentMessage> ProcessAsync(IAgentContext agentContext, UnpackedMessageContext messageContext)
        {
            switch (messageContext.GetMessageType())
            {
                case CustomMessageTypes.DeleteProofs:
                case CustomMessageTypes.DeleteProofsHttps:
                    {
                        DeleteProofsMessage deleteMessage = messageContext.GetMessage<DeleteProofsMessage>();

                        int count = 0;

                        if (deleteMessage.DeleteIds.Count == 0)
                        {
                            List<ProofRecord> proofList =
                                await _recordService.SearchAsync<ProofRecord>(agentContext.Wallet, count: 2147483647);

                            try
                            {
                                proofList = proofList.Where(x =>
                                    !string.IsNullOrEmpty(x.ConnectionId) &&
                                    x.ConnectionId.Equals(messageContext.Connection.Id)).ToList();
                            }
                            catch (Exception)
                            {
                                //ignore
                            }

                            foreach (ProofRecord proof in proofList)
                            {
                                await _recordService.DeleteAsync<ProofRecord>(agentContext.Wallet, proof.Id);
                                count++;
                            }
                        }
                        else
                        {
                            List<ProofRecord> proofList =
                                await _recordService.SearchAsync<ProofRecord>(agentContext.Wallet, count: 2147483647);
                            try
                            {
                                proofList = proofList.Where(x =>
                                    !string.IsNullOrEmpty(x.ConnectionId) &&
                                    x.ConnectionId.Equals(messageContext.Connection.Id)).ToList();
                            }
                            catch (Exception)
                            {
                                //ignore
                            }

                            foreach (string proofDeleteId in deleteMessage.DeleteIds)
                            {
                                try
                                {
                                    foreach (ProofRecord proof in proofList)
                                    {
                                        string thisDeleteId = null;
                                        try
                                        {
                                            thisDeleteId = proof.GetTag("delete_id").ToString();
                                        }
                                        catch (System.Exception)
                                        {
                                            //ignore
                                        }

                                        if (!string.IsNullOrEmpty(thisDeleteId) && proofDeleteId.Equals(thisDeleteId))
                                        {
                                            await _recordService.DeleteAsync<ProofRecord>(agentContext.Wallet, proof.Id);
                                            count++;
                                        }
                                    }
                                }
                                catch (System.Exception)
                                {
                                    //ignore
                                }
                            }
                        }

                        await _messageService.SendAsync(agentContext, new ProofsDeletedMessage() { DeletedProofs = count },
                            messageContext.Connection);

                        return null;
                    }

                case CustomMessageTypes.ProofsDeleted:
                case CustomMessageTypes.ProofsDeletedHttps:
                    {
                        ProofsDeletedMessage proofsDeletedMessage = messageContext.GetMessage<ProofsDeletedMessage>();

                        int deletionsCount = proofsDeletedMessage.DeletedProofs;
                        ProofsDeletedPopUp popUp =
                            new ProofsDeletedPopUp(messageContext.Connection.Alias.Name, deletionsCount);
                        await popUp.ShowPopUp();

                        return null;
                    }

                default:
                    throw new AriesFrameworkException(ErrorCode.InvalidMessage,
                        $"Unsupported message type {messageContext.GetMessageType()}");
            }
        }
    }
}