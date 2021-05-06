using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Services;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Proof.PopUps;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Extensions;
using Microsoft.Extensions.Options;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class AuthViewModel : CustomViewModel
    {
        private readonly IAppDeeplinkService _appDeeplinkService = App.Container.Resolve<IAppDeeplinkService>();
        private readonly ICustomWalletRecordService _walletRecordService =
            App.Container.Resolve<ICustomWalletRecordService>();
        private readonly InboxService _inboxService = App.Container.Resolve<InboxService>();
        private readonly ICustomSecureStorageService _secureStorageService =
            App.Container.Resolve<ICustomSecureStorageService>();

        private readonly Timer _t1 = new Timer
        {
            Interval = 5000,
            AutoReset = false
        };

        private bool _indicatorRunning;
        private bool _indicatorVisible;
        private string _pin;
        private string _pinLength = WalletParams.PinLength.ToString();
        private bool _pinMessageVisible;
        private bool _pinPadVisible;
        private string _pinText;
        private int _wrongPinCount;
        private PinRecord _pinRecordLoaded;
        private ProofViewModel _proofViewModel;
        private ProofAuthenticationPopUp _authPopUp;

        public AuthViewModel(ProofViewModel proofViewModel)
        {
            _proofViewModel = proofViewModel;

            Task.Run(async () =>
                _wrongPinCount = await GetWrongPinCount()
            ).Wait();

            GetPinRecord();

            IndicatorRunning = false;
            IndicatorVisible = false;
            PinMessageVisible = true;

            if (_wrongPinCount >= 5)
            {
                PinText = Resources.Lang.PopUp_Login_Ultimately_Failed_Text;
                PinPadVisible = false;
            }
            else if(_wrongPinCount == 4){
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

        private async void GetPinRecord()
        {
            _pinRecordLoaded = (await _walletRecordService.SearchAsync<PinRecord>(App.Wallet, null, null, 1, false)).FirstOrDefault();
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

        public ProofViewModel ProofViewModel
        {
            get => _proofViewModel;
            set
            {
                _proofViewModel = value;
                OnPropertyChanged(nameof(ProofViewModel));
            }
        }

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

        private async Task ErrorCommandTask()
        {
            _wrongPinCount++;
            await _secureStorageService.SetAsync(WalletParams.KeyAppBadPwdCount, _wrongPinCount);

            if (_wrongPinCount >= 5)
            {
                _proofViewModel.AuthError = true;

                await _secureStorageService.SetAsync(WalletParams.WalletPreKeyTag, null);
                await _secureStorageService.SetAsync(WalletParams.WalletSaltByteTag, null);

                BasicPopUp popUp = new BasicPopUp(
                   Resources.Lang.PopUp_Login_Ultimately_Failed_Title,
                   Resources.Lang.PopUp_Login_Ultimately_Failed_Text,
                   Resources.Lang.PopUp_Login_Ultimately_Failed_Button)
                {
                    ProofSendPopUp = true
                };
                await popUp.ShowPopUp();
                Process.GetCurrentProcess().Kill();
            }
            else if (_wrongPinCount < 4)
            {
                BasicPopUp popUp = new BasicPopUp(
                    Resources.Lang.PopUp_Login_Failed_Title,
                    Resources.Lang.PopUp_Login_Failed_Text,
                    Resources.Lang.PopUp_Login_Failed_Button)
                {
                    ProofSendPopUp = true
                };
                await popUp.ShowPopUp();
            }

            else if (_wrongPinCount == 4)
            {
                BasicPopUp popUp = new BasicPopUp(
                    Resources.Lang.PopUp_Login_Failed_Title,
                    Resources.Lang.PopUp_Login_Last_Try_Label,
                    Resources.Lang.PopUp_Login_Failed_Button)
                {
                    ProofSendPopUp = true
                };
                await popUp.ShowPopUp();
                PinText = Resources.Lang.PopUp_Login_Last_Try_Label;
            }

            Toggle();
        }

        private void PinPadVisble(object source, ElapsedEventArgs e)
        {
            PinMessageVisible = true;
        }

        private async Task SuccessCommandTask()
        {
            _proofViewModel.AuthSuccess = true;
        }

        private void Toggle()
        {
            IndicatorRunning = !IndicatorRunning;
            IndicatorVisible = !IndicatorVisible;
            PinMessageVisible = !PinMessageVisible;
        }

        private bool ValidatorFunction(IList<char> arg)
        {
            Toggle();

            if (_wrongPinCount >= 5)
            {
                return false;
            }

            _pin = string.Concat(arg.TakeWhile(char.IsNumber));

            Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(_pin, _pinRecordLoaded.WalletPinSaltByte, 100000);
            byte[] keyByte = rfc2898DeriveBytes.GetBytes(16);

            return (keyByte.ToBase64String().Equals(_pinRecordLoaded.WalletPinPBKDF2.ToBase64String()));
        }
    }
}