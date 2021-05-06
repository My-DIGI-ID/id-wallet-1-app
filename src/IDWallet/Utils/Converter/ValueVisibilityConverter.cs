using System;
using System.Globalization;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    public class ValueVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                if ((string)value == "embeddedImage" || (string)value == "embeddedDocument")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                if ((string)value == (string)parameter)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
