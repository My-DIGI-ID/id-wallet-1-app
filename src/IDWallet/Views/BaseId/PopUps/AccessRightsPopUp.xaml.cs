using IDWallet.Views.Customs.PopUps;
using System.Collections.ObjectModel;
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
            foreach (string attribute in effective)
            {
                AttributeList.Add(attribute);
            }
            InitializeComponent();
        }
    }
}