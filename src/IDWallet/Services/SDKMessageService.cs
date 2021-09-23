using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Models.AusweisSDK;
using IDWallet.Resources;
using IDWallet.Utils;
using IDWallet.Views.BaseId.PopUps;
using IDWallet.Views.DDL.PopUps;
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
        public HttpClient SdkHttpClient;
        public HttpClientHandler SdkHttpClientHandler;
        public CookieContainer SdkCookieContainer;
        public string PublicKey;
        private SdkMessageFlow _messageFlow = SdkMessageFlow.BaseId;

        public SDKMessageService()
        {
        }

        public void StartBaseIdFlow()
        {
            _messageFlow = SdkMessageFlow.BaseId;
            InitHttpClient();
        }

        public void StartDdlFlow()
        {
            _messageFlow = SdkMessageFlow.DDL;
            InitHttpClient();
        }

        public void InitHttpClient()
        {
            SdkCookieContainer = new CookieContainer();
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    CreateBaseIdHttpClientHandler();
                    break;
                case SdkMessageFlow.DDL:
                    CreateDdlHttpClientHandler();
                    break;
            }
            SdkHttpClient = new HttpClient(SdkHttpClientHandler);
        }

        private void CreateBaseIdHttpClientHandler()
        {
            SdkHttpClientHandler = new HttpClientHandler()
            {
                CookieContainer = SdkCookieContainer,
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
        }

        private void CreateDdlHttpClientHandler()
        {
            SdkHttpClientHandler = new HttpClientHandler()
            {
                CookieContainer = SdkCookieContainer,
                UseCookies = true,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, error) =>
                {
                    List<string> allowedPublicKeys = new List<string>
                    {
                        // BDR API Public Key Pinning
                    };
                    if (sender.RequestUri.Host.Equals(new Uri($"https://{WalletParams.DdlHost}").Host))
                    {
                        string pubKey = cert.GetPublicKeyString();
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
        }

        public void ReceiveAccessRights(SdkMessage sdkMessage)
        {
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    MessagingCenter.Send(this, BaseIDEvents.AccessRights, sdkMessage);
                    break;
                case SdkMessageFlow.DDL:
                    MessagingCenter.Send(this, DDLEvents.AccessRights, sdkMessage);
                    break;
            }
        }

        public void ReceiveEnterPIN(SdkMessage sdkMessage)
        {
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    MessagingCenter.Send(this, BaseIDEvents.EnterPIN, sdkMessage);
                    break;
                case SdkMessageFlow.DDL:
                    MessagingCenter.Send(this, DDLEvents.EnterPIN, sdkMessage);
                    break;
            }
        }

        public void ReceiveEnterNewPIN(SdkMessage sdkMessage)
        {
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    MessagingCenter.Send(this, BaseIDEvents.EnterNewPIN, sdkMessage);
                    break;
                case SdkMessageFlow.DDL:
                    MessagingCenter.Send(this, DDLEvents.EnterNewPIN, sdkMessage);
                    break;
            }
        }

        public void ReceiveEnterCAN(SdkMessage sdkMessage)
        {
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    MessagingCenter.Send(this, BaseIDEvents.EnterCAN, sdkMessage);
                    break;
                case SdkMessageFlow.DDL:
                    MessagingCenter.Send(this, DDLEvents.EnterCAN, sdkMessage);
                    break;
            }
        }

        public void ReceiveEnterPUK(SdkMessage sdkMessage)
        {
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    MessagingCenter.Send(this, BaseIDEvents.EnterPUK, sdkMessage);
                    break;
                case SdkMessageFlow.DDL:
                    MessagingCenter.Send(this, DDLEvents.EnterPUK, sdkMessage);
                    break;
            }
        }

        public void ReceiveAuth(SdkMessage sdkMessage)
        {
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    MessagingCenter.Send(this, BaseIDEvents.Auth, sdkMessage);
                    break;
                case SdkMessageFlow.DDL:
                    MessagingCenter.Send(this, DDLEvents.Auth, sdkMessage);
                    break;
            }
        }

        public void ReceiveChangePIN(SdkMessage sdkMessage)
        {
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    MessagingCenter.Send(this, BaseIDEvents.ChangePIN, sdkMessage);
                    break;
                case SdkMessageFlow.DDL:
                    MessagingCenter.Send(this, DDLEvents.ChangePIN, sdkMessage);
                    break;
            }
        }

        public void ReceiveInsertCard(SdkMessage sdkMessage)
        {
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    MessagingCenter.Send(this, BaseIDEvents.InsertCard, sdkMessage);
                    break;
                case SdkMessageFlow.DDL:
                    MessagingCenter.Send(this, DDLEvents.InsertCard, sdkMessage);
                    break;
            }
        }

        public void ReceiveReader(SdkMessage sdkMessage)
        {
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    MessagingCenter.Send(this, BaseIDEvents.Reader, sdkMessage);
                    break;
                case SdkMessageFlow.DDL:
                    MessagingCenter.Send(this, DDLEvents.Reader, sdkMessage);
                    break;
            }
        }

        public async Task SendRunAuth()
        {
            try
            {
                string hwAlias = "";
                switch (_messageFlow)
                {
                    case SdkMessageFlow.BaseId:
                        hwAlias = WalletParams.BaseIdAlias;
                        break;
                    case SdkMessageFlow.DDL:
                        hwAlias = WalletParams.DdlAlias;
                        break;
                }

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

                            string hwKey = hardwareKeyService.GetPublicKeyAsBase64(GetNonce(tcToken.IssuerNonce, 1), hwAlias);

                            string signedNonce = hardwareKeyService.Sign(GetNonce(tcToken.IssuerNonce, 2), hwAlias);

                            Uri uri = CreateDeviceDependentUri(Device.RuntimePlatform);
                            StringContent body = CreateDeviceDependentRequestBody(Device.RuntimePlatform, hwKey, signedNonce);

                            HttpResponseMessage response = SdkHttpClient.PostAsync(uri, body).GetAwaiter().GetResult();

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
                SdkHttpClient.DefaultRequestHeaders.Clear();
            }
            catch (Exception)
            {
                switch (_messageFlow)
                {
                    case SdkMessageFlow.BaseId:
                        BaseIdBasicPopUp popUpBaseId = new BaseIdBasicPopUp(
                            Lang.PopUp_BaseID_Auth_Error_Title,
                            Lang.PopUp_BaseID_Auth_Error_Text,
                            Lang.PopUp_BaseID_Auth_Error_Button
                        );
                        await popUpBaseId.ShowPopUp();
                        break;
                    case SdkMessageFlow.DDL:
                        DdlBasicPopUp popUpDdl = new DdlBasicPopUp(
                            Lang.PopUp_DDL_Auth_Error_Title,
                            Lang.PopUp_DDL_Auth_Error_Text,
                            Lang.PopUp_DDL_Auth_Error_Button
                        );
                        await popUpDdl.ShowPopUp();
                        break;
                }

                SendCancel();

                Views.CustomTabbedPage mainPage = Application.Current.MainPage as Views.CustomTabbedPage;
                INavigation Navigation = ((NavigationPage)mainPage.CurrentPage).Navigation;
                try
                {
                    await Navigation.PopAsync();
                }
                catch (Exception)
                { }
            }
        }

        private async Task<TcTokenModel> GetToken()
        {
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    SdkHttpClient.DefaultRequestHeaders.Add(WalletParams.ApiHeader, StringCipher.Decrypt(WalletParams.ApiKeyBaseId, WalletParams.PackageName + WalletParams.AppVersion));
                    break;
                case SdkMessageFlow.DDL:
                    SdkHttpClient.DefaultRequestHeaders.Add(WalletParams.ApiHeader, StringCipher.Decrypt(WalletParams.ApiKeyDdl, WalletParams.PackageName + WalletParams.AppVersion));
                    break;
            }

            HttpResponseMessage result = new HttpResponseMessage();
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    result = await SdkHttpClient.GetAsync($"https://{WalletParams.AusweisHost}/oauth2/authorization/ausweisident-integrated");
                    break;
                case SdkMessageFlow.DDL:
                    result = await SdkHttpClient.GetAsync($"https://{WalletParams.DdlHost}/oauth2/authorization/ausweisident-integrated");
                    break;
            }

            TcTokenModel tcToken = new TcTokenModel();
            if (result.IsSuccessStatusCode)
            {
                tcToken = JObject.Parse(await result.Content.ReadAsStringAsync()).ToObject<TcTokenModel>();
                DependencyService.Get<IAusweisSdk>().SendCall($"{{\"cmd\": \"RUN_AUTH\", \"tcTokenURL\": \"{tcToken.TcTokenUrl}\"}}");
            }
            else
            {
                switch (_messageFlow)
                {
                    case SdkMessageFlow.BaseId:
                        BaseIdBasicPopUp popUpBaseId = new BaseIdBasicPopUp(
                            Lang.PopUp_BaseID_Auth_Error_Title,
                            Lang.PopUp_BaseID_Auth_Error_Text,
                            Lang.PopUp_BaseID_Auth_Error_Button
                        );
                        await popUpBaseId.ShowPopUp();
                        break;
                    case SdkMessageFlow.DDL:
                        DdlBasicPopUp popUpDdl = new DdlBasicPopUp(
                            Lang.PopUp_DDL_Auth_Error_Title,
                            Lang.PopUp_DDL_Auth_Error_Text,
                            Lang.PopUp_DDL_Auth_Error_Button
                        );
                        await popUpDdl.ShowPopUp();
                        break;
                }

                SendCancel();

                Views.CustomTabbedPage mainPage = Application.Current.MainPage as Views.CustomTabbedPage;
                INavigation Navigation = ((NavigationPage)mainPage.CurrentPage).Navigation;
                try
                {
                    await Navigation.PopAsync();
                }
                catch (Exception)
                { }
            }

            return tcToken;
        }

        private Uri CreateDeviceDependentUri(string runtimePlatform)
        {
            string baseUrl = "";
            switch (_messageFlow)
            {
                case SdkMessageFlow.BaseId:
                    baseUrl = $"https://{WalletParams.AusweisHost}/api/integrated/wallet-validation";
                    break;
                case SdkMessageFlow.DDL:
                    baseUrl = $"https://{WalletParams.DdlHost}/api/integrated/wallet-validation";
                    break;
            }

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
            {
                hex.AppendFormat("{0:x2}", b);
            }

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
