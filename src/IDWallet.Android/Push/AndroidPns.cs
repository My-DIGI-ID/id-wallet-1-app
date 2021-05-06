using IDWallet.Droid.Push;
using IDWallet.Interfaces;
using Firebase.Installations;
using System;
using System.Threading;

[assembly: Xamarin.Forms.Dependency(typeof(AndroidPns))]
namespace IDWallet.Droid.Push
{
    internal class AndroidPns : IAndroidPns
    {
        public void Renew()
        {
            App.SetNewPnsHandle(App.NativeStorageService, true);
            Thread thread = new Thread(new ThreadStart(RenewFirebase));
            thread.Start();
        }

        private void RenewFirebase()
        {
            try
            {
                FirebaseInstallations.Instance.Delete();
            }
            catch (Exception ex)
            {
                App.PnsError = ex.Message;
            }
            finally
            {
                App.FinishedNewPnsHandle = true;
            }
        }
    }
}