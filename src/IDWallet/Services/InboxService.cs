using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Routing;
using Hyperledger.Aries.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IDWallet.Services
{
    public class InboxService : IObserver<InboxEvent>
    {
        private const string _activeAgentKey = "ActiveAgent";
        private const string _mediatorConnectionIdTagName = "MediatorConnectionId";
        private const string _mediatorInboxIdTagName = "MediatorInboxId";
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IEdgeClientService _edgeClientService = App.Container.Resolve<IEdgeClientService>();
        private readonly IMessageService _messageService = App.Container.Resolve<IMessageService>();
        private readonly IProvisioningService _provisioningService = App.Container.Resolve<IProvisioningService>();

        private readonly ICustomSecureStorageService _secureStorageService =
            App.Container.Resolve<ICustomSecureStorageService>();

        private readonly ICustomWalletRecordService _walletRecordService =
                    App.Container.Resolve<ICustomWalletRecordService>();
        private readonly IWalletService _walletService = App.Container.Resolve<IWalletService>();
        private AgentOptions _activeAgent;
        private bool _processing = false;
        private IDisposable _unsubscriber;
        public InboxService()
        {
        }

        public void AddDevice(IAgentContext agentContext)
        {
            try
            {
                string deviceId = App.PnsHandle;
                string platform = Device.RuntimePlatform.ToLowerInvariant();

                string pushServiceName = !App.PollingIsActive ? WalletParams.PushServiceName : "Polling";
                App.PushService = pushServiceName;

                Dictionary<string, string> deviceMetadata = new Dictionary<string, string>
                {
                    {"Push", pushServiceName},
                    {"CreatedAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()}
                };


                AddDeviceInfoMessage deviceInfoMessage = new AddDeviceInfoMessage()
                {
                    DeviceId = deviceId.ToString(),
                    DeviceVendor = platform,
                    DeviceMetadata = deviceMetadata
                };

                try
                {
                    Task.Run(async () =>
                        await _edgeClientService.AddDeviceAsync(agentContext, deviceInfoMessage)
                    ).Wait();
                }
                catch (Exception)
                {
                    //ignore
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public void OnCompleted()
        {
            Unsubscribe();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public async void OnNext(InboxEvent value)
        {
            if (App.IsLoggedIn)
            {
                while (_processing)
                {
                    await Task.Delay(100);
                }

                _processing = true;
                if (App.Wallet != null && App.Wallet.IsOpen)
                {
                    await ProcessMessages();
                }
                else
                {
                    _processing = false;
                }
            }
        }

        public async void PollMessages()
        {
            if (App.IsLoggedIn)
            {
                while (_processing)
                {
                    await Task.Delay(100);
                }

                _processing = true;
                if (App.Wallet != null && App.Wallet.IsOpen)
                {
                    await ProcessMessages();
                }
                else
                {
                    _processing = false;
                }
            }
        }

        public async Task ProcessMessages()
        {
            try
            {
                NetworkAccess connectivity = Connectivity.NetworkAccess;
                IAgentContext agentContext = null;
                if (connectivity == NetworkAccess.ConstrainedInternet || connectivity == NetworkAccess.Internet)
                {
                    agentContext = await _agentProvider.GetContextAsync();
                }
                else
                {
                    return;
                }

                ConnectionRecord connection = await GetMediatorConnectionAsync(agentContext);
                if (connection == null)
                {
                    throw new InvalidOperationException("This agent is not configured with a mediator");
                }

                GetInboxItemsResponseMessage response = new GetInboxItemsResponseMessage();
                GetInboxItemsMessage createInboxMessage = new GetInboxItemsMessage();
                try
                {
                    response = await _messageService.SendReceiveAsync<GetInboxItemsResponseMessage>(agentContext,
                        createInboxMessage, connection);
                }
                catch (Exception)
                {
                    //ignore
                }

                List<string> processedItems = new List<string>();
                List<string> notProcessedItems = new List<string>();
                try
                {
                    foreach (InboxItemMessage item in response.Items)
                    {
                        try
                        {
                            await agentContext.Agent.ProcessAsync(agentContext, new PackedMessageContext(item.Data));
                            processedItems.Add(item.Id);
                        }
                        catch (Exception)
                        {
                            notProcessedItems.Add(item.Id);
                        }
                    }
                }
                catch (Exception)
                {
                    processedItems = new List<string>();
                    notProcessedItems = new List<string>();
                }

                try
                {
                    if (processedItems.Count > 0)
                    {
                        await _messageService.SendAsync(agentContext,
                            new DeleteInboxItemsMessage { InboxItemIds = processedItems }, connection);
                    }
                }
                catch (Exception)
                {
                    //ignore
                }

                try
                {
                    if (notProcessedItems.Count > 0)
                    {
                        await _messageService.SendAsync(agentContext,
                            new DeleteInboxItemsMessage { InboxItemIds = notProcessedItems }, connection);
                    }
                }
                catch (Exception)
                {
                    //ignore
                }
            }
            finally
            {
                _processing = false;
            }
        }

        public void RenewPush()
        {
            _activeAgent = _secureStorageService.GetAsync<AgentOptions>(_activeAgentKey).GetAwaiter().GetResult();

            _activeAgent.WalletConfiguration.StorageConfiguration.Path =
                    Path.Combine(FileSystem.AppDataDirectory, ".indy_client");
            if (_agentProvider.AgentExists(_activeAgent))
            {
                CustomAgentContext agentContext = new CustomAgentContext
                {
                    Wallet = App.Wallet
                };

                AddDevice(agentContext);
            }

            Task.Run(async () =>
                await _secureStorageService.SetAsync(WalletParams.PushService, App.PushService)
            ).Wait();
        }

        public virtual void Subscribe(IObservable<InboxEvent> provider)
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

        public async Task<bool> WaitForPnsHandle()
        {
            await _secureStorageService.SetAsync(WalletParams.PollingWasActive, false);

            int counter = 1;
            bool executeNewDevice = false;
            while (string.IsNullOrEmpty(App.PnsHandle))
            {
                App.GetPnsHandle(_secureStorageService);

                if (counter == 100)
                {
                    if (!App.PollingWasActive)
                    {
                        BasicPopUp popUp = new BasicPopUp(
                        Resources.Lang.PopUp_Error_Pns_Title,
                        Resources.Lang.PopUp_Error_Pns_Message,
                        Resources.Lang.PopUp_Error_Pns_Button)
                        {
                            PreLoginPopUp = true
                        };
                        await popUp.ShowPopUp();
                    }

                    if (Device.RuntimePlatform == Device.Android)
                    {
                        DependencyService.Get<IAndroidPns>().Renew();

                        while (!App.FinishedNewPnsHandle)
                        {
                            await Task.Delay(100);
                        }

                        App.PnsHandle = App.GetPollingHandle(_secureStorageService);

                        App.PollingIsActive = true;

                        await _secureStorageService.SetAsync(WalletParams.PollingWasActive, App.PollingIsActive);
                    }
                }

                if (counter == 5 && App.PollingWasActive)
                {
                    App.PnsHandle = App.GetPollingHandle(_secureStorageService);
                    App.PollingIsActive = true;

                    await _secureStorageService.SetAsync(WalletParams.PollingWasActive, App.PollingIsActive);
                }

                if (counter == 25)
                {
                    if (Device.RuntimePlatform == Device.iOS)
                    {
                        if (!App.PollingWasActive)
                        {
                            BasicPopUp popUp = new BasicPopUp(
                                Resources.Lang.PopUp_Pns_Polling_Title,
                                Resources.Lang.PopUp_Pns_Polling_Message,
                                Resources.Lang.PopUp_Pns_Polling_Button)
                            {
                                PreLoginPopUp = true
                            };
                            await popUp.ShowPopUp();
                        }

                        App.PnsHandle = App.GetPollingHandle(_secureStorageService);
                        App.PollingIsActive = true;

                        await _secureStorageService.SetAsync(WalletParams.PollingWasActive, App.PollingIsActive);
                    }
                }

                if (!string.IsNullOrEmpty(App.PnsHandle) && !App.PollingWasActive)
                {
                    executeNewDevice = true;
                }

                counter++;
                await Task.Delay(300);
            }

            return executeNewDevice;
        }

        private async Task<ConnectionRecord> GetMediatorConnectionAsync(IAgentContext agentContext)
        {
            ProvisioningRecord provisioning = await _provisioningService.GetProvisioningAsync(agentContext.Wallet);
            if (provisioning.GetTag(_mediatorConnectionIdTagName) == null)
            {
                return null;
            }

            ConnectionRecord connection = await _walletRecordService.GetAsync<ConnectionRecord>(agentContext.Wallet,
                provisioning.GetTag(_mediatorConnectionIdTagName));
            if (connection == null)
            {
                throw new AriesFrameworkException(ErrorCode.RecordNotFound,
                    "Couldn't locate a connection to mediator agent");
            }

            if (connection.State != ConnectionState.Connected)
            {
                throw new AriesFrameworkException(ErrorCode.RecordInInvalidState,
                    $"You must be connected to the mediator agent. Current state is {connection.State}");
            }

            return connection;
        }
    }
}