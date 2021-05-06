using Hyperledger.Aries.Features.IssueCredential;
using System.Collections.ObjectModel;

namespace IDWallet.Models
{
    public class HistoryCredentialElement : HistorySubElement
    {
        public ObservableCollection<CredentialPreviewAttribute> Claims { get; set; }
    }
}
