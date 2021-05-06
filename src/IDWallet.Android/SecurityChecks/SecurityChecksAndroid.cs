using Android.Gms.Common;
using Android.Gms.SafetyNet;
using Android.Gms.Tasks;
using Android.Security;
using IDWallet.Droid.SecurityChecks;
using IDWallet.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(SecurityChecksAndroid))]
namespace IDWallet.Droid.SecurityChecks
{
    public class SecurityChecksAndroid : ISecurityChecks
    {
        public void SafetyCheck(byte[] nonce)
        {
            if (GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(MainActivity.Instance, 13000000) == ConnectionResult.Success)
            {
                // The SafetyNet Attestation API is available.
                SafetyNetClient safetyNetClient = SafetyNetClass.GetClient(MainActivity.Instance);

                safetyNetClient.Attest(nonce, WalletParams.SafetyNetApiKey).AddOnSuccessListener(MainActivity.Instance, new OnSuccessListener<SafetyNetApiAttestationResponse>()).AddOnFailureListener(MainActivity.Instance, new OnFailureListener<SafetyNetApiAttestationResponse>());
            }
            else
            {
                App.SafetyResult = "failed";
                // Prompt user to update Google Play Services.
            }
        }
    }

    public class OnSuccessListener<T> : Java.Lang.Object, IOnSuccessListener
    {
        public void OnSuccess(Java.Lang.Object result)
        {
            SafetyNetApiAttestationResponse response = (SafetyNetApiAttestationResponse)result;
            App.SafetyResult = response.JwsResult;
        }
    }

    public class OnFailureListener<T> : Java.Lang.Object, IOnFailureListener
    {
        public void OnFailure(Java.Lang.Exception e)
        {
            App.SafetyResult = "failed";
        }
    }

    public class KeyChainAliasCallback : Java.Lang.Object, IKeyChainAliasCallback
    {
        public void Alias(string alias)
        {
            if (alias != null)
            {
                MainActivity.Alias = alias;
            }
        }
    }
}