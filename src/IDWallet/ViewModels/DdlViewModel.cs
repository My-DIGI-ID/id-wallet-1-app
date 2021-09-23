using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Events;
using IDWallet.Models;
using IDWallet.Models.AusweisSDK;
using IDWallet.Resources;
using IDWallet.Services;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.DDL.PopUps;
using IDWallet.Views.Proof.PopUps;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Storage;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class DdlViewModel : CustomViewModel
    {
        private readonly SDKMessageService _sdkService = App.Container.Resolve<SDKMessageService>();
        private readonly ConnectService _connectService = App.Container.Resolve<ConnectService>();
        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();
        private readonly ICredentialService _credentialService = App.Container.Resolve<ICredentialService>();
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly IMessageService _messageService = App.Container.Resolve<IMessageService>();
        private readonly ICustomWalletRecordService _walletRecordService = App.Container.Resolve<ICustomWalletRecordService>();

        private bool _alreadySubscribed;
        private bool _isActivityIndicatorVisible;
        private bool _isNotMoveTextVisible;
        private bool _progressBarIsVisible;
        private int _carouselPosition;
        private int _progress;
        private SdkMessageType _activeMessageType;
        private DdlProcessType _ddlProcessType;
        private bool _isStartEnabled;
        private bool _isInfoVisible;
        private int _ddlPinLength;
        private string _ddlPinHeaderLabel;
        private string _ddlPinBoldLabel;
        private bool _ddlPinBoldIsVisible;
        private string _ddlPinBodyLabel;
        private bool _ddlPinLinkIsVisible;
        private bool _moreInformationLinkIsVisible;
        private bool _forgotPINLinkIsVisible;
        private bool _pinPadIsVisible;
        private int _scanProcessCounter;
        private string _ddlConnection;
        private bool _hasAcceptedAccess;
        private string _newPIN;
        private readonly ReadyToScanPopUp _scanPopUp;

        public Func<IList<char>, bool> DdlPinValidator { get; }
        public INavigation Navigation { get; set; }
        public bool ViewModelWasResetted { get; set; }
        public bool IsActivityIndicatorVisible
        {
            get => _isActivityIndicatorVisible;
            set => SetProperty(ref _isActivityIndicatorVisible, value);
        }

        public bool IsNotMoveTextVisible
        {
            get => _isNotMoveTextVisible;
            set => SetProperty(ref _isNotMoveTextVisible, value);
        }

        public bool ProgressBarIsVisible
        {
            get => _progressBarIsVisible;
            set => SetProperty(ref _progressBarIsVisible, value);
        }
        public int CarouselPosition
        {
            get => _carouselPosition;
            set => SetProperty(ref _carouselPosition, value);
        }
        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }
        public bool IsStartEnabled
        {
            get => _isStartEnabled;
            set => SetProperty(ref _isStartEnabled, value);
        }
        public bool IsInfoVisible
        {
            get => _isInfoVisible;
            set => SetProperty(ref _isInfoVisible, value);
        }
        public int DdlPinLength
        {
            get => _ddlPinLength;
            set => SetProperty(ref _ddlPinLength, value);
        }
        public string DdlPinHeaderLabel
        {
            get => _ddlPinHeaderLabel;
            set => SetProperty(ref _ddlPinHeaderLabel, value);
        }
        public string DdlPinBoldLabel
        {
            get => _ddlPinBoldLabel;
            set => SetProperty(ref _ddlPinBoldLabel, value);
        }
        public bool DdlPinBoldIsVisible
        {
            get => _ddlPinBoldIsVisible;
            set => SetProperty(ref _ddlPinBoldIsVisible, value);
        }
        public string DdlPinBodyLabel
        {
            get => _ddlPinBodyLabel;
            set => SetProperty(ref _ddlPinBodyLabel, value);
        }
        public bool DdlPinLinkIsVisible
        {
            get => _ddlPinLinkIsVisible;
            set => SetProperty(ref _ddlPinLinkIsVisible, value);
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
        public bool PinPadIsVisible
        {
            get => _pinPadIsVisible;
            set => SetProperty(ref _pinPadIsVisible, value);
        }
        public int ScanProcessCounter
        {
            get => _scanProcessCounter;
            set => SetProperty(ref _scanProcessCounter, value);
        }

        private Command _ddlPinErrorCommand;
        public Command DdlPinErrorCommand =>
            _ddlPinErrorCommand ??= new Command(DdlPinErrorTask);

        private Command _ddlPinSuccessCommand;
        public Command DdlPinSuccessCommand =>
            _ddlPinSuccessCommand ??= new Command(DdlPinSuccessTask);

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
            _forgotPINTappedCommand ??= new Command(async () => { await ForgotPINTapped(); });

        private Command _moreInformationTappedCommand;
        public Command MoreInformationTappedCommand =>
            _moreInformationTappedCommand ??= new Command(MoreInformationTapped);

        public DdlViewModel()
        {
            _activeMessageType = SdkMessageType.UNKNOWN_COMMAND;
            _ddlProcessType = DdlProcessType.None;
            ViewModelWasResetted = false;
            IsActivityIndicatorVisible = false;
            IsNotMoveTextVisible = true;
            ProgressBarIsVisible = true;
            CarouselPosition = 0;
            Progress = 0;

            Views.CustomTabbedPage mainPage = Application.Current.MainPage as Views.CustomTabbedPage;
            mainPage.CurrentPage = mainPage.Children.First();
            Navigation = ((NavigationPage)mainPage.CurrentPage).Navigation;

            DdlPinLength = 6;

            _scanPopUp = new ReadyToScanPopUp(this);
            ScanProcessCounter = 0;

            IsStartEnabled = true;
            DdlPinBoldIsVisible = true;
            PinPadIsVisible = false;
            DdlPinLinkIsVisible = false;
            ForgotPINLinkIsVisible = false;
            MoreInformationLinkIsVisible = false;

            if (WalletParams.AusweisHost.Equals("api.demo.digitaler-fuehrerschein.bundesdruckerei.de/ssi"))
            {
                IsInfoVisible = true;
            }
            else
            {
                IsInfoVisible = false;
            }

            DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
            DdlPinBoldLabel = Lang.BaseIDPage_PINScreen_Selection_Bold_Text;
            DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_Selection_Body_Text;

            DdlPinValidator = arg => { return ValidateDdlPin(arg); };

            _alreadySubscribed = false;
        }

        public void Subscribe()
        {
            if (!_alreadySubscribed)
            {
                _alreadySubscribed = true;
                _sdkService.StartDdlFlow();
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, DDLEvents.AccessRights, Access_Rights);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, DDLEvents.EnterPIN, Enter_PIN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, DDLEvents.EnterNewPIN, Enter_New_PIN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, DDLEvents.EnterCAN, Enter_CAN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, DDLEvents.EnterPUK, Enter_PUK);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, DDLEvents.Auth, Auth);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, DDLEvents.ChangePIN, Change_PIN);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, DDLEvents.InsertCard, Insert_Card);
                MessagingCenter.Subscribe<SDKMessageService, SdkMessage>(this, DDLEvents.Reader, Reader);
                MessagingCenter.Subscribe<ServiceMessageEventService, string>(this, WalletEvents.DdlCredentialOffer, DdlCredentialOffer);
                MessagingCenter.Subscribe<ServiceMessageEventService, string>(this, WalletEvents.DdlCredentialIssue, DdlCredentialIssue);
            }
        }

        public void Unsubscribe()
        {
            _alreadySubscribed = false;
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, DDLEvents.AccessRights);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, DDLEvents.EnterPIN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, DDLEvents.EnterNewPIN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, DDLEvents.EnterCAN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, DDLEvents.EnterPUK);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, DDLEvents.Auth);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, DDLEvents.ChangePIN);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, DDLEvents.InsertCard);
            MessagingCenter.Unsubscribe<SDKMessageService, SdkMessage>(this, DDLEvents.Reader);
            MessagingCenter.Unsubscribe<ServiceMessageEventService, string>(this, WalletEvents.DdlCredentialOffer);
            MessagingCenter.Unsubscribe<ServiceMessageEventService, string>(this, WalletEvents.DdlCredentialIssue);
        }

        private void Auth(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (sdkMessage.Result != null && sdkMessage.Result.Description != "The process has been cancelled.")
                {
                    string redirectUrl = sdkMessage.Url;
                    HttpResponseMessage result = await _sdkService.SdkHttpClient.GetAsync(redirectUrl);

                    if (result.IsSuccessStatusCode)
                    {
                        IsNotMoveTextVisible = false;

                        try
                        {
                            string resultString = await result.Content.ReadAsStringAsync();
                            SdkInvitation ausweisSdkInvitation = JObject.Parse(resultString).ToObject<SdkInvitation>();

                            Agent.Models.CustomConnectionInvitationMessage connectionInvitationMessage = _connectService.ReadInvitationUrl(ausweisSdkInvitation.InvitationUrl);

                            ConnectionRecord ddlConnection = await _connectService.AcceptInvitationAsync(connectionInvitationMessage);
                            App.DdlConnectionId = _ddlConnection = ddlConnection.Id;

                            MessagingCenter.Send(this, WalletEvents.ReloadConnections);
                        }
                        catch (Exception)
                        {
                            _sdkService.SendCancel();
                            _sdkService.StartDdlFlow();

                            if (sdkMessage.Result != null && !string.IsNullOrEmpty(sdkMessage.Result.Message) && sdkMessage.Result.Message.Equals("The authenticity of your ID card could not be verified. Please make sure that you are using a genuine ID card. Please note that test applications require the use of a test ID card."))
                            {
                                DdlBasicPopUp popUp = new DdlBasicPopUp(
                                        Lang.PopUp_DDL_Auth_Error_Title,
                                        Lang.PopUp_DDL_Auth_Error_Card_Text,
                                        Lang.PopUp_DDL_Auth_Error_Button
                                        );
                                await popUp.ShowPopUp();
                                try
                                {
                                    await Navigation.PopAsync();
                                }
                                catch (Exception)
                                { }
                            }
                            else
                            {
                                DdlBasicPopUp popUp = new DdlBasicPopUp(
                                        Lang.PopUp_DDL_Auth_Error_Title,
                                        Lang.PopUp_DDL_Auth_Message_Error_Text,
                                        Lang.PopUp_DDL_Auth_Error_Button
                                        );
                                await popUp.ShowPopUp();
                                try
                                {
                                    await Navigation.PopAsync();
                                }
                                catch (Exception)
                                { }
                            }
                        }
                    }
                    else if (result.StatusCode.Equals(HttpStatusCode.UnprocessableEntity))
                    {
                        try
                        {
                            string resultString = await result.Content.ReadAsStringAsync();
                            SdkHttpError error = JObject.Parse(resultString).ToObject<SdkHttpError>();
                            await DisplayKBAError(error.Code);
                        }
                        catch (Exception)
                        { }
                        finally
                        {
                            await Navigation.PopAsync();
                        }
                    }
                    else
                    {
                        _sdkService.SendCancel();
                        _sdkService.StartDdlFlow();
                        if (sdkMessage.Result != null && !string.IsNullOrEmpty(sdkMessage.Result.Message) && sdkMessage.Result.Message.Equals("The authenticity of your ID card could not be verified. Please make sure that you are using a genuine ID card. Please note that test applications require the use of a test ID card."))
                        {
                            DdlBasicPopUp popUp = new DdlBasicPopUp(
                                    Lang.PopUp_DDL_Auth_Error_Title,
                                    Lang.PopUp_DDL_Auth_Error_Card_Text,
                                    Lang.PopUp_DDL_Auth_Error_Button
                                    );
                            await popUp.ShowPopUp();
                            try
                            {
                                await Navigation.PopAsync();
                            }
                            catch (Exception)
                            { }
                        }
                        else
                        {
                            DdlBasicPopUp popUp = new DdlBasicPopUp(
                                    Lang.PopUp_DDL_Auth_Error_Title,
                                    Lang.PopUp_DDL_Auth_Message_Error_Text,
                                    Lang.PopUp_DDL_Auth_Error_Button
                                    );
                            await popUp.ShowPopUp();
                            try
                            {
                                await Navigation.PopAsync();
                            }
                            catch (Exception)
                            { }
                        }
                    }
                }
                else if (_ddlProcessType == DdlProcessType.TransportPIN)
                {
                    _sdkService.SendRunChangePIN();
                }
            });
        }

        private void Access_Rights(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                _activeMessageType = SdkMessageType.ACCESS_RIGHTS;
                if (!_hasAcceptedAccess)
                {
                    DdlAccessRightsPopUp accessRightsPopUp = new DdlAccessRightsPopUp(sdkMessage.Chat.Effective);
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
                        _sdkService.StartDdlFlow();
                        _ddlProcessType = DdlProcessType.None;
                        await Navigation.PopAsync();
                    }
                }
                else
                {
                    _sdkService.SendAccept();
                }
            });
        }

        private void Insert_Card(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            if (ScanProcessCounter < 2)
            {
                ScanProcessCounter += 1;
            }

            Device.BeginInvokeOnMainThread(async () =>
            {
                if (_activeMessageType == SdkMessageType.ACCESS_RIGHTS)
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

        private void Enter_PIN(SDKMessageService arg1, SdkMessage sdkMessage)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                if (CarouselPosition != 3)
                {
                    CarouselPosition = 3;
                }

                switch (_ddlProcessType)
                {
                    case DdlProcessType.Authentication:
                        await HandleAuthenticationEnterPin(sdkMessage);
                        _activeMessageType = SdkMessageType.ENTER_PIN;
                        UseRegularPIN();
                        break;
                    case DdlProcessType.ChangePIN:
                        _activeMessageType = SdkMessageType.ENTER_PIN;
                        await HandleChangePinEnterPin(sdkMessage);
                        break;
                    case DdlProcessType.TransportPIN:
                        if (_activeMessageType == SdkMessageType.ENTER_CAN)
                        {
                            _activeMessageType = SdkMessageType.ENTER_PIN;
                            UseTransportPinNoCancel();
                        }
                        else
                        {
                            _activeMessageType = SdkMessageType.ENTER_PIN;
                        }
                        await HandleTransportPinEnterPin(sdkMessage);
                        break;
                    default:
                        break;
                }
            });
        }

        private void Enter_CAN(SDKMessageService arg1, SdkMessage arg2)
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IsActivityIndicatorVisible = false;
                if (CarouselPosition != 3)
                {
                    CarouselPosition = 3;
                }
                if (_activeMessageType == SdkMessageType.ENTER_CAN)
                {
                    DdlBasicPopUp canPopUp = new DdlBasicPopUp(
                        Lang.PopUp_BaseID_Wrong_CAN_Title,
                        Lang.PopUp_BaseID_Wrong_CAN_Text,
                        Lang.PopUp_BaseID_Wrong_CAN_Button);
                    await canPopUp.ShowPopUp();
                }
                else
                {
                    UseRegularPIN();
                    EnterCANPopUp canPopUp = new EnterCANPopUp(Lang.PopUp_BaseID_Enter_CAN_Text_1);

                    if (_ddlProcessType == DdlProcessType.TransportPIN)
                    {
                        canPopUp = new EnterCANPopUp(Lang.PopUp_BaseID_Enter_CAN_Text_4);
                    }

                    await canPopUp.ShowPopUp();

                    DdlPinLength = 6;
                    DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_CAN_Header_Label;
                    DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_CAN_Body_Label;
                    DdlPinLinkIsVisible = false;
                    ForgotPINLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                }
                _activeMessageType = SdkMessageType.ENTER_CAN;
            });
        }

        private void Enter_PUK(SDKMessageService arg1, SdkMessage sdkMessage)
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
                    if (_activeMessageType == SdkMessageType.ENTER_PUK)
                    {
                        DdlBasicPopUp pukPopUp = new DdlBasicPopUp(
                            Lang.PopUp_BaseID_Wrong_PUK_Title,
                            Lang.PopUp_BaseID_Wrong_PUK_Text,
                            Lang.PopUp_BaseID_Wrong_PUK_Button
                            );
                        await pukPopUp.ShowPopUp();
                    }
                    else if (_activeMessageType == SdkMessageType.ENTER_PIN)
                    {
                        DdlPinLength = 10;
                        DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_PUK_Header_Label;
                        DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_PUK_Body_Label;
                        DdlPinLinkIsVisible = false;
                        ForgotPINLinkIsVisible = false;
                        MoreInformationLinkIsVisible = true;

                        EnterPUKPopUp pukPopUp = new EnterPUKPopUp();
                        await pukPopUp.ShowPopUp();
                    }

                    _activeMessageType = SdkMessageType.ENTER_PUK;
                }
                else
                {
                    DdlBasicPopUp popUp = new DdlBasicPopUp(
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
                _activeMessageType = SdkMessageType.ENTER_NEW_PIN;

                DdlPinLength = 6;
                DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_New_Header_Label;
                DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_New_Body_Label;
                DdlPinLinkIsVisible = false;
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
                    _ddlProcessType = DdlProcessType.None;
                    IsActivityIndicatorVisible = false;
                    ProgressBarIsVisible = false;
                    CarouselPosition = 6;
                }
            });
        }

        private void DdlCredentialIssue(ServiceMessageEventService arg1, string sdkMessage)
        {
            IsActivityIndicatorVisible = false;
            GoToNext();
        }

        private void DdlCredentialOffer(ServiceMessageEventService arg1, string credentialRecordId)
        {
            IsActivityIndicatorVisible = false;
            GoToNext(credentialRecordId);
        }

        public void GoToNext(string credentialRecordId = "")
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                IAgentContext agentContext = await _agentProvider.GetContextAsync();
                switch (CarouselPosition)
                {
                    case 0:
                        if (Progress == 0)
                        {
                            await DeleteOldDdlCredentials(agentContext);
                            CarouselPosition = 1;
                            Progress = 1;
                            IsStartEnabled = false;
                        }
                        break;
                    case 1:
                        IsInfoVisible = false;
                        if (await ShowPinPrompt())
                        {
                            IsActivityIndicatorVisible = true;

                            await _sdkService.SendRunAuth();
                            _ddlProcessType = DdlProcessType.Authentication;
                        }
                        break;
                    case 2:
                        CarouselPosition = 3;
                        break;
                    case 3:
                        CarouselPosition = 4;
                        IsActivityIndicatorVisible = true;
                        break;
                    case 4:
                        if (Progress != 3)
                        {
                            Progress = 3;

                            CredentialRecord credentialRecord = await _credentialService.GetAsync(agentContext, credentialRecordId);
                            ConnectionRecord connectionRecord = await _connectionService.GetAsync(agentContext, App.DdlConnectionId);
                            DdlOfferPopUp offerPopUp = new DdlOfferPopUp(new DdlOfferMessage(connectionRecord, credentialRecord));
                            PopUpResult popUpResult = await offerPopUp.ShowPopUp();
                            IsActivityIndicatorVisible = true;

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
                                }
                                catch (Exception)
                                {
                                    credentialRecord =
                                    await _walletRecordService.GetAsync<CredentialRecord>(agentContext.Wallet,
                                        credentialRecordId, true);
                                    credentialRecord.SetTag("AutoError", "true");
                                    await _walletRecordService.UpdateAsync(agentContext.Wallet, credentialRecord);

                                    IsActivityIndicatorVisible = false;
                                    BasicPopUp alertPopUp = new BasicPopUp(
                                        Lang.PopUp_Credential_Error_Title,
                                        Lang.PopUp_Credential_Error_Message,
                                        Lang.PopUp_Credential_Error_Button);
                                    await alertPopUp.ShowPopUp();
                                    await Navigation.PopAsync();
                                }
                            }

                            MessagingCenter.Send(this, WalletEvents.ReloadCredentials);
                            MessagingCenter.Send(this, WalletEvents.ReloadHistory);
                        }
                        else
                        {
                            _sdkService.StartDdlFlow();

                            try
                            {
                                ConnectionRecord ddlConnectionRecord = await _connectionService.GetAsync(agentContext, _ddlConnection);
                                string lockPin = ddlConnectionRecord.GetTag(WalletParams.KeyRevocationPassphrase);
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
                    default:
                        await Navigation.PopAsync();
                        MessagingCenter.Send(this, WalletEvents.ReloadCredentials);
                        MessagingCenter.Send(this, WalletEvents.ReloadHistory);
                        break;
                }
            });
        }

        private async Task<bool> ShowPinPrompt()
        {
            ProofRequest proofRequest = new ProofRequest();
            ProofViewModel viewModel = new ProofViewModel(proofRequest, "");

            ProofAuthenticationPopUp authPopUp = new ProofAuthenticationPopUp(new AuthViewModel(viewModel))
            {
                AlwaysDisplay = true
            };
#pragma warning disable CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
            authPopUp.ShowPopUp(); // No await.
#pragma warning restore CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.

            while (!viewModel.AuthSuccess)
            {
                if (viewModel.AuthError)
                {
                    return false;
                }
                await Task.Delay(100);
            }
            authPopUp.OnAuthCanceled(authPopUp, null);

            if (!viewModel.AuthError && viewModel.AuthSuccess)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        private bool ValidateDdlPin(IList<char> arg)
        {
            string enteredDigits = string.Concat(arg.TakeWhile(char.IsNumber));

            if (_activeMessageType == SdkMessageType.ENTER_NEW_PIN)
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
                if (_activeMessageType == SdkMessageType.ENTER_CAN)
                {
                    IsActivityIndicatorVisible = true;
                    _sdkService.SendSetCAN(enteredDigits);
                }
                else if (_activeMessageType == SdkMessageType.ENTER_PUK)
                {
                    IsActivityIndicatorVisible = true;
                    _sdkService.SendSetPUK(enteredDigits);
                }
                else
                {
                    IsActivityIndicatorVisible = true;
                    _sdkService.SendSetPIN(enteredDigits);

                    if (_ddlProcessType == DdlProcessType.Authentication)
                    {
                        GoToNext();
                    }
                }

                return true;
            }
        }

        private void DdlPinErrorTask()
        {
            if (_activeMessageType == SdkMessageType.ENTER_NEW_PIN)
            {
                DdlPinLength = 6;
                DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_New_Header_Label;
                DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_New_Body_Label;
                DdlPinLinkIsVisible = false;
                ForgotPINLinkIsVisible = false;
                MoreInformationLinkIsVisible = false;
            }
        }

        private void DdlPinSuccessTask()
        {
            if (_activeMessageType == SdkMessageType.ENTER_NEW_PIN)
            {
                DdlPinLength = 6;
                DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_Confirm_Header_Label;
                DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_Confirm_Body_Label;
                DdlPinLinkIsVisible = false;
                ForgotPINLinkIsVisible = false;
                MoreInformationLinkIsVisible = false;
            }
        }

        private void ChangeDigitsTapped()
        {
            IsActivityIndicatorVisible = true;
            if (_ddlProcessType == DdlProcessType.Authentication)
            {
                UseTransportPIN();
            }
            else if (_ddlProcessType == DdlProcessType.TransportPIN)
            {
                GoToStart();
            }
            else if (_ddlProcessType == DdlProcessType.None)
            {
                GoToStart();
            }
        }

        private void UseTransportPIN()
        {
            IsActivityIndicatorVisible = true;
            ProgressBarIsVisible = false;
            DdlPinBoldIsVisible = false;
            PinPadIsVisible = true;

            switch (_activeMessageType)
            {
                case SdkMessageType.ENTER_PIN:
                    DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_Transport_Header_Label;
                    DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_Transport_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    DdlPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = false;
                    DdlPinLength = 5;
                    break;
                case SdkMessageType.ENTER_CAN:
                    DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_CAN_Header_Label;
                    DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_CAN_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    DdlPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    DdlPinLength = 6;
                    break;
                case SdkMessageType.ENTER_PUK:
                    DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_PUK_Header_Label;
                    DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_PUK_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    DdlPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    DdlPinLength = 10;
                    break;
            }

            _ddlProcessType = DdlProcessType.TransportPIN;
            _activeMessageType = SdkMessageType.UNKNOWN_COMMAND;

            _sdkService.SendCancel();
            _sdkService.StartDdlFlow();
        }

        private void UseTransportPinNoCancel()
        {
            ProgressBarIsVisible = false;
            DdlPinBoldIsVisible = false;
            PinPadIsVisible = true;

            switch (_activeMessageType)
            {
                case SdkMessageType.ENTER_PIN:
                    DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_Transport_Header_Label;
                    DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_Transport_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    DdlPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = false;
                    DdlPinLength = 5;
                    break;
                case SdkMessageType.ENTER_CAN:
                    DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_CAN_Header_Label;
                    DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_CAN_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    DdlPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    DdlPinLength = 6;
                    break;
                case SdkMessageType.ENTER_PUK:
                    DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_PUK_Header_Label;
                    DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_PUK_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    DdlPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    DdlPinLength = 10;
                    break;
            }

            _ddlProcessType = DdlProcessType.TransportPIN;
            _activeMessageType = SdkMessageType.UNKNOWN_COMMAND;
        }

        private void UseRegularPIN()
        {
            DdlPinBoldIsVisible = false;
            PinPadIsVisible = true;

            switch (_activeMessageType)
            {
                case SdkMessageType.ENTER_PIN:
                    DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
                    DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_Default_Body_Label;
                    ForgotPINLinkIsVisible = true;
                    DdlPinLinkIsVisible = true;
                    MoreInformationLinkIsVisible = false;
                    DdlPinLength = 6;
                    break;
                case SdkMessageType.ENTER_CAN:
                    DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_CAN_Header_Label;
                    DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_CAN_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    DdlPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    DdlPinLength = 6;
                    break;
                case SdkMessageType.ENTER_PUK:
                    DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_PUK_Header_Label;
                    DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_PUK_Body_Label;
                    ForgotPINLinkIsVisible = false;
                    DdlPinLinkIsVisible = false;
                    MoreInformationLinkIsVisible = true;
                    DdlPinLength = 10;
                    break;
            }
        }

        private async Task ForgotPINTapped()
        {
            DdlBasicPopUp popUp = new DdlBasicPopUp(
                Lang.PopUp_BaseID_Forgot_My_PIN_Title,
                Lang.PopUp_BaseID_Forgot_My_PIN_Text,
                Lang.PopUp_BaseID_Forgot_My_PIN_Button
                );
            await popUp.ShowPopUp();
        }

        private void MoreInformationTapped()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (_activeMessageType == SdkMessageType.ENTER_CAN)
                {
                    CANInfoPopUp canPopUp = new CANInfoPopUp();
                    await canPopUp.ShowPopUp();
                }
                else if (_activeMessageType == SdkMessageType.ENTER_PUK)
                {
                    DdlBasicPopUp pupPopUp = new DdlBasicPopUp(
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
            _sdkService.StartDdlFlow();
            _ddlProcessType = DdlProcessType.None;
        }

        public void GoToStart()
        {
            if (WalletParams.AusweisHost.Equals("api.demo.digitaler-fuehrerschein.bundesdruckerei.de/ssi"))
            {
                IsInfoVisible = true;
            }
            else
            {
                IsInfoVisible = false;
            }

            App.PopUpIsOpen = false;
            _activeMessageType = SdkMessageType.UNKNOWN_COMMAND;
            _ddlProcessType = DdlProcessType.None;
            _sdkService.SendCancel();
            _sdkService.StartDdlFlow();
            _ddlConnection = "";
            _hasAcceptedAccess = false;
            IsActivityIndicatorVisible = false;
            IsNotMoveTextVisible = true;
            CarouselPosition = 0;
            Progress = 0;
            ProgressBarIsVisible = true;
            DdlPinLength = 6;
            IsStartEnabled = true;
            DdlPinBoldIsVisible = true;
            PinPadIsVisible = false;
            DdlPinLinkIsVisible = false;
            ForgotPINLinkIsVisible = false;
            MoreInformationLinkIsVisible = false;
            ScanProcessCounter = 0;

            DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
            DdlPinBoldLabel = Lang.BaseIDPage_PINScreen_Selection_Bold_Text;
            DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_Selection_Body_Text;

            ViewModelWasResetted = true;
        }

        private async Task HandleAuthenticationEnterPin(SdkMessage sdkMessage)
        {
            switch (sdkMessage.Reader.Card.RetryCounter)
            {
                case 3:
                    if (_activeMessageType == SdkMessageType.ENTER_PUK)
                    {
                        DdlBasicPopUp popUp = new DdlBasicPopUp(
                            Lang.PopUp_BaseID_PUK_Success_Title,
                            Lang.PopUp_BaseID_PUK_Success_Text,
                            Lang.PopUp_BaseID_PUK_Success_Button);
                        await popUp.ShowPopUp();

                        _sdkService.SendCancel();
                        _sdkService.StartDdlFlow();
                        await Navigation.PopAsync();
                    }
                    break;
                case 2:
                    if (_activeMessageType == SdkMessageType.ENTER_PIN)
                    {
                        WrongPINPopUp wrongPinPopUp2 = new WrongPINPopUp(sdkMessage.Reader.Card.RetryCounter, Lang.PopUp_BaseID_Wrong_PIN_Pre_Text);
                        await wrongPinPopUp2.ShowPopUp();
                    }
                    break;
                case 1:
                    if (_activeMessageType == SdkMessageType.ENTER_CAN)
                    {
                        ProgressBarIsVisible = true;
                        DdlPinLinkIsVisible = true;
                        ForgotPINLinkIsVisible = true;
                        MoreInformationLinkIsVisible = false;
                        DdlPinLength = 6;
                        DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_Default_Header_Label;
                        DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_Default_Body_Label;
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
                    if (_activeMessageType == SdkMessageType.ENTER_PUK)
                    {
                        DdlBasicPopUp popUp = new DdlBasicPopUp(
                            Lang.PopUp_BaseID_PUK_Success_Title,
                            Lang.PopUp_BaseID_PUK_Success_Text,
                            Lang.PopUp_BaseID_PUK_Success_Button);
                        await popUp.ShowPopUp();

                        _sdkService.SendCancel();
                        _sdkService.StartDdlFlow();
                        await Navigation.PopAsync();
                    }
                    break;
                case 2:
                    if (_activeMessageType == SdkMessageType.ENTER_PIN)
                    {
                        WrongPINPopUp wrongPinPopUp2 = new WrongPINPopUp(sdkMessage.Reader.Card.RetryCounter, Lang.PopUp_BaseID_Wrong_PIN_Pre_Text);
                        await wrongPinPopUp2.ShowPopUp();
                    }
                    break;
                case 1:
                    if (_activeMessageType == SdkMessageType.ENTER_CAN)
                    {
                        ProgressBarIsVisible = false;
                        DdlPinLinkIsVisible = false;
                        ForgotPINLinkIsVisible = false;
                        MoreInformationLinkIsVisible = false;
                        DdlPinLength = 6;
                        DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_ChangePIN_Header_Label;
                        DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_ChangePIN_Body_Label;
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
                    if (_activeMessageType == SdkMessageType.ENTER_PUK)
                    {
                        DdlBasicPopUp popUp = new DdlBasicPopUp(
                            Lang.PopUp_BaseID_PUK_Success_Title,
                            Lang.PopUp_BaseID_PUK_Success_Text,
                            Lang.PopUp_BaseID_PUK_Success_Button);
                        await popUp.ShowPopUp();

                        _sdkService.SendCancel();
                        _sdkService.StartDdlFlow();
                        await Navigation.PopAsync();
                    }
                    break;
                case 2:
                    if (_activeMessageType == SdkMessageType.ENTER_PIN)
                    {
                        WrongPINPopUp wrongPinPopUp2 = new WrongPINPopUp(sdkMessage.Reader.Card.RetryCounter, Lang.PopUp_BaseID_Wrong_PIN_Pre_Text_2);
                        await wrongPinPopUp2.ShowPopUp();
                    }
                    break;
                case 1:
                    if (_activeMessageType == SdkMessageType.ENTER_CAN)
                    {
                        ProgressBarIsVisible = false;
                        DdlPinLinkIsVisible = false;
                        ForgotPINLinkIsVisible = false;
                        MoreInformationLinkIsVisible = false;
                        DdlPinLength = 5;
                        DdlPinHeaderLabel = Lang.BaseIDPage_PINScreen_Transport_Header_Label;
                        DdlPinBodyLabel = Lang.BaseIDPage_PINScreen_Transport_Body_Label;
                    }
                    break;
                default:
                    break;
            }
        }

        private async Task DisplayKBAError(string errorCode)
        {
            DdlBasicPopUp popUp;
            switch (errorCode)
            {
                case "001":
                    popUp = new DdlBasicPopUp(Lang.PopUp_Ddl_KBA_Error_001_Title, Lang.PopUp_Ddl_KBA_Error_001_Text, Lang.PopUp_Ddl_KBA_Error_Button);
                    await popUp.ShowPopUp();
                    break;
                case "002":
                    popUp = new DdlBasicPopUp(Lang.PopUp_Ddl_KBA_Error_002_Title, Lang.PopUp_Ddl_KBA_Error_002_Text, Lang.PopUp_Ddl_KBA_Error_Button);
                    await popUp.ShowPopUp();
                    break;
                case "003":
                    popUp = new DdlBasicPopUp(Lang.PopUp_Ddl_KBA_Error_003_Title, Lang.PopUp_Ddl_KBA_Error_003_Text, Lang.PopUp_Ddl_KBA_Error_Button);
                    await popUp.ShowPopUp();
                    break;
                case "004":
                    popUp = new DdlBasicPopUp(Lang.PopUp_Ddl_KBA_Error_004_Title, Lang.PopUp_Ddl_KBA_Error_004_Text, Lang.PopUp_Ddl_KBA_Error_Button);
                    await popUp.ShowPopUp();
                    break;
                case "005":
                    popUp = new DdlBasicPopUp(Lang.PopUp_Ddl_KBA_Error_005_Title, Lang.PopUp_Ddl_KBA_Error_005_Text, Lang.PopUp_Ddl_KBA_Error_Button);
                    await popUp.ShowPopUp();
                    break;
                default:
                    break;
            }
        }

        private async Task DeleteOldDdlCredentials(IAgentContext agentContext)
        {
            List<CredentialRecord> allOldCredentials = new List<CredentialRecord>();
            List<CredentialRecord> oldDdlCredentials = await _credentialService.ListAsync(agentContext, SearchQuery.Equal(nameof(CredentialRecord.CredentialDefinitionId), WalletParams.DdlCredentialId), 2147483647);
            List<CredentialRecord> oldDdlCredentialsDemo = await _credentialService.ListAsync(agentContext, SearchQuery.Equal(nameof(CredentialRecord.CredentialDefinitionId), WalletParams.DdlDemoCredentialId), 2147483647);
            List<CredentialRecord> oldDdlCredentialsDemoOld = await _credentialService.ListAsync(agentContext, SearchQuery.Equal(nameof(CredentialRecord.CredentialDefinitionId), WalletParams.DdlDemoCredentialIdOld), 2147483647);
            allOldCredentials.AddRange(oldDdlCredentials);
            allOldCredentials.AddRange(oldDdlCredentialsDemo);
            allOldCredentials.AddRange(oldDdlCredentialsDemoOld);
            foreach (CredentialRecord oldDdlCredential in allOldCredentials)
            {
                await _credentialService.DeleteCredentialAsync(agentContext, oldDdlCredential.Id);
            }
        }
    }
}
