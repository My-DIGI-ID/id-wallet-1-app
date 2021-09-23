using System.Text;

namespace IDWallet.Utils
{
    public class Sha256
    {
        public static byte[] sha256(string input)
        {
            System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(input));

            return crypto;
        }

        public static byte[] sha256(byte[] input)
        {
            System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();
            byte[] crypto = crypt.ComputeHash(input);

            return crypto;
        }
    }
}
