using Autofac;
using IDWallet.Interfaces;
using IDWallet.Models;
using Hyperledger.Aries.Features.PresentProof;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.Services
{
    public class ProofListService
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClientHandler _httpClientHandler = new HttpClientHandler();

        private readonly ICustomSecureStorageService _secureStorageService =
            App.Container.Resolve<ICustomSecureStorageService>();

        public ProofListService()
        {
            _httpClientHandler.Proxy = DependencyService.Get<IProxyInfoProvider>().GetProxySettings();
            _httpClient = new HttpClient(_httpClientHandler);
            _httpClient.Timeout = new TimeSpan(0, 0, 30);
        }

        public async Task<List<ProofRequest>> GetProofListAsync()
        {
            ObservableCollection<Gateway> endpointList = new ObservableCollection<Gateway>();
            try
            {
                endpointList = _secureStorageService.GetAsync<ObservableCollection<Gateway>>(WalletParams.AllGatewaysTag)
                    .GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                //ignore
            }

            List<ProofRequest> proofList = new List<ProofRequest>();

            foreach (Gateway endpoint in endpointList)
            {
                try
                {
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("Mobile-Token", endpoint.Key);

                    HttpResponseMessage result = await _httpClient.GetAsync(endpoint.Address);

                    if (result.IsSuccessStatusCode)
                    {
                        string content = await result.Content.ReadAsStringAsync();

                        proofList.AddRange(JsonConvert.DeserializeObject<List<ProofRequest>>(content));
                    }
                }
                catch (Exception)
                {
                    //ignore
                }
            }

            return proofList;
        }
    }
}