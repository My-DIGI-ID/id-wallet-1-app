using IDWallet.Models;
using IDWallet.Resources;
using IDWallet.ViewModels;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Inbox;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.SwitchLanguage
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SwitchLanguagePage : ContentPage
    {
        private Command<Language> _languageTappedCommand;
        private Command _notificationsClickedCommand;
        public SwitchLanguagePage()
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingOpen_Icon.svg");

            CustomViewModel viewModel = new CustomViewModel();
            BindingContext = viewModel;
            viewModel.DisableNotificationAlert();
            try
            {
                _oldCulture = _newCulture = CultureInfo.CurrentCulture.Name;
            }
            catch (Exception)
            {
                _oldCulture = _newCulture = CultureInfo.CurrentUICulture.Name;
                Application.Current.Properties[WalletParams.KeyLanguage] = CultureInfo.CurrentUICulture.Name;
                Application.Current.SavePropertiesAsync().GetAwaiter();
            }

            Languages = new ObservableCollection<Language>
            {
                new Language
                {
                    Name = "English",
                    CultureCode = "en-US",
                    IconSource = "mdi-checkbox-blank-circle-outline"
                },

                new Language
                {
                    Name = "Deutsch",
                    CultureCode = "de-DE",
                    IconSource = "mdi-checkbox-blank-circle-outline"
                }
            };

            switch (_oldCulture)
            {
                case "en-GB":
                case "en-US":
                    Languages[0].IconSource = "mdi-circle-slice-8";
                    break;
                case "de-DE":
                    Languages[1].IconSource = "mdi-circle-slice-8";
                    break;
                default:
                    Languages[0].IconSource = "mdi-circle-slice-8";
                    break;
            }

            InitializeComponent();
        }

        public ObservableCollection<Language> Languages { get; set; }
        public Command<Language> LanguageTappedCommand =>
            _languageTappedCommand ??= new Command<Language>(LanguageTapped);

        public Command NotificationsClickedCommand =>
            _notificationsClickedCommand ??= new Command(Notifications_Clicked);

        public ImageSource SettingsIconImage { get; set; }
        private string _newCulture { get; set; }
        private string _oldCulture { get; }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            EnableAll();
        }

        protected override bool OnBackButtonPressed()
        {
            DisableAll();
            return base.OnBackButtonPressed();
        }

        private void DisableAll()
        {
            NotificationsToolBarItem.IsEnabled = false;
            SettingsToolBarItem.IsEnabled = false;
            LanguageStack.IsEnabled = false;
        }

        private void EnableAll()
        {
            NotificationsToolBarItem.IsEnabled = true;
            SettingsToolBarItem.IsEnabled = true;
            LanguageStack.IsEnabled = true;
        }

        private async void LanguageTapped(Language tappedLanguage)
        {
            DisableAll();
            if (tappedLanguage.IconSource == "mdi-circle-slice-8")
            {
                EnableAll();
                return;
            }

            foreach (Language language in Languages)
            {
                if (language.IconSource == "mdi-circle-slice-8")
                {
                    language.IconSource = "mdi-checkbox-blank-circle-outline";
                }
            }

            tappedLanguage.IconSource = "mdi-circle-slice-8";

            Application.Current.Properties[WalletParams.KeyLanguage] = tappedLanguage.CultureCode;
            await Application.Current.SavePropertiesAsync();

            Lang.Culture = new CultureInfo(tappedLanguage.CultureCode);
            BasicPopUp popUp = new BasicPopUp(Lang.PopUp_Language_Changed_Title, Lang.PopUp_Language_Changed_Text,
                Lang.PopUp_Language_Changed_Button);
            await popUp.ShowPopUp();
            EnableAll();
        }

        private async void Notifications_Clicked()
        {
            DisableAll();
            InboxPage notificationsPage = null;
            try
            {
                bool nextPageExists = false;
                System.Collections.Generic.IEnumerator<Page> oldPageEnumerator =
                    Application.Current.MainPage.Navigation.ModalStack.GetEnumerator();
                do
                {
                    nextPageExists = oldPageEnumerator.MoveNext();
                } while (nextPageExists && !(oldPageEnumerator.Current is InboxPage));

                if (oldPageEnumerator.Current is InboxPage)
                {
                    notificationsPage = (InboxPage)oldPageEnumerator.Current;
                }
            }
            catch (Exception)
            {
                notificationsPage = new InboxPage();
            }
            finally
            {
                if (notificationsPage == null)
                {
                    notificationsPage = new InboxPage();
                }
            }

            await Navigation.PushAsync(notificationsPage);
            EnableAll();
        }
    }
}