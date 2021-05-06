using System;
using System.Globalization;
using System.IO;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    internal class EmbeddedImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is byte[])
                {
                    MemoryStream newStream = new MemoryStream(value as byte[]);
                    return (ImageSource.FromStream(() => newStream));
                }
                else if (value is string)
                {
                    byte[] byteStream = System.Convert.FromBase64String(value as string);
                    MemoryStream newStream = new MemoryStream(byteStream);
                    return (ImageSource.FromStream(() => newStream));
                }
            }
            catch (Exception ex)
            {
                //ignore
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}