using System;

namespace Chronos
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfDay(this DateTime date)
        {
            return date.Date;
        }

        public static DateTime EndOfDay(this DateTime date)
        {
            return date.Date.AddDays(1).AddTicks(-1);
        }

        public static DateTime StartOfWeek(this DateTime date)
        {
            return date.Date.AddDays(-(int)date.DayOfWeek);
        }

        public static DateTime EndOfWeek(this DateTime date)
        {
            return date.Date.AddDays(7 - (int)date.DayOfWeek).AddTicks(-1);
        }

        /// <summary>
        /// Converts a System.DateTime object to Unix timestamp
        /// </summary>
        /// <returns>The Unix timestamp</returns>
        public static long ToUnixTimestamp(this DateTime date)
        {
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0);
            var unixTimeSpan = date - unixEpoch;

            return (long)unixTimeSpan.TotalSeconds;
        }

        /// <summary>
        /// Converts a Unix timestamp to a System.DateTime
        /// </summary>
        /// <returns>A System.DateTime</returns>
        public static DateTime FromUnixTimestamp(this long unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(unixTime);
        }

        /// <summary>
        /// Is a date between the start and endDate?
        /// </summary>
        /// <param name="compareTime">Compare the time rather than date?</param>
        public static Boolean IsBetween(this DateTime dt, DateTime startDate, DateTime endDate, Boolean compareTime = false)
        {
            return compareTime ?
               dt >= startDate && dt <= endDate :
               dt.Date >= startDate.Date && dt.Date <= endDate.Date;
        }

        public static DateTime StartOfLastMonth(this DateTime from)
        {
            return new DateTime(from.Date.Year, from.Date.Month, 1).AddMonths(-1);
        }

        public static DateTime EndOfLastMonth(this DateTime from)
        {
            return new DateTime(from.Date.Year, from.Date.Month, 1).AddDays(-1);
        }

    }
}