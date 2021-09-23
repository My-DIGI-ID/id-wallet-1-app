using IDWallet.Models;
using IDWallet.Resources;
using IDWallet.Views.Customs.PopUps;
using Hyperledger.Aries.Features.IssueCredential;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using ZXing.Net.Mobile.Forms;

namespace IDWallet.Views.History.PopUps
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HistoryQrPopUp : CustomPopUp
    {
        public HistoryQrPopUp(HistoryCredentialElement historyCredentialElement)
        {
            BindingContext = historyCredentialElement;
            InitializeComponent();

            try
            {
                DateTimeSpan.Text = historyCredentialElement.CreatedAtUtc.Value.ToLocalTime().ToString();
            }
            catch
            {
                //ignore
            }
        }

        private Command<string> _openPdfButtonTappedCommand;
        public Command<string> OpenPdfButtonTappedCommand =>
            _openPdfButtonTappedCommand ??= new Command<string>(OpenPdfButtonTapped);

        private void OpenPdfButtonTapped(string documentString)
        {
            App.ViewFile(documentString);
        }

        private void Label_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Label nameLabel = sender as Label;
            if (nameLabel.Text != null && ((StackLayout)nameLabel.Parent).Children.Count < 2)
            {
                StackLayout stackLayout = nameLabel.Parent as StackLayout;
                CredentialPreviewAttribute claim = nameLabel.BindingContext as CredentialPreviewAttribute;
                if (nameLabel.Text == "embeddedImage")
                {
                    Utils.Converter.EmbeddedImageConverter converter = new Utils.Converter.EmbeddedImageConverter();
                    FFImageLoading.Forms.CachedImage cachedImage = new FFImageLoading.Forms.CachedImage
                    {
                        Source = (ImageSource)converter.Convert(claim.Value, null, null, null),
                        Aspect = Aspect.AspectFit,
                        ErrorPlaceholder = ImageSource.FromFile("default_logo.png"),
                        LoadingPlaceholder = ImageSource.FromFile("default_logo.png")
                    };
                    Frame imageFrame = new Frame
                    {
                        CornerRadius = 0,
                        HeightRequest = 50,
                        WidthRequest = 100,
                        HorizontalOptions = LayoutOptions.Start
                    };

                    imageFrame.Content = cachedImage;
                    stackLayout.Children.Add(imageFrame);
                }
                else if (nameLabel.Text == "embeddedDocument")
                {
                    Plugin.Iconize.IconButton iconButton = new Plugin.Iconize.IconButton
                    {
                        WidthRequest = 35,
                        HeightRequest = 35,
                        Padding = 0,
                        BackgroundColor = Color.Transparent,
                        TextColor = (Color)Application.Current.Resources["PrimaryTextColor"],
                        Text = "mdi-file-pdf",
                        FontSize = 30,
                        Command = OpenPdfButtonTappedCommand,
                        CommandParameter = claim.Value,
                        HorizontalOptions = LayoutOptions.Start
                    };

                    stackLayout.Children.Add(iconButton);
                }
                else if (nameLabel.Text == "COVID-Zertifikat")
                {
                    HistoryCredentialTitle.Text = Lang.Qr_History_Message_1;
                    HistoryCredentialText2.Text = Lang.Qr_History_Message_2;
                    nameLabel.Text = "";
                    ZXingBarcodeImageView cachedImage = new ZXingBarcodeImageView
                    {
                        BarcodeValue = claim.Value.ToString(),
                        Aspect = Aspect.AspectFit,
                        BarcodeFormat = ZXing.BarcodeFormat.QR_CODE,
                        BarcodeOptions = new ZXing.Common.EncodingOptions { Width = 350, Height = 350 },
                        HeightRequest = 350
                    };
                    Frame imageFrame = new Frame
                    {
                        CornerRadius = 0,
                        HeightRequest = 350,
                        WidthRequest = 350,
                        HorizontalOptions = LayoutOptions.Center
                    };

                    imageFrame.Content = cachedImage;
                    stackLayout.Children.Add(imageFrame);
                }
                else
                {
                    Label valueLabel = new Label
                    {
                        Text = claim.Value.ToString(),
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 16,
                        TextColor = (Color)Application.Current.Resources["AttributeValueColor"]
                    };
                    stackLayout.Children.Add(valueLabel);
                }
            }
        }
    }
}