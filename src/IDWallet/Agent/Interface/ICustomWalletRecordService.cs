using Hyperledger.Aries.Storage;
using Hyperledger.Indy.WalletApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IDWallet.Agent.Interface
{
    public interface ICustomWalletRecordService
    {
        Task AddAsync<T>(Wallet wallet, T record) where T : RecordBase, new();

        Task<bool> DeleteAsync<T>(Wallet wallet, string id) where T : RecordBase, new();

        Task<T> GetAsync<T>(Wallet wallet, string id, bool blocking = false) where T : RecordBase, new();

        Task<T> GetAsync<T>(Wallet wallet, string id) where T : RecordBase, new();

        Task<List<T>> SearchAsync<T>(Wallet wallet, ISearchQuery query = null, SearchOptions options = null,
                    int count = 10, bool blocking = false) where T : RecordBase, new();
        Task<List<T>> SearchAsync<T>(Wallet wallet, ISearchQuery query = null, SearchOptions options = null,
            int count = 10, int skip = 0) where T : RecordBase, new();

        Task UpdateAsync(Wallet wallet, RecordBase record);
    }
}