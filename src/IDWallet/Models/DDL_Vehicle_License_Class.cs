using Hyperledger.Aries.Features.IssueCredential;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Svg;

namespace IDWallet.Models
{
    public class DDL_Vehicle_License_Class
    {
        public string Identifier { get; set; }
        public ImageSource IconSource { get; set; }
        public string IssuingDate { get; set; }
        public string ExpiryDate { get; set; }
        public string Restrictions { get; set; }
        public DDL_Class_Type ClassType { get; set; }

        public DDL_Vehicle_License_Class(List<CredentialPreviewAttribute> attributes)
        {
            if (attributes == null || (attributes.Count != 2 && attributes.Count != 3))
            {
                return;
            }

            CredentialPreviewAttribute restrictions = (from CredentialPreviewAttribute attribute in attributes
                                                       where attribute.Name.Contains("Restrictions")
                                                       select attribute).First();

            CredentialPreviewAttribute dateOfIssuance = (from CredentialPreviewAttribute attribute in attributes
                                                         where attribute.Name.Contains("DateOfIssuance")
                                                         select attribute).First();

            string category = restrictions.Name.Split('_')[0];

            Identifier = category.Replace("licenseCategory", "");
            Enum.TryParse(Identifier, out DDL_Class_Type result);
            ClassType = result;
            Restrictions = restrictions.Value.ToString();

            IconSource = SvgImageSource.FromSvgResource("Resources.imagesources.DDL.DDL_" + Identifier + ".svg", (Color)Application.Current.Resources["DDLBlue"]);
            try
            {
                IssuingDate = DateTime.ParseExact(dateOfIssuance.Value.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToShortDateString();
            }
            catch
            {
                try
                {
                    IssuingDate = DateTime.ParseExact(dateOfIssuance.Value.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture).ToShortDateString();
                }
                catch
                {
                    try
                    {
                        IssuingDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((double)dateOfIssuance.Value).ToShortDateString();
                    }
                    catch
                    {
                        IssuingDate = "";
                    }
                }
            }

            if (attributes.Count == 3)
            {
                CredentialPreviewAttribute dateOfExpiry = (from CredentialPreviewAttribute attribute in attributes
                                                           where attribute.Name.Contains("DateOfExpiry")
                                                           select attribute).First();

                try
                {
                    ExpiryDate = DateTime.ParseExact(dateOfExpiry.Value.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToShortDateString();
                }
                catch
                {
                    try
                    {
                        ExpiryDate = DateTime.ParseExact(dateOfExpiry.Value.ToString(), "yyyy-MM-dd", CultureInfo.InvariantCulture).ToShortDateString();
                    }
                    catch
                    {
                        try
                        {
                            ExpiryDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((double)dateOfExpiry.Value).ToShortDateString();
                        }
                        catch
                        {
                            ExpiryDate = "";
                        }
                    }
                }
            }
            else
            {
                ExpiryDate = "-";
            }
        }

        public enum DDL_Class_Type
        {
            AM,
            A1,
            A2,
            A,
            B1,
            B,
            C1,
            C,
            D1,
            D,
            BE,
            C1E,
            CE,
            D1E,
            DE,
            L,
            T,
            M
        }
    }
}
