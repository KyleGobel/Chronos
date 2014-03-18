using System;
using System.Collections.Generic;
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

        public static string ToCamelCase(this string value)
        {
            var str = value.ToTitleCase();
            if (string.IsNullOrEmpty(str))
                return string.Empty;
            var firstPart = str[0].ToString(CultureInfo.InvariantCulture).ToLower();
            var secondPart = str.Substring(1);

            return string.Join("", firstPart, secondPart);
        }

        public static bool IsBlank(this string This)
        {
            return string.IsNullOrWhiteSpace(This);
        }

        //service stack plagarism
        public static string Join(this List<string> items, string delimeter)
        {
            return String.Join(delimeter, items.ToArray());
        }
    }
}