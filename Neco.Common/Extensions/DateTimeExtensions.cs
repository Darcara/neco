namespace Neco.Common.Extensions;

using System;
using System.Globalization;

public static class DateTimeExtensions {
	/// <summary>
	/// Returns a <see cref="DateTime"/> as Iso8601 format in the UTC/ZULU timezone: yyyy-MM-ddTHHmmss.ffffffZ <br/>
	/// In contrast the iso-format-option 'O' will include colons ':' in the time part and the timezone offset '+xx:xx' if not in UTC.
	/// </summary>
	/// <example>30.12.1970 18:55:33 = 1970-12-30T185533.123456Z</example>
	public static String ToIso8601(this DateTime source) => source.ToUniversalTime().ToString(@"yyyy-MM-dd\THHmmss.ffffff\Z");

	/// <summary>
	/// Returns a <see cref="DateTime"/> as a sortable timestamp in the UTC/ZULU timezone: yyyy-MM-dd-HHmmss
	/// </summary>
	/// <example>30.12.1970 18:55:33 = 1970-12-30-185533</example>
	public static String ToSortableString(this DateTime source) => source.ToUniversalTime().ToString("yyyy-MM-dd-HHmmss");

	/// <summary>
	/// Calculates the milliseconds since 01.01.1970 UTC
	/// </summary>
	/// <param name="date">The date to convert to unix time stamp. the date will be converted to UTC</param>
	/// <returns>Milliseconds since 01.01.1970 UTC</returns>
	public static Int64 ToUnixTime(this DateTime date) {
		DateTime utc = date.ToUniversalTime();
		if (utc < DateTime.UnixEpoch)
			return (Int64)(utc - DateTime.UnixEpoch).TotalMilliseconds;
		return (Int64)(utc - DateTime.UnixEpoch).TotalMilliseconds;
	}

	/// <summary>
	/// Calculates the seconds since 01.01.1970 UTC
	/// </summary>
	/// <remarks>
	/// Beware of accuracy issues when using double. Prefer to use DateTime.AddMilliseconds(500) instead of DateTime.AddSeconds(0.5) <br/>
	/// Be aware that UInt32 CANNOT encode times after 03:14:07 UTC on 19 January 2038.
	/// </remarks>
	/// <param name="date">The date to convert to unix time stamp. the date will be converted to UTC</param>
	/// <returns>Milliseconds since 01.01.1970 UTC</returns>
	public static Int64 ToUnixTimeSeconds(this DateTime date) {
		DateTime utc = date.ToUniversalTime();
		if (utc < DateTime.UnixEpoch)
			return (Int64)(utc - DateTime.UnixEpoch).TotalSeconds;
		return (Int64)(utc - DateTime.UnixEpoch).TotalSeconds;
	}

	/// <summary>
	/// Cahnges the <see cref="DateTime.TimeOfDay"/>, leaving the date untouched.
	/// </summary>
	/// <param name="dt">The datetime instance to change the time for</param>
	/// <param name="hour">The hours (0 through 23). Can be 24, when all other components are 0. This will change the time to the start of the next day.</param>
	/// <param name="minute">The minutes (0 through 59).</param>
	/// <param name="second">The seconds (0 through 59).</param>
	/// <param name="ms">The milliseconds (0 through 999).</param>
	/// <param name="microsecond">The microseconds (0 through 999).</param>
	/// <returns>A new instance of DateTime</returns>
	public static DateTime ChangeTime(this DateTime dt, Int32 hour = 0, Int32 minute = 0, Int32 second = 0, Int32 ms = 0, Int32 microsecond = 0) {
		if (hour == 24 && minute == 0 && second == 0 && ms == 0 && microsecond == 0)
			return dt.ChangeTime().AddDays(1);

		return new DateTime(dt.Year, dt.Month, dt.Day, hour, minute, second, ms, microsecond, dt.Kind);
	}

	/// <summary>
	/// <para>Calculates the first week of the year using ISO-8601, which defines the first week as the week that contains the first thursday or Januaray the 4th</para>
	/// <para><see href="https://en.wikipedia.org/wiki/Week#Week_numbering"/></para>
	/// </summary>
	public static Int32 WeekOfYear(this DateTime dt) {
		if (dt.DayOfWeek >= DayOfWeek.Monday && dt.DayOfWeek <= DayOfWeek.Wednesday) dt = dt.AddDays(3);
		return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
	}

	/// <summary>
	/// Returns true if the date of both <see cref="DateTime"/> values points to the same day and year. 
	/// </summary>
	public static Boolean IsOnSameDay(this DateTime dt, DateTime otherDay) => dt.DayOfYear == otherDay.DayOfYear && dt.Year == otherDay.Year;

	/// <summary>
	/// Returns the first day of the week for the given culture. If no culture is given, the <see cref="CultureInfo.CurrentCulture"/> is used.
	/// </summary>
	public static DateTime FirstDayOfWeekLocal(this DateTime date, CultureInfo? cultureInfo = null) {
		return date.FirstDayOfWeek((cultureInfo ?? CultureInfo.CurrentCulture).DateTimeFormat.FirstDayOfWeek);
	}

	/// <summary>
	/// Returns the first day of the week for the given <see cref="DateTimeFormatInfo"/>. If no <see cref="DateTimeFormatInfo"/> is given, the <see cref="CultureInfo.CurrentCulture"/> is used.
	/// </summary>
	public static DateTime FirstDayOfWeekLocal(this DateTime date, DateTimeFormatInfo? dateTimeFormatInfo = null) {
		return date.FirstDayOfWeek((dateTimeFormatInfo ?? CultureInfo.CurrentCulture.DateTimeFormat).FirstDayOfWeek);
	}

	/// <summary>
	/// Returns the first day of the week for the given <see cref="DayOfWeek"/>. If no day is given <see cref="DayOfWeek.Monday"/> (as per ISO 8601) is used.
	/// </summary>
	public static DateTime FirstDayOfWeek(this DateTime date, DayOfWeek firstDayOfWeek = DayOfWeek.Monday) {
		while (date.DayOfWeek != firstDayOfWeek) date = date.AddDays(-1);
		return date;
	}

	/// <summary>
	/// Returns the <see cref="DateOnly"/> for the given <see cref="DateTime"/>.
	/// </summary>
	public static DateOnly DateOnly(this DateTime dt) => new(dt.Year, dt.Month, dt.Day);

	/// <summary>
	/// Returns the <see cref="TimeOnly"/> for the given <see cref="DateTime"/>. This should be equivalent to <see cref="DateTime.TimeOfDay"/>.
	/// </summary>
	public static TimeOnly TimeOnly(this DateTime dt) => new(dt.Hour, dt.Minute, dt.Second, dt.Millisecond, dt.Microsecond);
}