using System.Globalization;
using System.Linq;

namespace Chronos
{
    public static class StringExtensions
    {
        public static string ToTitleCase(this string This)
        {
            if (string.IsNullOrEmpty(This))
                return string.Empty;
            var info = new CultureInfo("en-US");
            return info.TextInfo.ToTitleCase(This).Replace(" ", "");
        }

        public static string ToCamelCase(this string This)
        {
            var str = This.ToTitleCase();
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
    }
}