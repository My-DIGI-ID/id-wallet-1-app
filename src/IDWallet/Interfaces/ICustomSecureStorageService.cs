using System.Threading.Tasks;

namespace IDWallet.Interfaces
{
    public interface ICustomSecureStorageService
    {
        Task<T> GetAsync<T>(string key);

        Task SetAsync(string key, object value);
    }
}