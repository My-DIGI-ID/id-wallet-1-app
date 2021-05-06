using IDWallet.Views.Customs.PopUps;
using System;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Mediator.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MediatorImagesPopUp : CustomPopUp
    {
        private Command _linkClickedCommand;
        public MediatorImagesPopUp()
        {
            InitializeComponent();
        }

        public Command LinkClickedCommand => _linkClickedCommand ??= new Command(OnLinkClicked);
        private void Button_Clicked(object sender, EventArgs e)
        {
            if (CheckBox.IsChecked)
            {
                CheckBox.IsChecked = false;
            }
            else
            {
                CheckBox.IsChecked = true;
            }
        }

        private void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            AcceptButton.IsVisible = CheckBox.IsChecked;
        }

        private void DisableAll()
        {
            CancelButton.IsEnabled = false;
            AcceptButton.IsEnabled = false;
            CheckBox.IsEnabled = false;
        }

        private void EnableAll()
        {
            CancelButton.IsEnabled = true;
            AcceptButton.IsEnabled = true;
            CheckBox.IsEnabled = true;
        }

        private async void OnLinkClicked(object obj)
        {
            try
            {
                DisableAll();
                await Launcher.OpenAsync(new Uri("https://digital-enabling.com/datenschutzerklaerung"));
                LinkSpan.TextColor = Color.FromRgb(102, 51, 102);
            }
            catch (Exception)
            {
                //ignore
            }
            finally
            {
                EnableAll();
            }
        }
    }
}