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
                    case "lastname":
                    case "nam_fn":
                        return Lang.LabelTextConvert_familyName;
                    case "placeofbirth":
                        return Lang.LabelTextConvert_placeOfBirth;
                    case "birthname":
                        return Lang.LabelTextConvert_birthName;
                    case "firstname":
                    case "nam_gn":
                        return Lang.LabelTextConvert_firstName;
                    case "dateofbirth":
                    case "dob":
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
                    case "firmname":
                        return Lang.LabelTextConvert_firmName;
                    case "firmsubject":
                        return Lang.LabelTextConvert_firmSubject;
                    case "firmstreet":
                        return Lang.LabelTextConvert_firmStreet;
                    case "firmcity":
                        return Lang.LabelTextConvert_firmCity;
                    case "firmpostalcode":
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
                    case "vac_co":
                        return Lang.LabelTextConvert_CountryOfVaccination;
                    case "vac_dt":
                        return Lang.LabelTextConvert_DateOfVaccination;
                    case "vac_tg":
                        return Lang.LabelTextConvert_DeseaseTargeted;
                    case "vac_dn":
                        return Lang.LabelTextConvert_DoseNumber;
                    case "vac_ma":
                        return Lang.LabelTextConvert_Manufacturer;
                    case "vac_sd":
                        return Lang.LabelTextConvert_TotalDoseNumber;
                    case "vac_ci":
                        return Lang.LabelTextConvert_Uvci;
                    case "vac_vp":
                        return Lang.LabelTextConvert_Vaccine;
                    case "nam_fnt":
                        return Lang.LabelTextConvert_StandardisedFamilyName;
                    case "nam_gnt":
                        return Lang.LabelTextConvert_StandardisedGivenName;
                    case "ver":
                        return Lang.LabelTextConvert_SchemaVersion;
                    case "vac_mp":
                        return Lang.LabelTextConvert_ProductDesc;
                    case "vac_is":
                        return Lang.LabelTextConvert_Issuer;
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
