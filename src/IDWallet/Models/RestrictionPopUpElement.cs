using System.Collections.Generic;

namespace IDWallet.Models
{
    public class RestrictionPopUpElement
    {
        public string Name { get; set; }
        public List<RestrictionSet> Restrictions { get; set; }
    }

    public class RestrictionSet
    {
        public List<SingleRestriction> RestrictionContent { get; set; }
    }

    public class SingleRestriction
    {
        public string RestrictionType { get; set; }
        public string RestrictionValue { get; set; }
    }
}