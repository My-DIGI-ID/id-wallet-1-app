using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries.Features.DidExchange;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Connections.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ActivateAutoAcceptPopUp : CustomPopUp
    {
        private readonly ICustomAgentProvider _customAgentProvider = App.Container.Resolve<ICustomAgentProvider>();

        private readonly ICustomWalletRecordService _walletRecordService =
                    App.Container.Resolve<ICustomWalletRecordService>();
        private Command<CheckBox> _checkBoxTappedCommand;
        private Command<string> _itemClicked;
        public ActivateAutoAcceptPopUp(ConnectionRecord connectionRecord, bool isProof = false)
        {
            InitializeComponent();
            _connectionId = connectionRecord.Id;
            _isProof = isProof;

            if (isProof)
            {
                TextSpan1.Text = IDWallet.Resources.Lang.PopUp_ActivateAutoAccept_Proof_Text_Before_Alias + " ";
                CheckBoxText.Text = IDWallet.Resources.Lang.PopUp_ActivateAutoAccept_Proof_Notification;
                TextSpan3.Text = " " + IDWallet.Resources.Lang.PopUp_ActivateAutoAccept_Proof_Text_After_Alias +
                                 "?";
            }
            else
            {
                TextSpan1.Text = IDWallet.Resources.Lang.PopUp_ActivateAutoAccept_Credential_Text_Before_Alias +
                                 " ";
                CheckBoxText.Text = IDWallet.Resources.Lang.PopUp_ActivateAutoAccept_Credential_Notification;
                TextSpan3.Text = " " +
                                 IDWallet.Resources.Lang.PopUp_ActivateAutoAccept_Credential_Text_After_Alias + "?";
            }

            TextSpan2.Text = connectionRecord.Alias.Name;

            _autoAccept = false;
            _askLater = true;
            _askNever = false;
        }

        public Command<CheckBox> CheckBoxTappedCommand =>
            _checkBoxTappedCommand ??= new Command<CheckBox>(CheckBoxTapped);

        public Command<string> ItemClicked => _itemClicked ??= new Command<string>(OnItemClicked);
        private bool _askLater { get; set; }
        private bool _askNever { get; set; }
        private bool _autoAccept { get; set; }
        private string _connectionId { get; }
        private bool _isProof { get; }
        private async void Button_Clicked(object sender, System.EventArgs e)
        {
            SaveButton.IsEnabled = false;
            if (_askLater)
            {
                OnPopUpCanceled(sender, e);
                return;
            }

            Hyperledger.Aries.Agents.IAgentContext context = await _customAgentProvider.GetContextAsync();
            ConnectionRecord connectionRecord =
                await _walletRecordService.GetAsync<ConnectionRecord>(context.Wallet, _connectionId, true);

            if (_autoAccept)
            {
                if (_isProof)
                {
                    connectionRecord.SetTag("AutoAcceptProof", "True");
                    connectionRecord.SetTag("AutoProofNotification", CheckBox.IsChecked.ToString());
                    connectionRecord.SetTag("TakeNewest", "True");
                }
                else
                {
                    connectionRecord.SetTag("AutoAcceptCredential", "True");
                    connectionRecord.SetTag("AutoCredentialNotification", CheckBox.IsChecked.ToString());
                }
            }
            else
            {
                if (_isProof)
                {
                    connectionRecord.SetTag("NeverAskAgainProof", "NeverAskAgainProof");
                }
                else
                {
                    connectionRecord.SetTag("NeverAskAgainCred", "NeverAskAgainCred");
                }
            }

            await _walletRecordService.UpdateAsync(context.Wallet, connectionRecord);
            OnPopUpAccepted(sender, e);
        }

        private void CheckBoxTapped(CheckBox checkBox)
        {
            checkBox.IsChecked = !checkBox.IsChecked;
        }

        private void OnItemClicked(string buttonName)
        {
            switch (buttonName)
            {
                case "AutoAccept":
                    if (!_autoAccept)
                    {
                        CheckBoxStack.IsVisible = true;
                        _autoAccept = true;
                        AutoAcceptIconImage.Icon = "mdi-circle-slice-8";
                        _askLater = false;
                        AskLaterIconImage.Icon = "mdi-checkbox-blank-circle-outline";
                        _askNever = false;
                        AskNeverIconImage.Icon = "mdi-checkbox-blank-circle-outline";
                    }

                    break;
                case "AskLater":
                    if (!_askLater)
                    {
                        CheckBoxStack.IsVisible = false;
                        _autoAccept = false;
                        AutoAcceptIconImage.Icon = "mdi-checkbox-blank-circle-outline";
                        _askLater = true;
                        AskLaterIconImage.Icon = "mdi-circle-slice-8";
                        _askNever = false;
                        AskNeverIconImage.Icon = "mdi-checkbox-blank-circle-outline";
                    }

                    break;
                case "AskNever":
                    if (!_askNever)
                    {
                        CheckBoxStack.IsVisible = false;
                        _autoAccept = false;
                        AutoAcceptIconImage.Icon = "mdi-checkbox-blank-circle-outline";
                        _askLater = false;
                        AskLaterIconImage.Icon = "mdi-checkbox-blank-circle-outline";
                        _askNever = true;
                        AskNeverIconImage.Icon = "mdi-circle-slice-8";
                    }

                    break;
            }
        }
    }
}