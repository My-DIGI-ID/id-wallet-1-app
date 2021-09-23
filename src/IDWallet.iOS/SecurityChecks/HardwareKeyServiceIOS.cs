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
        public string GetPublicKeyAsBase64(byte[] nonce, string alias)
        {
            SecKey privKey = GetPrivateKey(alias);
            while (privKey != null)
            {
                Debug.WriteLine("Key found");
                var deleted = SecKeyChain.Remove(new SecRecord(SecKind.Key)
                {
                    ApplicationTag = NSData.FromString(alias),
                    KeyType = SecKeyType.ECSecPrimeRandom,
                });

                Debug.WriteLine($"Key deleted: {deleted}");

                privKey = GetPrivateKey(alias);
            }

            CreateKey(nonce, alias);

            privKey = GetPrivateKey(alias);

            SecKey publKey = privKey.GetPublicKey();
            return publKey.GetExternalRepresentation().GetBase64EncodedString(NSDataBase64EncodingOptions.None);
        }

        public string Sign(byte[] nonce, string alias)
        {
            SecKey key = GetPrivateKey(alias);
            if (key != null)
            {
                NSError nSError;
                NSData encrypt = key.CreateSignature(SecKeyAlgorithm.EcdsaSignatureMessageX962Sha256, NSData.FromArray(nonce), out nSError);

                return encrypt.GetBase64EncodedString(NSDataBase64EncodingOptions.None);
            }

            return null;
        }

        public void CreateKey(byte[] nonce, string alias)
        {
            using (SecAccessControl access = new SecAccessControl(SecAccessible.WhenUnlockedThisDeviceOnly, SecAccessControlCreateFlags.PrivateKeyUsage))
            {
                SecKeyGenerationParameters keyParameters = new SecKeyGenerationParameters
                {
                    KeyType = SecKeyType.ECSecPrimeRandom,
                    KeySizeInBits = 256,
                    Label = alias,
                    ApplicationTag = NSData.FromString(alias),
                    // CanSign = true,
                    PrivateKeyAttrs = new SecKeyParameters
                    {
                        //IsPermanent = true,
                        ApplicationTag = NSData.FromString(alias),
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
                    ApplicationTag = NSData.FromString(alias),
                    KeyType = SecKeyType.ECSecPrimeRandom,
                };
                sr.SetKey(genKey);

                SecStatusCode ssc = SecKeyChain.Add(sr);
            }
        }

        private SecKey GetPrivateKey(string alias)
        {
            object privateKey = SecKeyChain.QueryAsConcreteType(
                new SecRecord(SecKind.Key)
                {
                    ApplicationTag = NSData.FromString(alias),
                    KeyType = SecKeyType.ECSecPrimeRandom,
                },
                out SecStatusCode code);
            return code == SecStatusCode.Success ? privateKey as SecKey : null;
        }
    }
}