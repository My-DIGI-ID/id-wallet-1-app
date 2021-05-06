using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Agent.Services;
using IDWallet.Interfaces;
using IDWallet.Models;
using IDWallet.Resources;
using IDWallet.Services;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.QRScanner.PopUps;
using IDWallet.Views.Settings.Connections.PopUps;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Plugin.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Mobile;
using ZXing.Net.Mobile.Forms;

namespace IDWallet.Views.QRScanner.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanningPage : ContentPage
    {
        private static bool _checkIsRunning = false;
        private static Plugin.Permissions.Abstractions.PermissionStatus _permissionStatus = Plugin.Permissions.Abstractions.PermissionStatus.Unknown;
        private readonly AddGatewayService _addGatewayService = App.Container.Resolve<AddGatewayService>();
        private readonly ICustomAgentProvider _customAgentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly ConnectService _invitationService = App.Container.Resolve<ConnectService>();
        private readonly TransactionService _transactionService = App.Container.Resolve<TransactionService>();
        private readonly UrlShortenerService _urlShortenerService = App.Container.Resolve<UrlShortenerService>();
        private readonly ZXingScannerView scanner = new ZXingScannerView();
        private CameraResolution _resolution = new CameraResolution();

        public ScanningPage()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            CheckPermission();

            if (_permissionStatus != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
            {
                Task<Plugin.Permissions.Abstractions.PermissionStatus> task = Task.Run(async () =>
                    _ = await CrossPermissions.Current.RequestPermissionAsync<CameraPermission>()
                );
                task.Wait();
                CheckPermission();
            }

            if (_permissionStatus == Plugin.Permissions.Abstractions.PermissionStatus.Granted)
            {
                scanner.HorizontalOptions = LayoutOptions.FillAndExpand;
                scanner.VerticalOptions = LayoutOptions.FillAndExpand;

                scanner.Options = new MobileBarcodeScanningOptions
                {
                    PossibleFormats = new List<ZXing.BarcodeFormat>
                {
                    ZXing.BarcodeFormat.QR_CODE
                },
                    CameraResolutionSelector = SelectLowestResolutionMatchingDisplayAspectRatio,
                    AutoRotate = false
                };

                scanningGrid.Children.Add(scanner);

                HandleScan();
            }
        }

        // Based on https://msicc.net/how-to-avoid-a-distorted-android-camera-preview-with-zxing-net-mobile/
        public CameraResolution SelectLowestResolutionMatchingDisplayAspectRatio(
            List<CameraResolution> availableResolutions)
        {
            CameraResolution result = null;
            double aspectTolerance = 0.1;
            double displayOrientationHeight = DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait
                ? DeviceDisplay.MainDisplayInfo.Height
                : DeviceDisplay.MainDisplayInfo.Width;
            double displayOrientationWidth = DeviceDisplay.MainDisplayInfo.Orientation == DisplayOrientation.Portrait
                ? DeviceDisplay.MainDisplayInfo.Width
                : DeviceDisplay.MainDisplayInfo.Height;
            double targetRatio = displayOrientationHeight / displayOrientationWidth;
            double targetHeight = displayOrientationHeight;
            double minDiff = double.MaxValue;
            foreach (CameraResolution r in availableResolutions.Where(r =>
                Math.Abs((double)r.Width / r.Height - targetRatio) < aspectTolerance))
            {
                if (Math.Abs(r.Height - targetHeight) < minDiff)
                {
                    minDiff = Math.Abs(r.Height - targetHeight);
                }

                if (r.Width >= 720 && r.Height >= 720 && r.Height > r.Width)
                {
                    result = r;
                    _resolution = r;
                }
            }

            return result;
        }

        protected override void OnAppearing()
        {
            try
            {
                base.OnAppearing();

                scanner.IsScanning = true;
                scanner.IsAnalyzing = true;
                scanner.IsEnabled = true;
            }
            catch (Exception)
            {
                //ignore
            }
        }

        protected override void OnDisappearing()
        {
            try
            {
                base.OnDisappearing();

                scanner.IsScanning = false;
                scanner.IsAnalyzing = false;
                scanner.IsEnabled = false;

                Navigation.PopToRootAsync();
                App.ScanActive = false;
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private static async void CheckPermission()
        {
            if (!_checkIsRunning)
            {
                _checkIsRunning = true;
                _permissionStatus = await CrossPermissions.Current.CheckPermissionStatusAsync<CameraPermission>();
                _checkIsRunning = false;
            }
        }

        private void ForceAutofocus(TimeSpan ts)
        {
            Device.StartTimer(ts, () =>
            {
                if (scanner.IsScanning)
                {
                    scanner.AutoFocus(_resolution.Height, _resolution.Width);
                }

                return true;
            });
        }

        private async void HandleScan()
        {
            int permissionCounter = 0;
            while (_permissionStatus == null ||
                   _permissionStatus != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
            {
                await Task.Delay(100);

                CheckPermission();

                permissionCounter++;

                if (permissionCounter > 100)
                {
                    NavigateToWallet();
                }
            }

            scanner.IsScanning = true;
            scanner.IsAnalyzing = true;
            scanner.IsEnabled = true;

            TimeSpan ts = new TimeSpan(0, 0, 0, 1, 0);
            if (Device.RuntimePlatform == Device.Android)
            {
                string osVersion = DependencyService.Get<INativeHelper>().GetOsVersion();

                if ((!string.IsNullOrEmpty(osVersion) && int.Parse(osVersion) < 28) || App.ForceFocus)
                {
                    ForceAutofocus(ts);
                }
            }
            else if (App.ForceFocus)
            {
                ForceAutofocus(ts);
            }

            scanner.OnScanResult += result =>
                Device.BeginInvokeOnMainThread(async () =>
                {
                    scanner.IsScanning = false;
                    scanner.IsAnalyzing = false;
                    scanner.IsEnabled = false;

                    frameIndicator.IsVisible = true;
                    scanningIndicator.IsVisible = true;

                    string messageType = "";
                    string transactionId = null;
                    CustomConnectionInvitationMessage connectionInvitationMessage = null;
                    bool awaitableConnection = false;
                    bool awaitableProof = false;
                    CustomConnectionInvitationMessage invitation = null;
                    ProofRecord proofRecord = null;
                    CustomServiceDecorator service = null;
                    CredentialRecord credentialRecord = null;
                    GatewayQR gatewayQR = null;

                    try
                    {
                        gatewayQR = _addGatewayService.ReadGatewayJson(result.Text);
                        (transactionId, connectionInvitationMessage, awaitableConnection, awaitableProof) =
                            _transactionService.ReadTransactionUrl(result.Text);
                        invitation = _invitationService.ReadInvitationUrl(result.Text);
                    }
                    catch (Exception)
                    {
                        //ignore
                    }

                    if (gatewayQR != null)
                    {
                        messageType = "gateway_qr";
                    }

                    if (transactionId == null && invitation == null)
                    {
                        (proofRecord, service, credentialRecord, invitation) =
                            await _urlShortenerService.ProcessShortenedUrl(result.Text);
                    }

                    if (transactionId != null && connectionInvitationMessage != null)
                    {
                        messageType = "transaction_offer";
                    }

                    if (invitation != null)
                    {
                        messageType = MessageTypes.ConnectionInvitation;
                    }

                    if (proofRecord != null)
                    {
                        messageType = MessageTypes.PresentProofNames.RequestPresentation;
                    }

                    if (credentialRecord != null)
                    {
                        messageType = MessageTypes.IssueCredentialNames.OfferCredential;
                    }

                    Hyperledger.Aries.Configuration.AgentOptions activeAgent =
                        _customAgentProvider.GetActiveAgentOptions();

                    IAgentContext agentContext = await _customAgentProvider.GetContextAsync();

                    switch (messageType)
                    {
                        case MessageTypes.ConnectionInvitation:
                            PopUpResult popUpResult = PopUpResult.Canceled;
                            if (string.IsNullOrEmpty(invitation.Ledger))
                            {
                                NewConnectionPopUp popUp = new NewConnectionPopUp(invitation);
                                popUpResult = await popUp.ShowPopUp();
                            }
                            else
                            {
                                Hyperledger.Aries.Configuration.AgentOptions recommendedLedger =
                                    _customAgentProvider.GetAgentOptionsRecommendedLedger(invitation.Ledger);
                                if (recommendedLedger == null || recommendedLedger.PoolName == activeAgent.PoolName)
                                {
                                    NewConnectionPopUp popUp = new NewConnectionPopUp(invitation);
                                    popUpResult = await popUp.ShowPopUp();
                                }
                                else
                                {
                                    ConnectionLedgerChangePopUp popUp =
                                        new ConnectionLedgerChangePopUp(invitation, activeAgent, recommendedLedger);
                                    popUpResult = await popUp.ShowPopUp();
                                }
                            }

                            if (PopUpResult.Accepted == popUpResult)
                            {
                                await _invitationService.AcceptInvitationAsync(invitation);
                                NavigateToWallet();
                            }
                            else
                            {
                                NavigateToWallet();
                            }

                            break;

                        case "transaction_offer":
                            if (!string.IsNullOrEmpty(connectionInvitationMessage.Ledger))
                            {
                                Hyperledger.Aries.Configuration.AgentOptions recommendedLedger =
                                    _customAgentProvider.GetAgentOptionsRecommendedLedger(connectionInvitationMessage
                                        .Ledger);
                                if (!(recommendedLedger == null || recommendedLedger.PoolName == activeAgent.PoolName))
                                {
                                    TransactionLedgerChangePopUp popUp =
                                        new TransactionLedgerChangePopUp(connectionInvitationMessage, activeAgent,
                                            recommendedLedger);
                                    popUpResult = await popUp.ShowPopUp();
                                }
                            }

                            agentContext = await _customAgentProvider.GetContextAsync();

                            Hyperledger.Aries.Features.DidExchange.ConnectionRecord transactionConnectionRecord =
                                await _transactionService.CheckForExistingConnection(agentContext,
                                    connectionInvitationMessage, awaitableConnection);

                            if (transactionConnectionRecord == null)
                            {
                                NewConnectionPopUp popUpTransactionConnectionOffer =
                                    new NewConnectionPopUp(connectionInvitationMessage);
                                PopUpResult popUpTransactionConnectionOfferResult =
                                    await popUpTransactionConnectionOffer.ShowPopUp();

                                if (PopUpResult.Accepted == popUpTransactionConnectionOfferResult)
                                {
                                    transactionConnectionRecord =
                                        await _invitationService.AcceptInvitationAsync(connectionInvitationMessage,
                                            awaitableConnection);

                                    if (awaitableConnection)
                                    {
                                        App.WaitForConnection = true;
                                        App.AwaitableInvitation = connectionInvitationMessage;
                                    }
                                }
                                else
                                {
                                    BasicPopUp popNoConnection = new BasicPopUp(
                                        Lang.PopUp_Connection_Needed_Title,
                                        Lang.PopUp_Connection_Needed_Text,
                                        Lang.PopUp_Connection_Needed_Button);
                                    await popNoConnection.ShowPopUp();

                                    NavigateToWallet();

                                    break;
                                }
                            }

                            if (transactionConnectionRecord != null)
                            {
                                await _transactionService.SendTransactionResponse(agentContext, transactionId,
                                    transactionConnectionRecord);
                            }
                            else if (App.WaitForConnection)
                            {
                                int counter = 0;
                                while (App.WaitForConnection)
                                {
                                    await Task.Delay(100);

                                    counter++;

                                    if (counter == 1000)
                                    {
                                        break;
                                    }
                                }

                                if (App.AwaitableConnection != null)
                                {
                                    transactionConnectionRecord = App.AwaitableConnection;

                                    await _transactionService.SendTransactionResponse(agentContext, transactionId,
                                        App.AwaitableConnection);
                                }

                                App.AwaitableInvitation = null;
                                App.AwaitableConnection = null;
                            }

                            App.WaitForProof = awaitableProof;

                            if (App.WaitForProof)
                            {
                                App.AwaitableProofConnectionId = transactionConnectionRecord.Id;

                                while (App.WaitForProof)
                                {
                                    int counter = 0;
                                    await Task.Delay(100);

                                    counter++;

                                    if (counter == 1000)
                                    {
                                        break;
                                    }
                                }
                            }

                            App.AwaitableProofConnectionId = null;

                            NavigateToWallet();

                            break;

                        case MessageTypes.PresentProofNames.RequestPresentation:
                            NavigateToWallet();
                            break;

                        case MessageTypes.IssueCredentialNames.OfferCredential:
                            NavigateToWallet();
                            break;

                        case "gateway_qr":
                            SaveNewGatewayPopUp saveGwPopUp = new SaveNewGatewayPopUp();
                            PopUpResult saveGwPopUpResult = await saveGwPopUp.ShowPopUp();

                            if (PopUpResult.Accepted == saveGwPopUpResult)
                            {
                                await _addGatewayService.AddGateway(gatewayQR);
                            }

                            BasicPopUp popUpSuccess = new BasicPopUp(
                                Lang.PopUp_Add_GW_Success_Title,
                                Lang.PopUp_Add_GW_Success_Message,
                                Lang.PopUp_Add_GW_Success_Button
                            );
                            await popUpSuccess.ShowPopUp();

                            NavigateToWallet();
                            break;

                        default:
                            BasicPopUp popUpAlert = new BasicPopUp(
                                Lang.PopUp_QR_Fail_Title,
                                Lang.PopUp_QR_Fail_Text,
                                Lang.PopUp_QR_Fail_Button);
                            await popUpAlert.ShowPopUp();

                            scanner.IsScanning = true;
                            scanner.IsAnalyzing = true;
                            scanner.IsEnabled = true;

                            break;
                    }

                    frameIndicator.IsVisible = false;
                    scanningIndicator.IsVisible = false;
                });
        }

        private void NavigateToWallet()
        {
            if (Application.Current.MainPage is TabbedPage tabbedPage)
            {
                tabbedPage.CurrentPage = tabbedPage.Children[0];
            }
        }
    }
}