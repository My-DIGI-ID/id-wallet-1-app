using IDWallet.Events;
using IDWallet.Models;
using IDWallet.Resources;
using IDWallet.ViewModels;
using IDWallet.Views.Customs.PopUps;
using IDWallet.Views.Inbox;
using IDWallet.Views.Settings.Connections.PopUps;
using IDWallet.Views.Wallet;
using Hyperledger.Aries.Features.DidExchange;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Svg;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Settings.Connections.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ConnectionEditPage : ContentPage
    {
        private readonly bool _autoAcceptCredential;
        private readonly bool _autoAcceptProof;
        private readonly bool _autoNotificationCredential;
        private readonly bool _autoNotificationProof;
        private readonly ConnectionRecord _connectionRecord;
        private readonly ConnectionsViewModel _connectionsViewModel;
        private readonly string _oldConnectionName;
        private readonly bool _onlyKnownProofs;
        private readonly string _takeLastUsed;
        private readonly string _takeNewest;
        private readonly ConnectionEditViewModel _viewModel;
        private Command<CheckBox> _checkBoxTappedCommand;

        private Command _deleteTappedCommand;

        private bool _isSaved;

        private Command _notificationsClickedCommand;

        private Command _proofAdvancedOptionsTapped;

        private Command<string> _radioButtonClicked;

        public ConnectionEditPage(ConnectionRecord connectionRecord, ConnectionsViewModel connectionsViewModel)
        {
            SettingsIconImage = SvgImageSource.FromSvgResource("imagesources.SettingOpen_Icon.svg");

            InitializeComponent();

            BindingContext = _viewModel = new ConnectionEditViewModel(connectionRecord);
            _viewModel.DisableNotificationAlert();
            _connectionRecord = connectionRecord;
            _connectionsViewModel = connectionsViewModel;

            _isSaved = false;

            Task.Run(async () =>
                await _viewModel.CheckForTags()
            ).Wait();

            _autoAcceptCredential = _viewModel.AutoCredential;
            _autoNotificationCredential = _viewModel.NoteCredential;
            _autoAcceptProof = _viewModel.AutoProof;
            _autoNotificationProof = _viewModel.NoteProof;
            _onlyKnownProofs = _viewModel.OnlyKnownProofs;
            _takeNewest = _viewModel.TakeNewestIcon;
            _takeLastUsed = _viewModel.TakeLastUsedIcon;
            _oldConnectionName = ConnectionAliasLabel.Text;
        }

        public Command<CheckBox> CheckBoxTappedCommand =>
                                                            _checkBoxTappedCommand ??= new Command<CheckBox>(CheckBoxTapped);
        public Command DeleteTappedCommand => _deleteTappedCommand ??= new Command(DeleteTapped);
        public Command NotificationsClickedCommand =>
            _notificationsClickedCommand ??= new Command(Notifications_Clicked);

        public Command ProofAdvancedOptionsTapped => _proofAdvancedOptionsTapped ??= new Command(AdvancedOptionsTapped);
        public Command<string> RadioButtonClicked => _radioButtonClicked ??= new Command<string>(OnRadioButtonClicked);
        public ImageSource SettingsIconImage { get; set; }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            EnableAll();
        }

        protected override bool OnBackButtonPressed()
        {
            DisableAll();
            if (App.BlockedRecordTypes.Contains(typeof(ConnectionRecord).ToString()))
            {
                App.BlockedRecordTypes.Remove(typeof(ConnectionRecord).ToString());
            }

            return base.OnBackButtonPressed();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();

            if (!_isSaved)
            {
                if (PreferencesChanged() || NameChanged())
                {
                    SaveChangesPopUp popUp = new SaveChangesPopUp();
                    PopUpResult accepted = await popUp.ShowPopUp();

                    if (accepted == PopUpResult.Accepted)
                    {
                        SaveChanges();
                    }
                }
            }
        }

        private void AdvancedOptionsTapped()
        {
            ProofAdvancedOptionsStack.IsVisible = !ProofAdvancedOptionsStack.IsVisible;
        }

        private async void Check_Clicked(object sender, EventArgs e)
        {
            if (NameCheck(ConnectionAliasEntry.Text))
            {
                ConnectionAliasLabel.Text = _viewModel.ConnectionName;
                ConnectionAliasEntry.IsVisible = false;
                ConnectionAliasLabel.IsVisible = true;

                EditButton.IsVisible = true;
                CheckButton.IsVisible = false;
                CrossButton.IsVisible = false;
            }
            else
            {
                string popUpText = Lang.PopUp_New_Connection_Name_Failed_Text + " " + WalletParams.MediatorConnectionAliasName + ".";
                BasicPopUp popUp = new BasicPopUp(
                    Lang.PopUp_New_Connection_Name_Failed_Title,
                    popUpText,
                    Lang.PopUp_New_Connection_Name_Failed_Button);
                await popUp.ShowPopUp();
            }
        }

        private void CheckBoxTapped(CheckBox checkBox)
        {
            checkBox.IsChecked = !checkBox.IsChecked;
        }

        private void Credential_Toggled(object sender, ToggledEventArgs e)
        {
            CredentialStack.IsVisible = CredentialSwitch.IsToggled;
        }

        private void Cross_Clicked(object sender, EventArgs e)
        {
            ConnectionAliasLabel.IsVisible = true;
            ConnectionAliasEntry.IsVisible = false;

            EditButton.IsVisible = true;
            CheckButton.IsVisible = false;
            CrossButton.IsVisible = false;
        }

        private async void DeleteTapped()
        {
            DisableAll();
            bool navigate = await _viewModel.DeleteConnection();
            if (navigate)
            {
                ConnectionsPageItem connectionsPageItem = null;
                try
                {
                    connectionsPageItem =
                        _connectionsViewModel.Connections.First(x => x.ConnectionRecord.Id == _connectionRecord.Id);

                    if (_connectionsViewModel.Connections.Count == 1)
                    {
                        _connectionsViewModel.EmptyLayoutVisible = true;
                    }

                    _connectionsViewModel.Connections.Remove(connectionsPageItem);
                }
                catch
                {
                    _connectionsViewModel.ReloadConnections();
                }

                if (Navigation.NavigationStack.Any())
                {
                    try
                    {
                        await Navigation.PopAsync();
                    }
                    catch (Exception)
                    {
                        //ignore
                    }
                }
            }

            EnableAll();
        }

        private void DisableAll()
        {
            NotificationsToolBarItem.IsEnabled = false;
            SettingsToolBarItem.IsEnabled = false;
            ConnectionAliasEntry.IsEnabled = false;
            EditButton.IsEnabled = false;
            CheckButton.IsEnabled = false;
            CrossButton.IsEnabled = false;
            CredentialSwitch.IsEnabled = false;
            CredentialCheckBox.IsEnabled = false;
            ProofSwitch.IsEnabled = false;
            ProofCheckBox.IsEnabled = false;
        }

        private void Edit_Clicked(object sender, EventArgs e)
        {
            ConnectionAliasLabel.IsVisible = false;
            ConnectionAliasEntry.Text = _viewModel.ConnectionName;
            ConnectionAliasEntry.IsVisible = true;

            EditButton.IsVisible = false;
            CheckButton.IsVisible = true;
            CrossButton.IsVisible = true;
        }

        private void EnableAll()
        {
            NotificationsToolBarItem.IsEnabled = true;
            SettingsToolBarItem.IsEnabled = true;
            ConnectionAliasEntry.IsEnabled = true;
            EditButton.IsEnabled = true;
            CheckButton.IsEnabled = true;
            CrossButton.IsEnabled = true;
            CredentialSwitch.IsEnabled = true;
            CredentialCheckBox.IsEnabled = true;
            ProofSwitch.IsEnabled = true;
            ProofCheckBox.IsEnabled = true;
        }

        private bool NameChanged()
        {
            if (_oldConnectionName != ConnectionAliasLabel.Text)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool NameCheck(string name)
        {
            if (name == "" || name == WalletParams.MediatorConnectionAliasName)
            {
                return false;
            }
            else
            {
                return true;
            }
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
        private void OnRadioButtonClicked(string buttonName)
        {
            switch (buttonName)
            {
                case "Take Newest":
                    TakeNewestIcon.Icon = "mdi-circle-slice-8";
                    TakeLastUsedIcon.Icon = "mdi-checkbox-blank-circle-outline";
                    break;
                case "Take Last":
                    TakeLastUsedIcon.Icon = "mdi-circle-slice-8";
                    TakeNewestIcon.Icon = "mdi-checkbox-blank-circle-outline";
                    break;
                default:
                    break;
            }
        }

        private bool PreferencesChanged()
        {
            if (_autoAcceptCredential != CredentialSwitch.IsToggled ||
                _autoNotificationCredential != CredentialCheckBox.IsChecked ||
                _autoAcceptProof != ProofSwitch.IsToggled ||
                _autoNotificationProof != ProofCheckBox.IsChecked ||
                _onlyKnownProofs != OnlyKnownCheckBox.IsChecked ||
                _takeNewest != TakeNewestIcon.Icon ||
                _takeLastUsed != TakeLastUsedIcon.Icon)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Proof_Toggled(object sender, ToggledEventArgs e)
        {
            ProofStack.IsVisible = ProofSwitch.IsToggled;
            ProofAdvancedOptionsLabel.IsVisible = ProofSwitch.IsToggled;
        }

        private async void SaveChanges()
        {
            _isSaved = true;
            bool autoCred = CredentialSwitch.IsToggled;
            bool noteCred = CredentialCheckBox.IsChecked;
            bool autoProof = ProofSwitch.IsToggled;
            bool noteProof = ProofCheckBox.IsChecked;
            bool onlyKnownProofs = OnlyKnownCheckBox.IsChecked;
            string takeNewest = TakeNewestIcon.Icon;

            if (PreferencesChanged())
            {
                _viewModel.UpdateTags(autoCred, noteCred, autoProof, noteProof, onlyKnownProofs, takeNewest);

                try
                {
                    MessagingCenter.Send(this, WalletEvents.ReloadConnections);
                }
                catch (Exception)
                {
                    //ignore
                }
            }

            if (NameChanged())
            {
                await _viewModel.ChangeName(ConnectionAliasEntry.Text);

                try
                {
                    MessagingCenter.Send(this, WalletEvents.ReloadConnections);

                    TabbedPage mainPage = (TabbedPage)Application.Current.MainPage;
                    ((WalletPage)((NavigationPage)mainPage.Children[0]).RootPage).ReloadCredentials();

                    System.Collections.Generic.IReadOnlyList<Page> navigationStack =
                        mainPage.CurrentPage.Navigation.NavigationStack;
                }
                catch (Exception)
                {
                    //ignore
                }
            }
            else
            {
                if (App.BlockedRecordTypes.Contains(typeof(ConnectionRecord).ToString()))
                {
                    App.BlockedRecordTypes.Remove(typeof(ConnectionRecord).ToString());
                }
            }

            try
            {
                await Navigation.PopAsync();
            }
            catch (Exception)
            {
                //ignore
            }
        }
    }
}