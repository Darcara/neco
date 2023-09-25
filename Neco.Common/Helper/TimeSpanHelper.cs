namespace Neco.Common.Helper;

using System;
using System.Text.RegularExpressions;

public static partial class TimeSpanHelper {
	/// <summary>
	/// <para>Constructs a TimeSpan from a string like "_d _h _m _s _ms" where _ denotes a <see cref="Double"/>. Spaces are optional</para>
	/// <para>A value of 'infinite' or 'never' will return <see cref="TimeSpan.MaxValue"/> or 10675199.02:48:05.4775807</para>
	/// </summary>
	public static TimeSpan FromString(String? source) {
		TimeSpan ts = TimeSpan.Zero;
		if (String.IsNullOrWhiteSpace(source)) return ts;
		if (String.Equals(source, "never", StringComparison.Ordinal) || String.Equals(source, "infinite", StringComparison.Ordinal)) return TimeSpan.MaxValue;

		MatchCollection matches = TimeSpanStringRegex().Matches(source);
		foreach (Match match in matches) {
			String numberStr = match.Groups[1].ToString();
			String time = match.Groups[2].ToString();
			Double number = Double.Parse(numberStr);
			if (Math.Abs(number) < Double.Epsilon) continue;

			ts += time switch {
				"d" => TimeSpan.FromDays(number),
				"h" => TimeSpan.FromHours(number),
				"m" => TimeSpan.FromMinutes(number),
				"s" => TimeSpan.FromSeconds(number),
				"ms" => TimeSpan.FromMilliseconds(number),
				var _ => TimeSpan.Zero,
			};
		}

		return ts;
	}

	[GeneratedRegex(@"([0-9]*\.?[0-9]+)\s*([d|h|m|s]+)")]
	private static partial Regex TimeSpanStringRegex();
}