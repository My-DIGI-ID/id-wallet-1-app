using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDWallet.Models
{
    public class Language : INotifyPropertyChanged
    {
        private string _iconSource;
        public event PropertyChangedEventHandler PropertyChanged;

        public string CultureCode { get; set; }
        public string IconSource
        {
            get => _iconSource;
            set => SetProperty(ref _iconSource, value);
        }

        public string Name { get; set; }
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