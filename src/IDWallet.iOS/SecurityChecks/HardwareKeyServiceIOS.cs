using IDWallet.Interfaces;
using IDWallet.iOS.SecurityChecks;
using IDWallet.Utils;
using Foundation;
using Security;
using System.Diagnostics;

[assembly: Xamarin.Forms.Dependency(typeof(HardwareKeyServiceIOS))]
namespace IDWallet.iOS.SecurityChecks
{
    public class HardwareKeyServiceIOS : IHardwareKeyService
    {
        private const string ALIAS = "BaseIdHWKey";

        public string GetPublicKeyAsBase64(byte[] nonce)
        {
            SecKey privKey = GetPrivateKey();
            while (privKey != null)
            {
                Debug.WriteLine("Key found");
                var deleted = SecKeyChain.Remove(new SecRecord(SecKind.Key)
                {
                    ApplicationTag = NSData.FromString(ALIAS),
                    KeyType = SecKeyType.ECSecPrimeRandom,
                });

                Debug.WriteLine($"Key deleted: {deleted}");

                privKey = GetPrivateKey();
            }

            CreateKey(nonce);

            privKey = GetPrivateKey();

            SecKey publKey = privKey.GetPublicKey();
            return publKey.GetExternalRepresentation().GetBase64EncodedString(NSDataBase64EncodingOptions.None);
        }

        public string Sign(byte[] nonce)
        {
            SecKey key = GetPrivateKey();
            if (key != null)
            {
                NSError nSError;
                NSData encrypt = key.CreateSignature(SecKeyAlgorithm.EcdsaSignatureMessageX962Sha256, NSData.FromArray(nonce), out nSError);

                return encrypt.GetBase64EncodedString(NSDataBase64EncodingOptions.None);
            }

            return null;
        }

        public void CreateKey(byte[] nonce)
        {
            using (SecAccessControl access = new SecAccessControl(SecAccessible.WhenUnlockedThisDeviceOnly, SecAccessControlCreateFlags.PrivateKeyUsage))
            {
                SecKeyGenerationParameters keyParameters = new SecKeyGenerationParameters
                {
                    KeyType = SecKeyType.ECSecPrimeRandom,
                    KeySizeInBits = 256,
                    Label = ALIAS,
                    ApplicationTag = NSData.FromString(ALIAS),
                    // CanSign = true,
                    PrivateKeyAttrs = new SecKeyParameters
                    {
                        //IsPermanent = true,
                        ApplicationTag = NSData.FromString(ALIAS),
                        AccessControl = access
                    },
                    PublicKeyAttrs = new SecKeyParameters
                    {
                        ApplicationTag = NSData.FromArray(Sha256.sha256(nonce))
                    }
                };

                NSError error;
                SecKey genKey = SecKey.CreateRandomKey(keyParameters, out error);
                if (genKey == null)
                {
                    throw new NSErrorException(error);
                }

                SecRecord sr = new SecRecord(SecKind.Key)
                {
                    ApplicationTag = NSData.FromString(ALIAS),
                    KeyType = SecKeyType.ECSecPrimeRandom,
                };
                sr.SetKey(genKey);

                SecStatusCode ssc = SecKeyChain.Add(sr);
            }
        }

        private SecKey GetPrivateKey()
        {
            object privateKey = SecKeyChain.QueryAsConcreteType(
                new SecRecord(SecKind.Key)
                {
                    ApplicationTag = NSData.FromString(ALIAS),
                    KeyType = SecKeyType.ECSecPrimeRandom,
                },
                out SecStatusCode code);
            return code == SecStatusCode.Success ? privateKey as SecKey : null;
        }
    }
}