using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IDWallet.ViewModels
{
    public class License
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public string Version { get; set; }
    }

    public class LicenseViewModel : CustomViewModel
    {
        public LicenseViewModel()
        {
            string apache20 = LoadEmbeddedTxt("Apache_2_0.txt");
            string mIT = LoadEmbeddedTxt("MIT.txt");
            string netContributors = LoadEmbeddedTxt("MIT_Net_Contributors.txt");
            string autofac = LoadEmbeddedTxt("MIT_Autofac.txt");
            string micExtDI = LoadEmbeddedTxt("Apache_MicExtDI.txt");
            string nLog = LoadEmbeddedTxt("NLog.txt");
            string fingerprint = LoadEmbeddedTxt("Fingerprint.txt");
            string permissions = LoadEmbeddedTxt("MIT_Permissions.txt");
            string iconize = LoadEmbeddedTxt("Apache_Iconize.txt");
            string fFImage = LoadEmbeddedTxt("MIT_FFImage.txt");
            string essentials = LoadEmbeddedTxt("MIT_Essentials.txt");
            string filePicker = LoadEmbeddedTxt("MIT_FilePicker.txt");
            string googlePlay = LoadEmbeddedTxt("MIT_Google_Play.txt");
            string microsoftAzure = LoadEmbeddedTxt("MIT_Microsoft_Azure.txt");
            string formsPinView = LoadEmbeddedTxt("FormsPinView.txt");
            string MsPL = LoadEmbeddedTxt("Ms-PL.txt");
            string svg = LoadEmbeddedTxt("MIT_Svg.txt");
            string dagger = LoadEmbeddedTxt("MIT_dagger.txt");
			string ausweis = LoadEmbeddedTxt("EUPLv1-2.txt");

            List<License> unorder = new List<License>
            {
                new License {Name = "Autofac.Extensions.DependencyInjection", Version = "7.1.0", Text = autofac},
                new License {Name = "Com.Airbnb.Xamarin.Forms.Lottie", Version = "4.0.8", Text = apache20},
                new License {Name = "dotnetstandard-bip39", Version = "1.0.2", Text = apache20},
                new License {Name = "FormsPinView", Version = "2.0.0", Text = formsPinView},
                new License {Name = "Hyperledger.Aries", Version = "1.5.5", Text = apache20},
                new License {Name = "Hyperledger.Aries.Routing.Edge", Version = "1.5.5", Text = apache20},
                new License {Name = "Hyperledger.Indy.Sdk", Version = "1.11.1", Text = apache20},
                new License {Name = "Microsoft.Extensions.DependencyInjection", Version = "5.0.1", Text = apache20},
                new License {Name = "PCLStorage", Version = "1.0.2", Text = MsPL},
                new License {Name = "Plugin.Fingerprint", Version = "2.1.3", Text = fingerprint},
                new License {Name = "Plugin.Permissions", Version = "6.0.1", Text = permissions},
                new License {Name = "Rg.Plugins.Popup", Version = "2.0.0.10", Text = mIT},
                new License {Name = "Xam.Plugin.Iconize", Version = "3.5.0.129", Text = iconize},
                new License {Name = "Xam.Plugin.Iconize.Material", Version = "3.5.0.129", Text = iconize},
                new License {Name = "Xam.Plugin.Iconize.MaterialDesignIcons", Version = "3.5.0.129", Text = iconize},
                new License {Name = "Xamarin.Essentials", Version = "1.6.1", Text = essentials},
                new License {Name = "Xamarin.FFImageLoading.Forms", Version = "2.4.11.982", Text = fFImage},
                new License {Name = "Xamarin.Forms", Version = "5.0.0.2012", Text = mIT},
                new License {Name = "Xamarin.Forms.Svg", Version = "1.0.3", Text = svg},
                new License {Name = "ZXing.Net.Mobile", Version = "3.1.0-beta2", Text = mIT},
                new License {Name = "ZXing.Net.Mobile.Forms", Version = "3.1.0-beta2", Text = mIT},

                new License {Name = "Plugin.CurrentActivity", Version = "2.1.0.4", Text = permissions},
                new License {Name = "Xamarin.Android.Support.Core.Utils", Version = "28.0.0.3", Text = netContributors},
                new License {Name = "Xamarin.Android.Support.CustomTabs", Version = "28.0.0.3", Text = netContributors},
                new License {Name = "Xamarin.Android.Support.Design", Version = "28.0.0.3", Text = netContributors},
                new License {Name = "Xamarin.Android.Support.v4", Version = "28.0.0.3", Text = netContributors},
                new License
                {
                    Name = "Xamarin.Android.Support.v7.AppCompat", Version = "28.0.0.3", Text = netContributors
                },
                new License
                {
                    Name = "Xamarin.Android.Support.v7.CardView", Version = "28.0.0.3", Text = netContributors
                },
                new License
                {
                    Name = "Xamarin.Android.Support.v7.MediaRouter", Version = "28.0.0.3", Text = netContributors
                },
                new License {Name = "Xamarin.AndroidX.Browser", Version = "1.3.0.5", Text = netContributors},
                new License {Name = "Xamarin.AndroidX.ConstraintLayout", Version = "2.0.4.2", Text = netContributors},
                new License {Name = "Xamarin.AndroidX.Legacy.Support.V4", Version = "1.0.0.7", Text = netContributors},
                new License {Name = "Xamarin.AndroidX.Lifecycle.LiveData", Version = "2.3.0.1", Text = netContributors},
                new License {Name = "Xamarin.AndroidX.MediaRouter", Version = "1.2.2.1", Text = netContributors},
                new License {Name = "Xamarin.AndroidX.Migration", Version = "1.0.8", Text = netContributors},
                new License {Name = "Xamarin.AndroidX.Palette", Version = "1.0.0.7", Text = netContributors},
                new License
                {
                    Name = "Xamarin.Azure.NotificationHubs.Android", Version = "1.1.4.1", Text = microsoftAzure
                },
                new License {Name = "Xamarin.Firebase.Messaging", Version = "121.0.1", Text = googlePlay},
                new License {Name = "Xamarin.Forms.AppLinks", Version = "5.0.0.2012", Text = mIT},
                new License
                {
                    Name = "Xamarin.Google.Android.DataTransport.TransportRuntime", Version = "2.2.5",
                    Text = netContributors
                },
                new License {Name = "Xamarin.Google.Android.Material", Version = "1.3.0.1", Text = netContributors},
                new License {Name = "Xamarin.Google.Dagger", Version = "2.27.0", Text = dagger},
                new License {Name = "Xamarin.GooglePlayServices.Base", Version = "117.6.0", Text = googlePlay},
                new License {Name = "Xamarin.GooglePlayServices.SafetyNet", Version = "117.0.0", Text = googlePlay},

                new License {Name = "Xamarin.Azure.NotificationHubs.iOS", Version = "3.1.1", Text = microsoftAzure},
                new License {Name = "AusweisApp2 SDK", Version = "1.22.1", Text = ausweis}             
            };

            Licenses = unorder.OrderBy(x => x.Name).ToList();
        }

        public List<License> Licenses { get; set; }
        private string LoadEmbeddedTxt(string fileName)
        {
            string result = "";
            Assembly assembly = typeof(App).GetTypeInfo().Assembly;
            string filePath = assembly.GetManifestResourceNames().Single(x => x.EndsWith(fileName));
            Stream stream = assembly.GetManifestResourceStream(filePath);

            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }

            return result;
        }
    }
}