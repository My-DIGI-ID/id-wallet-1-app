using Autofac;
using IDWallet.Services.Interfaces;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IDWallet.Extensions
{
    [ContentProperty("Text")]
    public class L10nExtension : IMarkupExtension
    {
        private readonly IL10nService _resx;

        public L10nExtension()
        {
            _resx = App.Container.Resolve<IL10nService>();
        }

        public string Text { get; set; }
        public object ProvideValue(IServiceProvider serviceProvider)
        {
            if (Text == null)
            {
                return "";
            }

            string translation = _resx.GetLocalizedText(Text);

            if (translation == null)
            {
                translation = Text;
            }

            return translation;
        }
    }
}