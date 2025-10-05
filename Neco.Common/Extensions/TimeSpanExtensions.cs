namespace Neco.Common.Extensions;

using System;
using System.Globalization;
using System.Text;

public static class TimeSpanExtensions {
	/// <summary>
	/// Days, HH:mm:ss.fff = 123, 12:23:44.003
	/// </summary>
	public static String ToReadableString(this TimeSpan source) {
		StringBuilder fmt = new();
		if (source.TotalDays > 1)
			fmt.Append(CultureInfo.InvariantCulture, $"{Math.Floor(source.TotalDays):0}, ");
		if (source.TotalHours > 1)
			fmt.Append(CultureInfo.InvariantCulture, $"{source.Hours:00}:");
		if (source.TotalMinutes > 1)
			fmt.Append(CultureInfo.InvariantCulture, $"{source.Minutes:00}:");
		fmt.Append(CultureInfo.InvariantCulture, $"{source.Seconds:00}.{source.Milliseconds:000}");
		return fmt.ToString();
	}

	/// <summary>
	/// +-HH:mm = +55:44
	/// </summary>
	public static String ToReadableStringHours(this TimeSpan span, Boolean signed = true) {
		Double hours = Math.Floor(Math.Abs(span.TotalHours));
		Int32 minutes = Math.Abs(span.Minutes);
		if (span.Seconds > 30) minutes += 1;
		if (minutes >= 60) {
			++hours;
			minutes -= 60;
		}

		return $"{(signed ? span < TimeSpan.Zero ? "-" : "+" : String.Empty)}{hours:00}:{minutes:00}";
	}

	/// <summary>
	/// +-HH:mm:ss.fff = +55:23:01.004
	/// </summary>
	public static String ToReadableStringExact(this TimeSpan span) => $"{(span < TimeSpan.Zero ? "-" : "+")}{Math.Floor(Math.Abs(span.TotalHours)):00}:{Math.Abs(span.Minutes):00}:{Math.Abs(span.Seconds):00}.{Math.Abs(span.Milliseconds):000}";
}