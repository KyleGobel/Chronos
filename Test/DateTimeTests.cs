using System;
using Chronos;
using Xunit;

namespace Test
{
    [Trait("DateTime Extensions", "")]
    public class DateTimeTests
    {
        [Fact(DisplayName = "Start of day returns start of day")]
        public void StartOfDay()
        {
            var anyDate = new DateTime(2014, 1, 3, 3, 24, 50);
            var expectedResult = new DateTime(anyDate.Year, anyDate.Month, anyDate.Day, 0, 0, 0);

            Assert.Equal(expectedResult, anyDate.StartOfDay());
        }

        [Fact(DisplayName = "EndOfDay returns the end of the day")]
        public void EndOfDay()
        {
            var anyDate = new DateTime(2014, 1, 3, 3, 24, 50);
            var expectedResult = DateTime.Parse(
                string.Format(
                    "{0}-{1}-{2} {3}",
                    anyDate.Year,
                    anyDate.Month,
                    anyDate.Day,
                    "23:59:59.9999999"));


            Assert.Equal(expectedResult,anyDate.EndOfDay());
        }

        [Fact(DisplayName = "Converts to unix timestamp")]
        public void ToUnixTimeStampTest()
        {
            var testDate = new DateTime(2014, 1, 1);
            const long expectedTimestamp = 1388534400;

            Assert.Equal(expectedTimestamp, testDate.ToUnixTimestamp());

        }

        [Fact(DisplayName = "Converts a timestamp to System.DateTime")]
        public void FromUnixTimeStampTest()
        {
            const long testTimestamp = 1388534400;
            var expectedDate = new DateTime(2014, 1, 1);

            Assert.Equal(expectedDate, testTimestamp.FromUnixTimestamp());
        }
    }
}