using IDWallet.Models;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries.Features.PresentProof;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Proof.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProofMissingCredentialsPopUp : CustomPopUp
    {
        private readonly List<ProofModel> _failedRequests;
        private readonly ProofRequest _proofRequest;
        public string ProofName { get; set; }
        public ObservableCollection<MissingCredential> RequiredCredentials { get; set; }

        private Command<MissingCredential> _infoTappedCommand;
        public Command<MissingCredential> InfoTappedCommand =>
            _infoTappedCommand ?? (_infoTappedCommand = new Command<MissingCredential>(OnInfoTapped));

        public ProofMissingCredentialsPopUp(ProofRequest proofRequest, List<ProofModel> failedRequests)
        {
            InitializeComponent();

            _failedRequests = failedRequests;
            _proofRequest = proofRequest;
            ProofName = proofRequest.Name;
            RequiredCredentials = new ObservableCollection<MissingCredential>();
            List<string> allFailedKeys = new List<string>();

            foreach (ProofModel failedRequest in failedRequests)
            {
                allFailedKeys.Add(failedRequest.DictionaryKey);
            }

            foreach (KeyValuePair<string, ProofAttributeInfo> requestedAttribute in proofRequest.RequestedAttributes)
            {
                if (allFailedKeys.Contains(requestedAttribute.Key))
                {
                    foreach (AttributeFilter restriction in requestedAttribute.Value.Restrictions)
                    {
                        if (!string.IsNullOrEmpty(restriction.SchemaId))
                        {
                            string name = restriction.SchemaId.Split(':')[2];
                            if (!CheckRequestedAttributes(name))
                            {
                                ObservableCollection<string> attributeList = new ObservableCollection<string>();
                                foreach (ProofModel request in _failedRequests)
                                {
                                    if (request.Restrictions.Exists(x => !string.IsNullOrEmpty(x.SchemaId) && x.SchemaId.Split(':')[2] == name))
                                    {
                                        attributeList.Add(request.RequestedValue);
                                    }
                                }
                                RequiredCredentials.Add(new MissingCredential { Name = name, AttributeList = attributeList, InfoOpen = false });
                            }
                        }
                        else if (!string.IsNullOrEmpty(restriction.CredentialDefinitionId))
                        {
                            string name = restriction.CredentialDefinitionId.Split(':')[4];
                            if (!CheckRequestedAttributes(name))
                            {
                                ObservableCollection<string> attributeList = new ObservableCollection<string>();
                                foreach (ProofModel request in _failedRequests)
                                {
                                    if (request.Restrictions.Exists(x => !string.IsNullOrEmpty(x.CredentialDefinitionId) && x.CredentialDefinitionId.Split(':')[4] == name))
                                    {
                                        attributeList.Add(request.RequestedValue);
                                    }
                                }
                                RequiredCredentials.Add(new MissingCredential { Name = name, AttributeList = attributeList, InfoOpen = false });
                            }
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, ProofPredicateInfo> requestedPredicate in proofRequest.RequestedPredicates)
            {
                if (allFailedKeys.Contains(requestedPredicate.Key))
                {
                    foreach (AttributeFilter restriction in requestedPredicate.Value.Restrictions)
                    {
                        if (!string.IsNullOrEmpty(restriction.SchemaId))
                        {
                            string name = restriction.SchemaId.Split(':')[2];
                            if (!CheckRequestedAttributes(name))
                            {
                                ObservableCollection<string> attributeList = new ObservableCollection<string>();
                                foreach (ProofModel request in _failedRequests)
                                {
                                    if (request.Restrictions.Find(x => !string.IsNullOrEmpty(x.SchemaId) && x.SchemaId == restriction.SchemaId.Split(':')[2]) != null)
                                    {
                                        attributeList.Add(request.RequestedValue);
                                    }
                                }
                                RequiredCredentials.Add(new MissingCredential { Name = name, AttributeList = attributeList, InfoOpen = false });
                            }
                        }
                        else if (!string.IsNullOrEmpty(restriction.CredentialDefinitionId))
                        {
                            string name = restriction.CredentialDefinitionId.Split(':')[4];
                            if (!CheckRequestedAttributes(name))
                            {
                                ObservableCollection<string> attributeList = new ObservableCollection<string>();
                                foreach (ProofModel request in _failedRequests)
                                {
                                    if (request.Restrictions.Find(x => !string.IsNullOrEmpty(x.CredentialDefinitionId) && x.CredentialDefinitionId == restriction.CredentialDefinitionId.Split(':')[4]) != null)
                                    {
                                        attributeList.Add(request.RequestedValue);
                                    }
                                }
                                RequiredCredentials.Add(new MissingCredential { Name = name, AttributeList = attributeList, InfoOpen = false });
                            }
                        }
                    }
                }
            }
            BindingContext = this;
        }

        private async void OnInfoTapped(MissingCredential missingCredential)
        {
            missingCredential.InfoOpen = !missingCredential.InfoOpen;
        }

        private bool CheckRequestedAttributes(string schemaName)
        {
            foreach (MissingCredential missingCredential in RequiredCredentials)
            {
                if (missingCredential.Name == schemaName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}