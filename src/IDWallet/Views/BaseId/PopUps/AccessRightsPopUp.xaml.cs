using IDWallet.Views.Customs.PopUps;
using System.Collections.ObjectModel;
using System.Linq;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AccessRightsPopUp : CustomPopUp
    {
        public ObservableCollection<string> AttributeList { get; set; }
        public AccessRightsPopUp(string[] effective)
        {
            AttributeList = new ObservableCollection<string>();

            string[] tmpEffective = effective;
            ObservableCollection<string> orderedAttributeNames = new ObservableCollection<string> { "firstname", "familyname", "birthname", "academictitle", "addressStreet", "addresszipcode", "addresscity", "addressCountry", "dateofbirth", "placeofbirth", "dateofexpiry", "documenttype", "pseudonym" };
            foreach (string attributeName in orderedAttributeNames)
            {
                if (tmpEffective.Contains(attributeName))
                {
                    AttributeList.Add(attributeName);
                    tmpEffective = tmpEffective.Where(x => x != attributeName).ToArray<string>();
                }
            }

            foreach (string attribute in tmpEffective)
            {
                AttributeList.Add(attribute);
            }

            InitializeComponent();
        }
    }
}