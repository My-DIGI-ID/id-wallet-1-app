using System;
using System.Threading.Tasks;

namespace IDWallet.Interfaces
{
    public interface IAppDeeplinkService
    {
        Uri AppDeeplinkUri { get; set; }
        bool CalledFromAppDeeplink { get; }
        Task ProcessAppDeeplink();
    }
}