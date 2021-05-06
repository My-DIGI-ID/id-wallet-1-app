using Hyperledger.Aries.Extensions;
using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace IDWallet.Agent.Utils
{
    public class CustomCredentialUtils
    {
        private static readonly SHA256 sha256 = SHA256.Create();

        public static string GetEncoded(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                value = string.Empty;
            }

            if (int.TryParse(value, out int result))
            {
                return result.ToString();
            }

            byte[] data = new byte[] { 0 }
                .Concat(sha256.ComputeHash(value.GetUTF8Bytes()))
                .ToArray();

            Array.Reverse(data);
            return new BigInteger(value: data).ToString();
        }
    }
}