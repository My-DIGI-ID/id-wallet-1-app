using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.IssueCredential;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace IDWallet.Models
{
    public class BaseIdOfferMessage : INotifyPropertyChanged
    {
        private ObservableCollection<CredentialPreviewAttribute> _attributes;
        public ObservableCollection<CredentialPreviewAttribute> Attributes
        {
            get => _attributes;
            set => SetProperty(ref _attributes, value);
        }

        private ObservableCollection<CredentialPreviewAttribute> _qrCodes;
        public ObservableCollection<CredentialPreviewAttribute> QrCodes
        {
            get => _qrCodes;
            set => SetProperty(ref _qrCodes, value);
        }

        private bool _infoStackIsVisible;
        public bool InfoStackIsVisible
        {
            get => _infoStackIsVisible;
            set => SetProperty(ref _infoStackIsVisible, value);
        }

        private string _connectionAlias;
        public string ConnectionAlias
        {
            get => _connectionAlias;
            set => SetProperty(ref _connectionAlias, value);
        }

        private string _mappedConnectionAlias;
        public string MappedConnectionAlias
        {
            get => _mappedConnectionAlias;
            set => SetProperty(ref _mappedConnectionAlias, value);
        }

        private string _credentialName;
        public string CredentialName
        {
            get => _credentialName;
            set => SetProperty(ref _credentialName, value);
        }

        private ImageSource _connectionImage;
        public ImageSource ConnectionImage
        {
            get => _connectionImage;
            set => SetProperty(ref _connectionImage, value);
        }

        public BaseIdOfferMessage(ConnectionRecord connectionRecord, CredentialRecord credentialRecord)
        {
            InfoStackIsVisible = false;
            CredentialName = credentialRecord.CredentialDefinitionId.Split(':')[4];
            ConnectionAlias = connectionRecord.Alias.Name;
            ConnectionImage = string.IsNullOrEmpty(connectionRecord.Alias.ImageUrl)
                ? ImageSource.FromFile("default_logo.png")
                : ImageSource.FromUri(new Uri(connectionRecord.Alias.ImageUrl));
            MappedConnectionAlias = connectionRecord.Alias.Name;
            switch (MappedConnectionAlias)
            {
                case "Bundesdruckerei":
                    MappedConnectionAlias = "Bundesdruckerei GmbH";
                    break;
                default:
                    break;
            }
            Attributes = new ObservableCollection<CredentialPreviewAttribute>();

            ObservableCollection<CredentialPreviewAttribute> tmpAttributes = new ObservableCollection<CredentialPreviewAttribute>();
            foreach (CredentialPreviewAttribute attrib in credentialRecord.CredentialAttributesValues)
            {
                tmpAttributes.Add(attrib);
            }
            ObservableCollection<string> orderedAttributeNames = new ObservableCollection<string> { "firstname", "familyname", "birthname", "academictitle", "addressstreet", "addresszipcode", "addresscity", "addresscountry", "dateofbirth", "placeofbirth", "dateofexpiry", "documenttype", "pseudonym" };
            foreach (string attributeName in orderedAttributeNames)
            {
                CredentialPreviewAttribute attribute = tmpAttributes.FirstOrDefault(x => x.Name.ToLower().Equals(attributeName));
                if (attribute != null)
                {
                    Attributes.Add(attribute);
                    tmpAttributes.Remove(attribute);
                }
            }

            foreach (CredentialPreviewAttribute credentialPreviewAttribute in tmpAttributes)
            {
                Attributes.Add(credentialPreviewAttribute);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler changed = PropertyChanged;
            if (changed == null)
            {
                return;
            }

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "",
                                                    Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
            {
                return false;
            }

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
