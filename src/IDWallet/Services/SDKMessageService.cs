using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Models.AusweisSDK;
using IDWallet.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
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
            TcTokenModel tcToken = await GetToken();

            if (!string.IsNullOrEmpty(tcToken.IssuerNonce))
            {
                // Safety Check is done here
                DependencyService.Get<ISecurityChecks>().SafetyCheck(GetNonce(tcToken.IssuerNonce, 0));

                Device.StartTimer(new TimeSpan(0, 0, 0, 0, 100), () =>
                {
                    if (!string.IsNullOrEmpty(App.SafetyResult))
                    {
                        IHardwareKeyService hardwareKeyService = DependencyService.Resolve<IHardwareKeyService>();

                        string hwKey = hardwareKeyService.GetPublicKeyAsBase64(GetNonce(tcToken.IssuerNonce, 1));

                        string signedNonce = hardwareKeyService.Sign(GetNonce(tcToken.IssuerNonce, 2));

                        Uri uri = CreateDeviceDependentUri(Device.RuntimePlatform);
                        StringContent body = CreateDeviceDependentRequestBody(Device.RuntimePlatform, hwKey, signedNonce);

                        HttpResponseMessage response = AusweisSdkHttpClient.PostAsync(uri, body).GetAwaiter().GetResult();

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

        private async Task<TcTokenModel> GetToken()
        {
            AusweisSdkHttpClient.DefaultRequestHeaders.Add(WalletParams.ApiHeader, StringCipher.Decrypt(WalletParams.ApiKey, WalletParams.PackageName + WalletParams.AppVersion));

            HttpResponseMessage result = await AusweisSdkHttpClient.GetAsync($"https://{WalletParams.AusweisHost}/oauth2/authorization/ausweisident-integrated");

            TcTokenModel tcToken = new TcTokenModel();
            if (result.IsSuccessStatusCode)
            {
                tcToken = JObject.Parse(await result.Content.ReadAsStringAsync()).ToObject<TcTokenModel>();
                DependencyService.Get<IAusweisSdk>().SendCall($"{{\"cmd\": \"RUN_AUTH\", \"tcTokenURL\": \"{tcToken.TcTokenUrl}\"}}");
            }

            return tcToken;
        }

        private Uri CreateDeviceDependentUri(string runtimePlatform)
        {
            string baseUrl = $"https://{WalletParams.AusweisHost}/api/integrated/wallet-validation";

            if (runtimePlatform == Device.Android)
            {
                return new Uri($"{baseUrl}/android");
            }

            return new Uri($"{baseUrl}/ios");
        }

        private StringContent CreateDeviceDependentRequestBody(string runtimePlatform, string keyAttestationCerts, string keyChallengeSignature)
        {
            if (runtimePlatform == Device.Android)
            {
                string androidString = $"{{\"appVersion\": \"{WalletParams.AppVersion}\", \"attestation\": \"{App.SafetyResult}\", \"keyAttestationCerts\": {keyAttestationCerts}, \"keyChallengeSignature\": \"{keyChallengeSignature}\"}}";
                return new StringContent(androidString, Encoding.UTF8, "application/json");
            }

            string iOSString = $"{{\"teamId\": \"{WalletParams.TeamId}\", \"bundleId\": \"{WalletParams.PackageName}\", \"keyId\": \"{App.SafetyKey}\", \"appVersion\": \"{WalletParams.AppVersion}\", \"attestation\": \"{App.SafetyResult}\", \"keyAttestationPublicKey\": \"{keyAttestationCerts}\", \"keyChallengeSignature\": \"{keyChallengeSignature}\"}}";
            return new StringContent(iOSString, Encoding.UTF8, "application/json");
        }

        private static byte[] GetNonce(string issuerNonce, byte ending)
        {
            byte[] bytesNonce = Convert.FromBase64String(issuerNonce);
            byte[] bytesConst = new byte[] { ending };
            byte[] bytesNonceAndConst = new byte[bytesNonce.Length + bytesConst.Length];
            Buffer.BlockCopy(bytesNonce, 0, bytesNonceAndConst, 0, bytesNonce.Length);
            Buffer.BlockCopy(bytesConst, 0, bytesNonceAndConst, bytesNonce.Length, bytesConst.Length);
            return Sha256.sha256(bytesNonceAndConst);
        }

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
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
