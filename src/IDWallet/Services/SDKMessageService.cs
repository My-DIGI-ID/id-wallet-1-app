using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Models.AusweisSDK;
using IDWallet.Utils;
using Hyperledger.Aries.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using Xamarin.Forms;

namespace IDWallet.Services
{
    public class SDKMessageService
    {
        public HttpClient AusweisSdkHttpClient;
        public HttpClientHandler AusweisSdkHttpClientHandler;
        public CookieContainer AusweisSdkCookieContainer;
        public string PublicKey;

        public SDKMessageService()
        {
            InitHttpClient();
        }

        public void InitHttpClient()
        {
            AusweisSdkCookieContainer = new CookieContainer();
            AusweisSdkHttpClientHandler = new HttpClientHandler()
            {
                CookieContainer = AusweisSdkCookieContainer,
                UseCookies = true,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, error) =>
                {
                    List<string> allowedPublicKeys = new List<string>
                    {
						// BDR API Public Key Pinning
                    };
                    if (sender.RequestUri.Host.Equals(new Uri($"https://{WalletParams.AusweisHost}").Host))
                    {
                        if (allowedPublicKeys.Contains(cert.GetPublicKeyString()))
                        {
                            return error == SslPolicyErrors.None;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return error == SslPolicyErrors.None;
                    }
                }
            };

            AusweisSdkHttpClient = new HttpClient(AusweisSdkHttpClientHandler);
        }

        public void ReceiveAccessRights(SdkMessage sdkMessage)
        {
            MessagingCenter.Send(this, BaseIDEvents.AccessRights, sdkMessage);
        }

        public void ReceiveEnterPIN(SdkMessage sdkMessage)
        {
            MessagingCenter.Send(this, BaseIDEvents.EnterPIN, sdkMessage);
        }

        public void ReceiveEnterNewPIN(SdkMessage sdkMessage)
        {
            MessagingCenter.Send(this, BaseIDEvents.EnterNewPIN, sdkMessage);
        }

        public void ReceiveEnterCAN(SdkMessage sdkMessage)
        {
            MessagingCenter.Send(this, BaseIDEvents.EnterCAN, sdkMessage);
        }

        public void ReceiveEnterPUK(SdkMessage sdkMessage)
        {
            MessagingCenter.Send(this, BaseIDEvents.EnterPUK, sdkMessage);
        }

        public void ReceiveAuth(SdkMessage sdkMessage)
        {
            MessagingCenter.Send(this, BaseIDEvents.Auth, sdkMessage);
        }

        public void ReceiveChangePIN(SdkMessage sdkMessage)
        {
            MessagingCenter.Send(this, BaseIDEvents.ChangePIN, sdkMessage);
        }

        public void ReceiveInsertCard(SdkMessage sdkMessage)
        {
            MessagingCenter.Send(this, BaseIDEvents.InsertCard, sdkMessage);
        }

        public void ReceiveReader(SdkMessage sdkMessage)
        {
            MessagingCenter.Send(this, BaseIDEvents.Reader, sdkMessage);
        }

        public async void SendRunAuth()
        {
            App.SafetyResult = "";
            string apiKey = StringCipher.Decrypt("", WalletParams.PackageName + WalletParams.AppVersion); // decrypt and set BDR API-KEY
            AusweisSdkHttpClient.DefaultRequestHeaders.Add("X-API-KEY", apiKey);

            HttpResponseMessage result = await AusweisSdkHttpClient.GetAsync($"https://{WalletParams.AusweisHost}/ssi/oauth2/authorization/ausweisident-integrated");

            TcTokenModel TcToken = new TcTokenModel();
            if (result.IsSuccessStatusCode)
            {
                TcToken = JObject.Parse(await result.Content.ReadAsStringAsync()).ToObject<TcTokenModel>();
                DependencyService.Get<IAusweisSdk>().SendCall($"{{\"cmd\": \"RUN_AUTH\", \"tcTokenURL\": \"{TcToken.TcTokenUrl}\"}}");
            }

            if (!string.IsNullOrEmpty(TcToken.Challenge))
            {
                var nonce = TcToken.Challenge.FromBase64();
                var nonceByte = Sha256.sha256(nonce);
                DependencyService.Get<ISecurityChecks>().SafetyCheck(Sha256.sha256(TcToken.Challenge.FromBase64()));

                Device.StartTimer(new TimeSpan(0, 0, 0, 0, 100), () =>
                {
                    if (!string.IsNullOrEmpty(App.SafetyResult))
                    {
                        if (Device.RuntimePlatform == Device.Android)
                        {
                            string androidString = $"{{\"appVersion\": \"{WalletParams.AppVersion}\", \"attestation\": \"{App.SafetyResult}\"}}";
                            StringContent androidContent = new StringContent(androidString, System.Text.Encoding.UTF8, "application/json");
                            AusweisSdkHttpClient.PostAsync($"https://{WalletParams.AusweisHost}/ssi/api/integrated/wallet-validation/android", androidContent).GetAwaiter().GetResult();
                        }
                        else if (Device.RuntimePlatform == Device.iOS)
                        {
                            string iOSString = $"{{\"teamId\": \"{WalletParams.TeamId}\", \"bundleId\": \"{WalletParams.PackageName}\", \"keyId\": \"{App.SafetyKey}\", \"appVersion\": \"{WalletParams.AppVersion}\", \"attestation\": \"{App.SafetyResult}\"}}";
                            StringContent iOSContent = new StringContent(iOSString, System.Text.Encoding.UTF8, "application/json");
                            AusweisSdkHttpClient.PostAsync($"https://{WalletParams.AusweisHost}/ssi/api/integrated/wallet-validation/ios", iOSContent).GetAwaiter().GetResult();
                        }

                        App.SafetyResult = "";
                        App.SafetyKey = "";
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                });
            }
            AusweisSdkHttpClient.DefaultRequestHeaders.Clear();
        }

        public void SendCancel()
        {
            DependencyService.Get<IAusweisSdk>().SendCall("{\"cmd\": \"CANCEL\"}");
        }

        public void SendRunChangePIN()
        {
            DependencyService.Get<IAusweisSdk>().SendCall("{\"cmd\": \"RUN_CHANGE_PIN\"}");
        }

        public void SendSetPIN(string enteredPIN)
        {
            DependencyService.Get<IAusweisSdk>().SendCall("{\"cmd\": \"SET_PIN\", \"value\": \"" + enteredPIN + "\"}");
        }

        public void SendSetNewPIN(string enteredPIN)
        {
            DependencyService.Get<IAusweisSdk>().SendCall("{\"cmd\": \"SET_NEW_PIN\", \"value\": \"" + enteredPIN + "\"}");
        }

        public void SendAccept()
        {
            DependencyService.Get<IAusweisSdk>().SendCall("{\"cmd\": \"ACCEPT\"}");
        }

        public void SendSetCAN(string enteredCAN)
        {
            DependencyService.Get<IAusweisSdk>().SendCall("{\"cmd\": \"SET_CAN\", \"value\": \"" + enteredCAN + "\"}");
        }

        public void SendSetPUK(string enteredPUK)
        {
            DependencyService.Get<IAusweisSdk>().SendCall("{\"cmd\": \"SET_PUK\", \"value\": \"" + enteredPUK + "\"}");
        }
    }

    public static class EnumerableEx
    {
        public static IEnumerable<string> SplitBy(this string str, int chunkLength)
        {
            if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentException();
            }

            if (chunkLength < 1)
            {
                throw new ArgumentException();
            }

            for (int i = 0; i < str.Length; i += chunkLength)
            {
                if (chunkLength + i > str.Length)
                {
                    chunkLength = str.Length - i;
                }

                yield return str.Substring(i, chunkLength);
            }
        }
    }
}
