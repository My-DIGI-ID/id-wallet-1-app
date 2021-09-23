using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Models;
using IDWallet.Resources;
using IDWallet.Utils;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.QRScanner.PopUps;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.Services
{
    public class AddVacService
    {
        private readonly ConnectService _connectService = App.Container.Resolve<ConnectService>();
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly IMessageService _messageService = App.Container.Resolve<IMessageService>();
        private readonly ICustomSecureStorageService _secureStorageService =
            App.Container.Resolve<ICustomSecureStorageService>();
        private readonly ICustomWalletRecordService _walletRecordService =
                    App.Container.Resolve<ICustomWalletRecordService>();

        private bool _alreadySubscribed;

        private readonly HttpClient _addVacHttpClient;

        public AddVacService()
        {
            Subscribe();
        }

        public void Subscribe()
        {
            if (!_alreadySubscribed)
            {
                _alreadySubscribed = true;
                MessagingCenter.Subscribe<ServiceMessageEventService, string>(this, WalletEvents.VacCredentialOffer, VacCredentialOffer);
                MessagingCenter.Subscribe<ServiceMessageEventService, string>(this, WalletEvents.VacCredentialIssue, VacCredentialIssue);
            }
        }

        private async void VacCredentialIssue(ServiceMessageEventService arg1, string credentialRecordId)
        {
            MessagingCenter.Send(this, WalletEvents.ReloadCredentials);
            MessagingCenter.Send(this, WalletEvents.ReloadHistory);

            IAgentContext agentContext = await _agentProvider.GetContextAsync();
            CredentialRecord credentialRecord = await _credentialService.GetAsync(agentContext, credentialRecordId);
            CredentialAddedPopUp popUp = new CredentialAddedPopUp(credentialRecord);
            await popUp.ShowPopUp();

            App.VacConnectionId = "";
        }

        private async void VacCredentialOffer(ServiceMessageEventService arg1, string credentialRecordId)
        {
            IAgentContext agentContext = await _agentProvider.GetContextAsync();

            CredentialRecord credentialRecord = await _credentialService.GetAsync(agentContext, credentialRecordId);
            ConnectionRecord connectionRecord = await _connectionService.GetAsync(agentContext, App.VacConnectionId);
            VacOfferPopUp offerPopUp = new VacOfferPopUp(new VacOfferMessage(connectionRecord, credentialRecord));
            PopUpResult popUpResult = await offerPopUp.ShowPopUp();

            if (popUpResult != PopUpResult.Accepted)
            {
                App.VacConnectionId = "";
            }
            else
            {
                try
                {
                    (CredentialRequestMessage request, CredentialRecord record) = await _credentialService.CreateRequestAsync(agentContext, credentialRecordId);
                    await _messageService.SendAsync(agentContext, request, connectionRecord);
                }
                catch (Exception)
                {
                    credentialRecord =
                    await _walletRecordService.GetAsync<CredentialRecord>(agentContext.Wallet, credentialRecordId, true);
                    credentialRecord.SetTag("AutoError", "true");
                    await _walletRecordService.UpdateAsync(agentContext.Wallet, credentialRecord);

                    BasicPopUp alertPopUp = new BasicPopUp(
                        Lang.PopUp_Credential_Error_Title,
                        Lang.PopUp_Credential_Error_Message,
                        Lang.PopUp_Credential_Error_Button);
                    await alertPopUp.ShowPopUp();

                    App.VacConnectionId = "";
                }
            }
        }

        public async Task AddVac(string codeData)
        {
            IAgentContext agentContext = await _agentProvider.GetContextAsync();

            VacQrCredential vacQrCredential = new VacQrCredential() { Id = Guid.NewGuid().ToString(), QrContent = codeData, Name = "COVID-Zertifikat" };

            await _walletRecordService.AddAsync(agentContext.Wallet, vacQrCredential);

            MessagingCenter.Send(this, WalletEvents.VacAdded, vacQrCredential.Id);
        }

        public string ReadVacJson(string codeData)
        {
            try
            {
                // The base45 encoded data should begin with HC1
                if (codeData.StartsWith("HC1:"))
                {
                    string base45CodedData = codeData.Substring(4);

                    // Base 45 decode data
                    byte[] base45DecodedData = Base45.Decode(Encoding.GetEncoding("UTF-8").GetBytes(base45CodedData));

                    // zlib decompression
                    byte[] uncompressedData = ZlibDecompression(base45DecodedData);

                    return codeData;
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        protected static byte[] ZlibDecompression(byte[] compressedData)
        {
            if (compressedData[0] == 0x78)
            {
                MemoryStream outputStream = new MemoryStream();
                using (MemoryStream compressedStream = new MemoryStream(compressedData))
                using (InflaterInputStream inputStream = new InflaterInputStream(compressedStream))
                {
                    inputStream.CopyTo(outputStream);
                    outputStream.Position = 0;
                    return outputStream.ToArray();
                }
            }
            else
            {
                // The data is not compressed
                return compressedData;
            }
        }
    }
}