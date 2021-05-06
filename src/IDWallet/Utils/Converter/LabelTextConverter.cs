using IDWallet.Resources;
using System;
using System.Globalization;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    public class LabelTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                string valueAsString = value as string;
                switch (valueAsString.ToLower())
                {
                    case "addresscity":
                        return Lang.LabelTextConvert_addressCity;
                    case "familyname":
                        return Lang.LabelTextConvert_familyName;
                    case "placeofbirth":
                        return Lang.LabelTextConvert_placeOfBirth;
                    case "birthname":
                        return Lang.LabelTextConvert_birthName;
                    case "firstname":
                        return Lang.LabelTextConvert_firstName;
                    case "dateofbirth":
                        return Lang.LabelTextConvert_dateOfBirth;
                    case "addressstreet":
                        return Lang.LabelTextConvert_addressStreet;
                    case "addresscountry":
                        return Lang.LabelTextConvert_addressCountry;
                    case "dateofexpiry":
                        return Lang.LabelTextConvert_dateOfExpiry;
                    case "academictitle":
                        return Lang.LabelTextConvert_academicTitle;
                    case "addresszipcode":
                        return Lang.LabelTextConvert_addressZipCode;
                    case "firmName":
                        return Lang.LabelTextConvert_firmName;
                    case "firmSubject":
                        return Lang.LabelTextConvert_firmSubject;
                    case "firmStreet":
                        return Lang.LabelTextConvert_firmStreet;
                    case "firmCity":
                        return Lang.LabelTextConvert_firmCity;
                    case "firmPostalcode":
                        return Lang.LabelTextConvert_firmPostalcode;
                    case "doctoraldegree":
                        return Lang.LabelTextConvert_DoctoralDegree;
                    case "givennames":
                        return Lang.LabelTextConvert_GivenNames;
                    case "address":
                        return Lang.LabelTextConvert_Address;
                    case "validuntil":
                        return Lang.LabelTextConvert_ValidUntil;
                    case "nationality":
                        return Lang.LabelTextConvert_Nationality;
                    case "pseudonym":
                        return Lang.LabelTextConvert_Pseudonym;
                    case "documenttype":
                        return Lang.LabelTextConvert_Documenttype;
                    default:
                        return value;
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
