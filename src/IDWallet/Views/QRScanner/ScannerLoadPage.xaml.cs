using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.QRScanner.Content;
using Plugin.Permissions;
using System;
using System.ComponentModel;
using Xamarin.Forms;

namespace IDWallet.Views.QRScanner
{
    [DesignTimeVisible(false)]
    public partial class ScannerLoadPage : NavigationPage
    {
        public ScannerLoadPage(Page page) : base(page)
        {
            InitializeComponent();
            BindingContext = this;
        }

        public async void OnSelection()
        {
            if (Device.RuntimePlatform == Device.Android)
            {
                Plugin.Permissions.Abstractions.PermissionStatus cameraPermissionStatus =
                    await CrossPermissions.Current.CheckPermissionStatusAsync<CameraPermission>();
                if (cameraPermissionStatus != Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                {
                    BasicPopUp alertPopUp = new BasicPopUp(
                        IDWallet.Resources.Lang.PopUp_Scanner_Permission_Title,
                        IDWallet.Resources.Lang.PopUp_Scanner_Permission_Text,
                        IDWallet.Resources.Lang.PopUp_Scanner_Permission_Button);
                    await alertPopUp.ShowPopUp();
                    OnBackButtonPressed();
                    return;
                }
            }

            ScanningPage scanningPage = null;
            try
            {
                bool nextPageExists = false;
                System.Collections.Generic.IEnumerator<Page> oldPageEnumerator =
                    Application.Current.MainPage.Navigation.NavigationStack.GetEnumerator();
                do
                {
                    nextPageExists = oldPageEnumerator.MoveNext();
                } while (nextPageExists && !(oldPageEnumerator.Current is ScanningPage));

                if (oldPageEnumerator.Current is ScanningPage)
                {
                    scanningPage = (ScanningPage)oldPageEnumerator.Current;
                }
            }
            catch (Exception)
            {
                scanningPage = new ScanningPage();
            }
            finally
            {
                if (scanningPage == null)
                {
                    scanningPage = new ScanningPage();
                }
            }

            await Navigation.PushAsync(scanningPage);
        }

        protected override bool OnBackButtonPressed()
        {
            if (Parent is TabbedPage tabbedPage)
            {
                tabbedPage.CurrentPage = tabbedPage.Children[0];
            }

            return true;
        }
    }
}