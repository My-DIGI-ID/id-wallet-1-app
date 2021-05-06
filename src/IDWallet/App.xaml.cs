                                                                                                                                                                                                                        using Autofac;
using Autofac.Extensions.DependencyInjection;
using IDWallet.Agent;
using IDWallet.Agent.Handler;
using IDWallet.Agent.Handlers;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Services;
using IDWallet.Events;
using IDWallet.Interfaces;
using IDWallet.Models;
using IDWallet.Resources;
using IDWallet.Services;
using IDWallet.Services.Interfaces;
using IDWallet.ViewModels;
using IDWallet.Views;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Login;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Configuration;
using Hyperledger.Aries.Contracts;
using Hyperledger.Aries.Features.BasicMessage;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using Hyperledger.Aries.Features.PresentProof;
using Hyperledger.Aries.Features.TrustPing;
using Hyperledger.Aries.Ledger;
using Hyperledger.Aries.Models.Events;
using Hyperledger.Aries.Payments;
using Hyperledger.Aries.Routing.Edge;
using Hyperledger.Aries.Runtime;
using Hyperledger.Aries.Storage;
using Hyperledger.Indy.WalletApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using PCLStorage;
using Plugin.Iconize;
using Plugin.Iconize.Fonts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Svg;

namespace IDWallet
{
    public partial class App : Application
    {
        public static System.Timers.Timer PollingTimer = new System.Timers.Timer
        {
            Interval = 10000,
            AutoReset = true
        };

        public static System.Timers.Timer SleepTimer = new System.Timers.Timer
        {
            Interval = 30000,
            AutoReset = false
        };

        private readonly BaseIdViewModel _baseIdViewModel;
        private readonly ICustomAgentProvider _agentProvider;
        private readonly IAppDeeplinkService _appDeeplinkService;
        private readonly IEventAggregator _eventAggregator;
        private readonly InboxService _inboxService;
        private readonly ICustomSecureStorageService _secureStorageService;
        private LoginPage _loginPage;
        private bool _lockOnSleep = false;

        public App()
        {
            InitLanguage();

            InitializeComponent();

            RegisterContainer();

            if (DeviceInfo.DeviceType == DeviceType.Physical)
            {
                _inboxService = Container.Resolve<InboxService>();
                _eventAggregator = Container.Resolve<IEventAggregator>();
                _appDeeplinkService = Container.Resolve<IAppDeeplinkService>();
                _secureStorageService = Container.Resolve<ICustomSecureStorageService>();
                _agentProvider = Container.Resolve<ICustomAgentProvider>();
                _baseIdViewModel = App.Container.Resolve<BaseIdViewModel>();
                _ = Container.Resolve<ConnectionsViewModel>();

                NativeStorageService = _secureStorageService;

                if (!_agentProvider.AgentExists())
                {
                    ClearAllData();
                }

                InitGatewayList();

                GetNewPnsHandle(_secureStorageService);

                PollingTimer.Elapsed += PollMessages;

                SleepTimer.Elapsed += LockOnSleep;

                GetPollingActiveInfo(_secureStorageService);

                GetPushService(_secureStorageService);

                GetIntroCompleted(_secureStorageService);

                GetSecondIosDeviceMigration(_secureStorageService);

                GetShowMediatorConnection(_secureStorageService);

                GetUseMediatorImages(_secureStorageService);

                ForceFocus = GetForceFocus(_secureStorageService);

                Iconize.With(new MaterialModule()).With(new MaterialDesignIconsModule());

                _eventAggregator.GetEventByType<InboxEvent>().Subscribe(_inboxService);
                _eventAggregator.GetEventByType<ServiceMessageProcessingEvent>()
                    .Subscribe(new ServiceMessageEventService());

                SvgImageSource.RegisterAssembly();

                Services.AppContext.Restore();

                ExperimentalFeatures.Enable("ShareFileRequest_Experimental");

                AutoAcceptViewModel = new AutoAcceptViewModel();

                MainPage = new CustomTabbedPage();
            }
            else
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Lang.PopUp_Vitual_Device_Title,
                        Lang.PopUp_Vitual_Device_Message,
                        Lang.PopUp_Vitual_Device_Button)
                    {
                        PreLoginPopUp = true
                    };
                    await popUp.ShowPopUp();

                    Process.GetCurrentProcess().Kill();
                });
            }
        }

        private void LockOnSleep(object sender, ElapsedEventArgs e)
        {
            _lockOnSleep = true;
        }

        public static bool AlreadySubscribed { get; set; } = false;
        public static AutoAcceptViewModel AutoAcceptViewModel { get; private set; }
        public static ConnectionRecord AwaitableConnection { get; set; }
        public static ConnectionInvitationMessage AwaitableInvitation { get; set; }
        public static string AwaitableProofConnectionId { get; set; }
        public static bool BiometricLoginActive { get; set; } = false;
        public static List<string> BlockedRecordTypes { get; set; } = new List<string>();
        public static bool ConnectionsLoaded { get; set; } = false;
        public static IContainer Container { get; private set; }
        public static bool CredentialsLoaded { get; set; } = false;
        public static bool FinishedNewPnsHandle { get; set; } = false;
        public static bool ForceFocus { get; set; } = false;
        public static bool HistoryLoaded { get; set; }
        public static bool IntroCompleted { get; set; } = false;
        public static bool IsInForeground { get; set; } = false;
        public static bool IsLoggedIn { get; set; } = false;
        public static bool LoggedInOnce { get; set; } = false;
        public static ICustomSecureStorageService NativeStorageService { get; private set; }
        public static bool NewPnsHandle { get; set; } = false;
        public static CustomPopUp OpenPopupPage { get; set; } = null;
        public static string PnsError { get; set; } = "";
        public static string PnsHandle { get; set; }
        public static string PollingHandle { get; set; }
        public static bool PollingIsActive { get; set; } = false;
        public static bool PollingWasActive { get; set; } = false;
        public static bool PopUpIsOpen { get; set; } = false;
        public static bool ProofsLoaded { get; set; } = false;
        public static string PushService { get; set; }
        public static bool ResetCarouselPosition { get; set; } = false;
        public static bool ScanActive { get; set; } = false;
        public static bool ScanFromPlaceholder { get; set; } = false;
        public static bool SecondIosDeviceMigration { get; set; } = false;
        public static bool ShowMediatorConnection { get; set; } = false;
        public static string TransactionConnectionId { get; set; }
        public static bool UseMediatorImages { get; set; } = false;
        public static bool WaitForConnection { get; set; } = false;
        public static bool WaitForProof { get; set; } = false;
        public static string BaseIdConnectionId { get; set; } = "";
        public static string SafetyResult { get; set; } = "";
        public static string SafetyKey { get; set; } = "";
        public static string SecurityCert { get; set; } = "";
        public static Wallet Wallet {get; set;}

        public static bool GetBiometricsInfo(ICustomSecureStorageService storageService)
        {
            bool isBiometricActivated;
            try
            {
                isBiometricActivated = storageService.GetAsync<bool>(WalletParams.KeyBiometricActivated).GetAwaiter()
                    .GetResult();
            }
            catch (Exception)
            {
                isBiometricActivated = false;
            }

            return isBiometricActivated;
        }

        public static bool GetForceFocus(ICustomSecureStorageService storageService)
        {
            bool forceFocus;
            try
            {
                forceFocus = storageService.GetAsync<bool>(WalletParams.KeyForceFocusActivated).GetAwaiter()
                    .GetResult();
            }
            catch (Exception)
            {
                storageService.SetAsync(WalletParams.KeyForceFocusActivated, false);
                forceFocus = false;
            }

            return forceFocus;
        }

        public static void GetNewPnsHandle(ICustomSecureStorageService storageService)
        {
            try
            {
                NewPnsHandle = (storageService.GetAsync<bool>(WalletParams.NewPnsHandle)).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                NewPnsHandle = false;
            }
        }

        public static void GetPnsHandle(ICustomSecureStorageService storageService)
        {
            try
            {
                PnsHandle = (storageService.GetAsync<string>(WalletParams.PnsHandle)).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                if (!string.IsNullOrEmpty(PnsHandle) && !PollingIsActive)
                {
                    storageService.SetAsync(WalletParams.PnsHandle, PnsHandle);
                }
            }
        }

        public static void GetPollingActiveInfo(ICustomSecureStorageService storageService)
        {
            try
            {
                PollingWasActive = (storageService.GetAsync<bool>(WalletParams.PollingWasActive)).GetAwaiter()
                    .GetResult();
            }
            catch (Exception)
            {
                PollingWasActive = false;
            }
        }

        public static string GetPollingHandle(ICustomSecureStorageService storageService)
        {
            try
            {
                PollingHandle = storageService.GetAsync<string>(WalletParams.PollingHandle).GetAwaiter().GetResult();
                if (PollingHandle.ToLower().Equals("true") || PollingHandle.ToLower().Equals("false"))
                {
                    PollingHandle = Guid.NewGuid().ToString();
                    storageService.SetAsync(WalletParams.PollingHandle, PollingHandle);
                }
            }
            catch (Exception)
            {
                PollingHandle = Guid.NewGuid().ToString();
                storageService.SetAsync(WalletParams.PollingHandle, PollingHandle);
            }

            return PollingHandle;
        }

        public static void GetSecondIosDeviceMigration(ICustomSecureStorageService storageService)
        {
            try
            {
                SecondIosDeviceMigration = (storageService.GetAsync<bool>(WalletParams.SecondIosDeviceMigration))
                    .GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                SecondIosDeviceMigration = false;
            }
        }

        public static void GetShowMediatorConnection(ICustomSecureStorageService storageService)
        {
            try
            {
                ShowMediatorConnection = (storageService.GetAsync<bool>(WalletParams.ShowMediatorConnection))
                    .GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                ShowMediatorConnection = false;
                storageService.SetAsync(WalletParams.ShowMediatorConnection, ShowMediatorConnection);
            }
        }

        public static async Task GetTransactionConnectionId(ICustomSecureStorageService storageService,
            ICustomWalletRecordService recordService, IAgentContext agentContext, IConnectionService connectionService,
            string walletId)
        {
            try
            {
                TransactionConnectionId =
                    await storageService.GetAsync<string>(WalletParams.TransactionConnectionId + walletId);
            }
            catch (Exception)
            {
                InviteConfiguration inviteConfiguration = new InviteConfiguration()
                {
                    MultiPartyInvitation = true,
                    AutoAcceptConnection = true
                };

                (ConnectionInvitationMessage invitation, ConnectionRecord record) =
                    await connectionService.CreateInvitationAsync(agentContext, inviteConfiguration);

                record.SetTag("InvitationMessage", JObject.FromObject(invitation).ToString());
                await recordService.UpdateAsync(agentContext.Wallet, record);

                TransactionConnectionId = record.Id;

                await storageService.SetAsync(WalletParams.TransactionConnectionId + walletId, record.Id);
            }
        }

        public static void GetUseMediatorImages(ICustomSecureStorageService storageService)
        {
            try
            {
                UseMediatorImages = (storageService.GetAsync<bool>(WalletParams.UseMediatorImages)).GetAwaiter()
                    .GetResult();
            }
            catch (Exception)
            {
                UseMediatorImages = false;
                storageService.SetAsync(WalletParams.UseMediatorImages, UseMediatorImages);
            }
        }

        public static void SetNewPnsHandle(ICustomSecureStorageService storageService, bool newPnsHandle)
        {
            storageService.SetAsync(WalletParams.NewPnsHandle, newPnsHandle);

            NewPnsHandle = newPnsHandle;
        }

        public static void SetPnsHandle(ICustomSecureStorageService storageService, string handle)
        {
            storageService.SetAsync(WalletParams.PnsHandle, handle);
            PnsHandle = handle;

            if (NewPnsHandle)
            {
                App.SetNewPnsHandle(App.NativeStorageService, false);
                InboxService notificationService = Container.Resolve<InboxService>();
                notificationService.RenewPush();
            }
        }

        public static async void ViewFile(string base64Pdf)
        {
            byte[] fileBytes = Convert.FromBase64String(base64Pdf);
            string filename = $"EmbeddedDocument.pdf";
            string filepath = "";

            if (Device.RuntimePlatform == Device.Android)
            {
                try
                {
                    IAndroidExternalStorageWriter fileWriter = DependencyService.Get<IAndroidExternalStorageWriter>();
                    filepath = fileWriter.CreateFile(filename, fileBytes.ToArray());
                }
                catch (Exception)
                {
                    //ignore
                }
            }
            else if (Device.RuntimePlatform == Device.iOS)
            {
                using (MemoryStream pdfBytes = new MemoryStream(fileBytes))
                {
                    IFolder rootFolder = PCLStorage.FileSystem.Current.LocalStorage;
                    IFile file = await rootFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                    filepath = file.Path;
                    Stream newFile = await file.OpenAsync(PCLStorage.FileAccess.ReadAndWrite);

                    using (Stream outputStream = newFile)
                    {
                        pdfBytes.CopyTo(outputStream);
                    }
                }
            }

            if (string.IsNullOrEmpty(filepath))
            {
                return;
            }

            string mimeType = "application/pdf";

            try
            {
                DependencyService.Get<IDocumentViewer>().ShowDocumentFile(filepath, mimeType);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public void ClearAllData()
        {
            try
            {
                SecureStorage.RemoveAll();
            }
            catch (Exception)
            {
            }

            try
            {
                string cachePath = Path.GetTempPath();

                if (Directory.Exists(cachePath))
                {
                    Directory.Delete(cachePath, true);
                }

                if (!Directory.Exists(cachePath))
                {
                    Directory.CreateDirectory(cachePath);
                }
            }
            catch (Exception)
            {
                //ignore
            }

            try
            {
                string dataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                if (Directory.Exists(dataPath))
                {
                    Directory.Delete(dataPath, true);
                }

                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }
            }
            catch (Exception)
            {
                //ignore
            }

            try
            {
                string dataPath = Path.Combine(Xamarin.Essentials.FileSystem.AppDataDirectory, ".indy_client");

                if (Directory.Exists(dataPath))
                {
                    Directory.Delete(dataPath, true);
                }

                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        protected override async void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            await Task.Run(() => { _appDeeplinkService.AppDeeplinkUri = uri; });
        }

        protected override void OnResume()
        {
            IsInForeground = true;
            try
            {
                if (!IntroCompleted)
                {
                    Services.AppContext.Restore();

                    IsLoggedIn = true;

                    if (!AlreadySubscribed)
                    {
                        AlreadySubscribed = true;
                        AutoAcceptViewModel.Subscribe();
                    }
                }
                else if (LoggedInOnce && !IsLoggedIn && !_lockOnSleep)
                {
                    Services.AppContext.Restore();

                    IsLoggedIn = true;

                    if (!AlreadySubscribed)
                    {
                        AlreadySubscribed = true;
                        AutoAcceptViewModel.Subscribe();
                    }

                    _appDeeplinkService.ProcessAppDeeplink();
                    _inboxService.PollMessages();
                    PollingTimer.Start();
                    SleepTimer.Stop();
                    _lockOnSleep = false;
                }
                else if (!BiometricLoginActive && !IsLoggedIn)
                {
                    Services.AppContext.Restore();

                    try
                    {
                        _baseIdViewModel.GoToStart();
                    }
                    catch (Exception)
                    {
                        //ignore
                    }

                    try
                    {
                        if (OpenPopupPage != null)
                        {
                            OpenPopupPage.CloseOnSleep();
                        }
                    }
                    catch (Exception)
                    {
                        //ignore
                    }

                    if (MainPage.Navigation.ModalStack.Count == 0)
                    {
                        _loginPage = new LoginPage();
                        MainPage.Navigation.PushModalAsync(_loginPage);
                    }
                    else
                    {
                        IEnumerator<Page> oldPageEnumerator = MainPage.Navigation.ModalStack.GetEnumerator();
                        oldPageEnumerator.MoveNext();

                        if (!(oldPageEnumerator.Current is LoginPage))
                        {
                            SleepTimer.Stop();
                            _lockOnSleep = false;

                            _loginPage = new LoginPage();
                            MainPage.Navigation.PushModalAsync(_loginPage);
                        }
                    }
                }
            }
            catch (Exception)
            {
                try
                {
                    _baseIdViewModel.GoToStart();
                }
                catch (Exception)
                {
                    //ignore
                }

                AutoAcceptViewModel.Sleep();
                AlreadySubscribed = false;
                IsLoggedIn = false;
                LoggedInOnce = false;
                BiometricLoginActive = false;

                SleepTimer.Stop();
                _lockOnSleep = false;

                _loginPage = new LoginPage();
                MainPage.Navigation.PushModalAsync(_loginPage);
            }
        }

        protected override void OnSleep()
        {
            if (!BiometricLoginActive)
            {
                IsInForeground = false;
                IsLoggedIn = false;

                AutoAcceptViewModel.Sleep();
                AlreadySubscribed = false;

                _lockOnSleep = false;
                SleepTimer.Start();

                PollingTimer.Stop();

                _secureStorageService.SetAsync(WalletParams.PnsHandle, PnsHandle);
                Services.AppContext.Save();
            }
        }

        protected override void OnStart()
        {
            IsInForeground = true;
            AlreadySubscribed = false;
            LoggedInOnce = false;

            _lockOnSleep = false;

            
            _loginPage = new LoginPage();
            MainPage.Navigation.PushModalAsync(_loginPage);
            if (!IntroCompleted)
            {
                MainPage.Navigation.PushModalAsync(new Views.Intro.IntroPage());
            }
        }

        private static void InitLanguage()
        {
            if (!Current.Properties.ContainsKey(WalletParams.KeyLanguage))
            {
                Current.Properties.Add(WalletParams.KeyLanguage, CultureInfo.CurrentUICulture.Name);
            }

            Thread.CurrentThread.CurrentCulture =
                new CultureInfo(Current.Properties[WalletParams.KeyLanguage].ToString(), false);
            Thread.CurrentThread.CurrentUICulture =
                new CultureInfo(Current.Properties[WalletParams.KeyLanguage].ToString(), false);
        }

        private void GetIntroCompleted(ICustomSecureStorageService storageService)
        {
            try
            {
                IntroCompleted = (storageService.GetAsync<bool>(WalletParams.IntroCompletedTag)).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                IntroCompleted = false;
                storageService.SetAsync(WalletParams.IntroCompletedTag, IntroCompleted);
            }
        }

        private void GetPushService(ICustomSecureStorageService storageService)
        {
            try
            {
                PushService = (storageService.GetAsync<string>(WalletParams.PushService)).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                PushService = "Polling";
            }
        }

        private async void InitGatewayList()
        {
            ObservableCollection<Gateway> gateways = new ObservableCollection<Gateway>();
            try
            {
                gateways = await _secureStorageService.GetAsync<ObservableCollection<Gateway>>(WalletParams.AllGatewaysTag);
            }
            catch (Exception)
            {
                //ignore
            }

            if (gateways == null || !gateways.Any())
            {
                try
                {
                    if (!string.IsNullOrEmpty(WalletParams.ProofCallingEndpoints))
                    {
                        gateways = new ObservableCollection<Gateway>();
                        string[] baseGateways = WalletParams.ProofCallingEndpoints.Split(',');
                        foreach (string baseGateway in baseGateways)
                        {
                            gateways.Add(new Gateway
                            {
                                Address = baseGateway,
                                Key = WalletParams.MobileToken,
                                Name = baseGateway.Split('/')[2]
                            });
                        }

                        await _secureStorageService.SetAsync(WalletParams.AllGatewaysTag, gateways);
                    }
                }
                catch (Exception)
                {
                    //ignore
                }
            }
        }

        private void PollMessages(object source, ElapsedEventArgs e)
        {
            _inboxService.PollMessages();
        }
        private void RegisterContainer()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddHttpClient();

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterType<CustomWalletRecordService>().AsImplementedInterfaces().AsSelf();
            builder.RegisterType<CustomHttpMessageDispatcher>().As<IMessageDispatcher>().AsSelf();
            builder.RegisterType<EventAggregator>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DefaultProvisioningService>().AsImplementedInterfaces();
            builder.RegisterType<DefaultWalletService>().AsImplementedInterfaces();
            builder.RegisterType<DefaultLedgerSigningService>().AsImplementedInterfaces();
            builder.RegisterType<DefaultLedgerService>().AsImplementedInterfaces();
            builder.RegisterType<DefaultBasicMessageHandler>().AsImplementedInterfaces().AsSelf();
            builder.RegisterType<DefaultSchemaService>().AsImplementedInterfaces();
            builder.RegisterType<CustomTailsService>().AsImplementedInterfaces();
            builder.RegisterType<DefaultPaymentService>().AsImplementedInterfaces();
            builder.RegisterType<DefaultCredentialService>().AsImplementedInterfaces();
            builder.RegisterType<CustomPoolService>().AsImplementedInterfaces().AsSelf();
            builder.RegisterType<CustomConnectionService>().AsImplementedInterfaces();
            builder.RegisterType<CustomConnectionService>().AsSelf();
            builder.RegisterType<CustomProofService>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<DefaultMessageService>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<ConnectService>().AsSelf();
            builder.RegisterType<UrlShortenerService>().AsSelf();
            builder.RegisterType<CustomSecureStorageService>().AsImplementedInterfaces();
            builder.RegisterType<AddGatewayService>().AsSelf();
            builder.RegisterType<AppDeeplinkService>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterInstance(new L10nService(Lang.ResourceManager)).As<IL10nService>();
            builder.RegisterType<EdgeClientService>().AsImplementedInterfaces().AsSelf();
            builder.RegisterType<InboxService>().AsSelf();
            builder.RegisterType<CheckRevocationService>().AsSelf();
            builder.RegisterType<SDKMessageService>().AsSelf().SingleInstance();
            builder.RegisterType<ProofRequestService>().AsSelf();
            builder.RegisterType<DefaultProofService>().AsImplementedInterfaces();
            builder.RegisterType<CustomWalletAgent>().As<IAgent>();
            builder.RegisterType<CustomAgentProvider>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<CustomForwardHandler>().AsSelf();
            builder.RegisterType<CustomConnectionHandler>().AsSelf();
            builder.RegisterType<DefaultTrustPingMessageHandler>().AsSelf();
            builder.RegisterType<CustomProofHandler>().AsSelf();
            builder.RegisterType<CustomCredentialHandler>().AsSelf();
            builder.RegisterType<TransactionService>().AsSelf();
            builder.RegisterType<TransactionService>().AsImplementedInterfaces();
            builder.RegisterType<TransactionHandler>().AsSelf();
            builder.RegisterType<PrivacyHandler>().AsSelf();
            builder.RegisterType<WalletAgentOptions>().As<IOptions<List<AgentOptions>>>();
            builder.RegisterType<ConnectionsViewModel>().AsSelf().SingleInstance();
            builder.RegisterType<TransactionOfferService>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<BaseIdViewModel>().AsSelf().SingleInstance();
            builder.Populate(services);

            Container = builder.Build();
        }
    }
}