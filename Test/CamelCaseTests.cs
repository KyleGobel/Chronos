using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chronos;
using Xunit;

namespace Test
{
    [Trait("String Extensions", "Camel Case")]
    public class CamelCaseTests
    {
        [Fact (DisplayName = "Converts a normal string with spaces")]
        public void CanConvertStringToCamelCase()
        {
            const string testString = "well this should be easy";
            var result = testString.ToCamelCase();
            Assert.Equal("wellThisShouldBeEasy", result);
        }

        [Fact (DisplayName = "Returns an empty string when the input is null or empty")]
        public void EmptyOrNullStringReturnsEmptyString()
        {
            const string testString = "";
            const string testString2 = null;

            var result1 = testString.ToCamelCase();
            var result2 = testString2.ToCamelCase();

            Assert.Equal(string.Empty, result1);
            Assert.Equal(string.Empty, result2);
        }
    }
}
