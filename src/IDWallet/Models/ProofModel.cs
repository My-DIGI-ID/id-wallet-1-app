using Hyperledger.Aries.Features.PresentProof;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDWallet.Models
{
    public class ProofElementOption : INotifyPropertyChanged
    {
        private ObservableCollection<CredentialClaim> _attributes = new ObservableCollection<CredentialClaim>();
        private string _iconSource = "mdi-checkbox-blank-circle-outline";
        private bool _showInfo = false;
        private bool _showSeparator = true;
        private bool _isSelfAttested = false;
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<CredentialClaim> Attributes
        {
            get => _attributes;
            set => SetProperty(ref _attributes, value);
        }

        public string ConnectionAlias { get; set; }
        public string CopyCounter { get; set; }
        public WalletElement WalletElement { get; set; }
        public string IconSource
        {
            get => _iconSource;
            set => SetProperty(ref _iconSource, value);
        }

        public bool ShowInfo
        {
            get => _showInfo;
            set => SetProperty(ref _showInfo, value);
        }

        public bool ShowSeparator
        {
            get => _showSeparator;
            set => SetProperty(ref _showSeparator, value);
        }

        public bool IsSelfAttested
        {
            get => _isSelfAttested;
            set => SetProperty(ref _isSelfAttested, value);
        }

        public string Value { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler changed = PropertyChanged;
            if (changed == null)
            {
                return;
            }

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
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

    public class ProofModel : INotifyPropertyChanged
    {
        private string _documentString;
        private bool _hasEmbeddedDocument;
        private bool _isEmbeddedImage;
        private bool _isRegular;
        private bool _isSelected;
        private bool _needToShow;
        private bool _onlyOneOption;
        private byte[] _portraitByteArray;
        private bool _revoked;
        private ProofElementOption _selectedOption;
        private string _selectedValue;
        public event PropertyChangedEventHandler PropertyChanged;

        public string DictionaryKey { get; set; }
        public string DocumentString
        {
            get => _documentString;
            set => SetProperty(ref _documentString, value);
        }

        public bool HasEmbeddedDocument
        {
            get => _hasEmbeddedDocument;
            set => SetProperty(ref _hasEmbeddedDocument, value);
        }

        public bool ImageVisibility { get; set; }
        public bool IsEmbeddedImage
        {
            get => _isEmbeddedImage;
            set => SetProperty(ref _isEmbeddedImage, value);
        }

        public bool IsRegular
        {
            get => _isRegular;
            set => SetProperty(ref _isRegular, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsSelfAttested { get; set; }
        public List<ProofElementOption> ProofElementOptions { get; set; }


        public bool NeedToShow
        {
            get => _needToShow;
            set => SetProperty(ref _needToShow, value);
        }

        public bool OnlyOneOption
        {
            get => _onlyOneOption;
            set => SetProperty(ref _onlyOneOption, value);
        }

        public byte[] PortraitByteArray
        {
            get => _portraitByteArray;
            set => SetProperty(ref _portraitByteArray, value);
        }

        public string RequestedValue { get; set; }

        public List<AttributeFilter> Restrictions { get; set; }

        public bool Revoked
        {
            get => _revoked;
            set => SetProperty(ref _revoked, value);
        }
        public ProofElementOption SelectedOption
        {
            get => _selectedOption;
            set => SetProperty(ref _selectedOption, value);
        }

        public string SelectedValue
        {
            get => _selectedValue;
            set => SetProperty(ref _selectedValue, value);
        }
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler changed = PropertyChanged;
            if (changed == null)
            {
                return;
            }

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
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