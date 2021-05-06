using Hyperledger.Aries.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDWallet.Models
{
    public class SwitchLedgerElement : INotifyPropertyChanged
    {
        private string _iconSource;
        private bool _isActive;
        private AgentOptions _options;
        private string _poolName;

        public SwitchLedgerElement(AgentOptions agentOptions, bool isActive, string poolName, string iconSource)
        {
            Options = agentOptions;
            IsActive = isActive;
            PoolName = poolName;
            IconSource = iconSource;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string IconSource
        {
            get => _iconSource;
            set => SetProperty(ref _iconSource, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public AgentOptions Options
        {
            get => _options;
            set => SetProperty(ref _options, value);
        }

        public string PoolName
        {
            get => _poolName;
            set => SetProperty(ref _poolName, value);
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