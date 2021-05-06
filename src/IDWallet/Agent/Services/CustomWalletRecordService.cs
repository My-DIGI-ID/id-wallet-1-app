using IDWallet.Agent.Interface;
using IDWallet.Events;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Storage;
using Hyperledger.Indy.WalletApi;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.Agent.Services
{
    public class CustomWalletRecordService : DefaultWalletRecordService, ICustomWalletRecordService
    {
        public new virtual async Task AddAsync<T>(Wallet wallet, T record) where T : RecordBase, new()
        {
            await base.AddAsync<T>(wallet, record);
        }

        public new virtual async Task<bool> DeleteAsync<T>(Wallet wallet, string id) where T : RecordBase, new()
        {
            return await base.DeleteAsync<T>(wallet, id);
        }

        public async Task<T> GetAsync<T>(Wallet wallet, string id, bool blocking = false) where T : RecordBase, new()
        {
            if (App.BlockedRecordTypes.Contains(typeof(T).ToString()))
            {
                while (App.BlockedRecordTypes.Contains(typeof(T).ToString()))
                {
                    await Task.Delay(100);
                }
            }
            if (blocking)
            {
                App.BlockedRecordTypes.Add(typeof(T).ToString());
            }

            return await base.GetAsync<T>(wallet, id);
        }

        public new async Task<T> GetAsync<T>(Wallet wallet, string id) where T : RecordBase, new()
        {
            while (App.BlockedRecordTypes.Contains(typeof(T).ToString()))
            {
                await Task.Delay(100);
            }

            return await base.GetAsync<T>(wallet, id);
        }

        public async Task<List<T>> SearchAsync<T>(Wallet wallet, ISearchQuery query = null,
                                    SearchOptions options = null, int count = 10, bool blocking = false) where T : RecordBase, new()
        {
            if (App.BlockedRecordTypes.Contains(typeof(T).ToString()))
            {
                while (App.BlockedRecordTypes.Contains(typeof(T).ToString()))
                {
                    await Task.Delay(100);
                }
            }

            if (blocking)
            {
                App.BlockedRecordTypes.Add(typeof(T).ToString());
            }

            return await base.SearchAsync<T>(wallet, query, options, count, 0);
        }

        public new async Task<List<T>> SearchAsync<T>(Wallet wallet, ISearchQuery query = null,
            SearchOptions options = null, int count = 10, int skip = 0) where T : RecordBase, new()
        {
            while (App.BlockedRecordTypes.Contains(typeof(T).ToString()))
            {
                await Task.Delay(100);
            }

            return await base.SearchAsync<T>(wallet, query, options, count, skip);
        }

        public new async Task UpdateAsync(Wallet wallet, RecordBase record)
        {
            await base.UpdateAsync(wallet, record);
            if (App.BlockedRecordTypes.Contains(record.GetType().ToString()))
            {
                App.BlockedRecordTypes.Remove(record.GetType().ToString());
            }

            if (record.GetType().ToString().Equals(typeof(ConnectionRecord).ToString()))
            {
                MessagingCenter.Send(this, WalletEvents.ReloadConnections);
            }
        }
    }
}