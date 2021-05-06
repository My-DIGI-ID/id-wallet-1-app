using System;
using System.Globalization;

namespace IDWallet.Models
{
    public class CredentialClaim
    {
        public string CredentialRecordId { get; set; }
        public string Name { get; set; }
        public bool NoValidDate
        {
            get
            {
                try
                {
                    if (Name.ToLower().Equals("enddate") || Name.ToLower().Equals("expirydate"))
                    {
                        DateTime date = DateTime.ParseExact(Value, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
                        DateTime now = DateTime.Now.Date;
                        if (date <= now)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else if (Name.ToLower().Equals("startdate"))
                    {
                        DateTime date = DateTime.ParseExact(Value, "yyyy-MM-dd", CultureInfo.InvariantCulture).Date;
                        DateTime now = DateTime.Now.Date;
                        if (date > now)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        public string PredicateType { get; set; }
        public string Value { get; set; }
    }
}