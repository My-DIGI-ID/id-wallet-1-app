using IDWallet.Agent.Models;
using IDWallet.Events;
using IDWallet.Resources;
using IDWallet.ViewModels;
using IDWallet.Views.Customs.PopUps;
using Plugin.Permissions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.Proof
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ProofPopUp : CustomPopUp
    {
        public string ProofOrigin;
        private readonly string _proofRecordId;
        private readonly CustomServiceDecorator _service;
        private readonly ProofViewModel _viewModel;
        private readonly AuthViewModel _authViewModel;
        private Command _linkClickedCommand;

        public ProofPopUp(ProofViewModel proofViewModel, string proofRecordId, CustomServiceDecorator service = null, string alias = "")
        {
            InitializeComponent();

            ConnectionAliasSpan.Text = alias;
            ConnectionAliasSpan2.Text = alias;

            _proofRecordId = proofRecordId;
            _service = service;

            BindingContext = _viewModel = proofViewModel;
            _authViewModel = new AuthViewModel(proofViewModel);
        }

        public Command LinkClickedCommand => _linkClickedCommand ??= new Command(OnLinkClicked);
        private async void OnLinkClicked(object obj)
        {
            await Launcher.OpenAsync(new Uri("https://digital-enabling.com/datenschutzerklaerung"));
        }

        private void DisableAll()
        {
            CancelButton.IsEnabled = false;
            SendButton.IsEnabled = false;
        }

        private void EnableAll()
        {
            CancelButton.IsEnabled = true;
            SendButton.IsEnabled = true;
        }

        private async void OnSend(object sender, EventArgs e)
        {
            var authPopUp = new PopUps.ProofAuthenticationPopUp(_authViewModel) 
            { 
                ProofSendPopUp = true 
            };
#pragma warning disable CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.
            authPopUp.ShowPopUp(); // No await.
#pragma warning restore CS4014 // Da auf diesen Aufruf nicht gewartet wird, wird die Ausführung der aktuellen Methode vor Abschluss des Aufrufs fortgesetzt.

            while (!_viewModel.AuthSuccess)
            {
                if (_viewModel.AuthError)
                {
                    break;
                }
                await Task.Delay(100);
            }
            authPopUp.OnAuthCanceled(authPopUp, null);

            if (!_viewModel.AuthError && _viewModel.AuthSuccess)
            {
                DisableAll();
                _viewModel.AuthSuccess = false;
                try
                {
                    NetworkAccess connectivity = Connectivity.NetworkAccess;
                    if (connectivity != NetworkAccess.ConstrainedInternet && connectivity != NetworkAccess.Internet)
                    {
                        BasicPopUp alertPopUp = new BasicPopUp(
                            Lang.PopUp_Network_Error_Title,
                            Lang.PopUp_Network_Error_Text,
                            Lang.PopUp_Network_Error_Button)
                        {
                            ProofSendPopUp = true
                        };
                        await alertPopUp.ShowPopUp();
                        OnPopUpCanceled(sender, e);
                    }

                    Plugin.Permissions.Abstractions.PermissionStatus storagePermissionStatus =
                        await CrossPermissions.Current.CheckPermissionStatusAsync<StoragePermission>();
                    if (storagePermissionStatus == Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                    {
                        MessagingCenter.Send(this, WalletEvents.SendProofRequest, _proofRecordId);
                        try
                        {
                            await _viewModel.CreateAndSendProof(_service);
                            MessagingCenter.Send(this, WalletEvents.SentProofRequest, _proofRecordId);
                            OnPopUpAccepted(sender, e);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                            BasicPopUp alertPopUp = new BasicPopUp(
                                Lang.PopUp_Proof_Sending_Error_Title,
                                Lang.PopUp_Proof_Sending_Error_Message,
                                Lang.PopUp_Proof_Sending_Error_Button)
                            {
                                ProofSendPopUp = true
                            };
                            await alertPopUp.ShowPopUp();
                            OnPopUpCanceled(sender, e);
                        }
                    }
                    else
                    {
                        BasicPopUp alertPopUp = new BasicPopUp(
                            Lang.PopUp_Storage_Permission_Needed_Proof_Title,
                            Lang.PopUp_Storage_Permission_Needed_Proof_Text,
                            Lang.PopUp_Storage_Permission_Needed_Proof_Button)
                        {
                            ProofSendPopUp = true
                        };
                        await alertPopUp.ShowPopUp();
                        OnPopUpCanceled(sender, e);
                    }
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
}
        
        

        

        

        

        

        

        

           
