using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Interfaces;
using IDWallet.Views;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    internal class ChangePinViewModel : CustomViewModel
    {
        private readonly ICustomSecureStorageService _secureStorageService =
            App.Container.Resolve<ICustomSecureStorageService>();
        private readonly ICustomWalletRecordService _walletRecordService =
            App.Container.Resolve<ICustomWalletRecordService>();
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IOptions<List<AgentOptions>> _agentOptions =
            App.Container.Resolve<IOptions<List<AgentOptions>>>();
        private AgentOptions _activeAgent;

        private bool _indicatorRunning;
        private bool _indicatorVisible;
        private string _pin;
        private string _oldPin;
        private int _pinCount;
        private bool _pinPadVisible;
        private string _pinLength = WalletParams.PinLength.ToString();
        private string _pinText;
        private bool _pinTextIsVisible;
        private int _wrongPinCount;
        private PinRecord _pinRecordLoaded;

        public ChangePinViewModel()
        {
            IndicatorRunning = false;
            IndicatorVisible = false;
            PinPadVisible = true;
            PinMessageVisible = true;

            if (_pinCount == 0)
            {
                PinText = Resources.Lang.ChangePasscodePage_First_Label;
            }

            Task.Run(async () =>
                _wrongPinCount = await GetWrongPinCount()
            ).Wait();

            GetPinRecord();

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
            get => _pinTextIsVisible;
            set
            {
                _pinTextIsVisible = value;
                OnPropertyChanged(nameof(PinMessageVisible));
            }
        }

        public bool PinPadVisible
        {
            get => _pinPadVisible;
            set => SetProperty(ref _pinPadVisible, value);
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
            switch (_pinCount)
            {
                case 1:
                    _wrongPinCount++;
                    await _secureStorageService.SetAsync(WalletParams.KeyAppBadPwdCount, _wrongPinCount);

                    if (_wrongPinCount >= 5)
                    {
                        PinPadVisible = false;
                        PinText = Resources.Lang.PopUp_Login_Ultimately_Failed_Text;
                        BasicPopUp popUp = new BasicPopUp(
                                           Resources.Lang.PopUp_Login_Ultimately_Failed_Title,
                                           Resources.Lang.PopUp_Login_Ultimately_Failed_Text,
                                           Resources.Lang.PopUp_Login_Ultimately_Failed_Button);
                        await popUp.ShowPopUp();
                        Process.GetCurrentProcess().Kill();
                    }
                    else if ((_wrongPinCount < 4))
                    {
                        PinText = Resources.Lang.PopUp_Login_Last_Try_Label;
                        BasicPopUp popUp = new BasicPopUp(
                            Resources.Lang.PopUp_Login_Failed_Title,
                            Resources.Lang.PopUp_Login_Last_Try_Label,
                            Resources.Lang.PopUp_Login_Failed_Button);
                        await popUp.ShowPopUp();
                    }
                    BasicPopUp popUp1 = new BasicPopUp(
                        Resources.Lang.PopUp_Old_PIN_Wrong_Title,
                        Resources.Lang.PopUp_Old_PIN_Wrong_Text,
                        Resources.Lang.PopUp_Old_PIN_Wrong_Button);
                    await popUp1.ShowPopUp();
                    break;
                case 3:
                    BasicPopUp popUp3 = new BasicPopUp(
                        Resources.Lang.PopUp_New_PIN_No_Match_Title,
                        Resources.Lang.PopUp_New_PIN_No_Match_Text,
                        Resources.Lang.PopUp_New_PIN_No_Match_Button);
                    await popUp3.ShowPopUp();
                    break;
                default:
                    break;
            }

            _pinCount = 0;
            PinText = Resources.Lang.ChangePasscodePage_First_Label;
            Toggle();
        }

        private async Task SuccessCommandTask()
        {
            switch (_pinCount)
            {
                case 1:
                    _wrongPinCount = 0;
                    await _secureStorageService.SetAsync(WalletParams.KeyAppBadPwdCount, _wrongPinCount);
                    PinText = Resources.Lang.ChangePasscodePage_Second_Label;
                    break;
                case 2:
                    PinText = Resources.Lang.ChangePasscodePage_Third_Label;
                    break;
                case 3:
                    PinText = Resources.Lang.ChangePasscodePage_First_Label;

                    byte[] salt = new byte[16];
                    using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
                    {
                        // Fill the array with a random value.
                        rngCsp.GetBytes(salt);
                    }

                    Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(_pin, salt, 100000);
                    byte[] keyByte = rfc2898DeriveBytes.GetBytes(16);

                    _pinRecordLoaded.WalletPinPBKDF2 = keyByte;
                    _pinRecordLoaded.WalletPinSaltByte = salt;

                    await _walletRecordService.UpdateAsync(App.Wallet, _pinRecordLoaded);

                    Task.Run(async () =>
                        _activeAgent = await _agentProvider.GetActiveAgent()
                    ).Wait();
                    await _agentProvider.OpenWallet(_activeAgent, _pin, _oldPin);

                    BasicPopUp popUp = new BasicPopUp(
                        Resources.Lang.PopUp_PIN_Changed_Title,
                        Resources.Lang.PopUp_PIN_Changed_Text,
                        Resources.Lang.PopUp_PIN_Changed_Button);
                    await popUp.ShowPopUp();

                    Application.Current.MainPage = await Task.Run(() => new CustomTabbedPage());
                    break;
                default:
                    break;
            }

            Toggle();
        }

        private async void GetPinRecord()
        {
            _pinRecordLoaded = (await _walletRecordService.SearchAsync<PinRecord>(App.Wallet, null, null, 1, false)).FirstOrDefault();
        }

        private void Toggle()
        {
            IndicatorRunning = !IndicatorRunning;
            IndicatorVisible = !IndicatorVisible;
            PinPadVisible = !PinPadVisible;
            PinMessageVisible = !PinMessageVisible;
        }

        private bool ValidatorFunction(IList<char> arg)
        {
            Toggle();

            switch (_pinCount)
            {
                case 0:
                    _pinCount++;

                    _pin = string.Concat(arg.TakeWhile(char.IsNumber));

                    Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(_pin, _pinRecordLoaded.WalletPinSaltByte, 100000);
                    byte[] keyByte = rfc2898DeriveBytes.GetBytes(16);
                    if (keyByte.ToBase64String().Equals(_pinRecordLoaded.WalletPinPBKDF2.ToBase64String()))
                    {
                        _oldPin = _pin;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                case 1:
                    _pinCount++;
                    _pin = string.Concat(arg.TakeWhile(char.IsNumber));
                    return true;
                case 2:
                    _pinCount++;
                    return string.Concat(arg.TakeWhile(char.IsNumber)) == _pin;
                default:
                    return false;
            }
        }
    }
}