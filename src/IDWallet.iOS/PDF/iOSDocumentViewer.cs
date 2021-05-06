using IDWallet.Interfaces;
using IDWallet.iOS.PDF;
using Foundation;
using UIKit;

[assembly: Xamarin.Forms.Dependency(typeof(DocumentViewer))]
namespace IDWallet.iOS.PDF
{
    public class DocumentViewer : IDocumentViewer
    {
        public DocumentViewer()
        {

        }

        public void ShowDocumentFile(string filepath, string mimeType)
        {
            UIDocumentInteractionController viewer = UIDocumentInteractionController.FromUrl(NSUrl.FromFilename(filepath));
            UIViewController controller = GetVisibleViewController();
            viewer.PresentOpenInMenu(controller.View.Frame, controller.View, true);
        }

        private UIViewController GetVisibleViewController(UIViewController controller = null)
        {
            controller = controller ?? UIApplication.SharedApplication.KeyWindow.RootViewController;

            if (controller.PresentedViewController == null)
            {
                return controller;
            }

            if (controller.PresentedViewController is UINavigationController)
            {
                return ((UINavigationController)controller.PresentedViewController).VisibleViewController;
            }

            if (controller.PresentedViewController is UITabBarController)
            {
                return ((UITabBarController)controller.PresentedViewController).SelectedViewController;
            }

            return GetVisibleViewController(controller.PresentedViewController);
        }
    }
}