using System.Net;

namespace IDWallet.Interfaces
{
    public interface IProxyInfoProvider
    {
        WebProxy GetProxySettings();
    }
}