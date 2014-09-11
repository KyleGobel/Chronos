using System;
using System.Security.Cryptography;
using System.Text;

namespace Chronos
{
    public class Encrypt
    {
        public static string Sha1(string item)
        {
            var buffer = Encoding.UTF8.GetBytes(item);
            var cryptoTransformSha1 = new SHA1CryptoServiceProvider();
            return BitConverter.ToString(cryptoTransformSha1.ComputeHash(buffer)).Replace("-", "").ToLower();
        }
        public static string Sha256(string item)
        {
            var crypt = new SHA256Managed();
            var hash = String.Empty;
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(item), 0, Encoding.UTF8.GetByteCount(item));
            foreach (var bit in crypto)
            {
                hash += bit.ToString("x2");
            }
            return hash;
        }

        public static string Md5(string text)
        {
            var md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(text);
            var hash = md5.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }
        
    }
}