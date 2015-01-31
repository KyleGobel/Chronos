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
            return Sha1(buffer);
        }
        public static string Sha256(string item)
        {
            var sha = Sha256(Encoding.UTF8.GetBytes(item));
            return sha;
        }

        public static string Sha256(byte[] bytes)
        {
            var crypto = new SHA256Managed();
            var hash = String.Empty;
            var bits = crypto.ComputeHash(bytes);
            foreach (var bit in bits)
            {
                hash += bit.ToString("x2");
            }
            return hash;
        }

        public static string Sha1(byte[] bytes)
        {
            var crypto = new SHA1CryptoServiceProvider();
            return BitConverter.ToString(crypto.ComputeHash(bytes)).Replace("-", "").ToLower();
        }

        public static string Md5(string text)
        {
            return Md5(Encoding.UTF8.GetBytes(text));
        }

        public static string Md5(byte[] bytes)
        {
            var md5 = MD5.Create();
            var hash = md5.ComputeHash(bytes);

            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString().ToLower();
        }
        
    }
}