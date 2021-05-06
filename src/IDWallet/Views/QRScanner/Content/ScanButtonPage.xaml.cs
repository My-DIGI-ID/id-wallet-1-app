using IDWallet.Views.Customs.PopUps;
using Plugin.Permissions;
using System;
using Xamarin.Forms;

namespace IDWallet.Views.QRScanner.Content
{
    public partial class ScanButtonPage : ContentPage
    {
        public ScanButtonPage()
        {
            InitializeComponent();
            NavigationPage.SetHasNavigationBar(this, false);
        }

        public async void RestartScan_Clicked(object sender, EventArgs e)
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
    }
}