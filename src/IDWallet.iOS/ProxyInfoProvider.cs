using CoreFoundation;
using IDWallet.Interfaces;
using IDWallet.iOS;
using System.Net;

[assembly: Xamarin.Forms.Dependency(typeof(ProxyInfoProvider))]
namespace IDWallet.iOS
{
    public class ProxyInfoProvider : IProxyInfoProvider
    {
        public WebProxy GetProxySettings()
        {
            CFProxySettings systemProxySettings = CFNetwork.GetSystemProxySettings();

            int proxyPort = systemProxySettings.HTTPPort;
            string proxyHost = systemProxySettings.HTTPProxy;

            return !string.IsNullOrEmpty(proxyHost) && proxyPort != 0
                ? new WebProxy($"{proxyHost}:{proxyPort}")
                : null;
        }
    }
}