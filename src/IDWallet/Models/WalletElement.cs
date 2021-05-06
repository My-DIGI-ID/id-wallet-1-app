using Hyperledger.Aries.Features.IssueCredential;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace IDWallet.Models
{
    public enum DeletionStatus
    {
        Deletable,
        Deleted,
        Undeletable
    }

    public class WalletElement : INotifyPropertyChanged
    {
        private ObservableCollection<HistoryProofElement> _historyItems;
        private bool _isHistoryOpen;
        private bool _isHistorySet;
        private bool _isInfoOpen;
        private bool _revoked;
        private bool _hasDocument = false;
        private bool _hasImage = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<CredentialClaim> Claims { get; set; }
        public Color CredentialBarColor { get; set; }
        public ImageSource CredentialImageSource { get; set; }
        public CredentialRecord CredentialRecord { get; set; }
        public string DocumentString { get; set; }
        public bool HasDocument
        {
            get => _hasDocument;
            set => SetProperty(ref _hasDocument, value);
        }
        public ObservableCollection<HistoryProofElement> HistoryItems
        {
            get => _historyItems;
            set => SetProperty(ref _historyItems, value);
        }

        public ImageSource ImageUri { get; set; }
        public bool IsHistoryOpen
        {
            get => _isHistoryOpen;
            set => SetProperty(ref _isHistoryOpen, value);
        }

        public bool IsHistorySet 
        { 
            get => _isHistorySet; 
            set => SetProperty(ref _isHistorySet, value); 
        }

        public bool IsInfoOpen
        {
            get => _isInfoOpen;
            set => SetProperty(ref _isInfoOpen, value);
        }

        public string IssuedBy { get; set; }
        public string Name { get; set; }
        public byte[] PortraitByteArray { get; set; }
        public bool HasImage
        {
            get => _hasImage;
            set => SetProperty(ref _hasImage, value);
        }
        public bool Revoked
        {
            get => _revoked;
            set
            {
                SetProperty(ref _revoked, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CredentialsPageItem"));
            }
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