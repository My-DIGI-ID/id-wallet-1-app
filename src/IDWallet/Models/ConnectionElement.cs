using Hyperledger.Aries.Features.PresentProof;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace IDWallet.Models
{
    public class ConnectionDetailsCredential : ConnectionElement
    {
        public List<CredentialContentAttribute> Attributes { get; set; }
        public string DocumentString { get; set; } = "";
        public byte[] EmbeddedByteArray { get; set; }
        public bool HasDocument { get; set; } = false;
    }

    public class ConnectionDetailsPresentation : ConnectionElement
    {
        private ProofRecord _proofRecord;

        public ObservableCollection<CredentialClaim> NonRevealedAttributes { get; set; }

        public ObservableCollection<CredentialClaim> Predicates { get; set; }

        public ProofRecord ProofRecord
        {
            get => _proofRecord;
            set => SetProperty(ref _proofRecord, value);
        }

        public ObservableCollection<CredentialClaim> RevealedAttributes { get; set; }
        public ObservableCollection<CredentialClaim> SelfAttested { get; set; }
    }

    public class ConnectionElement : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ImageSource ImageUri { get; set; }
        public string RecordId { get; set; }
        public string State { get; set; }
        public string Title { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
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