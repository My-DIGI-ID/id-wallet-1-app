using IDWallet.Interfaces;
using IDWallet.Resources;
using IDWallet.ViewModels;
using IDWallet.Views.BaseId;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.DDL;
using System.Globalization;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Wallet.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AddDocumentPopUp : CustomPopUp
    {
        private readonly WalletViewModel _walletViewModel;
        public string AddOthersImage { get; set; }
        private bool _clicked = false;

        public AddDocumentPopUp(WalletViewModel viewModel)
        {
            if (CultureInfo.CurrentUICulture.Name.Equals("de-DE"))
            {
                AddOthersImage = "imagesources.WalletPage.AddOther.svg";
            }
            else
            {
                AddOthersImage = "imagesources.WalletPage.AddOther_en.svg";
            }

            _walletViewModel = viewModel;
            InitializeComponent();

            BaseIdImage.Source = _walletViewModel.BaseIdExisting
                ? SvgImageSource.FromSvgResource("Resources.imagesources.WalletPage.IsAddedBaseId.svg")
                : SvgImageSource.FromSvgResource("Resources.imagesources.WalletPage.AddBaseId.svg");

            DdlImage.Source = _walletViewModel.DdlExisting
                ? SvgImageSource.FromSvgResource("Resources.imagesources.WalletPage.IsAddedDdl.svg")
                : SvgImageSource.FromSvgResource("Resources.imagesources.WalletPage.AddDdl.svg");

            VacCertImage.Source = _walletViewModel.VacCertExisting
                ? SvgImageSource.FromSvgResource("Resources.imagesources.WalletPage.IsAddedVacCert.svg")
                : SvgImageSource.FromSvgResource("Resources.imagesources.WalletPage.AddVacCert.svg");
        }

        private async void AddBaseIdTapped(object sender, System.EventArgs e)
        {
            if (!DependencyService.Get<IAusweisSdk>().IsConnected() || _clicked)
            {
                if (!DependencyService.Get<IAusweisSdk>().DeviceHasNfc())
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Lang.PopUp_NFC_No_NFC_Title,
                        Lang.PopUp_NFC_No_NFC_Text,
                        Lang.PopUp_NFC_No_NFC_Button
                    )
                    {
                        AlwaysDisplay = true
                    };
                    await popUp.ShowPopUp();

                    return;
                }

                if (!DependencyService.Get<IAusweisSdk>().IsConnected())
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Lang.PopUp_SDK_Not_Connected_Title,
                        Lang.PopUp_SDK_Not_Connected_Text,
                        Lang.PopUp_SDK_Not_Connected_Button
                    )
                    {
                        AlwaysDisplay = true
                    };
                    await popUp.ShowPopUp();

                    return;
                }

                return;
            }

            _clicked = true;
            try
            {
                if (!_walletViewModel.BaseIdExisting)
                {
                    if (DependencyService.Get<IAusweisSdk>().NfcEnabled())
                    {
                        DependencyService.Get<IAusweisSdk>().StartSdkIos();
                        DependencyService.Get<IAusweisSdk>().EnableNfcDispatcher();

                        try
                        {
                            CustomTabbedPage mainPage = Application.Current.MainPage as CustomTabbedPage;
                            mainPage.CurrentPage = mainPage.Children.First();
                            await ((NavigationPage)mainPage.CurrentPage).Navigation.PushAsync(new BaseIdPage());
                            OnPopUpCanceled(sender, e);
                        }
                        catch
                        {
                            //...
                        }
                    }
                    else
                    {
                        OnPopUpCanceled(sender, e);
                        BasicPopUp popUp = new BasicPopUp(
                            Lang.PopUp_NFC_Not_Enabled_Title,
                            Lang.PopUp_NFC_Not_Enabled_Text,
                            Lang.PopUp_NFC_Not_Enabled_Button
                    )
                        {
                            AlwaysDisplay = true
                        };
                        await popUp.ShowPopUp();
                    }
                }
            }
            finally
            {
                _clicked = false;
            }
        }

        private async void AddDdlTapped(object sender, System.EventArgs e)
        {
            if (!DependencyService.Get<IAusweisSdk>().IsConnected() || _clicked)
            {
                if (!DependencyService.Get<IAusweisSdk>().DeviceHasNfc())
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Lang.PopUp_NFC_No_NFC_Title,
                        Lang.PopUp_NFC_No_NFC_Text,
                        Lang.PopUp_NFC_No_NFC_Button
                    )
                    {
                        AlwaysDisplay = true
                    };
                    await popUp.ShowPopUp();

                    return;
                }

                if (!DependencyService.Get<IAusweisSdk>().IsConnected())
                {
                    BasicPopUp popUp = new BasicPopUp(
                        Lang.PopUp_SDK_Not_Connected_Title,
                        Lang.PopUp_SDK_Not_Connected_Text,
                        Lang.PopUp_SDK_Not_Connected_Button
                    )
                    {
                        AlwaysDisplay = true
                    };
                    await popUp.ShowPopUp();

                    return;
                }

                return;
            }

            _clicked = true;
            try
            {
                if (DependencyService.Get<IAusweisSdk>().NfcEnabled())
                {
                    DependencyService.Get<IAusweisSdk>().StartSdkIos();
                    DependencyService.Get<IAusweisSdk>().EnableNfcDispatcher();
                    try
                    {
                        CustomTabbedPage mainPage = Application.Current.MainPage as CustomTabbedPage;
                        mainPage.CurrentPage = mainPage.Children.First();
                        await ((NavigationPage)mainPage.CurrentPage).Navigation.PushAsync(new DdlPage());
                        OnPopUpCanceled(sender, e);
                    }
                    catch
                    {
                        //...
                    }
                }
                else
                {
                    OnPopUpCanceled(sender, e);
                    BasicPopUp popUp = new BasicPopUp(
                        Lang.PopUp_NFC_Not_Enabled_Title,
                        Lang.PopUp_NFC_Not_Enabled_Text,
                        Lang.PopUp_NFC_Not_Enabled_Button
                    )
                    {
                        AlwaysDisplay = true
                    };
                    await popUp.ShowPopUp();
                }
            }
            finally
            {
                _clicked = false;
            }
        }

        private async void AddVacCertTapped(object sender, System.EventArgs e)
        {
            if (_clicked)
            {
                return;
            }

            _clicked = true;
            try
            {
                OnPopUpCanceled(sender, e);
                AddVacCertPopUpSoon addVacCertPopUp = new AddVacCertPopUpSoon();
                await addVacCertPopUp.ShowPopUp();
            }
            finally
            {
                _clicked = false;
            }
        }

        private async void AddOtherTapped(object sender, System.EventArgs e)
        {
            if (_clicked)
            {
                return;
            }

            _clicked = true;
            try
            {
                CustomTabbedPage mainPage = Application.Current.MainPage as CustomTabbedPage;
                mainPage.CurrentPage = mainPage.Children[1];
                OnPopUpCanceled(sender, e);
            }
            catch
            {
                //...
            }
            finally
            {
                _clicked = false;
            }
        }
    }
}