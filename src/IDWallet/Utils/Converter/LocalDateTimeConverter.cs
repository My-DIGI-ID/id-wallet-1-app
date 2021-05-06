using System;
using System.Globalization;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    public class LocalDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                CultureInfo currentUICulture = CultureInfo.CurrentUICulture ?? new CultureInfo("en-GB", false);
                dateTime = dateTime.ToLocalTime();
                if (parameter as string == "Date")
                {
                    return dateTime.ToString(currentUICulture.DateTimeFormat.ShortDatePattern);
                }
                else if (parameter as string == "Time")
                {
                    return dateTime.ToString(currentUICulture.DateTimeFormat.ShortTimePattern);
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToUniversalTime();
            }

            return null;
        }
    }
}