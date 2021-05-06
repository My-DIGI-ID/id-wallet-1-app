using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Models;
using IDWallet.Resources;
using IDWallet.Services;
using IDWallet.ViewModels;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Inbox;
using IDWallet.Views.Settings.Ledger.PopUps;
using Hyperledger.Aries.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Ledger
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SwitchLedgerPage : ContentPage
    {
        private readonly ICustomAgentProvider _customAgentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly InboxService _inboxService = App.Container.Resolve<InboxService>();

        private bool _isLoading;

        private Command<SwitchLedgerElement> _ledgerTappedCommand;

        private Command _notificationsClickedCommand;

        public SwitchLedgerPage()
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingOpen_Icon.svg");
            Ledgers = new ObservableCollection<SwitchLedgerElement>();

            InitializeComponent();

            CustomViewModel viewModel = new CustomViewModel();
            BindingContext = viewModel;
            viewModel.DisableNotificationAlert();
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading == value)
                {
                    return;
                }

                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public ObservableCollection<SwitchLedgerElement> Ledgers { get; set; }

        public Command<SwitchLedgerElement> LedgerTappedCommand =>
                                            _ledgerTappedCommand ??= new Command<SwitchLedgerElement>(LedgerTapped);
        public Command NotificationsClickedCommand =>
            _notificationsClickedCommand ??= new Command(Notifications_Clicked);

        public ImageSource SettingsIconImage { get; set; }
        protected override void OnAppearing()
        {
            EnableAll();
            Ledgers.Clear();

            AgentOptions activeAgent = _customAgentProvider.GetActiveAgentOptions();

            List<AgentOptions> allAgents = _customAgentProvider.GetAllAgentOptions();
            Dictionary<AgentOptions, int> allAgentsWithOrder = new Dictionary<AgentOptions, int>();
            foreach (AgentOptions agent in allAgents)
            {
                switch (agent.PoolName.ToLower())
                {
                    case "idw_eesdi":
                        allAgentsWithOrder.Add(agent, 1);
                        break;
                    case "idw_live":
                        allAgentsWithOrder.Add(agent, 2);
                        break;
                    case "idw_builder":
                        allAgentsWithOrder.Add(agent, 3);
                        break;
                    case "idw_staging":
                        //allAgentsWithOrder.Add(agent, 4);
                        break;
                    case "idw_bcgov":
                        allAgentsWithOrder.Add(agent, 5);
                        break;
                    case "idw_esatus":
                        allAgentsWithOrder.Add(agent, 6);
                        break;
                    case "idw_iduniontest":
                        allAgentsWithOrder.Add(agent, 7);
                        break;
                    case "idw_devledger":
                        allAgentsWithOrder.Add(agent, 8);
                        break;
                    default:
                        break;
                }
            }

            foreach (KeyValuePair<AgentOptions, int> agent in allAgentsWithOrder.OrderBy(x => x.Value)
                .ThenBy(x => x.Key.PoolName))
            {
                if (agent.Key.PoolName != activeAgent.PoolName)
                {
                    Ledgers.Add(new SwitchLedgerElement(agent.Key, false, _customAgentProvider.GetPoolName(agent.Key),
                        "mdi-checkbox-blank-circle-outline"));
                }
                else
                {
                    Ledgers.Add(new SwitchLedgerElement(agent.Key, true, _customAgentProvider.GetPoolName(agent.Key),
                        "mdi-circle-slice-8"));
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            DisableAll();
            return base.OnBackButtonPressed();
        }

        private void DisableAll()
        {
            NotificationsToolBarItem.IsEnabled = false;
            SettingsToolBarItem.IsEnabled = false;
            LedgerStack.IsEnabled = false;
        }

        private void EnableAll()
        {
            NotificationsToolBarItem.IsEnabled = true;
            SettingsToolBarItem.IsEnabled = true;
            LedgerStack.IsEnabled = true;
        }

        private async void LedgerTapped(SwitchLedgerElement tappedItem)
        {
            DisableAll();
            if (tappedItem.IsActive)
            {
                return;
            }

            SwitchLedgerPopUp popUp = new SwitchLedgerPopUp(tappedItem.PoolName);
            PopUpResult result = await popUp.ShowPopUp();
            if (result != PopUpResult.Accepted)
            {
                EnableAll();
                return;
            }

            IsLoading = true;
            foreach (SwitchLedgerElement changeLedgerPageItem in Ledgers)
            {
                changeLedgerPageItem.IsActive = false;
                changeLedgerPageItem.IconSource = "mdi-checkbox-blank-circle-outline";
            }

            try
            {
                App.ResetCarouselPosition = true;

                await _customAgentProvider.SwitchLedger(tappedItem.Options);
                tappedItem.IsActive = true;
                tappedItem.IconSource = "mdi-circle-slice-8";

                while (!App.CredentialsLoaded || !App.ConnectionsLoaded)
                {
                    await Task.Delay(100);
                }

                _inboxService.PollMessages();

                AutoAcceptViewModel autoAcceptViewModel = App.AutoAcceptViewModel;
                autoAcceptViewModel.LoadItemsCommand.Execute(null);
            }
            catch (Exception)
            {
                BasicPopUp alert = new BasicPopUp(
                    Lang.PopUp_Ledger_Change_Failed_Title,
                    Lang.PopUp_Ledger_Change_Failed_Text,
                    Lang.PopUp_Ledger_Change_Failed_Button);
                await alert.ShowPopUp();
                try
                {
                    await Navigation.PopAsync();
                }
                catch (System.Exception)
                {
                    //ignore
                }
            }
            finally
            {
                IsLoading = false;
                EnableAll();
            }
        }
        private async void Notifications_Clicked()
        {
            DisableAll();
            InboxPage notificationsPage = null;
            try
            {
                bool nextPageExists = false;
                IEnumerator<Page> oldPageEnumerator =
                    Application.Current.MainPage.Navigation.ModalStack.GetEnumerator();
                do
                {
                    nextPageExists = oldPageEnumerator.MoveNext();
                } while (nextPageExists && !(oldPageEnumerator.Current is InboxPage));

                if (oldPageEnumerator.Current is InboxPage)
                {
                    notificationsPage = (InboxPage)oldPageEnumerator.Current;
                }
            }
            catch (Exception)
            {
                notificationsPage = new InboxPage();
            }
            finally
            {
                if (notificationsPage == null)
                {
                    notificationsPage = new InboxPage();
                }
            }

            await Navigation.PushAsync(notificationsPage);
            EnableAll();
        }
    }
}