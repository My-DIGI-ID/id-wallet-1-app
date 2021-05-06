using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDWallet.Models
{
    public class HistoryElement : INotifyPropertyChanged
    {
        private ObservableCollection<HistorySubElement> _historySubElements;

        private DateTime _date;

        private string _name;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<HistorySubElement> HistorySubElements
        {
            get => _historySubElements;
            set => SetProperty(ref _historySubElements, value);
        }

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
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
