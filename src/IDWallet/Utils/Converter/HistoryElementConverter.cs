using IDWallet.Models;
using System;
using System.Globalization;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    public class HistoryElementConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is HistorySubElement)
            {
                if (value is HistoryCredentialElement)
                {
                    return Resources.Lang.CredentialHistoryListElement_From;
                }
                else
                {
                    return Resources.Lang.CredentialHistoryListElement_To;
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
