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
    public class VacOfferMessage : INotifyPropertyChanged
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

        private bool _qrCodesVisible;
        public bool QrCodesVisible
        {
            get => _qrCodesVisible;
            set => SetProperty(ref _qrCodesVisible, value);
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

        public VacOfferMessage(ConnectionRecord connectionRecord, CredentialRecord credentialRecord)
        {
            InfoStackIsVisible = false;
            CredentialName = credentialRecord.CredentialDefinitionId.Split(':')[4];
            ConnectionAlias = connectionRecord.Alias.Name;
            ConnectionImage = string.IsNullOrEmpty(connectionRecord.Alias.ImageUrl)
                ? ImageSource.FromFile("default_logo.png")
                : ImageSource.FromUri(new Uri(connectionRecord.Alias.ImageUrl));
            MappedConnectionAlias = connectionRecord.Alias.Name;

            Attributes = new ObservableCollection<CredentialPreviewAttribute>();

            ObservableCollection<CredentialPreviewAttribute> tmpAttributes = new ObservableCollection<CredentialPreviewAttribute>();
            foreach (var attrib in credentialRecord.CredentialAttributesValues)
            {
                tmpAttributes.Add(attrib);
            }
            ObservableCollection<string> orderedAttributeNames = new ObservableCollection<string> { "nam_gn", "nam_fn", "dob", "vac_tg", "vac_vp", "vac_ma", "vac_dn", "vac_sd", "vac_dt", "vac_co", "vac_is", "vac_ci", "nam_fnt", "nam_gnt", "ver", "vac_mp" };
            foreach (string attributeName in orderedAttributeNames)
            {
                CredentialPreviewAttribute attribute = tmpAttributes.FirstOrDefault(x => x.Name.ToLower().Equals(attributeName));
                if (attribute != null)
                {
                    Attributes.Add(attribute);
                    tmpAttributes.Remove(attribute);
                }
            }

            QrCodes = new ObservableCollection<CredentialPreviewAttribute>();

            foreach (CredentialPreviewAttribute credentialPreviewAttribute in tmpAttributes)
            {
                if (!credentialPreviewAttribute.Name.Equals("dgc"))
                {
                    Attributes.Add(credentialPreviewAttribute);
                }
                else
                {
                    QrCodes.Add(credentialPreviewAttribute);
                }
            }
            if (!QrCodes.Count.Equals(0))
            {
                QrCodesVisible = true;
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
