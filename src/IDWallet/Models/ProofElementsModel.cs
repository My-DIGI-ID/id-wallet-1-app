using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDWallet.Models
{
    public class ProofElementsModel : INotifyPropertyChanged
    {
        private bool _isGroup;
        private bool _isRevealedClaimsVisible;
        private bool _isRevealedDocumentVisible;
        private bool _isRevealedImageVisible;
        public ProofElementsModel()
        {
            IsRevealedClaimsVisible = false;
            IsRevealedDocumentVisible = false;
            IsRevealedImageVisible = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsGroup
        {
            get => _isGroup;
            set => SetProperty(ref _isGroup, value);
        }

        public bool IsRevealedClaimsVisible
        {
            get => _isRevealedClaimsVisible;
            set => SetProperty(ref _isRevealedClaimsVisible, value);
        }

        public bool IsRevealedDocumentVisible
        {
            get => _isRevealedDocumentVisible;
            set => SetProperty(ref _isRevealedDocumentVisible, value);
        }

        public bool IsRevealedImageVisible
        {
            get => _isRevealedImageVisible;
            set => SetProperty(ref _isRevealedImageVisible, value);
        }

        public ObservableCollection<CredentialClaim> RevealedClaims { get; set; }
        public string RevealedDocument { get; set; }
        public byte[] RevealedImage { get; set; }
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