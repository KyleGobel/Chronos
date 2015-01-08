using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Chronos
{
    public static class StringExtensions
    {
        public static string ToTitleCase(this string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var info = new CultureInfo("en-US");
            return info.TextInfo.ToTitleCase(value).Replace(" ", "");
        }

        public static string ToMd5(this string value)
        {
            return Encrypt.Md5(value);
        }

        public static string ToSha1(this string value)
        {
            return Encrypt.Sha1(value);
        }

        public static string ToSha256(this string value)
        {
            return Encrypt.Sha256(value);
        }
    }
}