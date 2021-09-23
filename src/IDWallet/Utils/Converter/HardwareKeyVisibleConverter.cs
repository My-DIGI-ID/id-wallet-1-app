using IDWallet.Models;
using System;
using System.Globalization;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    public class HardwareKeyVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                foreach (CredentialClaim credentialClaim in ((ProofElementOption)value).Attributes)
                {
                    if (credentialClaim.Name == WalletParams.HardwareSignature)
                    {
                        return true;
                    }
                    else if (credentialClaim.Name == WalletParams.HardwareSignatureDdl)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
