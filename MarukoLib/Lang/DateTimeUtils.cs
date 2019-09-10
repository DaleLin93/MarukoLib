using System;
// ReSharper disable InconsistentNaming

namespace MarukoLib.Lang
{
    public static class DateTimeUtils
    {

        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static readonly long Jan1st1970Ticks = Jan1st1970.Ticks;

        public static long TimeMillis(this DateTime date) => (date.Ticks - Jan1st1970Ticks) / TimeSpan.TicksPerMillisecond;

        public static long CurrentTimeTicks => DateTime.UtcNow.Ticks;

        public static long CurrentTimeMillis => DateTime.UtcNow.TimeMillis();

        public static DateTime TimeMillisToDate(long time) => new DateTime(Jan1st1970.Ticks + time * 10000, DateTimeKind.Utc);

        public static bool IsSameDay(this DateTime a, DateTime b) => a.Year == b.Year && a.Month == b.Month && a.Day == b.Day;

    }

}
