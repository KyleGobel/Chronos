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
    }
}