using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Services;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries.Configuration;
using Microsoft.Extensions.Options;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    internal class LoginViewModel : CustomViewModel
    {
        private readonly IOptions<List<AgentOptions>> _agentOptions =
            App.Container.Resolve<IOptions<List<AgentOptions>>>();

        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IAppDeeplinkService _appDeeplinkService = App.Container.Resolve<IAppDeeplinkService>();
        private readonly ICustomWalletRecordService _walletRecordService =
            App.Container.Resolve<ICustomWalletRecordService>();
        private readonly InboxService _inboxService = App.Container.Resolve<InboxService>();

        private readonly ICustomSecureStorageService _secureStorageService =
            App.Container.Resolve<ICustomSecureStorageService>();

        private AgentOptions _activeAgent;

        private readonly Timer _t1 = new Timer
        {
            Interval = 5000,
            AutoReset = false
        };

        private readonly bool _walletExists;
        private bool _indicatorRunning;
        private bool _indicatorVisible;
        private string _pin;
        private int _pinCount;
        private string _pinLength = WalletParams.PinLength.ToString();
        private bool _pinMessageVisible;
        private bool _pinPadVisible;
        private string _pinText;
        private int _wrongPinCount;

        public LoginViewModel()
        {
            Task.Run(async () =>
                _wrongPinCount = await GetWrongPinCount()
            ).Wait();

            IndicatorRunning = false;
            IndicatorVisible = false;
            PinMessageVisible = true;

            if (_agentProvider.AgentExists())
            {
                PinText = Resources.Lang.LoginPage_PIN_Label;
                _walletExists = true;
            }
            else
            {
                PinText = Resources.Lang.LoginPage_New_PIN_Label;
                _walletExists = false;
            }

            if (_wrongPinCount >= 5)
            {
                PinText = Resources.Lang.PopUp_Login_Ultimately_Failed_Text;
                PinPadVisible = false;
            }
            else if (_wrongPinCount == 4)
            {
                PinText = Resources.Lang.PopUp_Login_Last_Try_Label;
                PinPadVisible = true;
            }
            else
            {
                PinPadVisible = true;
            }

            Validator = arg => { return ValidatorFunction(arg); };

            ErrorCommand = new Command(async () => { await ErrorCommandTask(); });

            SuccessCommand = new Command(async () => { await SuccessCommandTask(); });
        }

        private async Task<int> GetWrongPinCount()
        {
            try
            {
                return await _secureStorageService.GetAsync<int>(WalletParams.KeyAppBadPwdCount);
            }
            catch (Exception)
            {
                await _secureStorageService.SetAsync(WalletParams.KeyAppBadPwdCountOverall, 0);
                return 0;
            }
        }

        public ICommand ErrorCommand { get; }

        public bool IndicatorRunning
        {
            get => _indicatorRunning;
            set
            {
                _indicatorRunning = value;
                OnPropertyChanged(nameof(IndicatorRunning));
            }
        }

        public bool IndicatorVisible
        {
            get => _indicatorVisible;
            set
            {
                _indicatorVisible = value;
                OnPropertyChanged(nameof(IndicatorVisible));
            }
        }

        public string PinLength
        {
            get => _pinLength;
            set
            {
                _pinLength = value;
                OnPropertyChanged(nameof(PinLength));
            }
        }

        public bool PinMessageVisible
        {
            get => _pinMessageVisible;
            set
            {
                _pinMessageVisible = value;
                OnPropertyChanged(nameof(PinMessageVisible));
            }
        }

        public bool PinPadVisible
        {
            get => _pinPadVisible;
            set
            {
                _pinPadVisible = value;
                OnPropertyChanged(nameof(PinPadVisible));
            }
        }

        public string PinText
        {
            get => _pinText;
            set
            {
                _pinText = value;
                OnPropertyChanged(nameof(PinText));
            }
        }
        public ICommand SuccessCommand { get; }


        public Func<IList<char>, bool> Validator { get; }

        public async void BiometricsLogin()
        {
            bool isBiometricActivated = App.GetBiometricsInfo(_secureStorageService);
            if (isBiometricActivated)
            {
                PinPadVisible = false;
                PinMessageVisible = false;

                _t1.Elapsed += EnablePinPad;
                _t1.Start();

                App.BiometricLoginActive = true;
            }
            else
            {
                _t1.Stop();

                PinPadVisible = true;
                PinMessageVisible = true;

                return;
            }

            bool isBiometricsAvailable = await CrossFingerprint.Current.IsAvailableAsync(true);
            if (!isBiometricsAvailable)
            {
                App.BiometricLoginActive = false;

                await _secureStorageService.SetAsync(WalletParams.KeyBiometricActivated, false);
                BasicPopUp popUp = new BasicPopUp(
                    Resources.Lang.PopUp_Biometrics_Not_Configured_Title,
                    Resources.Lang.PopUp_Biometrics_Not_Configured_Text,
                    Resources.Lang.PopUp_Biometrics_Not_Configured_Button)
                {
                    PreLoginPopUp = true
                };
                await popUp.ShowPopUp();
                return;
            }

            AuthenticationRequestConfiguration authReqConf =
                new AuthenticationRequestConfiguration(Resources.Lang.LoginPage_Biometrics_Title,
                    Resources.Lang.LoginPage_Biometrics_Label);
            FingerprintAuthenticationResult auth = await CrossFingerprint.Current.AuthenticateAsync(authReqConf);
            if (auth.Authenticated)
            {
                _t1.Stop();

                await _inboxService.WaitForPnsHandle();

                PinPadVisible = false;
                PinMessageVisible = false;

                IndicatorVisible = true;
                IndicatorRunning = true;

                App.IsLoggedIn = true;
                App.LoggedInOnce = true;
                App.BiometricLoginActive = false;

                await _secureStorageService.SetAsync(WalletParams.KeyAppBadPwdCount, 0);
                await _secureStorageService.SetAsync(WalletParams.KeyAppBadPwdCountOverall, 0);
                await _secureStorageService.SetAsync(WalletParams.KeyAppBadLoginTime, DateTime.Now.AddDays(-1));

                if (!App.AlreadySubscribed)
                {
                    App.AlreadySubscribed = true;
                    App.AutoAcceptViewModel.Subscribe();
                }

                if (!App.BiometricLoginActive && !App.IsLoggedIn)
                {
                    BiometricsLogin();
                }

                MessagingCenter.Send(this, WalletEvents.AppStarted);

                while (!App.CredentialsLoaded || !App.ConnectionsLoaded || !App.HistoryLoaded)
                {
                    await Task.Delay(100);
                }

                try
                {
                    await Application.Current.MainPage.Navigation.PopModalAsync();
                }
                catch (Exception)
                {
                    //ignore
                }

                await _appDeeplinkService.ProcessAppDeeplink();
                _inboxService.PollMessages();

                App.PollingTimer.Start();

                IndicatorVisible = false;
                IndicatorRunning = false;
            }
            else
            {
                App.BiometricLoginActive = false;
            }
        }

        private async Task ErrorCommandTask()
        {
            if (_walletExists)
            {
                _wrongPinCount++;
                await _secureStorageService.SetAsync(WalletParams.KeyAppBadPwdCount, _wrongPinCount);

                if (_wrongPinCount >= 5)
                {
                    await _secureStorageService.SetAsync(WalletParams.WalletPreKeyTag, null);
                    await _secureStorageService.SetAsync(WalletParams.WalletSaltByteTag, null);

                    BasicPopUp popUp = new BasicPopUp(
                       Resources.Lang.PopUp_Login_Ultimately_Failed_Title,
                       Resources.Lang.PopUp_Login_Ultimately_Failed_Text,
                       Resources.Lang.PopUp_Login_Ultimately_Failed_Button)
                    {
                        PreLoginPopUp = true
                    };
                    await popUp.ShowPopUp();
                }
                else if ((_wrongPinCount < 4))
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Resources.Lang.PopUp_Login_Failed_Title,
                        Resources.Lang.PopUp_Login_Failed_Text,
                        Resources.Lang.PopUp_Login_Failed_Button)
                    {
                        PreLoginPopUp = true
                    };
                    await popUp.ShowPopUp();
                }
                else if ((_wrongPinCount == 4))
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Resources.Lang.PopUp_Login_Failed_Title,
                        Resources.Lang.PopUp_Login_Last_Try_Label,
                        Resources.Lang.PopUp_Login_Failed_Button)
                    {
                        PreLoginPopUp = true
                    };
                    await popUp.ShowPopUp();
                }
            }
            else
            {
                if (_pinCount == 2)
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Resources.Lang.PopUp_New_PIN_Failed_Title,
                        Resources.Lang.PopUp_New_PIN_Failed_Text,
                        Resources.Lang.PopUp_New_PIN_Failed_Button)
                    {
                        PreLoginPopUp = true
                    };
                    await popUp.ShowPopUp();

                    PinText = Resources.Lang.LoginPage_New_PIN_Label;
                    _pinCount = 0;
                    _pin = null;
                }
            }

            Toggle();
        }

        private void EnablePinPad(object source, ElapsedEventArgs e)
        {
            PinMessageVisible = true;
            PinPadVisible = true;
        }

        private async Task SuccessCommandTask()
        {
            if (_pinCount == 1 && !_walletExists)
            {
                Toggle();
            }
            else
            {
                PinText = Resources.Lang.LoginPage_PIN_Label;
                _wrongPinCount = 0;
                await _secureStorageService.SetAsync(WalletParams.KeyAppBadPwdCount, _wrongPinCount);

                _pinCount = 0;

                await _inboxService.WaitForPnsHandle();

                if (!_walletExists)
                {
                    await _agentProvider.StoreAgentConfigs(_agentOptions.Value);
                    await _agentProvider.CreateAgentAsync(_agentOptions.Value.First(), null, _pin);

                    // Create a byte array to hold the random value.
                    byte[] salt = new byte[16];
                    using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
                    {
                        // Fill the array with a random value.
                        rngCsp.GetBytes(salt);
                    }

                    Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(_pin, salt, 100000);
                    byte[] keyByte = rfc2898DeriveBytes.GetBytes(16);

                    PinRecord pinRecord = new PinRecord() { Id = Guid.NewGuid().ToString(), WalletPinSaltByte = salt, WalletPinPBKDF2 = keyByte };
                    await _walletRecordService.AddAsync(App.Wallet, pinRecord);

                    await _secureStorageService.SetAsync(WalletParams.PushService, App.PushService);
                }

                PinRecord pinRecordLoaded = (await _walletRecordService.SearchAsync<PinRecord>(App.Wallet, null, null, 1, false)).FirstOrDefault();

                App.IsLoggedIn = true;
                App.LoggedInOnce = true;
                App.BiometricLoginActive = false;

                if (!App.AlreadySubscribed)
                {
                    App.AlreadySubscribed = true;
                    App.AutoAcceptViewModel.Subscribe();
                }

                MessagingCenter.Send(this, WalletEvents.AppStarted);

                while (!App.CredentialsLoaded || !App.ConnectionsLoaded || !App.HistoryLoaded)
                {
                    await Task.Delay(100);
                }

                try
                {
                    await Application.Current.MainPage.Navigation.PopModalAsync();
                }
                catch (Exception)
                {
                    //ignore
                }

                await _appDeeplinkService.ProcessAppDeeplink();
                _inboxService.PollMessages();

                App.PollingTimer.Start();

                Toggle();
            }
        }

        private void Toggle()
        {
            IndicatorRunning = !IndicatorRunning;
            IndicatorVisible = !IndicatorVisible;
            PinMessageVisible = !PinMessageVisible;
            PinPadVisible = !PinPadVisible;
        }

        private bool ValidatorFunction(IList<char> arg)
        {
            Toggle();

            if (_wrongPinCount >= 5)
            {
                return false;
            }

            if (_pinCount == 0 && !_walletExists)
            {
                _pinCount = 1;
                _pin = string.Concat(arg.TakeWhile(char.IsNumber));
                PinText = Resources.Lang.LoginPage_Confirm_PIN_Label;
                return true;
            }

            if (_pinCount == 1 && !_walletExists)
            {
                _pinCount = 2;
                return string.Concat(arg.TakeWhile(char.IsNumber)) == _pin;
            }

            Task.Run(async () =>
                _activeAgent = await _agentProvider.GetActiveAgent()
            ).Wait();

            Task<bool> task = Task.Run(async () =>
                await _agentProvider.OpenWallet(_activeAgent, string.Concat(arg.TakeWhile(char.IsNumber)))
            );
            task.Wait();

            return task.Result;
        }
    }
}