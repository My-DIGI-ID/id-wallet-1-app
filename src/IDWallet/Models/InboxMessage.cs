using Hyperledger.Aries.Features.DidExchange;
using Hyperledger.Aries.Features.PresentProof;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace IDWallet.Models
{
    public abstract class InboxMessage : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string ConnectionAlias { get; set; }
        public ConnectionRecord ConnectionRecord { get; set; }
        public DateTime? CreatedAtUtc { get; set; }
        public string Description { get; set; }
        public ImageSource MessageImageSource { get; set; }
        public ProofRecord ProofRecord { get; set; }
        public string RecordId { get; set; }
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