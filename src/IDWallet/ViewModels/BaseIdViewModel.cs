using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Events;
using IDWallet.Models;
using IDWallet.Models.AusweisSDK;
using IDWallet.Resources;
using IDWallet.Services;
using IDWallet.Views.BaseId.PopUps;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class BaseIdViewModel : CustomViewModel
    {
        private readonly SDKMessageService _sdkService = App.Container.Resolve<SDKMessageService>();
        private readonly ConnectService _connectService = App.Container.Resolve<ConnectService>();
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IMessageService _messageService = App.Container.Resolve<IMessageService>();
        private readonly ICustomWalletRecordService _walletRecordService =
                            App.Container.Resolve<ICustomWalletRecordService>();

        private int _progress;
        private int _carouselPosition;
        private bool _progressBarIsVisible;
        private bool _isStartEnabled;
        private int _idPinLength;
        private string _idPinHeaderLabel;
        private string _idPinBoldLabel;
        private string _idPinBodyLabel;
        private bool _idPinLinkIsVisible;
        private bool _forgotPINLinkIsVisible;
        private bool _moreInformationLinkIsVisible;
        private string _newPIN = null;
        private bool _isActivityIndicatorVisible;
        private SdkMessage _finalAuthMessage;
        private readonly ReadyToScanPopUp _scanPopUp;
        private bool _pinPadIsVisible;
        private bool _idPinBoldIsVisible;
        private string _baseIdConnection;
        private bool _hasAcceptedAccess = false;

        public BaseIdProcessType BaseIdProcessType;
        public bool ViewModelWasResetted { get; set; }
        public INavigation Navigation { get; set; }
        public SdkMessageType ActiveMessageType;

        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        public int CarouselPosition
        {
            get => _carouselPosition;
            set => SetProperty(ref _carouselPosition, value);
        }

        public bool ProgressBarIsVisible
        {
            get => _progressBarIsVisible;
            set => SetProperty(ref _progressBarIsVisible, value);
        }

        public Func<IList<char>, bool> IdPinValidator { get; }

        public bool IsStartEnabled
        {
            get => _isStartEnabled;
            set => SetProperty(ref _isStartEnabled, value);
        }

        public int IdPinLength
        {
            get => _idPinLength;
            set => SetProperty(ref _idPinLength, value);
        }

        public string IdPinHeaderLabel
        {
            get => _idPinHeaderLabel;
            set => SetProperty(ref _idPinHeaderLabel, value);
        }

        public string IdPinBoldLabel
        {
            get => _idPinBoldLabel;
            set => SetProperty(ref _idPinBoldLabel, value);
        }

        public bool IdPinBoldIsVisible
        {
            get => _idPinBoldIsVisible;
            set => SetProperty(ref _idPinBoldIsVisible, value);
        }

        public string IdPinBodyLabel
        {
            get => _idPinBodyLabel;
            set => SetProperty(ref _idPinBodyLabel, value);
        }

        public bool IdPinLinkIsVisible
        {
            get => _idPinLinkIsVisible;
            set => SetProperty(ref _idPinLinkIsVisible, value);
        }

        public bool MoreInformationLinkIsVisible
        {
            get => _moreInformationLinkIsVisible;
            set => SetProperty(ref _moreInformationLinkIsVisible, value);
        }

        public bool ForgotPINLinkIsVisible
        {
            get => _forgotPINLinkIsVisible;
            set => SetProperty(ref _forgotPINLinkIsVisible, value);
        }

        public bool IsActivityIndicatorVisible
        {
            get => _isActivityIndicatorVisible;
            set => SetProperty(ref _isActivityIndicatorVisible, value);
        }

        public bool PinPadIsVisible
        {
            get => _pinPadIsVisible;
            set => SetProperty(ref _pinPadIsVisible, value);
        }

        private bool _alreadySubscribed;

        private Command _idPinErrorCommand;
        public Command IdPinErrorCommand =>
            _idPinErrorCommand ??= new Command(async () => { await IdPinErrorTask(); });

        private Command _idPinSuccessCommand;
        public Command IdPinSuccessCommand =>
            _idPinSuccessCommand ??= new Command(async () => { await IdPinSuccessTask(); });

        private Command _changeDigitsTappedCommand;
        public Command ChangeDigitsTappedCommand =>
            _changeDigitsTappedCommand ??= new Command(ChangeDigitsTapped);

        private Command _sixDigitsTappedCommand;
        public Command SixDigitsTappedCommand =>
            _sixDigitsTappedCommand ??= new Command(UseRegularPIN);

        private Command _fiveDigitsTappedCommand;
        public Command FiveDigitsTappedCommand =>
            _fiveDigitsTappedCommand ??= new Command(UseTransportPIN);

        private Command _forgotPINTappedCommand;
        public Command ForgotPINTappedCommand =>
            _forgotPINTappedCommand ??= new Command(ForgotPINTapped);

        private Command _moreInformationTappedCommand;
        public Command MoreInformationTappedCommand =>
            _moreInformationTappedCommand ??= new Command(MoreInformationTapped);

        public BaseIdViewModel()
        {
            ViewModelWasResetted = false;
            IsActivityIndicatorVisible = false;
            ActiveMessageType = SdkMessageType.UNKNOWN_COMMAND;
            Progress = 0;
            ProgressBarIsVisible = true;
            IdPinLength = 6;
            BaseIdProcessType = BaseIdProcessType.None;
            _scanPopUp = new ReadyToScanPopUp();

            IsStartEnabled = true;
            IdPinBoldIsVisible = true;
            PinPadIsVisible = false;
            IdPinLinkIsVisible = false;
            ForgotPINLinkIsVisible = false;
            MoreInformationLinkIsVisible = false;

            IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
            IdPinBoldLabel = Lang.BaseIDPage_PINScreen_Selection_Bold_Text;
            IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Selection_Body_Text;

            IdPinValidator = arg => { return ValidateIdPin(arg); };

            _alreadySubscribed = false;
        }

        public void Subscribe()
        {
            if (!_alreadySubscribed)
            {
                _alreadySubscribed = true;
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.AccessRights, Access_Rights);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterPIN, Enter_PIN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterNewPIN, Enter_New_PIN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterCAN, Enter_CAN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterPUK, Enter_PUK);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.Auth, Auth);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.ChangePIN, Change_PIN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.InsertCard, Insert_Card);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.Reader, Reader);
                MessagingCenter.Subscribe<ServiceMessageEventService, string>(this, WalletEvents.BaseIdCredentialOffer, BaseIdCredentialOffer);
                MessagingCenter.Subscribe<ServiceMessageEventService, string>(this, WalletEvents.BaseIdCredentialIssue, BaseIdCredentialIssue);
            }
        }

        public void Unsubscribe()
        {
            _alreadySubscribed = false;

            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.AccessRights);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterPIN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterNewPIN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterCAN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.EnterPUK);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.Auth);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.ChangePIN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.InsertCard);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, BaseIDEvents.Reader);
            MessagingCenter.Unsubscribe<ServiceMessageEventService, string>(this, WalletEvents.BaseIdCredentialOffer);
            MessagingCenter.Unsubscribe<ServiceMessageEventService, string>(this, WalletEvents.BaseIdCredentialIssue);
        }

        private async void Auth(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (sdkMessage.Result != null && sdkMessage.Result.Description != "The process has been cancelled.")
                {
                    _finalAuthMessage = sdkMessage;

                    string redirectUrl = _finalAuthMessage.Url;

                    System.Net.Http.HttpResponseMessage result = await _sdkService.AusweisSdkHttpClient.GetAsync(redirectUrl);

                    if (result.IsSuccessStatusCode)
                    {
                        try
                        {
                            string resultString = await result.Content.ReadAsStringAsync();
                            AusweisSdkInvitation ausweisSdkInvitation = JObject.Parse(resultString).ToObject<AusweisSdkInvitation>();

                            Agent.Models.CustomConnectionInvitationMessage connectionInvitationMessage = _connectService.ReadInvitationUrl(ausweisSdkInvitation.InvitationUrl);

                            ConnectionRecord baseIdConnection = await _connectService.AcceptInvitationAsync(connectionInvitationMessage);
                            App.BaseIdConnectionId = _baseIdConnection = baseIdConnection.Id;

                            if (!string.IsNullOrEmpty(ausweisSdkInvitation.RevocationPassphrase))
                            {
                                baseIdConnection.SetTag(WalletParams.KeyRevocationPassphrase, ausweisSdkInvitation.RevocationPassphrase);
                                IAgentContext agentContext = await _agentProvider.GetContextAsync();
                                await _walletRecordService.UpdateAsync(agentContext.Wallet, baseIdConnection);
                            }

                            MessagingCenter.Send(this, WalletEvents.ReloadConnections);
                        }
                        catch (Exception)
                        {
                            _sdkService.SendCancel();
                            _sdkService.InitHttpClient();

                            BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                                    Lang.PopUp_BaseID_Auth_Error_Title,
                                    Lang.PopUp_BaseID_Auth_Error_Text,
                                    Lang.PopUp_BaseID_Auth_Error_Button
                                    );
                            await popUp.ShowPopUp();
                            await Navigation.PopAsync();
                        }
                    }
                    else
                    {
                        _sdkService.SendCancel();
                        _sdkService.InitHttpClient();
                        BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                        Lang.PopUp_BaseID_Auth_Error_Title,
                        Lang.PopUp_BaseID_Auth_Error_Text,
                        Lang.PopUp_BaseID_Auth_Error_Button
                        );
                        await popUp.ShowPopUp();
                        await Navigation.PopAsync();
                    }
                }
                else if (BaseIdProcessType == BaseIdProcessType.TransportPIN)
                {
                    _sdkService.SendRunChangePIN();
                }
            });
        }

        private async void Access_Rights(SDKMessageService obj, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                ActiveMessageType = SdkMessageType.ACCESS_RIGHTS;
                if (!_hasAcceptedAccess)
                {
                    AccessRightsPopUp accessRightsPopUp = new AccessRightsPopUp(sdkMessage.Chat.Effective);
                    PopUpResult accessRightsResult = await accessRightsPopUp.ShowPopUp();

                    if (accessRightsResult == PopUpResult.Accepted)
                    {
                        _hasAcceptedAccess = true;
                        _sdkService.SendAccept();
                        IsActivityIndicatorVisible = true;
                    }
                    else
                    {
                        _sdkService.SendCancel();
                        _sdkService.InitHttpClient();
                        BaseIdProcessType = BaseIdProcessType.None;
                        await Navigation.PopAsync();
                    }
                }
                else
                {
                    _sdkService.SendAccept();
                }
            });
        }

        private async void Insert_Card(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (ActiveMessageType == SdkMessageType.ACCESS_RIGHTS)
                {
                    IsActivityIndicatorVisible = false;
                    CarouselPosition = 2;
                    Progress = 2;
                }
                else
                {
                    _scanPopUp.ShowPopUp();
                    _scanPopUp.IsOpen = true;
                }
            });
        }

        private void Reader(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (_scanPopUp.IsOpen)
                {
                    try
                    {
                        _scanPopUp.IsOpen = false;
                        _scanPopUp.CancelScan();
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            });
        }

        private async void Enter_PIN(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                if (CarouselPosition != 3)
                {
                    CarouselPosition = 3;
                }

                switch (BaseIdProcessType)
                {
                    case BaseIdProcessType.Authentication:
                        await HandleAuthenticationEnterPin(sdkMessage);
                        break;
                    case BaseIdProcessType.ChangePIN:
                        await HandleChangePinEnterPin(sdkMessage);
                        break;
                    case BaseIdProcessType.TransportPIN:
                        await HandleTransportPinEnterPin(sdkMessage);
                        break;
                    default:
                        break;
                }

                ActiveMessageType = SdkMessageType.ENTER_PIN;

                UseRegularPIN();
            });
        }

        private async void Enter_CAN(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                if (CarouselPosition != 3)
                {
                    CarouselPosition = 3;
                }
                if (ActiveMessageType == SdkMessageType.ENTER_CAN)
                {
                    BaseIdBasicPopUp canPopUp = new BaseIdBasicPopUp(
                        Lang.PopUp_BaseID_Wrong_CAN_Title,
                        Lang.PopUp_BaseID_Wrong_CAN_Text,
                        Lang.PopUp_BaseID_Wrong_CAN_Button);
                    await canPopUp.ShowPopUp();
                }
                else if (ActiveMessageType == SdkMessageType.ENTER_PIN)
                {
                    EnterCANPopUp canPopUp = new EnterCANPopUp(Lang.PopUp_BaseID_Enter_CAN_Text_1);

                    if (BaseIdProcessType == BaseIdProcessType.TransportPIN)
                    {
                        canPopUp = new EnterCANPopUp(Lang.PopUp_BaseID_Enter_CAN_Text_4);
                    }

                    await canPopUp.ShowPopUp();

                    IdPinLength = 6;
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_CAN_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_CAN_Body_Label;
                    IdPinLinkIsVisible = false;
                    ForgotPINLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                }

                ActiveMessageType = SdkMessageType.ENTER_CAN;
            });
        }

        private async void Enter_PUK(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                if (CarouselPosition != 3)
                {
                    CarouselPosition = 3;
                }
                if (!sdkMessage.Reader.Card.Inoperative)
                {
                    if (ActiveMessageType == SdkMessageType.ENTER_PUK)
                    {
                        BaseIdBasicPopUp pukPopUp = new BaseIdBasicPopUp(
                            Lang.PopUp_BaseID_Wrong_PUK_Title,
                            Lang.PopUp_BaseID_Wrong_PUK_Text,
                            Lang.PopUp_BaseID_Wrong_PUK_Button
                            );
                        await pukPopUp.ShowPopUp();
                    }
                    else if (ActiveMessageType == SdkMessageType.ENTER_PIN)
                    {
                        IdPinLength = 10;
                        IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_PUK_Header_Label;
                        IdPinBodyLabel = Lang.BaseIDPage_PINScreen_PUK_Body_Label;
                        IdPinLinkIsVisible = false;
                        ForgotPINLinkIsVisible = false;
                        MoreInformationLinkIsVisible = true;

                        EnterPUKPopUp pukPopUp = new EnterPUKPopUp();
                        await pukPopUp.ShowPopUp();
                    }

                    ActiveMessageType = SdkMessageType.ENTER_PUK;
                }
                else
                {
                    BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                        Lang.PopUp_BaseID_Inoperative_PUK_Title,
                        Lang.PopUp_BaseID_Inoperative_PUK_Text,
                        Lang.PopUp_BaseID_Inoperative_PUK_Button);
                    await popUp.ShowPopUp();

                    await Navigation.PopAsync();
                }
            });
        }

        private void Enter_New_PIN(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                ActiveMessageType = SdkMessageType.ENTER_NEW_PIN;

                IdPinLength = 6;
                IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_New_Header_Label;
                IdPinBodyLabel = Lang.BaseIDPage_PINScreen_New_Body_Label;
                IdPinLinkIsVisible = false;
                ForgotPINLinkIsVisible = false;
                MoreInformationLinkIsVisible = false;
            });
        }

        private void Change_PIN(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (sdkMessage.Success)
                {
                    BaseIdProcessType = BaseIdProcessType.None;
                    IsActivityIndicatorVisible = false;
                    ProgressBarIsVisible = false;
                    CarouselPosition = 6;
                }
            });
        }

        private async void BaseIdCredentialIssue(ServiceMessageEventService arg1, string arg2)
        {
            IsActivityIndicatorVisible = false;
            GoToNext();
        }

        private void BaseIdCredentialOffer(ServiceMessageEventService arg1, string credentialRecordId)
        {
            IsActivityIndicatorVisible = false;
            GoToNext(credentialRecordId);
        }

        public async void GoToNext(string credentialRecordId = "")
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IAgentContext agentContext = await _agentProvider.GetContextAsync();
                switch (CarouselPosition)
                {
                    case 0:
                        if (Progress == 0)
                        {
                            CarouselPosition = 1;
                            Progress = 1;
                            IsStartEnabled = false;
                        }
                        else
                        {
                            MessagingCenter.Send(this, WalletEvents.ReloadCredentials);
                            MessagingCenter.Send(this, WalletEvents.ReloadHistory);
                            _sdkService.InitHttpClient();

                            try
                            {
                                ConnectionRecord baseIdconnectionRecord = await _connectionService.GetAsync(agentContext, _baseIdConnection);
                                string lockPin = baseIdconnectionRecord.GetTag(WalletParams.KeyRevocationPassphrase);
                                if (!string.IsNullOrEmpty(lockPin))
                                {
                                    LockPINPopUp lockPopUp = new LockPINPopUp(lockPin);
                                    await lockPopUp.ShowPopUp();
                                }
                            }
                            catch (Exception)
                            {
                                // ignore.
                            }


                            CarouselPosition = 5;
                            Progress = 4;
                        }
                        break;
                    case 1:
                        _sdkService.SendRunAuth();
                        BaseIdProcessType = BaseIdProcessType.Authentication;
                        IsActivityIndicatorVisible = true;
                        break;
                    case 2:
                        CarouselPosition = 3;
                        break;
                    case 3:
                        CarouselPosition = 4;
                        IsActivityIndicatorVisible = true;
                        break;
                    case 4:
                        CarouselPosition = 0;
                        Progress = 3;

                        CredentialRecord credentialRecord = await _credentialService.GetAsync(agentContext, credentialRecordId);
                        ConnectionRecord connectionRecord = await _connectionService.GetAsync(agentContext, App.BaseIdConnectionId);
                        BaseIdOfferPopUp offerPopUp = new BaseIdOfferPopUp(new BaseIdOfferMessage(connectionRecord, credentialRecord));
                        PopUpResult popUpResult = await offerPopUp.ShowPopUp();

                        if (popUpResult != PopUpResult.Accepted)
                        {
                            await Navigation.PopAsync();
                        }
                        else
                        {
                            try
                            {
                                (CredentialRequestMessage request, CredentialRecord record) = await _credentialService.CreateRequestAsync(agentContext, credentialRecordId);
                                await _messageService.SendAsync(agentContext, request, connectionRecord);
                                IsActivityIndicatorVisible = true;
                            }
                            catch (Exception)
                            {
                                credentialRecord =
                                await _walletRecordService.GetAsync<CredentialRecord>(agentContext.Wallet,
                                    credentialRecordId, true);
                                credentialRecord.SetTag("AutoError", "true");
                                await _walletRecordService.UpdateAsync(agentContext.Wallet, credentialRecord);

                                BasicPopUp alertPopUp = new BasicPopUp(
                                    Lang.PopUp_Credential_Error_Title,
                                    Lang.PopUp_Credential_Error_Message,
                                    Lang.PopUp_Credential_Error_Button);
                                await alertPopUp.ShowPopUp();
                            }
                        }
                        break;
                    default:
                        await Navigation.PopAsync();
                        break;
                }
            });
        }

        private bool ValidateIdPin(IList<char> arg)
        {
            string enteredDigits = string.Concat(arg.TakeWhile(char.IsNumber));

            if (ActiveMessageType == SdkMessageType.ENTER_NEW_PIN)
            {
                if (string.IsNullOrEmpty(_newPIN))
                {
                    _newPIN = enteredDigits;
                    return true;
                }
                else if (_newPIN == enteredDigits)
                {
                    IsActivityIndicatorVisible = true;
                    _sdkService.SendSetNewPIN(enteredDigits);
                    return true;
                }
                else
                {
                    _newPIN = null;
                    return false;
                }
            }
            else
            {
                if (ActiveMessageType == SdkMessageType.ENTER_CAN)
                {
                    IsActivityIndicatorVisible = true;
                    _sdkService.SendSetCAN(enteredDigits);
                }
                else if (ActiveMessageType == SdkMessageType.ENTER_PUK)
                {
                    IsActivityIndicatorVisible = true;
                    _sdkService.SendSetPUK(enteredDigits);
                }
                else
                {
                    IsActivityIndicatorVisible = true;
                    _sdkService.SendSetPIN(enteredDigits);

                    if (BaseIdProcessType == BaseIdProcessType.Authentication)
                    {
                        GoToNext();
                    }
                }

                return true;
            }
        }

        private async Task IdPinErrorTask()
        {
            if (ActiveMessageType == SdkMessageType.ENTER_NEW_PIN)
            {
                IdPinLength = 6;
                IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_New_Header_Label;
                IdPinBodyLabel = Lang.BaseIDPage_PINScreen_New_Body_Label;
                IdPinLinkIsVisible = false;
                ForgotPINLinkIsVisible = false;
                MoreInformationLinkIsVisible = false;
            }
        }

        private async Task IdPinSuccessTask()
        {
            if (ActiveMessageType == SdkMessageType.ENTER_NEW_PIN)
            {
                IdPinLength = 6;
                IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Confirm_Header_Label;
                IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Confirm_Body_Label;
                IdPinLinkIsVisible = false;
                ForgotPINLinkIsVisible = false;
                MoreInformationLinkIsVisible = false;
            }
        }

        private void ChangeDigitsTapped()
        {
            IsActivityIndicatorVisible = true;
            if (BaseIdProcessType == BaseIdProcessType.Authentication)
            {
                UseTransportPIN();
            }
            else if (BaseIdProcessType == BaseIdProcessType.TransportPIN)
            {
                GoToStart();
            }
            else if (BaseIdProcessType == BaseIdProcessType.None)
            {
                GoToStart();
            }
        }

        private void UseTransportPIN()
        {
            IsActivityIndicatorVisible = true;
            ProgressBarIsVisible = false;
            IdPinBoldIsVisible = false;
            PinPadIsVisible = true;

            switch (ActiveMessageType)
            {
                case SdkMessageType.ENTER_PIN:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Transport_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Transport_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = false;
                    IdPinLength = 5;
                    break;
                case SdkMessageType.ENTER_CAN:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_CAN_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_CAN_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    IdPinLength = 6;
                    break;
                case SdkMessageType.ENTER_PUK:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_PUK_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_PUK_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    IdPinLength = 10;
                    break;
            }

            BaseIdProcessType = BaseIdProcessType.TransportPIN;
            ActiveMessageType = SdkMessageType.UNKNOWN_COMMAND;

            _sdkService.SendCancel();
            _sdkService.InitHttpClient();
        }

        private void UseRegularPIN()
        {
            IdPinBoldIsVisible = false;
            PinPadIsVisible = true;

            switch (ActiveMessageType)
            {
                case SdkMessageType.ENTER_PIN:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Default_Body_Label;
                    ForgotPINLinkIsVisible = true;
                    IdPinLinkIsVisible = true;
                    MoreInformationLinkIsVisible = false;
                    IdPinLength = 6;
                    break;
                case SdkMessageType.ENTER_CAN:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_CAN_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_CAN_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    IdPinLength = 6;
                    break;
                case SdkMessageType.ENTER_PUK:
                    IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_PUK_Header_Label;
                    IdPinBodyLabel = Lang.BaseIDPage_PINScreen_PUK_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    IdPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    IdPinLength = 10;
                    break;
            }
        }

        private async void ForgotPINTapped()
        {
            BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                Lang.PopUp_BaseID_Forgot_My_PIN_Title,
                Lang.PopUp_BaseID_Forgot_My_PIN_Text,
                Lang.PopUp_BaseID_Forgot_My_PIN_Button
                );
            await popUp.ShowPopUp();
        }

        private async void MoreInformationTapped()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (ActiveMessageType == SdkMessageType.ENTER_CAN)
                {
                    CANInfoPopUp canPopUp = new CANInfoPopUp();
                    await canPopUp.ShowPopUp();
                }
                else if (ActiveMessageType == SdkMessageType.ENTER_PUK)
                {
                    BaseIdBasicPopUp pupPopUp = new BaseIdBasicPopUp(
                        Lang.PopUp_BaseID_PUK_Info_Title,
                        Lang.PopUp_BaseID_PUK_Info_Text,
                        Lang.PopUp_BaseID_PUK_Info_Button
                        );
                    await pupPopUp.ShowPopUp();
                }
            });
        }

        public void CancelCurrentProcess()
        {
            _sdkService.SendCancel();
            _sdkService.InitHttpClient();
            BaseIdProcessType = BaseIdProcessType.None;
        }

        public void GoToStart()
        {
            App.PopUpIsOpen = false;
            ActiveMessageType = SdkMessageType.UNKNOWN_COMMAND;
            BaseIdProcessType = BaseIdProcessType.None;
            _sdkService.SendCancel();
            _sdkService.InitHttpClient();
            _baseIdConnection = "";
            _hasAcceptedAccess = false;
            IsActivityIndicatorVisible = false;
            CarouselPosition = 0;
            Progress = 0;
            ProgressBarIsVisible = true;
            IdPinLength = 6;
            IsStartEnabled = true;
            IdPinBoldIsVisible = true;
            PinPadIsVisible = false;
            IdPinLinkIsVisible = false;
            ForgotPINLinkIsVisible = false;
            MoreInformationLinkIsVisible = false;

            IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
            IdPinBoldLabel = Lang.BaseIDPage_PINScreen_Selection_Bold_Text;
            IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Selection_Body_Text;

            ViewModelWasResetted = true;
        }

        private async Task HandleAuthenticationEnterPin(SdkMessage sdkMessage)
        {
            switch (sdkMessage.Reader.Card.RetryCounter)
            {
                case 3:
                    if (ActiveMessageType == SdkMessageType.ENTER_PUK)
                    {
                        BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                            Lang.PopUp_BaseID_PUK_Success_Title,
                            Lang.PopUp_BaseID_PUK_Success_Text,
                            Lang.PopUp_BaseID_PUK_Success_Button);
                        await popUp.ShowPopUp();

                        _sdkService.SendCancel();
                        _sdkService.InitHttpClient();
                        await Navigation.PopAsync();
                    }
                    break;
                case 2:
                    if (ActiveMessageType == SdkMessageType.ENTER_PIN)
                    {
                        WrongPINPopUp wrongPinPopUp2 = new WrongPINPopUp(sdkMessage.Reader.Card.RetryCounter, Lang.PopUp_BaseID_Wrong_PIN_Pre_Text);
                        await wrongPinPopUp2.ShowPopUp();
                    }
                    break;
                case 1:
                    if (ActiveMessageType == SdkMessageType.ENTER_CAN)
                    {
                        ProgressBarIsVisible = true;
                        IdPinLinkIsVisible = true;
                        ForgotPINLinkIsVisible = true;
                        MoreInformationLinkIsVisible = false;
                        IdPinLength = 6;
                        IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
                        IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Default_Body_Label;
                    }
                    break;
                default:
                    break;
            }
        }

        private async Task HandleChangePinEnterPin(SdkMessage sdkMessage)
        {
            switch (sdkMessage.Reader.Card.RetryCounter)
            {
                case 3:
                    if (ActiveMessageType == SdkMessageType.ENTER_PUK)
                    {
                        BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                            Lang.PopUp_BaseID_PUK_Success_Title,
                            Lang.PopUp_BaseID_PUK_Success_Text,
                            Lang.PopUp_BaseID_PUK_Success_Button);
                        await popUp.ShowPopUp();

                        _sdkService.SendCancel();
                        _sdkService.InitHttpClient();
                        await Navigation.PopAsync();
                    }
                    break;
                case 2:
                    if (ActiveMessageType == SdkMessageType.ENTER_PIN)
                    {
                        WrongPINPopUp wrongPinPopUp2 = new WrongPINPopUp(sdkMessage.Reader.Card.RetryCounter, Lang.PopUp_BaseID_Wrong_PIN_Pre_Text);
                        await wrongPinPopUp2.ShowPopUp();
                    }
                    break;
                case 1:
                    if (ActiveMessageType == SdkMessageType.ENTER_CAN)
                    {
                        ProgressBarIsVisible = false;
                        IdPinLinkIsVisible = false;
                        ForgotPINLinkIsVisible = false;
                        MoreInformationLinkIsVisible = false;
                        IdPinLength = 6;
                        IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_ChangePIN_Header_Label;
                        IdPinBodyLabel = Lang.BaseIDPage_PINScreen_ChangePIN_Body_Label;
                    }
                    break;
                default:
                    break;
            }
        }

        private async Task HandleTransportPinEnterPin(SdkMessage sdkMessage)
        {
            switch (sdkMessage.Reader.Card.RetryCounter)
            {
                case 3:
                    if (ActiveMessageType == SdkMessageType.ENTER_PUK)
                    {
                        BaseIdBasicPopUp popUp = new BaseIdBasicPopUp(
                            Lang.PopUp_BaseID_PUK_Success_Title,
                            Lang.PopUp_BaseID_PUK_Success_Text,
                            Lang.PopUp_BaseID_PUK_Success_Button);
                        await popUp.ShowPopUp();

                        _sdkService.SendCancel();
                        _sdkService.InitHttpClient();
                        await Navigation.PopAsync();
                    }
                    break;
                case 2:
                    if (ActiveMessageType == SdkMessageType.ENTER_PIN)
                    {
                        WrongPINPopUp wrongPinPopUp2 = new WrongPINPopUp(sdkMessage.Reader.Card.RetryCounter, Lang.PopUp_BaseID_Wrong_PIN_Pre_Text_2);
                        await wrongPinPopUp2.ShowPopUp();
                    }
                    break;
                case 1:
                    if (ActiveMessageType == SdkMessageType.ENTER_CAN)
                    {
                        ProgressBarIsVisible = false;
                        IdPinLinkIsVisible = false;
                        ForgotPINLinkIsVisible = false;
                        MoreInformationLinkIsVisible = false;
                        IdPinLength = 5;
                        IdPinHeaderLabel = Lang.BaseIDPage_PINScreen_Transport_Header_Label;
                        IdPinBodyLabel = Lang.BaseIDPage_PINScreen_Transport_Body_Label;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
