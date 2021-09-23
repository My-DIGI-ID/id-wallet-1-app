using IDWallet.Models;
using IDWallet.Views.Customs.PopUps;
using System.IO;
using Xamarin.Forms;

namespace IDWallet.Views.Wallet.PopUps
{
    public partial class OfferCredentialPopUp : CustomPopUp
    {
        private readonly WalletCredentialOfferMessage _viewModel;

        public OfferCredentialPopUp(WalletCredentialOfferMessage walletCredentialOfferMessage)
        {
            switch (walletCredentialOfferMessage.CredentialRecord.CredentialDefinitionId.Split(':')[0])
            {
                case "JiVLsA5wxVnbHQ5s7pDN58":
                    walletCredentialOfferMessage.MessageImageSource = ImageSource.FromFile("ibm_logo.png");
                    break;
                case "Vq2C7Wfc44Q1cSroPuXaw2":
                case "MGfd8JjWRoiXMm2YGL4SGj":
                    walletCredentialOfferMessage.MessageImageSource = ImageSource.FromFile("bdr_logo.png");
                    break;
                case "9hsRe5jdzAbbyLAStV6sPc":
                case "KqtBRiQSyWqnzaxN3u2d7G":
                    walletCredentialOfferMessage.MessageImageSource = ImageSource.FromFile("kraftfahrtbundesamt_logo.png");
                    break;
                case "En38baYaTqVYSB8SFwguhT":
                case "9HX4bs8pdH2uJB7sjeWPtU":
                    walletCredentialOfferMessage.MessageImageSource = ImageSource.FromFile("bosch_logo.png");
                    break;
                case "VkVqDPzeDCQe31H3RsMzbf":
                case "EKtoaKk2ifgmY4cQxYAwcE":
                    walletCredentialOfferMessage.MessageImageSource = ImageSource.FromFile("dbahn_logo.png");
                    break;
                case "7vyyugPwC3ArWtRWbz6LCm":
                case "PsDLsaget7L9duoaxzC2DZ":
                    walletCredentialOfferMessage.MessageImageSource = ImageSource.FromFile("bwi_logo.png");
                    break;
                case "X2p16G1BeEceJauzqofjQW":
                    walletCredentialOfferMessage.MessageImageSource = ImageSource.FromFile("dlufthansa_logo.png");
                    break;
                case "XnGEZ7gJxDNfxwnZpkkVcs":
                    //case "KuXsPLZAsxgjbaAeQd4rr8":
                    walletCredentialOfferMessage.MessageImageSource = ImageSource.FromFile("kba_logo.png");
                    break;
                default:
                    if (walletCredentialOfferMessage.CredentialRecord.CredentialDefinitionId.Equals("KXtvfp6c9ma1NBtttKpV6W:3:CL:75:Impfzertifikat"))
                    {
                        walletCredentialOfferMessage.MessageImageSource = ImageSource.FromFile("vac_transparent_icon.png");
                    }
                    else
                    {
                        walletCredentialOfferMessage.MessageImageSource = ImageSource.FromFile("default_logo.png");
                    }
                    break;
            }

            InitializeComponent();
            BindingContext = _viewModel = walletCredentialOfferMessage;

            if (_viewModel.EmbeddedByteArray != null)
            {
                Stream embeddedImageStream = new MemoryStream(_viewModel.EmbeddedByteArray);
                _viewModel.EmbeddedImage = ImageSource.FromStream(() => embeddedImageStream);
                EmbeddedImageLabel.IsVisible = true;
                EmbeddedImageFrame.IsVisible = true;
            }
        }
    }
}