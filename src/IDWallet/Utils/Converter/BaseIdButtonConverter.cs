using System;
using System.Globalization;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    public class BaseIdButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return 1;
            }
            else
            {
                return 0.2;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
