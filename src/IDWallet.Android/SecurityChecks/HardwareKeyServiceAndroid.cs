using Android.Content.PM;
using Android.Security.Keystore;
using IDWallet.Droid.SecurityChecks;
using IDWallet.Interfaces;
using Java.Security;
using Java.Security.Cert;
using Java.Security.Spec;
using Java.Util;
using Newtonsoft.Json;
using Xamarin.Essentials;

[assembly: Xamarin.Forms.Dependency(typeof(HardwareKeyServiceAndroid))]
namespace IDWallet.Droid.SecurityChecks
{
    public class HardwareKeyServiceAndroid : IHardwareKeyService
    {
        private const string ALIAS = "BaseIdHWKey";
        private const string TRANSFORMATION = "SHA256withECDSA";
        private const string KEYALGORITHM = KeyProperties.KeyAlgorithmEc;
        private const string KEYSTORE_TYPE = "AndroidKeyStore";
        private const int AUTHENTICATION_LEVEL = 32768;

        public string GetPublicKeyAsBase64(byte[] nonce)
        {
            // Check if PPK pair is allready generated        
            bool strongBoxBacked = StrongBoxFeatureAvailable();
            bool isPhysicalDevice = IsPhysicalDevice();

            GenerateNewKeyPair(nonce, strongBoxBacked);

            KeyStore keyStore = KeyStore.GetInstance(KEYSTORE_TYPE);
            keyStore.Load(null);
            Certificate[] certificates = keyStore.GetCertificateChain(ALIAS);
            string[] certArr = CreateCertificateChainArray(certificates);

            return JsonConvert.SerializeObject(certArr);
        }

        public string Sign(byte[] nonce)
        {
            KeyStore.PrivateKeyEntry entry = null;
            bool exists = TryGetPrivateKey(out entry);
            if (exists)
            {
                IPrivateKey privateKey = entry.PrivateKey;

                Java.Security.Signature signature = Java.Security.Signature.GetInstance(TRANSFORMATION);
                signature.InitSign(privateKey);

                signature.Update(nonce);

                try
                {
                    byte[] signed = signature.Sign();

                    Base64.Encoder encoder = Base64.GetEncoder();
                    return encoder.EncodeToString(signed);
                }
                catch (SignatureException)
                {
                    return null;
                }
            }
            return null;
        }

        private bool TryGetPrivateKey(out KeyStore.PrivateKeyEntry entry)
        {
            KeyStore keyStore = KeyStore.GetInstance(KEYSTORE_TYPE);
            keyStore.Load(null);
            entry = (KeyStore.PrivateKeyEntry)keyStore.GetEntry(ALIAS, null);

            // Check if PPK pair is allready generated        
            if (entry == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private static string[] CreateCertificateChainArray(Certificate[] certificates)
        {
            string[] certArr = new string[certificates.Length];

            for (int i = 0; i < certificates.Length; i++)
            {
                certArr[i] = $"-----BEGIN CERTIFICATE-----{EncodeToString(certificates[i].GetEncoded())}-----END CERTIFICATE-----";
            }

            return certArr;
        }

        private static string EncodeToString(byte[] bytesToEncde)
        {
            Base64.Encoder encoder = Base64.GetEncoder();
            return encoder.EncodeToString(bytesToEncde);
        }

        private static void GenerateNewKeyPair(byte[] nonce, bool strongBoxBacked)
        {
            KeyPairGenerator kpGenerator = KeyPairGenerator.GetInstance(KEYALGORITHM, KEYSTORE_TYPE);
            // This failes, when the user does not have a biometric enrollment
            KeyGenParameterSpec spec = new KeyGenParameterSpec.Builder(ALIAS, KeyStorePurpose.Sign)
                .SetDigests(KeyProperties.DigestSha256)
                .SetAlgorithmParameterSpec(new ECGenParameterSpec("secp256r1"))
                .SetRandomizedEncryptionRequired(false)
                .SetAttestationChallenge(nonce)
                .SetIsStrongBoxBacked(strongBoxBacked)
                .Build();

            kpGenerator.Initialize(spec);
            kpGenerator.GenerateKeyPair();
        }

        private bool StrongBoxFeatureAvailable()
        {
            return Android.App.Application.Context.PackageManager.HasSystemFeature(PackageManager.FeatureStrongboxKeystore);
        }

        private bool IsPhysicalDevice()
        {
            return DeviceInfo.DeviceType == DeviceType.Physical;
        }
    }
}