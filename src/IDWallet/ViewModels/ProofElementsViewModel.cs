using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Events;
using IDWallet.Models;
using IDWallet.Services;
using Hyperledger.Aries.Extensions;
using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.PresentProof;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.ViewModels
{
    public class ProofElementsViewModel : CustomViewModel
    {
        private readonly ICustomAgentProvider _agentProvider = App.Container.Resolve<ICustomAgentProvider>();
        private readonly ProofRequest _proofRequest;
        private readonly TransactionOfferService _transactionOfferService = App.Container.Resolve<TransactionOfferService>();
        private readonly ICustomWalletRecordService _walletRecordService =
            App.Container.Resolve<ICustomWalletRecordService>();

        private string _barcodeValue;
        private bool _isRevealedLabelVisible;
        private bool _isRevealedStackVisible;
        private Command<string> _openPdfButtonClickedCommand;
        private bool _qrFrameIsVisible;
        private ImageSource _qrImageSource;
        private ObservableCollection<ProofElementsModel> _revealed;
        private ObservableCollection<ProofAttributeInfo> _sendableRequestAttributes;
        private ObservableCollection<ProofPredicateInfo> _sendableRequestPredicates;
        private string _sendableRequestTitle;
        public ProofElementsViewModel(SendableRequest sendableRequest)
        {
            IsRevealedStackVisible = false;
            IsRevealedLabelVisible = false;
            Pending = true;

            _sendableRequestTitle = sendableRequest.Name;
            _proofRequest = sendableRequest.ProofRequest;

            _sendableRequestAttributes = new ObservableCollection<ProofAttributeInfo>();
            List<(string, ProofAttributeInfo)> temporaryAttributes = new List<(string, ProofAttributeInfo)>();
            List<(string, ProofAttributeInfo)> temporaryAttributeGroups = new List<(string, ProofAttributeInfo)>();
            foreach ((string key, ProofAttributeInfo value) in sendableRequest.ProofRequest.RequestedAttributes)
            {
                if (!key.Contains("grp_"))
                {
                    temporaryAttributes.Add((key, value));
                }
                else
                {
                    temporaryAttributeGroups.Add((key, value));
                }
            }

            temporaryAttributes = temporaryAttributes.OrderBy(x => x.Item1).ToList();
            temporaryAttributeGroups = temporaryAttributeGroups.OrderBy(x => x.Item1).ToList();
            foreach ((string, ProofAttributeInfo) info in temporaryAttributes)
            {
                SendableRequestAttributes.Add(info.Item2);
            }

            foreach ((string, ProofAttributeInfo) info in temporaryAttributeGroups)
            {
                SendableRequestAttributes.Add(info.Item2);
            }

            _sendableRequestPredicates = new ObservableCollection<ProofPredicateInfo>();
            List<(string, ProofPredicateInfo)> temporaryPredicates = new List<(string, ProofPredicateInfo)>();
            List<(string, ProofPredicateInfo)> temporaryPredicateGroups = new List<(string, ProofPredicateInfo)>();
            foreach ((string key, ProofPredicateInfo value) in sendableRequest.ProofRequest.RequestedPredicates)
            {
                if (!key.Contains("grp_"))
                {
                    temporaryPredicates.Add((key, value));
                }
                else
                {
                    temporaryPredicateGroups.Add((key, value));
                }
            }

            temporaryPredicates = temporaryPredicates.OrderBy(x => x.Item1).ToList();
            temporaryPredicateGroups = temporaryPredicateGroups.OrderBy(x => x.Item1).ToList();
            foreach ((string, ProofPredicateInfo) info in temporaryPredicates)
            {
                SendableRequestPredicates.Add(info.Item2);
            }

            foreach ((string, ProofPredicateInfo) info in temporaryPredicateGroups)
            {
                SendableRequestPredicates.Add(info.Item2);
            }

            Revealed = new ObservableCollection<ProofElementsModel>();

            BarcodeValue = "0000";
            LoadItemsCommand = new Command(async () => await ExecuteLoadTransactionCommand());
        }

        public string BarcodeValue
        {
            get => _barcodeValue;
            set => SetProperty(ref _barcodeValue, value);
        }

        public bool IsRevealedLabelVisible
        {
            get => _isRevealedLabelVisible;
            set => SetProperty(ref _isRevealedLabelVisible, value);
        }

        public bool IsRevealedStackVisible
        {
            get => _isRevealedStackVisible;
            set => SetProperty(ref _isRevealedStackVisible, value);
        }

        public Command LoadItemsCommand { get; set; }
        public Command<string> OpenPdfButtonClickedCommand =>
            _openPdfButtonClickedCommand ??= new Command<string>(OpenPdfButtonClicked);

        public bool Pending { get; set; }
        public bool QRFrameIsVisible
        {
            get => _qrFrameIsVisible;
            set => SetProperty(ref _qrFrameIsVisible, value);
        }

        public ImageSource QRVerifyImage
        {
            get => _qrImageSource;
            set => SetProperty(ref _qrImageSource, value);
        }

        public ObservableCollection<ProofElementsModel> Revealed
        {
            get => _revealed;
            set => SetProperty(ref _revealed, value);
        }

        public ObservableCollection<ProofAttributeInfo> SendableRequestAttributes
        {
            get => _sendableRequestAttributes;
            set => SetProperty(ref _sendableRequestAttributes, value);
        }

        public ObservableCollection<ProofPredicateInfo> SendableRequestPredicates
        {
            get => _sendableRequestPredicates;
            set => SetProperty(ref _sendableRequestPredicates, value);
        }

        public string SendableRequestTitle
        {
            get => _sendableRequestTitle;
            set => SetProperty(ref _sendableRequestTitle, value);
        }

        private ConnectionInvitationMessage _connectionInvitation { get; set; }
        private string _transactionId { get; set; }
        public async void RevealProof()
        {
            TransactionOfferService.State state = await _transactionOfferService.Verify(_transactionId);

            while (state == TransactionOfferService.State.Pending && Pending)
            {
                await Task.Delay(3000);
                state = await _transactionOfferService.Verify(_transactionId);
            }

            switch (state)
            {
                case TransactionOfferService.State.True:
                    QRFrameIsVisible = true;
                    QRVerifyImage = ImageSource.FromFile("big_green_checkmark.png");
                    await LoadRevealedAttributes();
                    MessagingCenter.Send(this, WalletEvents.ReloadConnections);
                    break;
                case TransactionOfferService.State.False:
                    QRFrameIsVisible = true;
                    QRVerifyImage = ImageSource.FromFile("big_red_cross.png");
                    break;
                case TransactionOfferService.State.NoProof:
                    QRFrameIsVisible = true;
                    QRVerifyImage = ImageSource.FromFile("big_red_cross.png");
                    break;
                default:
                    QRFrameIsVisible = true;
                    QRVerifyImage = ImageSource.FromFile("big_red_cross.png");
                    break;
            }
        }

        private async Task ExecuteLoadTransactionCommand()
        {
            (Agent.Models.CustomConnectionInvitationMessage connectionInvitationMessage, ConnectionRecord
                connectionRecord, Agent.Models.TransactionRecord transactionRecord) transaction =
                    await _transactionOfferService.CreateTransaction(_proofRequest);
            _connectionInvitation = transaction.connectionInvitationMessage;
            _transactionId = transaction.transactionRecord.Id;

            string barcodeValue = "https://mobile.proof?t_o="
                                  + _transactionId
                                  + "&c_i="
                                  + _connectionInvitation.ToJson().ToBase64()
                                  + "&waitconnection=true"
                                  + "&waitproof=true";
            BarcodeValue = barcodeValue;
        }
        private async Task LoadRevealedAttributes()
        {
            Debug.Write("LoadRevealedAttributes");
            (Newtonsoft.Json.Linq.JToken presentedCredentials, string proofRecordId) =
                await _transactionOfferService.GetPresentedCredentials(_transactionId);
            CredentialHistoryElements usedCredentials = new CredentialHistoryElements
            {
                ProofRecordId = proofRecordId,
                CredentialRecordIds = new List<string>(),
                ConnectionRecord = null,
                RevealedClaims = new List<CredentialClaim>(),
                NonRevealedClaims = new List<CredentialClaim>(),
                SelfAttestedClaims = new List<CredentialClaim>(),
                PredicateClaims = new List<CredentialClaim>()
            };

            Dictionary<string, Dictionary<string, string>> revealedAttributes = presentedCredentials["revealed_attrs"]
                .ToObject<Dictionary<string, Dictionary<string, string>>>();
            ProofElementsModel revealedPageItem = new ProofElementsModel();
            revealedPageItem.IsGroup = false;
            revealedPageItem.RevealedClaims = new ObservableCollection<CredentialClaim>();
            foreach (string key in revealedAttributes.Keys)
            {
                if (key == "embeddedImage")
                {
                    try
                    {
                        string portraitString = revealedAttributes[key]["raw"];
                        revealedPageItem.RevealedImage = Convert.FromBase64String(portraitString);
                        CredentialClaim newClaim = new CredentialClaim
                        { Name = key, Value = revealedAttributes[key]["raw"] };
                        usedCredentials.RevealedClaims.Add(newClaim);
                        revealedPageItem.IsRevealedImageVisible = true;
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
                else if (key == "embeddedDocument")
                {
                    revealedPageItem.IsRevealedDocumentVisible = true;
                    revealedPageItem.RevealedDocument = revealedAttributes[key]["raw"];
                    CredentialClaim newClaim = new CredentialClaim { Name = key, Value = revealedAttributes[key]["raw"] };
                    usedCredentials.RevealedClaims.Add(newClaim);
                }
                else
                {
                    CredentialClaim newClaim = new CredentialClaim { Name = key, Value = revealedAttributes[key]["raw"] };
                    revealedPageItem.RevealedClaims.Add(newClaim);
                    revealedPageItem.IsRevealedClaimsVisible = true;
                    usedCredentials.RevealedClaims.Add(newClaim);
                }
            }

            if (revealedPageItem.RevealedClaims.Any() || revealedPageItem.IsRevealedImageVisible ||
                revealedPageItem.IsRevealedDocumentVisible)
            {
                List<CredentialClaim> temporaryAttributes =
                    revealedPageItem.RevealedClaims.OrderBy(x => x.Name).ToList();
                revealedPageItem.RevealedClaims = new ObservableCollection<CredentialClaim>();
                foreach (CredentialClaim attribute in temporaryAttributes)
                {
                    revealedPageItem.RevealedClaims.Add(attribute);
                }

                Revealed.Add(revealedPageItem);
            }

            Dictionary<string, RevealedGroupedAttribute> revealedAttributeGroups =
                presentedCredentials["revealed_attr_groups"].ToObject<Dictionary<string, RevealedGroupedAttribute>>();
            List<(string, ProofElementsModel)> temporaryAttributeGroups = new List<(string, ProofElementsModel)>();
            foreach ((string key, RevealedGroupedAttribute value) in revealedAttributeGroups)
            {
                ProofElementsModel newItem = new ProofElementsModel();
                newItem.IsGroup = true;
                newItem.RevealedClaims = new ObservableCollection<CredentialClaim>();
                foreach ((string valueKey, Dictionary<string, string> valueValue) in value.values)
                {
                    if (valueKey == "embeddedImage")
                    {
                        try
                        {
                            string portraitString = valueValue["raw"];
                            newItem.RevealedImage = Convert.FromBase64String(portraitString);
                            CredentialClaim newClaim = new CredentialClaim { Name = valueKey, Value = valueValue["raw"] };
                            usedCredentials.RevealedClaims.Add(newClaim);
                            newItem.IsRevealedImageVisible = true;
                        }
                        catch (Exception)
                        {
                            //ignore
                        }
                    }
                    else if (valueKey == "embeddedDocument")
                    {
                        newItem.IsRevealedDocumentVisible = true;
                        newItem.RevealedDocument = valueValue["raw"];
                        CredentialClaim newClaim = new CredentialClaim { Name = valueKey, Value = valueValue["raw"] };
                        usedCredentials.RevealedClaims.Add(newClaim);
                    }
                    else
                    {
                        CredentialClaim newClaim = new CredentialClaim { Name = valueKey, Value = valueValue["raw"] };
                        newItem.RevealedClaims.Add(newClaim);
                        newItem.IsRevealedClaimsVisible = true;
                        usedCredentials.RevealedClaims.Add(newClaim);
                    }
                }

                List<CredentialClaim> temporaryAttributes = newItem.RevealedClaims.OrderBy(x => x.Name).ToList();
                newItem.RevealedClaims = new ObservableCollection<CredentialClaim>();
                foreach (CredentialClaim claim in temporaryAttributes)
                {
                    newItem.RevealedClaims.Add(claim);
                }

                temporaryAttributeGroups.Add((key, newItem));
            }

            temporaryAttributeGroups = temporaryAttributeGroups.OrderBy(x => x.Item1).ToList();

            foreach ((string key, ProofElementsModel item) in temporaryAttributeGroups)
            {
                Revealed.Add(item);
            }

            if (Revealed.Any())
            {
                IsRevealedStackVisible = true;
                IsRevealedLabelVisible = true;
            }

            Dictionary<string, Dictionary<string, string>> nonRevealedAttributes =
                presentedCredentials["unrevealed_attrs"].ToObject<Dictionary<string, Dictionary<string, string>>>();
            foreach (string key in nonRevealedAttributes.Keys)
            {
                CredentialClaim newClaim = new CredentialClaim { Name = key, Value = nonRevealedAttributes[key]["raw"] };
                usedCredentials.NonRevealedClaims.Add(newClaim);
            }

            Dictionary<string, Dictionary<string, string>> selfAttestedAttributes =
                presentedCredentials["self_attested_attrs"].ToObject<Dictionary<string, Dictionary<string, string>>>();
            foreach (string key in selfAttestedAttributes.Keys)
            {
                CredentialClaim newClaim = new CredentialClaim { Name = key, Value = selfAttestedAttributes[key]["raw"] };
                usedCredentials.SelfAttestedClaims.Add(newClaim);
            }

            Dictionary<string, Dictionary<string, string>> predicates = presentedCredentials["predicates"]
                .ToObject<Dictionary<string, Dictionary<string, string>>>();
            foreach (string key in predicates.Keys)
            {
                CredentialClaim newClaim = new CredentialClaim { Name = key, Value = predicates[key]["raw"] };
                usedCredentials.PredicateClaims.Add(newClaim);
            }

            usedCredentials.RevealedClaims = usedCredentials.RevealedClaims.OrderBy(x => x.Name).ToList();
            usedCredentials.NonRevealedClaims = usedCredentials.NonRevealedClaims.OrderBy(x => x.Name).ToList();
            usedCredentials.PredicateClaims = usedCredentials.PredicateClaims.OrderBy(x => x.Name).ToList();
            usedCredentials.SelfAttestedClaims = usedCredentials.SelfAttestedClaims.OrderBy(x => x.Name).ToList();

            Hyperledger.Aries.Agents.IAgentContext agentContext = await _agentProvider.GetContextAsync();
            ProofRecord proofRecord =
                await _walletRecordService.GetAsync<ProofRecord>(agentContext.Wallet, proofRecordId);
            proofRecord.SetTag(WalletParams.HistoryCredentialsTag, JsonConvert.SerializeObject(usedCredentials));
            await _walletRecordService.UpdateAsync(agentContext.Wallet, proofRecord);
        }

        private void OpenPdfButtonClicked(string documentString)
        {
            App.ViewFile(documentString);
        }

        public class RevealedGroupedAttribute
        {
            public int sub_proof_index { get; set; }
            public Dictionary<string, Dictionary<string, string>> values { get; set; }
        }
    }
}