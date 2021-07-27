using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Storage;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using Xamarin.Essentials;

namespace IDWallet
{
    public class WalletAgentOptions : IOptions<List<AgentOptions>>
    {
        public class CustomWalletConfiguration
        {
            public WalletConfiguration walletConfiguration { get; set; }

            public CustomWalletConfiguration(string id)
            {
                walletConfiguration = new WalletConfiguration
                {
                    Id = id,
                    StorageConfiguration = new WalletConfiguration.WalletStorageConfiguration
                    {
                        Path = Path.Combine(FileSystem.AppDataDirectory, ".indy_client")
                    }
                };
            }
        }

        public List<AgentOptions> Value => new List<AgentOptions>
        {
            new AgentOptions
            {
                AgentName = "ID Wallet",
                EndpointUri = WalletParams.MediatorEndpoint,
                WalletConfiguration = new CustomWalletConfiguration("EESDI Pilot").walletConfiguration,
                PoolName = "idw_eesdi",
                GenesisFilename = "idw_eesdi",
                ProtocolVersion = 2
            },
            new AgentOptions
            {
                AgentName = "ID Wallet",
                EndpointUri = WalletParams.MediatorEndpoint,
                WalletConfiguration = new CustomWalletConfiguration("EESDI Test").walletConfiguration,
                PoolName = "idw_eesditest",
                GenesisFilename = "idw_eesditest",
                ProtocolVersion = 2
            },
            new AgentOptions
            {
                AgentName = "ID Wallet",
                EndpointUri = WalletParams.MediatorEndpoint,
                WalletConfiguration = new CustomWalletConfiguration("Sovrin Live").walletConfiguration,
                PoolName = "idw_live",
                GenesisFilename = "idw_live",
                ProtocolVersion = 2
            },
            new AgentOptions
            {
                AgentName = "ID Wallet",
                EndpointUri = WalletParams.MediatorEndpoint,
                WalletConfiguration = new CustomWalletConfiguration("esatus").walletConfiguration,
                PoolName = "idw_esatus",
                GenesisFilename = "idw_esatus",
                ProtocolVersion = 2
            },
            new AgentOptions
            {
                AgentName = "ID Wallet",
                EndpointUri = WalletParams.MediatorEndpoint,
                WalletConfiguration = new CustomWalletConfiguration("BCGov Ledger").walletConfiguration,
                PoolName = "idw_bcgov",
                GenesisFilename = "idw_bcgov",
                ProtocolVersion = 2
            },
            new AgentOptions
            {
                AgentName = "ID Wallet",
                EndpointUri = WalletParams.MediatorEndpoint,
                WalletConfiguration = new CustomWalletConfiguration("Sovrin Builder").walletConfiguration,
                PoolName = "idw_builder",
                GenesisFilename = "idw_builder",
                ProtocolVersion = 2
            },
            //new AgentOptions
            //{
            //    AgentName = "ID Wallet",
            //    EndpointUri = WalletParams.MediatorEndpoint,
            //    WalletConfiguration = new CustomWalletConfiguration("Sovrin Staging").walletConfiguration,
            //    PoolName = "idw_staging",
            //    GenesisFilename = "idw_staging",
            //    ProtocolVersion = 2
            //},
            new AgentOptions
            {
                AgentName = "ID Wallet",
                EndpointUri = WalletParams.MediatorEndpoint,
                WalletConfiguration = new CustomWalletConfiguration("IDunion Test").walletConfiguration,
                PoolName = "idw_iduniontest",
                GenesisFilename = "idw_iduniontest",
                ProtocolVersion = 2
            },
            new AgentOptions
            {
                AgentName = "ID Wallet",
                EndpointUri = WalletParams.MediatorEndpoint,
                WalletConfiguration = new CustomWalletConfiguration("DEV Ledger").walletConfiguration,
                PoolName = "idw_devledger",
                GenesisFilename = "idw_devledger",
                ProtocolVersion = 2
            },
            new AgentOptions
            {
                AgentName = "ID Wallet",
                EndpointUri = WalletParams.MediatorEndpoint,
                WalletConfiguration = new CustomWalletConfiguration("DGC Dev").walletConfiguration,
                PoolName = "idw_dgcdev",
                GenesisFilename = "idw_dgcdev",
                ProtocolVersion = 2
            }
        };
    }
}