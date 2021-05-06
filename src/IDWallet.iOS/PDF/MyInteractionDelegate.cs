using UIKit;

namespace IDWallet.iOS.PDF
{
    public class MyInteractionDelegate : UIDocumentInteractionControllerDelegate
    {
        private readonly UIViewController parent;

        public MyInteractionDelegate(UIViewController controller)
        {
            parent = controller;
        }

        public override UIViewController ViewControllerForPreview(UIDocumentInteractionController controller)
        {
            return parent;
        }
    }
}