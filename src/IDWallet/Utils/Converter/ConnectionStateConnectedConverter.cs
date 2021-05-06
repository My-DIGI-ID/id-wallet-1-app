using Hyperledger.Aries.Features.DidExchange;
using System;
using System.Globalization;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    public class ConnectionStateConnectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((ConnectionState)value == ConnectionState.Connected)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}