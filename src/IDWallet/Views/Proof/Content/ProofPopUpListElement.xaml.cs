using IDWallet.Models;
using IDWallet.ViewModels;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Proof.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProofPopUpListElement : ContentView
    {
        private Command<ProofElementOption> _infoButtonTappedCommand;
        private Command<ProofElementOption> _listViewOptionTappedCommand;
        private Command<string> _openPdfButtonTappedCommand;

        public ProofPopUpListElement()
        {
            InitializeComponent();
        }

        public Command<ProofElementOption> InfoButtonTappedCommand =>
            _infoButtonTappedCommand ??= new Command<ProofElementOption>(InfoButtonTapped);

        public Command<ProofElementOption> ListViewOptionTappedCommand =>
                            _listViewOptionTappedCommand ??= new Command<ProofElementOption>(ListViewOptionTapped);

        public Command<string> OpenPdfButtonTappedCommand =>
            _openPdfButtonTappedCommand ??= new Command<string>(OpenPdfButtonTapped);

        private void OpenPdfButtonTapped(string documentString)
        {
            App.ViewFile(documentString);
        }

        private async void InfoButtonTapped(ProofElementOption listViewOption)
        {
            if (listViewOption != null)
            {
                if (listViewOption.ShowInfo)
                {
                    listViewOption.ShowInfo = false;
                }
                else
                {
                    if (!listViewOption.Attributes.Any())
                    {
                        ProofModel request = this.BindingContext as ProofModel;
                        ProofViewModel viewModel =
                            (Parent.Parent.Parent.Parent.Parent.Parent.BindingContext) as ProofViewModel;
                        await viewModel.LoadAttributes(request, listViewOption);
                    }

                    listViewOption.ShowInfo = true;
                }
            }
        }

        private void ListViewOptionTapped(ProofElementOption listViewOption)
        {
            if (listViewOption != null)
            {
                ProofModel request = this.BindingContext as ProofModel;
                ProofViewModel viewModel = (Parent.Parent.Parent.Parent.Parent.Parent.BindingContext) as ProofViewModel;
                int indexTapped = viewModel.Requests.IndexOf(request);
                viewModel.SetIndex(indexTapped);
                viewModel.SelectCredential(listViewOption);
            }
        }
    }
}