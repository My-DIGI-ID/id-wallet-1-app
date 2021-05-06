using IDWallet.Droid;
using IDWallet.Interfaces;
using Java.Lang;
using System.Net;

[assembly: Xamarin.Forms.Dependency(typeof(ProxyInfoProvider))]
namespace IDWallet.Droid
{
    public class ProxyInfoProvider : IProxyInfoProvider
    {
        public WebProxy GetProxySettings()
        {
            string proxyHost = JavaSystem.GetProperty("http.proxyHost");
            string proxyPort = JavaSystem.GetProperty("http.proxyPort");

            return !string.IsNullOrEmpty(proxyHost) && !string.IsNullOrEmpty(proxyPort)
                ? new WebProxy($"{proxyHost}:{proxyPort}")
                : null;
        }
    }
}