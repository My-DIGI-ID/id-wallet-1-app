using IDWallet.Models;
using IDWallet.Views.History.PopUps;
using System;
using System.Diagnostics;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Wallet.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DDLTemplate : ContentView
    {
        private Command<WalletElement> _deleteTappedCommand;
        private Command<WalletElement> _historyTappedCommand;
        private Command<HistoryProofElement> _historyProofElementTappedCommand;
        private double _expandedInfoHeight;
        private double _infoPopUpHeight;

        public Command<WalletElement> DeleteTappedCommand =>
            _deleteTappedCommand ?? (_deleteTappedCommand = new Command<WalletElement>(DeleteTapped));

        public Command<WalletElement> HistoryTappedCommand =>
                                    _historyTappedCommand ??= new Command<WalletElement>(HistoryTapped);

        public Command<HistoryProofElement> HistoryProofElementTappedCommand =>
            _historyProofElementTappedCommand ??= new Command<HistoryProofElement>(HistoryProofElementTapped);

        private bool _titleHeightChanged = false;
        private bool _infoHeightSet = false;
        private bool _popUpHeightSet = false;

        public DDLTemplate()
        {
            InitializeComponent();
        }

        private void DeleteTapped(WalletElement walletElement)
        {
            TabbedPage mainPage = (TabbedPage)Application.Current.MainPage;
            WalletPage credentialsPage = (WalletPage)((NavigationPage)mainPage.Children[0]).RootPage;
            credentialsPage.Delete_Credential(walletElement);
        }

        private async void HistoryTapped(WalletElement walletElement)
        {
            TabbedPage mainPage = (TabbedPage)Application.Current.MainPage;
            ViewModels.WalletViewModel credentialsViewModel =
                ((WalletPage)((NavigationPage)mainPage.Children[0]).RootPage).ViewModel;

            if (!walletElement.IsHistoryOpen)
            {
                if (!walletElement.IsHistorySet)
                {
                    credentialsViewModel.SetHistory(walletElement);
                }

                walletElement.IsHistoryOpen = true;
                if (walletElement.HistoryItems.Count > 0)
                {
                    HistoryEndSeparator.IsVisible = false;
                }
                else
                {
                    HistoryEndSeparator.IsVisible = true;
                }
            }
            else
            {
                walletElement.IsHistoryOpen = false;
                HistoryEndSeparator.IsVisible = true;
            }
        }

        private async void HistoryProofElementTapped(HistoryProofElement historyProofElement)
        {
            HistoryProofPopUp details = new HistoryProofPopUp(historyProofElement);
            await details.ShowPopUp();
        }

        private void Label_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Height" && !_titleHeightChanged)
            {
                _titleHeightChanged = true;
                HeaderTitleLabel.Scale = HeaderSeparator.Width / HeaderTitleLabel.Width;
                HeaderBodyLabel.Scale = HeaderSeparator.Width / HeaderBodyLabel.Width;

                HeaderGrid.RowDefinitions[0].Height = HeaderTitleLabel.Height * HeaderTitleLabel.Scale;
                HeaderGrid.RowDefinitions[2].Height = HeaderBodyLabel.Height * HeaderBodyLabel.Scale + HeaderBodyLabel.Margin.Top;
            }
        }

        private void TapGestureRecognizer_Tapped(object sender, System.EventArgs e)
        {
            Models.DDL ddl = (sender as Frame).BindingContext as Models.DDL;
            if (ddl.IsInfoOpen && _expandedInfoHeight > 0)
            {
                ddl.Content_HeightRequest -= _expandedInfoHeight;
            }
            ddl.IsInfoOpen = !ddl.IsInfoOpen;
            if (ddl.IsInfoOpen && _expandedInfoHeight > 0)
            {
                ddl.Content_HeightRequest += _expandedInfoHeight;
            }
        }

        private void ExpandedInfoGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Height" && !_infoHeightSet)
            {
                try
                {
                    _expandedInfoHeight = ExpandedInfoGrid.Height - (30 - ExpandedInfoFrame.Height);
                    Models.DDL ddl = (sender as Grid).BindingContext as Models.DDL;
                    ddl.Content_HeightRequest += _expandedInfoHeight;
                    _infoHeightSet = true;
                }
                catch (Exception)
                {
                    //ignore
                }
            }
        }

        private void OpenCloseInfoWindow(object sender, System.EventArgs e)
        {
            Models.DDL ddl = (sender as Frame).BindingContext as Models.DDL;
            if (ddl.InfoIsOpen && _infoPopUpHeight > 0)
            {
                if (ddl.IsInfoOpen)
                {
                    ddl.Content_HeightRequest -= _infoPopUpHeight - _expandedInfoHeight;
                }
                else
                {
                    ddl.Content_HeightRequest -= _infoPopUpHeight;
                }
            }
            ddl.InfoIsOpen = !ddl.InfoIsOpen;
            if (ddl.InfoIsOpen && _infoPopUpHeight > 0)
            {
                if (ddl.IsInfoOpen)
                {
                    ddl.Content_HeightRequest += _infoPopUpHeight - _expandedInfoHeight;
                }
                else
                {
                    ddl.Content_HeightRequest += _infoPopUpHeight;
                }
            }
        }

        private void InfoPopUp_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Height" && !_popUpHeightSet)
            {
                try
                {
                    Frame infoPopUp = sender as Frame;
                    Models.DDL ddl = infoPopUp.BindingContext as Models.DDL;
                    if (infoPopUp.Height + infoPopUp.Margin.VerticalThickness >= ddl.Content_HeightRequest)
                    {
                        ddl.Content_HeightRequest += 1;
                        _infoPopUpHeight += 1;
                    }
                    else
                    {
                        if (ddl.IsInfoOpen)
                        {
                            _infoPopUpHeight += _expandedInfoHeight;
                        }
                        _popUpHeightSet = true;
                    }
                }
                catch
                {

                }
            }
        }

        private async void OpenDescriptionLink(object sender, System.EventArgs e)
        {
            await Launcher.OpenAsync(new Uri(WalletParams.DdlDescriptionLink));
        }
    }
}