using Hyperledger.Aries.Ledger;
using System.Collections.Generic;

namespace IDWallet.Agent.Services
{
    internal class CustomPoolService : DefaultPoolService
    {
        public ICollection<string> GetPools()
        {
            return Pools.Keys;
        }
    }
}