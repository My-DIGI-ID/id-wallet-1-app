using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IDWallet.Models
{
    public abstract class CustomObservableModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetProperty<TType>(ref TType property, TType value,
            [CallerMemberName] string propertyName = null)
        {
            property = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}