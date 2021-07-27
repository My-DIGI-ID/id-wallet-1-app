using IDWallet.Models;
using IDWallet.Resources;
using System;
using System.Globalization;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    class AttributeValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                switch (value)
                {
                    case "840539006":
                        return Lang.AttributeValueConvert_Covid19;

                    case "1119305005":
                        return Lang.AttributeValueConvert_Antigen_Vaccine;
                    case "1119349007":
                        return Lang.AttributeValueConvert_Mrna_Vaccine;
                    case "J07BX03":
                        return Lang.AttributeValueConvert_Misc_Vaccine;

                    case "ORG-100001699":
                        return Lang.AttributeValueConvert_Astra_Zeneca;
                    case "ORG-100030215":
                        return Lang.AttributeValueConvert_Biontech;
                    case "ORG-100001417":
                        return Lang.AttributeValueConvert_Janssen;
                    case "ORG-100031184":
                        return Lang.AttributeValueConvert_Moderna;
                    case "ORG-100006270":
                        return Lang.AttributeValueConvert_Curevac;
                    case "ORG-100013793":
                        return Lang.AttributeValueConvert_Cansino;
                    case "ORG-100020693":
                        return Lang.AttributeValueConvert_Sinopharm_Int;
                    case "ORG-100010771":
                        return Lang.AttributeValueConvert_Sinopharm_Prague;
                    case "ORG-100024420":
                        return Lang.AttributeValueConvert_Sinopharm_Shenzhen;
                    case "ORG-100032020":
                        return Lang.AttributeValueConvert_Novovax;
                    case "Gamaleya-Research-Institute":
                        return Lang.AttributeValueConvert_Gamaleya;
                    case "Vector-Institute":
                        return Lang.AttributeValueConvert_Vector;
                    case "Sinovac-Biotech":
                        return Lang.AttributeValueConvert_Sinovac;
                    case "Bharat-Biotech":
                        return Lang.AttributeValueConvert_Bharat;

                    case "EU/1/20/1528":
                        return Lang.AttributeValueConvert_Comirnaty_Vacc;
                    case "EU/1/20/1507":
                        return Lang.AttributeValueConvert_Moderna_Vacc;
                    case "EU/1/21/1529":
                        return Lang.AttributeValueConvert_Vaxzevria_Vacc;
                    case "EU/1/20/1525":
                        return Lang.AttributeValueConvert_Jannsen_Vacc;
                    case "CVnCoV":
                        return Lang.AttributeValueConvert_CVnCoV_Vacc;
                    case "NVX-CoV2373":
                        return Lang.AttributeValueConvert_NVX_Vacc;
                    case "Sputnik-V":
                        return Lang.AttributeValueConvert_Sputnik_Vacc;
                    case "Convidecia":
                        return Lang.AttributeValueConvert_Convidecia_Vacc;
                    case "EpiVacCorona":
                        return Lang.AttributeValueConvert_EpiVac_Vacc;
                    case "BBIBP-CorV":
                        return Lang.AttributeValueConvert_BBIBP_Vacc;
                    case "Inactivated-SARS-CoV-2-Vero-Cell":
                        return Lang.AttributeValueConvert_Vero_Cell_Vacc;
                    case "CoronaVac":
                        return Lang.AttributeValueConvert_CoronaVac_Vacc;
                    case "Covaxin":
                        return Lang.AttributeValueConvert_Covaxin_Vacc;
                    default:
                        return value;
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
