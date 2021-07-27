using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Agent.Services;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Utils;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Ledger;
using Hyperledger.Aries.Routing;
using Hyperledger.Aries.Storage;
using Hyperledger.Aries.Utils;
using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SimpleBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Device = Xamarin.Forms.Device;
using IOException = System.IO.IOException;

namespace IDWallet.Agent
{
    public class CustomAgentProvider : ICustomAgentProvider
    {
        private const string _activeAgentKey = "ActiveAgent";
        private const string _allAgentsKey = "AllAgents";
        private const string _mediatorConnectionIdTagName = "MediatorConnectionId";
        private const string _mediatorInboxIdTagName = "MediatorInboxId";
        private readonly IAgent _agent;
        private readonly IConnectionService _connectionService;
        private readonly IEdgeClientService _edgeClientService;
        private readonly IMessageService _messageService;
        private readonly IPoolService _poolService;
        private readonly IProvisioningService _provisioningService;
        private readonly ICustomWalletRecordService _recordService;
        private readonly ICustomSecureStorageService _storageService;
        private readonly IWalletService _walletService;
        private AgentOptions _activeAgent;
        private List<AgentOptions> _allAgents = new List<AgentOptions>();

        public CustomAgentProvider(IProvisioningService provisioningService, ICustomSecureStorageService storageService,
            IWalletService walletService, IPoolService poolService, IAgent agent,
            IConnectionService connectionService,
            IEdgeClientService edgeClientService, IMessageService messageService,
            ICustomWalletRecordService recordService)
        {
            _provisioningService = provisioningService;
            _storageService = storageService;
            _walletService = walletService;
            _poolService = poolService;
            _agent = agent;
            _connectionService = connectionService;
            _edgeClientService = edgeClientService;
            _messageService = messageService;
            _recordService = recordService;

            try
            {
                IOptions<List<AgentOptions>> agentOptions = App.Container.Resolve<IOptions<List<AgentOptions>>>();
                List<AgentOptions> allAgentsTemp = _storageService.GetAsync<List<AgentOptions>>(_allAgentsKey)
                    .GetAwaiter().GetResult();

                foreach (AgentOptions thisAgent in agentOptions.Value)
                {
                    if (allAgentsTemp.Where(x => x.PoolName.Equals(thisAgent.PoolName)).Count() == 0)
                    {
                        allAgentsTemp.Add(thisAgent);
                        Task.Run(async () =>
                            await StoreAgentConfigs(allAgentsTemp)
                        ).Wait();
                    }
                }

                _allAgents = allAgentsTemp;

                _activeAgent = _storageService.GetAsync<AgentOptions>(_activeAgentKey).GetAwaiter().GetResult();

                if (_activeAgent != null)
                {
                    _activeAgent.WalletConfiguration.StorageConfiguration.Path =
                        Path.Combine(FileSystem.AppDataDirectory, ".indy_client");
                }
            }
            catch (ArgumentNullException)
            {
                //ignore
            }
        }

        public async Task AddDevice(IAgentContext agentContext)
        {
            try
            {
                string deviceId = App.PnsHandle;
                if (deviceId == null)
                {
                    deviceId = App.GetPollingHandle(_storageService);
                }

                string platform = Device.RuntimePlatform.ToLowerInvariant();

                string pushServiceName = WalletParams.PushServiceName;
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

                await _edgeClientService.AddDeviceAsync(agentContext, deviceInfoMessage);
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Device_Add_Error_Title,
                    Resources.Lang.PopUp_Device_Add_Error_Message,
                    Resources.Lang.PopUp_Device_Add_Error_Button)
                {
                    PreLoginPopUp = true
                };
                await alertPopUp.ShowPopUp();
            }
        }

        public bool AgentExists()
        {
            Task.Run(async () =>
                _activeAgent = await GetActiveAgent()
            ).Wait();

            if (_activeAgent == null)
            {
                return false;
            }

            try
            {
                Task<bool> task = Task.Run(async () =>
                    await CheckForWalletExists(_activeAgent.WalletConfiguration, _activeAgent.WalletCredentials)
                );
                task.Wait();

                return task.Result;
            }
            catch (AggregateException ex) when (ex.InnerException?.GetType() == typeof(WalletNotFoundException))
            {
                return false;
            }
        }

        public async Task<AgentOptions> GetActiveAgent()
        {
            try
            {
                var activeAgent = await _storageService.GetAsync<AgentOptions>(_activeAgentKey);
                activeAgent.WalletConfiguration.StorageConfiguration.Path =
                        Path.Combine(FileSystem.AppDataDirectory, ".indy_client");
                return activeAgent;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool AgentExists(AgentOptions agent = null)
        {
            try
            {
                Task<bool> task = Task.Run(async () =>
                    await CheckForWalletExists(agent.WalletConfiguration, agent.WalletCredentials)
                );
                task.Wait();

                return task.Result;
            }
            catch (WalletNotFoundException)
            {
                return false;
            }
            catch (AggregateException ex) when (ex.InnerException?.GetType() == typeof(WalletNotFoundException))
            {
                return false;
            }
        }

        private async Task<bool> CheckForWalletExists(WalletConfiguration walletConfiguration, WalletCredentials walletCredentials)
        {
            try
            {
                walletCredentials.Key = await Wallet.GenerateWalletKeyAsync(new { }.ToJson());
                Wallet wallet = await Wallet.OpenWalletAsync(walletConfiguration.ToJson(), walletCredentials.ToJson());
                return true;
            }
            catch (WalletAccessFailedException)
            {
                return true;
            }
            catch (WalletNotFoundException)
            {
                return false;
            }
            catch (WalletAlreadyOpenedException)
            {
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                walletCredentials.Key = "";
            }
        }

        public async Task<bool> CreateAgentAsync(AgentOptions options, string poolFile = null, string pin = "")
        {
            if (string.IsNullOrEmpty(await GetWalletKey(pin)))
            {
                await StoreNewWalletKeyParams();
            }

            options.WalletCredentials.KeyDerivationMethod = "RAW";

            options.WalletConfiguration.StorageConfiguration.Path =
                Path.Combine(FileSystem.AppDataDirectory, ".indy_client");

            AgentPublicConfiguration discovery =
                await _edgeClientService.DiscoverConfigurationAsync(WalletParams.MediatorEndpoint);

            options.EndpointUri = discovery.ServiceEndpoint;
            options.AgentKey = discovery.RoutingKey;

            await CreatePool(options.GenesisFilename, poolFile);
            await CreateWallet(options, pin);

            _activeAgent = options;

            await CreateInboxAtMediator();

            await _storageService.SetAsync(_activeAgentKey, options);

            MessagingCenter.Send(this, WalletEvents.AgentSwitched);

            return true;
        }

        public async Task SwitchLedger(AgentOptions agentOptions, string poolFile = null)
        {
            await CreatePool(agentOptions.GenesisFilename, poolFile);
            _activeAgent.GenesisFilename = agentOptions.GenesisFilename;
            _activeAgent.PoolName = agentOptions.PoolName;
            _activeAgent.ProtocolVersion = agentOptions.ProtocolVersion;

            await _storageService.SetAsync(_activeAgentKey, _activeAgent);
        }

        public async Task CreateInboxAtMediator()
        {
            try
            {
                AgentPublicConfiguration discovery =
                    await _edgeClientService.DiscoverConfigurationAsync(WalletParams.MediatorEndpoint);
                IAgentContext agentContext = await GetContextAsync();
                ProvisioningRecord provisioning = await _provisioningService.GetProvisioningAsync(agentContext.Wallet);

                if (provisioning.GetTag(_mediatorConnectionIdTagName) == null)
                {
                    (ConnectionRequestMessage request, ConnectionRecord record) =
                        await _connectionService.CreateRequestAsync(agentContext, discovery.Invitation);
                    ConnectionResponseMessage response =
                        await _messageService.SendReceiveAsync<ConnectionResponseMessage>(agentContext, request,
                            record);

                    await _connectionService.ProcessResponseAsync(agentContext, response, record);

                    record = await _connectionService.GetAsync(agentContext, record.Id);
                    record.Endpoint = new AgentEndpoint(record.Endpoint.Uri, null, null);
                    await _recordService.UpdateAsync(agentContext.Wallet, record);

                    provisioning.SetTag(_mediatorConnectionIdTagName, record.Id);
                    await _recordService.UpdateAsync(agentContext.Wallet, provisioning);

                    if (provisioning.GetTag(_mediatorInboxIdTagName) == null)
                    {
                        Dictionary<string, string> mobileSecret = new Dictionary<string, string>
                        {
                            {"Mobile-Secret", WalletParams.MobileSecret}
                        };
                        await _edgeClientService.CreateInboxAsync(agentContext, mobileSecret);
                    }

                    await AddDevice(agentContext);
                }
            }
            catch (Exception)
            {
                BasicPopUp alertPopUp = new BasicPopUp(
                    Resources.Lang.PopUp_Create_Inbox_Error_Title,
                    Resources.Lang.PopUp_Create_Inbox_Error_Message,
                    Resources.Lang.PopUp_Create_Inbox_Error_Button)
                {
                    PreLoginPopUp = true
                };
                await alertPopUp.ShowPopUp();
            }
        }

        public AgentOptions GetActiveAgentOptions()
        {
            return _activeAgent;
        }

        public Task<IAgent> GetAgentAsync(params object[] args)
        {
            return Task.FromResult(_agent);
        }

        public AgentOptions GetAgentOptionsRecommendedLedger(string recommendedLedgerName)
        {
            AgentOptions recommendedLedger = null;
            try
            {
                List<AgentOptions> allOptions = GetAllAgentOptions();
                string poolName = "";

                if (recommendedLedgerName == WalletParams.RecommendedLedger_Live)
                {
                    poolName = "idw_live";
                }
                else if (recommendedLedgerName == WalletParams.RecommendedLedger_Staging)
                {
                    poolName = "idw_staging";
                }
                else if (recommendedLedgerName == WalletParams.RecommendedLedger_Builder)
                {
                    poolName = "idw_builder";
                }
                else if (recommendedLedgerName == WalletParams.RecommendedLedger_Esatus)
                {
                    poolName = "idw_esatus";
                }
                else if (recommendedLedgerName == WalletParams.RecommendedLedger_BCGov)
                {
                    poolName = "idw_bcgov";
                }
                else if (recommendedLedgerName == WalletParams.RecommendedLedger_IDuniontest)
                {
                    poolName = "idw_iduniontest";
                }
                else if (recommendedLedgerName == WalletParams.RecommendedLedger_EESDI)
                {
                    poolName = "idw_eesdi";
                }
                    else if (recommendedLedgerName == WalletParams.RecommendedLedger_EESDITest)
                {
                    poolName = "idw_eesditest";
                }
                else if (recommendedLedgerName == WalletParams.RecommendedLedger_DEVLEDGER)
                {
                    poolName = "idw_devledger";
                }
                else if (recommendedLedgerName == WalletParams.RecommendedLedger_DGCDEVLEDGER)
                {
                    poolName = "idw_dgcdev";
                }

                if (poolName != "")
                {
                    recommendedLedger = allOptions.Find(x => x.PoolName == poolName);
                }
            }
            catch (Exception)
            {
                //ignore
            }

            return recommendedLedger;
        }

        public List<AgentOptions> GetAllAgentOptions()
        {
            return _allAgents;
        }

        public async Task<IAgentContext> GetContextAsync(params object[] args)
        {
            IAgent agent = await GetAgentAsync(args);

            CustomAgentContext context = new CustomAgentContext
            {
                Agent = agent,
                Wallet = App.Wallet,
                SupportedMessages = agent.GetSupportedMessageTypes()
            };

            try
            {
                context.Pool = new PoolAwaitable(() =>
                    _poolService.GetPoolAsync(_activeAgent.PoolName, _activeAgent.ProtocolVersion));

                NetworkAccess connectivity = Connectivity.NetworkAccess;
                if (connectivity == NetworkAccess.ConstrainedInternet || connectivity == NetworkAccess.Internet)
                {
                    await context.Pool;
                }
            }
            catch (Exception)
            {
                //ignore
            }

            return context;
        }

        public string GetPoolName(AgentOptions options)
        {
            switch (options.PoolName)
            {
                case "idw_live":
                    return Resources.Lang.ChangeLedgerPage_Live;
                case "idw_staging":
                    return Resources.Lang.ChangeLedgerPage_Staging;
                case "idw_builder":
                    return Resources.Lang.ChangeLedgerPage_builder;
                case "idw_esatus":
                    return Resources.Lang.ChangeLedgerPage_Esatus;
                case "idw_bcgov":
                    return Resources.Lang.ChangeLedgerPage_BCGov;
                case "idw_iduniontest":
                    return Resources.Lang.ChangeLedgerPage_IDunionTest;
                case "idw_eesdi":
                    return Resources.Lang.ChangeLedgerPage_EESDI;
                case "idw_eesditest":
                    return Resources.Lang.ChangeLedgerPage_EESDITest;
                case "idw_devledger":
                    return Resources.Lang.ChangeLedgerPage_DEVLEDGER;
                case "idw_dgcdev":
                    return Resources.Lang.ChangeLedgerPage_DGCDEVLEDGER;
                default:
                    return options.PoolName;
            }
        }

        public async Task ImportAgentAsync(string importConfig)
        {
            try
            {
                await _walletService.DeleteWalletAsync(_activeAgent.WalletConfiguration,
                    _activeAgent.WalletCredentials);
            }
            catch (WalletNotFoundException)
            {
                //ignore
            }
            finally
            {
                _activeAgent.WalletCredentials.Key = "";
            }


            try
            {
                await Pool.DeletePoolLedgerConfigAsync(_activeAgent.GenesisFilename);
            }
            catch (Exception)
            {
                //ignore
            }

            switch (_activeAgent.GenesisFilename.ToLower())
            {
                case "live":
                    DeleteOldPool(_activeAgent.GenesisFilename);
                    _activeAgent.PoolName = "idw_live";
                    _activeAgent.GenesisFilename = "idw_live";
                    break;
                case "builder":
                    DeleteOldPool(_activeAgent.GenesisFilename);
                    _activeAgent.PoolName = "idw_builder";
                    _activeAgent.GenesisFilename = "idw_builder";
                    break;
                case "staging":
                    DeleteOldPool(_activeAgent.GenesisFilename);
                    _activeAgent.PoolName = "idw_staging";
                    _activeAgent.GenesisFilename = "idw_staging";
                    break;
                case "bcgov":
                    DeleteOldPool(_activeAgent.GenesisFilename);
                    _activeAgent.PoolName = "idw_bcgov";
                    _activeAgent.GenesisFilename = "idw_bcgov";
                    break;
                case "esatus":
                    DeleteOldPool(_activeAgent.GenesisFilename);
                    _activeAgent.PoolName = "idw_esatus";
                    _activeAgent.GenesisFilename = "idw_esatus";
                    break;
                case "iduniontest":
                    DeleteOldPool(_activeAgent.GenesisFilename);
                    _activeAgent.PoolName = "idw_iduniontest";
                    _activeAgent.GenesisFilename = "idw_iduniontest";
                    break;
                case "eesdi":
                    DeleteOldPool(_activeAgent.GenesisFilename);
                    _activeAgent.PoolName = "idw_eesdi";
                    _activeAgent.GenesisFilename = "idw_eesdi";
                    break;
                case "eesditest":
                    DeleteOldPool(_activeAgent.GenesisFilename);
                    _activeAgent.PoolName = "idw_eesditest";
                    _activeAgent.GenesisFilename = "idw_eesditest";
                    break;
                case "devledger":
                    DeleteOldPool(_activeAgent.GenesisFilename);
                    _activeAgent.PoolName = "idw_devledger";
                    _activeAgent.GenesisFilename = "idw_devledger";
                    break;
                case "dgcdevledger":
                    DeleteOldPool(_activeAgent.GenesisFilename);
                    _activeAgent.PoolName = "idw_dgcdev";
                    _activeAgent.GenesisFilename = "idw_dgcdev";
                    break;
                default:
                    break;
            }

            await CreatePool(_activeAgent.GenesisFilename);

            AgentPublicConfiguration discovery =
                await _edgeClientService.DiscoverConfigurationAsync(WalletParams.MediatorEndpoint);

            _activeAgent.EndpointUri = discovery.ServiceEndpoint;
            _activeAgent.AgentKey = discovery.RoutingKey;
            _activeAgent.WalletConfiguration.StorageConfiguration.Path =
                Path.Combine(FileSystem.AppDataDirectory, ".indy_client");

            try
            {
                _activeAgent.WalletCredentials.Key = await GetWalletKey(_activeAgent.PoolName);
                await Wallet.ImportAsync(_activeAgent.WalletConfiguration.ToJson(), _activeAgent.WalletCredentials.ToJson(), importConfig);
            }
            catch (Exception)
            {
                //ignore
            }
            finally
            {
                _activeAgent.WalletCredentials.Key = "";
            }

            Wallet wallet = App.Wallet;

            AgentEndpoint oldEndpoint = (await _provisioningService.GetProvisioningAsync(wallet)).Endpoint;
            AgentEndpoint newEndpoint = new AgentEndpoint(discovery.ServiceEndpoint, oldEndpoint.Did,
                new string[] { discovery.RoutingKey });
            await _provisioningService.UpdateEndpointAsync(wallet, newEndpoint);

            ProvisioningRecord provisioning = await _provisioningService.GetProvisioningAsync(wallet);

            if (provisioning.GetTag(_mediatorConnectionIdTagName) == null &&
                provisioning.GetTag(_mediatorInboxIdTagName) == null)
            {
                await CreateInboxAtMediator();
            }
            else
            {
                ConnectionRecord oldMediatorConnection =
                    await _recordService.GetAsync<ConnectionRecord>(wallet,
                        provisioning.GetTag(_mediatorConnectionIdTagName));
                if (!oldMediatorConnection.Endpoint.Uri.Equals(discovery.ServiceEndpoint))
                {
                    await _recordService.DeleteAsync<ConnectionRecord>(wallet, oldMediatorConnection.Id);
                    try
                    {
                        provisioning.RemoveTag(_mediatorConnectionIdTagName);
                    }
                    catch (Exception)
                    {
                        //ignore
                    }

                    try
                    {
                        provisioning.RemoveTag(_mediatorInboxIdTagName);
                    }
                    catch (Exception)
                    {
                        //ignore
                    }

                    await _recordService.UpdateAsync(wallet, provisioning);
                    await CreateInboxAtMediator();
                }
                else
                {
                    IAgentContext agentContext = await GetContextAsync();
                    await AddDevice(agentContext);
                }
            }

            await _storageService.SetAsync(_activeAgentKey, _activeAgent);

            MessagingCenter.Send(this, WalletEvents.AgentSwitched);
        }


        public async Task StoreAgentConfigs(List<AgentOptions> agentOptions)
        {
            _allAgents = agentOptions;
            await _storageService.SetAsync(_allAgentsKey, agentOptions);
        }

        private static void DeleteOldPool(string genesisFilename)
        {
            try
            {
                Task.Run(async () =>
                    await Pool.DeletePoolLedgerConfigAsync(genesisFilename)
                ).Wait();
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private static async Task<string> ReadAndWriteEmbeddedGenesisTx(string embeddedGenesisFile)
        {
            Assembly assembly = typeof(CustomPoolService).GetTypeInfo().Assembly;
            Stream stream = assembly.GetManifestResourceStream($"IDWallet.Resources.genesis.{embeddedGenesisFile}");
            if (stream == null)
            {
                throw new NullReferenceException("Could not find embedded genesis tx file.");
            }

            string genesisTx = null;
            using (StreamReader reader = new StreamReader(stream))
            {
                try
                {
                    genesisTx = reader.ReadToEnd();
                }
                catch (IOException)
                {
                    throw;
                }
                catch (OutOfMemoryException)
                {
                    throw;
                }
            }

            string file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                embeddedGenesisFile);

            await Task.Run(() => File.WriteAllText(file, genesisTx));
            return file;
        }

        private async Task CreatePool(string genesisFileName, string file = null)
        {
            if (file == null)
            {
                try
                {
                    file = await ReadAndWriteEmbeddedGenesisTx(genesisFileName);
                }
                catch (NullReferenceException)
                {
                    return;
                }
            }

            try
            {
                await _poolService.CreatePoolAsync(genesisFileName, file);
            }
            catch (PoolLedgerConfigExistsException)
            {
                //ignore
            }
        }

        private async Task<Wallet> ProvisionAgentAsync(AgentOptions agentOptions)
        {
            if (agentOptions is null)
            {
                throw new ArgumentNullException(nameof(agentOptions));
            }

            // Create agent wallet
            await _walletService.CreateWalletAsync(
                configuration: agentOptions.WalletConfiguration,
                credentials: agentOptions.WalletCredentials);
            var wallet = await Wallet.OpenWalletAsync(agentOptions.WalletConfiguration.ToJson(), agentOptions.WalletCredentials.ToJson());

            // Configure agent endpoint
            AgentEndpoint endpoint = null;
            if (agentOptions.EndpointUri != null)
            {
                endpoint = new AgentEndpoint { Uri = agentOptions.EndpointUri.ToString() };
                if (agentOptions.AgentKeySeed != null)
                {
                    var agent = await Did.CreateAndStoreMyDidAsync(wallet, new { seed = agentOptions.AgentKeySeed }.ToJson());
                    endpoint.Did = agent.Did;
                    endpoint.Verkey = new[] { agent.VerKey };
                }
                else if (agentOptions.AgentKey != null)
                {
                    endpoint.Did = agentOptions.AgentDid;
                    endpoint.Verkey = new[] { agentOptions.AgentKey };
                }
                else
                {
                    var agent = await Did.CreateAndStoreMyDidAsync(wallet, "{}");
                    endpoint.Did = agent.Did;
                    endpoint.Verkey = new[] { agent.VerKey };
                }
            }
            var masterSecretId = await AnonCreds.ProverCreateMasterSecretAsync(wallet, null);

            var record = new ProvisioningRecord
            {
                MasterSecretId = masterSecretId,
                Endpoint = endpoint,
                Owner =
                {
                    Name = agentOptions.AgentName,
                    ImageUrl = agentOptions.AgentImageUri
                }
            };

            // Issuer Configuration
            if (agentOptions.IssuerKeySeed == null)
            {
                agentOptions.IssuerKeySeed = CryptoUtils.GetUniqueKey(32);
            }

            var issuer = await Did.CreateAndStoreMyDidAsync(
                wallet: wallet,
                didJson: new
                {
                    did = agentOptions.IssuerDid,
                    seed = agentOptions.IssuerKeySeed
                }.ToJson());

            record.IssuerSeed = agentOptions.IssuerKeySeed;
            record.IssuerDid = issuer.Did;
            record.IssuerVerkey = issuer.VerKey;
            record.TailsBaseUri = agentOptions.EndpointUri != null
                ? new Uri(new Uri(agentOptions.EndpointUri), "tails/").ToString()
                : null;

            record.UseMessageTypesHttps = agentOptions.UseMessageTypesHttps;

            record.SetTag("AgentKeySeed", agentOptions.AgentKeySeed);
            record.SetTag("IssuerKeySeed", agentOptions.IssuerKeySeed);

            // Add record to wallet
            await _recordService.AddAsync(wallet, record);

            return wallet;
        }

        private async Task CreateWallet(AgentOptions agentOptions, string pin)
        {
            try
            {
                agentOptions.WalletCredentials.Key = await GetWalletKey(pin);
                App.Wallet = await ProvisionAgentAsync(new AgentOptions
                {
                    WalletConfiguration = agentOptions.WalletConfiguration,
                    WalletCredentials = agentOptions.WalletCredentials,
                    EndpointUri = agentOptions.EndpointUri,
                    AgentKey = agentOptions.AgentKey,
                    AgentName = agentOptions.AgentName
                });
            }
            catch (WalletExistsException)
            {
                try
                {
                    if (App.Wallet != null && App.Wallet.IsOpen)
                    {
                        await App.Wallet.CloseAsync();
                    }
                    App.Wallet = await Wallet.OpenWalletAsync(agentOptions.WalletConfiguration.ToJson(), agentOptions.WalletCredentials.ToJson());
                }
                catch (WalletAccessFailedException)
                {
                    throw;
                }
            }
            catch (AriesFrameworkException ex) when (ex.ErrorCode == ErrorCode.WalletAlreadyProvisioned)
            {
                //ignore
            }
            catch (WalletStorageException)
            {
                throw;
            }
            finally
            {
                agentOptions.WalletCredentials.Key = "";
            }
        }

        private async Task<string> GetWalletKey(string pin = "")
        {
            try
            {
                string preKey = await _storageService.GetAsync<string>(WalletParams.WalletPreKeyTag);
                byte[] saltByte = await _storageService.GetAsync<byte[]>(WalletParams.WalletSaltByteTag);

                byte[] preKeyByte = Encoding.ASCII.GetBytes(preKey);

                Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(pin, saltByte, 100000);
                byte[] keyByte = rfc2898DeriveBytes.GetBytes(16);

                byte[] keyEncByte = new byte[keyByte.Length + preKeyByte.Length];
                Buffer.BlockCopy(keyByte, 0, keyEncByte, 0, keyByte.Length);
                Buffer.BlockCopy(preKeyByte, 0, keyEncByte, keyByte.Length, preKeyByte.Length);

                byte[] walletKey = Sha256.sha256(keyEncByte);

                return Base58.Bitcoin.Encode(walletKey);
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        private async Task StoreNewWalletKeyParams()
        {
            // Create a byte array to hold the random value.
            byte[] salt = new byte[16];
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with a random value.
                rngCsp.GetBytes(salt);
            }
            await _storageService.SetAsync(WalletParams.WalletSaltByteTag, salt);

            string preKey = await Wallet.GenerateWalletKeyAsync(new { }.ToJson());
            await _storageService.SetAsync(WalletParams.WalletPreKeyTag, preKey);
        }

        public async Task<bool> OpenWallet(AgentOptions agentOptions, string pin, string oldPin = "")
        {
            if (string.IsNullOrEmpty(oldPin))
            {
                try
                {
                    WalletCredentials walletCredentials = agentOptions.WalletCredentials;
                    walletCredentials.Key = await GetWalletKey(pin);
                    if (App.Wallet != null && App.Wallet.IsOpen)
                    {
                        await App.Wallet.CloseAsync();
                    }
                    App.Wallet = await Wallet.OpenWalletAsync(agentOptions.WalletConfiguration.ToJson(), walletCredentials.ToJson());
                    return true;
                }
                catch (WalletAccessFailedException ex)
                {
                    App.Wallet = null;
                    return false;
                }
                catch (WalletNotFoundException)
                {
                    App.Wallet = null;
                    return false;
                }
                catch (Exception)
                {
                    App.Wallet = null;
                    return false;
                }
            }
            else
            {
                try
                {
                    App.PollingTimer.Stop();
                    agentOptions.WalletConfiguration.StorageConfiguration.Path = Path.Combine(FileSystem.AppDataDirectory, ".indy_client");

                    WalletCredentials walletCredentials = new WalletCredentials();

                    walletCredentials.Key = await GetWalletKey(oldPin);
                    walletCredentials.KeyDerivationMethod = agentOptions.WalletCredentials.KeyDerivationMethod;
                    walletCredentials.StorageCredentials = agentOptions.WalletCredentials.StorageCredentials;

                    string pathWallet = Path.Combine(Path.Combine(FileSystem.AppDataDirectory, "newPin"));

                    ExportConfig exportConfig = new ExportConfig { Path = pathWallet, Key = walletCredentials.Key };

                    await App.Wallet.ExportAsync(exportConfig.ToJson());

                    try
                    {
                        await App.Wallet.CloseAsync();
                    }
                    catch (Exception)
                    { /*ignore*/ }
                    await Wallet.DeleteWalletAsync(agentOptions.WalletConfiguration.ToJson(), walletCredentials.ToJson());

                    await StoreNewWalletKeyParams();
                    walletCredentials.Key = await GetWalletKey(pin);
                    await Wallet.ImportAsync(agentOptions.WalletConfiguration.ToJson(), walletCredentials.ToJson(), exportConfig.ToJson());

                    File.Delete(pathWallet);

                    App.Wallet = await Wallet.OpenWalletAsync(agentOptions.WalletConfiguration.ToJson(), walletCredentials.ToJson());

                    await _storageService.SetAsync(_activeAgentKey, agentOptions);

                    App.PollingTimer.Start();
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public class ExportConfig
        {
            [JsonProperty("path")]
            public string Path;

            [JsonProperty("key")]
            public string Key;
        }
    }
}