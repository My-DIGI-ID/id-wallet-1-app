using Autofac;
using IDWallet.Agent.Interface;
using IDWallet.Resources;
using Hyperledger.Aries.Features.IssueCredential;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.Utils.Converter
{
    public class MultiLabelTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string label = "";
            string schemaId = "";

            try
            {
                label = values[0] as string;
                schemaId = values[1] as string;
            }
            catch (Exception)
            {
                label = "";
                schemaId = "";
            }

            if (!string.IsNullOrEmpty(label))
            {
                if (!string.IsNullOrEmpty(schemaId) && schemaId.Equals("XnGEZ7gJxDNfxwnZpkkVcs:2:Digitaler Führerschein:0.2"))
                {
                    switch (label.ToLower())
                    {
                        case "name":
                            return Lang.LabelTextConvert_DriveName;
                        case "geburtsname":
                            return Lang.LabelTextConvert_DriveBirthName;
                        case "vorname":
                            return Lang.LabelTextConvert_DriveFirstName;
                        case "geburtsdatum":
                            return Lang.LabelTextConvert_DriveBirthDate;
                        case "geburtsort":
                            return Lang.LabelTextConvert_DriveBirthPlace;
                        case "ausstellungsdatum":
                            return Lang.LabelTextConvert_DriveIssueDate;
                        case "ablaufdatum":
                            return Lang.LabelTextConvert_DriveExpiryDate;
                        case "aussteller":
                            return Lang.LabelTextConvert_DriveIssuer;
                        case "führerscheinnummer":
                            return Lang.LabelTextConvert_DriveNumber;
                        case "führerscheinklassen":
                            return Lang.LabelTextConvert_DriveClasses;
                        case "gültigab":
                            return Lang.LabelTextConvert_DriveValidSince;
                        case "gültigbis":
                            return Lang.LabelTextConvert_DriveValidTill;
                        case "beschränkungen":
                            return Lang.LabelTextConvert_DriveRestrictions;
                        default:
                            return label;
                    }
                }
                else
                {
                    switch (label.ToLower())
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
                            return label;
                    }
                }
            }

            return label;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { value };
        }
    }
}
