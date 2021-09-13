using IDWallet.ViewModels;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.BaseId.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BaseIdReadView : ContentView
    {
        public BaseIdReadView()
        {
            InitializeComponent();
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            App.SafetyResult = "";
            ((BaseIdViewModel)BindingContext).GoToNext();
        }
    }
}