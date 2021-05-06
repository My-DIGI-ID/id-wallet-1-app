using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Settings.Connections.PopUps;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Features.DidExchange;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    internal class ConnectionEditViewModel : CustomViewModel
    {
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();

        private readonly ConnectionRecord _connectionRecord;

        private readonly IConnectionService _connectionService = App.Container.Resolve<IConnectionService>();

        private readonly ICustomWalletRecordService _walletRecordService =
                            App.Container.Resolve<ICustomWalletRecordService>();
        private bool _autoCredential;

        private bool _autoProof;

        private bool _noteCredential;

        private bool _noteProof;

        private bool _onlyKnownProofs;

        private string _takeLastUsedIcon;

        private string _takeNewestIcon;

        public ConnectionEditViewModel(ConnectionRecord connectionRecord)
        {
            _connectionRecord = connectionRecord;
            ConnectionName = _connectionRecord.Alias.Name;
            ConnectionImage = string.IsNullOrEmpty(_connectionRecord.Alias.ImageUrl)
                ? ImageSource.FromFile("default_logo.png")
                : new Uri(_connectionRecord.Alias.ImageUrl);
        }

        public bool AutoCredential
        {
            get => _autoCredential;
            set => SetProperty(ref _autoCredential, value);
        }
        public bool AutoProof
        {
            get => _autoProof;
            set => SetProperty(ref _autoProof, value);
        }

        public ImageSource ConnectionImage { get; set; }

        public string ConnectionName { get; set; }

        public Command LoadItemsCommand { get; set; }

        public bool NoteCredential
        {
            get => _noteCredential;
            set => SetProperty(ref _noteCredential, value);
        }
        public bool NoteProof
        {
            get => _noteProof;
            set => SetProperty(ref _noteProof, value);
        }
        public bool OnlyKnownProofs
        {
            get => _onlyKnownProofs;
            set => SetProperty(ref _onlyKnownProofs, value);
        }
        public string TakeLastUsedIcon
        {
            get => _takeLastUsedIcon;
            set => SetProperty(ref _takeLastUsedIcon, value);
        }

        public string TakeNewestIcon
        {
            get => _takeNewestIcon;
            set => SetProperty(ref _takeNewestIcon, value);
        }
        public async Task ChangeName(string newName)
        {
            _connectionRecord.Alias.Name = newName;

            IAgentContext agentContext = await _agentProvider.GetContextAsync();
            await _walletRecordService.UpdateAsync(agentContext.Wallet, _connectionRecord);
        }

        public async Task CheckForTags()
        {
            Hyperledger.Aries.Agents.IAgentContext agentContext = await _agentProvider.GetContextAsync();

            if (string.IsNullOrEmpty(_connectionRecord.GetTag("AutoAcceptCredential")))
            {
                _connectionRecord.SetTag("AutoAcceptCredential", "False");
            }

            if (string.IsNullOrEmpty(_connectionRecord.GetTag("AutoCredentialNotification")))
            {
                _connectionRecord.SetTag("AutoCredentialNotification", "False");
            }

            if (string.IsNullOrEmpty(_connectionRecord.GetTag("AutoAcceptProof")))
            {
                _connectionRecord.SetTag("AutoAcceptProof", "False");
            }

            if (string.IsNullOrEmpty(_connectionRecord.GetTag("AutoProofNotification")))
            {
                _connectionRecord.SetTag("AutoProofNotification", "False");
            }

            if (string.IsNullOrEmpty(_connectionRecord.GetTag("OnlyKnownProofs")))
            {
                _connectionRecord.SetTag("OnlyKnownProofs", "False");
            }

            if (string.IsNullOrEmpty(_connectionRecord.GetTag("TakeNewest")))
            {
                _connectionRecord.SetTag("TakeNewest", "False");
            }

            await _walletRecordService.UpdateAsync(agentContext.Wallet, _connectionRecord);

            AutoCredential = Convert.ToBoolean(_connectionRecord.GetTag("AutoAcceptCredential"));
            NoteCredential = Convert.ToBoolean(_connectionRecord.GetTag("AutoCredentialNotification"));

            AutoProof = Convert.ToBoolean(_connectionRecord.GetTag("AutoAcceptProof"));
            NoteProof = Convert.ToBoolean(_connectionRecord.GetTag("AutoProofNotification"));

            OnlyKnownProofs = Convert.ToBoolean(_connectionRecord.GetTag("OnlyKnownProofs"));


            if (Convert.ToBoolean(_connectionRecord.GetTag("TakeNewest")))
            {
                TakeNewestIcon = "mdi-circle-slice-8";
                TakeLastUsedIcon = "mdi-checkbox-blank-circle-outline";
            }
            else
            {
                TakeNewestIcon = "mdi-checkbox-blank-circle-outline";
                TakeLastUsedIcon = "mdi-circle-slice-8";
            }
        }

        public async Task<bool> DeleteConnection()
        {
            DeleteConnectionPopUp popUp = new DeleteConnectionPopUp(_connectionRecord.Alias.Name);
            PopUpResult popupResult = await popUp.ShowPopUp();

            if (PopUpResult.Accepted == popupResult)
            {
                IAgentContext context = await _agentProvider.GetContextAsync();
                await _connectionService.DeleteAsync(context, _connectionRecord.Id);

                return true;
            }

            return false;
        }

        public async void UpdateTags(bool autoCred, bool noteCred, bool autoProof, bool noteProof,
                    bool onlyKnownProofs, string takeNewestIcon)
        {
            Hyperledger.Aries.Agents.IAgentContext agentContext = await _agentProvider.GetContextAsync();
            if (autoCred)
            {
                _connectionRecord.SetTag("AutoCredentialNotification", noteCred.ToString());
            }

            if (autoProof)
            {
                _connectionRecord.SetTag("AutoProofNotification", noteProof.ToString());
            }

            _connectionRecord.SetTag("AutoAcceptCredential", autoCred.ToString());
            _connectionRecord.SetTag("AutoAcceptProof", autoProof.ToString());
            _connectionRecord.SetTag("OnlyKnownProofs", onlyKnownProofs.ToString());
            if (takeNewestIcon == "mdi-checkbox-blank-circle-outline")
            {
                _connectionRecord.SetTag("TakeNewest", "False");
                _connectionRecord.SetTag("TakeLastUsed", "True");
            }
            else
            {
                _connectionRecord.SetTag("TakeNewest", "True");
                _connectionRecord.SetTag("TakeLastUsed", "False");
            }


            await _walletRecordService.UpdateAsync(agentContext.Wallet, _connectionRecord);
        }
    }
}