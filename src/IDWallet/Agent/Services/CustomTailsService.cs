using IDWallet.Interfaces;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Indy.BlobStorageApi;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IDWallet.Agent.Services
{
    internal class CustomTailsService : DefaultTailsService
    {
        protected new readonly HttpClient HttpClient;

        public CustomTailsService(ILedgerService ledgerService, IOptions<AgentOptions> agentOptions,
            IHttpClientFactory httpClientFactory) : base(ledgerService, agentOptions, httpClientFactory)
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            httpClientHandler.Proxy = DependencyService.Get<IProxyInfoProvider>().GetProxySettings();

            HttpClient = new HttpClient(httpClientHandler);
        }

        public override async Task<BlobStorageWriter> CreateTailsAsync()
        {
            var tailsWriterConfig = new
            {
                base_dir = Path.Combine(FileSystem.AppDataDirectory, ".indy_client", "tails"),
                uri_pattern = string.Empty
            };

            BlobStorageWriter blobWriter = await BlobStorage.OpenWriterAsync("default", tailsWriterConfig.ToJson());
            return blobWriter;
        }

        public override async Task<string> EnsureTailsExistsAsync(IAgentContext agentContext,
            string revocationRegistryId)
        {
            Hyperledger.Indy.LedgerApi.ParseResponseResult revocationRegistry =
                await LedgerService.LookupRevocationRegistryDefinitionAsync(agentContext, revocationRegistryId);

            string tailsUri = JObject.Parse(revocationRegistry.ObjectJson)["value"]["tailsLocation"].ToObject<string>();
            string tailsFileName =
                JObject.Parse(revocationRegistry.ObjectJson)["value"]["tailsHash"].ToObject<string>();

            string tailsfile = Path.Combine(Path.Combine(FileSystem.AppDataDirectory, ".indy_client", "tails"),
                tailsFileName);

            if (!Directory.Exists(Path.Combine(FileSystem.AppDataDirectory, ".indy_client", "tails")))
            {
                Directory.CreateDirectory(Path.Combine(FileSystem.AppDataDirectory, ".indy_client", "tails"));
            }

            if (!Directory.Exists(Path.Combine(FileSystem.AppDataDirectory, ".indy_client", "tails")))
            {
                try
                {
                    Directory.CreateDirectory(Path.Combine(FileSystem.AppDataDirectory, ".indy_client", "tails"));
                }
                catch (Exception)
                {
                    //ignore
                }
            }

            if (!File.Exists(tailsfile))
            {
                try
                {
                    string byteString = (await HttpClient.GetStringAsync(tailsUri)).Replace("\"", string.Empty);
                    byte[] byteArray = Convert.FromBase64String(byteString);

                    File.WriteAllBytes(
                        path: tailsfile,
                        bytes: byteArray);
                }
                catch (Exception)
                {
                    byte[] bytes = await HttpClient.GetByteArrayAsync(tailsUri);
                    File.WriteAllBytes(
                        path: tailsfile,
                        bytes: bytes);
                }
            }

            return Path.GetFileName(tailsfile);
        }

        public override async Task<BlobStorageReader> OpenTailsAsync(string filename)
        {
            string baseDir = Path.Combine(FileSystem.AppDataDirectory, ".indy_client", "tails");

            var tailsWriterConfig = new
            {
                base_dir = baseDir,
                uri_pattern = string.Empty,
                file = filename
            };

            if (BlobReaders.TryGetValue(filename, out BlobStorageReader blobReader))
            {
                return blobReader;
            }

            blobReader = await BlobStorage.OpenReaderAsync("default", tailsWriterConfig.ToJson());
            BlobReaders.TryAdd(filename, blobReader);
            return blobReader;
        }
    }
}