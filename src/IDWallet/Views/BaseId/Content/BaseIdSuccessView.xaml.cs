using IDWallet.ViewModels;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BaseIdSuccessView : ContentView
    {
        public BaseIdSuccessView()
        {
            InitializeComponent();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            ((BaseIdViewModel)BindingContext).GoToNext();
        }
    }
}