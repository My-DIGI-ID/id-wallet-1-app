using IDWallet.Interfaces;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace IDWallet.Services
{
    public class CustomSecureStorageService : ICustomSecureStorageService
    {
        public async Task<T> GetAsync<T>(string key)
        {
            string value = await SecureStorage.GetAsync(key);
            return JsonConvert.DeserializeObject<T>(value);
        }

        public async Task SetAsync(string key, object value)
        {
            await SecureStorage.SetAsync(key, JsonConvert.SerializeObject(value));
        }
    }
}