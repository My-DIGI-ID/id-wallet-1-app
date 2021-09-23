using IDWallet.ViewModels;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Views.DDL.Content
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DdlSuccessView : ContentView
    {
        public DdlSuccessView()
        {
            InitializeComponent();
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            ((DdlViewModel)BindingContext).GoToNext();
        }
    }
}