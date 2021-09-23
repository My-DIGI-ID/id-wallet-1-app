using System;
using System.Globalization;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    public class OnboardingProgressBarConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int valueAsInt = (int)value;
            int parameterAsInt = int.Parse(parameter as string);
            if (parameterAsInt == valueAsInt)
            {
                return (Color)Application.Current.Resources["OnboardingIndicatorSelectedColor"];
            }
            else
            {
                return (Color)Application.Current.Resources["OnboardingIndicatorDefaultColor"];
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
