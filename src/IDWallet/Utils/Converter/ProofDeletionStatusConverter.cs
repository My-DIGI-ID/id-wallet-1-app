using IDWallet.Models;
using System;
using System.Globalization;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    public class ProofDeletionStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string parameterAsString = "";
            DeletionStatus valueAsStatus = DeletionStatus.Undeletable;
            try
            {
                parameterAsString = parameter as string;
                valueAsStatus = (DeletionStatus)value;
            }
            catch (Exception)
            {
                //ignore
            }

            if (parameterAsString == "VisibleWhenDeletable")
            {
                if (valueAsStatus == DeletionStatus.Deletable)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (parameterAsString == "VisibleWhenUndeletable")
            {
                if (valueAsStatus == DeletionStatus.Undeletable)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (parameterAsString == "VisibleWhenDeleted")
            {
                if (valueAsStatus == DeletionStatus.Deleted)
                {
                    return true;
                }
                else
                {
                    return false;
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