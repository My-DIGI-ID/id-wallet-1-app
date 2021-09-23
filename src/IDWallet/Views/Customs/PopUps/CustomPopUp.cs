using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace IDWallet.Views.Customs.PopUps
{
    public enum PopUpResult
    {
        Canceled,
        Deleted,
        Accepted
    };

    public class CustomPopUp : PopupPage
    {
        public bool PopUpIsLoading = false;
        public bool PreLoginPopUp = false;
        public bool AlwaysDisplay = false;
        private TaskCompletionSource<PopUpResult> _taskCompletionSource = null;

        public static async Task WaitForOpenPopUp(CustomPopUp popUp)
        {
            await Task.Delay(10);
            while ((App.PopUpIsOpen || (!App.IsLoggedIn && !popUp.PreLoginPopUp)) && !popUp.AlwaysDisplay)
            {
                await Task.Delay(10);
            }
        }

        public void CloseOnSleep()
        {
            ClosePopUp();
        }

        public void DeactivateAllButtons()
        {
            try
            {
                StackLayout popUpStackLayout = (StackLayout)Content;
                Frame popUpFrame = (Frame)popUpStackLayout.Children[0];
                ContentView contentView = (ContentView)popUpFrame.Content;
                StackLayout innerStack = (StackLayout)contentView.Content;
                foreach (View child in innerStack.Children.Reverse())
                {
                    TraverseChildren(child);
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public async Task<PopUpResult> ShowPopUp()
        {
            await Task.Run(() => WaitForOpenPopUp(this));

            App.PopUpIsOpen = true;
            _taskCompletionSource = new TaskCompletionSource<PopUpResult>();
            await PopupNavigation.Instance.PushAsync(this, false);

            App.OpenPopupPage = this;

            return await _taskCompletionSource.Task;
        }

        protected void OnPopUpAccepted(object sender, EventArgs e)
        {
            DeactivateAllButtons();
            ClosePopUp();
            _taskCompletionSource?.SetResult(PopUpResult.Accepted);
        }

        protected override bool OnBackButtonPressed()
        {
            if (!PopUpIsLoading)
            {
                DeactivateAllButtons();
                ClosePopUp();
                _taskCompletionSource?.SetResult(PopUpResult.Canceled);
                return false;
            }
            else
            {
                return false;
            }
        }

        protected override bool OnBackgroundClicked()
        {
            if (!PopUpIsLoading)
            {
                DeactivateAllButtons();
                ClosePopUp();
                _taskCompletionSource?.SetResult(PopUpResult.Canceled);
                return false;
            }
            else
            {
                return false;
            }
        }

        protected void OnPopUpCanceled(object sender, EventArgs e)
        {
            DeactivateAllButtons();
            ClosePopUp();
            _taskCompletionSource?.SetResult(PopUpResult.Canceled);
        }

        protected void OnPopUpDeleted(object sender, EventArgs e)
        {
            DeactivateAllButtons();
            ClosePopUp();
            _taskCompletionSource?.SetResult(PopUpResult.Deleted);
        }

        private async void ClosePopUp()
        {
            App.PopUpIsOpen = false;
            if (PopupNavigation.Instance.PopupStack.Any())
            {
                await PopupNavigation.Instance.PopAsync(false);
            }

            App.OpenPopupPage = null;
        }

        private void TraverseChildren(View view)
        {
            try
            {
                if (view is Layout)
                {
                    if (view is Layout<View>)
                    {
                        foreach (View child in ((view as Layout<View>).Children))
                        {
                            TraverseChildren(child);
                        }
                    }
                    else
                    {
                        if (view is ContentView)
                        {
                            TraverseChildren((view as ContentView).Content);
                        }
                        else if (view is Frame)
                        {
                            TraverseChildren((view as Frame).Content);
                        }
                        else if (view is ScrollView)
                        {
                            TraverseChildren((view as ScrollView).Content);
                        }
                        else if (view is ContentPresenter)
                        {
                            TraverseChildren((view as ContentPresenter).Content);
                        }
                    }
                }
                else
                {
                    if (view is Button)
                    {
                        ((Button)view).IsEnabled = false;
                    }
                    else if (view is ImageButton)
                    {
                        ((ImageButton)view).IsEnabled = false;
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }
    }
}