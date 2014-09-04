using System;
using System.ComponentModel;
using Chronos;
using Xunit;

namespace Test
{
    [Trait("DateTime Extensions", "")]
    public class DateTimeTests
    {
        [Trait("DateTime Extensions", "StartOfDay")]
        public class StartOfDayMethod
        {
            [Fact(DisplayName = "Start of day returns start of day")]
            public void StartOfDay()
            {
                var anyDate = new DateTime(2014, 1, 3, 3, 24, 50);
                var expectedResult = new DateTime(anyDate.Year, anyDate.Month, anyDate.Day, 0, 0, 0);

                Assert.Equal(expectedResult, anyDate.StartOfDay());
            }
        }

        [Trait("DateTime Extensions", "EndOfDay")]
        public class EndOfDayMethod
        {
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


                Assert.Equal(expectedResult, anyDate.EndOfDay());
            }
        }

        [Trait("DateTime Extensions", "StartOfWeek")]
        public class StartOfWeekMethod
        {
            [Fact(DisplayName = "StartOfWeek return the start of the week")]
            public void StartOfWeek()
            {
                var anyDate = new DateTime(2014, 1, 3, 3, 24, 50);
                var expectedResult = new DateTime(2013, 12, 29, 0, 0, 0);

                Assert.Equal(expectedResult, anyDate.StartOfWeek());
            }
        }

        [Trait("DateTime Extensions", "EndOfWeek")]
        public class EndOfWeekMethod
        {
            [Fact(DisplayName = "EndOfWeek returns that weeks Saturday")]
            public void EndOfWeek()
            {
                var anyDate = new DateTime(2014, 1, 3, 3, 24, 50);
                var expectedResult = DateTime.Parse("2014-01-04 23:59:59.9999999");

                Assert.Equal(expectedResult, anyDate.EndOfWeek());
            }
        }



        [Trait("DateTime Extensions", "Unix Timestamps")]
        public class UnixTimestampMethods
        {
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

        [Trait("DateTime Extensions", "IsBetween")]
        public class IsBetweenMethod
        {
            [Fact(DisplayName = "Returns true when between two days")]
            public void IsBetweenValues()
            {
                DateTime start = new DateTime(2010, 1, 1);
                DateTime end = new DateTime(2010, 1, 3);

                DateTime testValue = new DateTime(2010, 1, 2);

                Assert.True(testValue.IsBetween(start, end));
            }

            [Fact(DisplayName = "Returns false when not between two days")]
            public void IsNotBetweenValues()
            {
                DateTime start = new DateTime(2010, 1, 1);
                DateTime end = new DateTime(2010, 1, 3);

                DateTime testValue = new DateTime(2010, 1, 4);

                Assert.False(testValue.IsBetween(start, end));
            }

            [Fact(DisplayName = "Returns true when between two times")]
            public void BetweenTimes()
            {
                DateTime start = new DateTime(2010, 1, 1, 1, 0, 0);
                DateTime end = new DateTime(2010, 1, 1, 3, 0, 0);

                DateTime testValue = new DateTime(2010, 1, 1, 2, 0, 0);

                Assert.True(testValue.IsBetween(start, end, true));
            }

            [Fact(DisplayName = "Returns false when not between two times")]
            public void IsFalseWhenNotSpecifiedTime()
            {
                DateTime start = DateTime.Parse("2010-1-1 1:30:00");
                DateTime end = DateTime.Parse("2010-1-1 3:00 AM");

                DateTime testValue = DateTime.Parse("2010-1-1 4:00 AM");

                Assert.False(testValue.IsBetween(start, end, true));
            }

        }

    }
}