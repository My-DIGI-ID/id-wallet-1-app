using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace IDWallet.Views.Customs.Pages
{
    public class CustomPage : ContentPage, INotifyPropertyChanged
    {
        private bool _elementsEnabled;

        public new event PropertyChangedEventHandler PropertyChanged;

        public bool ElementsEnabled
        {
            get => _elementsEnabled;
            set => SetProperty(ref _elementsEnabled, value);
        }

        protected void DisableAll()
        {
            ElementsEnabled = false;
        }

        protected void EnableAll()
        {
            ElementsEnabled = true;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            EnableAll();
        }

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = "")
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