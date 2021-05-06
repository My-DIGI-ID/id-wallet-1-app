using System;
using System.Globalization;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    public class BoolToChevronConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return "mdi-chevron-up";
            }
            else
            {
                return "mdi-chevron-down";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}