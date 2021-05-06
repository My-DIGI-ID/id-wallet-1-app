using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Agent.Models;
using IDWallet.Resources;
using IDWallet.Services;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries.Configuration;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.QRScanner.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TransactionLedgerChangePopUp : CustomPopUp
    {
        private readonly ICustomAgentProvider _customAgentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly InboxService _inboxService = App.Container.Resolve<InboxService>();
        public TransactionLedgerChangePopUp(CustomConnectionInvitationMessage invitation, AgentOptions activeLedger,
            AgentOptions recommendedLedger)
        {
            PopUpIsLoading = true;
            InitializeComponent();
            _invitation = invitation;
            _activeLedger = activeLedger;
            _recommendedLedger = recommendedLedger;

            ActiveLedgerName.Text = _customAgentProvider.GetPoolName(_activeLedger);
            RecommendedLedgerName.Text = _customAgentProvider.GetPoolName(_recommendedLedger);

            ConnectionImage.Source = string.IsNullOrEmpty(invitation.ImageUrl)
                ? ImageSource.FromFile("default_logo.png")
                : ImageSource.FromUri(new Uri(invitation.ImageUrl));

            StayButton.Text = Lang.PopUp_Connection_Ledger_Change_Stay_Button;
            ChangeButton.Text = Lang.PopUp_Connection_Ledger_Change_Change_Button;
        }

        private AgentOptions _activeLedger { get; }
        private CustomConnectionInvitationMessage _invitation { get; }
        private AgentOptions _recommendedLedger { get; }
        private async void Change_Button_Clicked(object sender, EventArgs e)
        {
            ActiveLedgerGrid.IsVisible = false;
            PopUpTitle.IsVisible = false;
            PopUpText.IsVisible = false;
            FirstButtonStack.IsVisible = false;
            LedgerIndicator.IsVisible = true;
            LedgerIndicator.IsRunning = true;

            try
            {
                await _customAgentProvider.SwitchLedger(_recommendedLedger);

                while (!App.CredentialsLoaded || !App.ConnectionsLoaded)
                {
                    await Task.Delay(100);
                }

                _inboxService.PollMessages();

                ViewModels.AutoAcceptViewModel autoAcceptViewModel = App.AutoAcceptViewModel;
                autoAcceptViewModel.LoadItemsCommand.Execute(null);
            }
            catch (Exception)
            {
                try
                {
                    OnPopUpCanceled(this, new EventArgs());

                    BasicPopUp alert = new BasicPopUp(
                        Lang.PopUp_Ledger_Change_Failed_Title,
                        Lang.PopUp_Ledger_Change_Failed_Text,
                        Lang.PopUp_Ledger_Change_Failed_Button);
                    await alert.ShowPopUp();
                }
                catch (Exception)
                {
                    //ignore
                }
            }
            finally
            {
                PopUpIsLoading = false;

                base.OnPopUpAccepted(sender, e);
            }
        }

        private void Stay_Button_Clicked(object sender, EventArgs e)
        {
            base.OnPopUpCanceled(sender, e);
        }
    }
}