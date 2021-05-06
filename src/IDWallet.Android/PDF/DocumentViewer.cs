using Android.Content;
using IDWallet.Droid.PDF;
using IDWallet.Interfaces;

[assembly: Xamarin.Forms.Dependency(typeof(DocumentViewer))]
namespace IDWallet.Droid.PDF
{
    public class DocumentViewer : IDocumentViewer
    {
        public void ShowDocumentFile(string filepath, string mimeType)
        {
            Android.Net.Uri uri = AndroidX.Core.Content.FileProvider.GetUriForFile(MainActivity.Instance, WalletParams.PackageName + ".provider", new Java.IO.File(filepath));
            Intent intent = new Intent(Intent.ActionView);
            intent.SetDataAndType(uri, mimeType);
            intent.SetFlags(ActivityFlags.ClearWhenTaskReset | ActivityFlags.NewTask);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);

            MainActivity.Instance.StartActivity(Intent.CreateChooser(intent, "Select App"));
        }

        public DocumentViewer()
        {

        }
    }
}