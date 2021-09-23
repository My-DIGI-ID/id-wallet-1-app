using Xamarin.Forms;

namespace IDWallet.Views.Wallet.Content
{
    public class DocumentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate RegularDocumentTemplate { get; set; }
        public DataTemplate DDLTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return item is Models.DDL ddl ? DDLTemplate : RegularDocumentTemplate;
        }
    }
}
