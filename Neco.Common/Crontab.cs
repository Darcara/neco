namespace Neco.Common;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;

/// <summary>
/// A
/// </summary>
/// <code>
/// Crontab expression format:
///
/// * * * * *
/// - - - - -
/// | | | | |
/// | | | | +----- day of week (0 - 6) (Sunday=0)
/// | | | +------- month (1 - 12)
/// | | +--------- day of month (1 - 31)
/// | +----------- hour (0 - 23)
/// +------------- min (0 - 59)
///
/// Star (*) in the value field above means all legal values as in
/// braces for that column. The value column can have a * or a list
/// of elements separated by commas. An element is either a number in
/// the ranges shown above or two numbers in the range separated by a
/// hyphen (meaning an inclusive range).
///
/// Source: http://www.adminschoice.com/crontab-quick-reference
///
/// Non standard:
///	  */X means every X. For minutes */15 is equal to 0,15,30,45 or every 15 minutes
///   Six-part expression format includes seconds
///
/// Six-part expression format:
///
/// * * * * * *
/// - - - - - -
/// | | | | | |
/// | | | | | +--- day of week (0 - 6) (Sunday=0)
/// | | | | +----- month (1 - 12)
/// | | | +------- day of month (1 - 31)
/// | | +--------- hour (0 - 23)
/// | +----------- min (0 - 59)
/// +------------- sec (0 - 59)
///
/// The six-part expression behaves similarly to the traditional
/// crontab format except that it can denotate more precise schedules
/// that use a seconds component.
///
/// Supported aliases:
///   @yearly  "0 0 0 1 1 *"
///   @monthly "0 0 0 1 * *"
///   @weekly  "0 0 0 * * 0" Sunday 00:00
///   @daily   "0 0 0 * * *"
///   @hourly  "0 0 * * * *"
/// </code>
public sealed class Crontab {
	public String Expression { get; }

	/// <summary>
	/// The whitespace separators that can separate each cron part.
	/// </summary>
	private static readonly Char[] _cronPartSeparators = { ' ', '\t' };

	private static readonly String[] _monthAlias = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

	private static readonly String[] _dayOfWeekAlias = { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
	private static Calendar Calendar => CultureInfo.InvariantCulture.Calendar;

	public DateTime PointInTime { get; set; } = DateTime.UtcNow;
	public DateTime NextPointInTime => CalculateNextOccurrence(PointInTime);

	// For each part this bitmask hold all allowed values
	// 194 = 60 seconds + 60 minutes + 24 hours + 31 days of month + 12 months + 7 days of week
	private readonly CrontabPart _seconds = new("second", 0, 59, null);

	private readonly CrontabPart _minutes = new("minute", 0, 59, null);

	private readonly CrontabPart _hours = new("hour", 0, 23, null);

	private readonly CrontabPart _dayOfMonth = new("day of month", 1, 31, null);

	private readonly CrontabPart _month = new("month", 1, 12, _monthAlias);

	private readonly CrontabPart _dayOfWeek = new("day of week", 0, 6, _dayOfWeekAlias);

	private Crontab(String expression) {
		Expression = expression;
	}

	public IEnumerable<DateTime> CalculateNextOccurrences(DateTime? startTime = null, DateTime? endTime = null) {
		DateTime pointInTime = startTime ?? PointInTime;
		DateTime exclusiveUpperBound = endTime ?? DateTime.MaxValue;

		while (pointInTime < exclusiveUpperBound) {
			DateTime next = CalculateNextOccurrence(pointInTime, exclusiveUpperBound);
			if (next >= exclusiveUpperBound) yield break;
			pointInTime = next;
			yield return next;
		}
	}

	public DateTime AdvancePointInTime() {
		DateTime next = NextPointInTime;
		PointInTime = NextPointInTime;
		return next;
	}

	public DateTime CalculateNextOccurrence(DateTime startTime) => CalculateNextOccurrence(startTime, DateTime.MaxValue);

	/// <summary>
	/// Endtime is exclusive.
	/// </summary>
	/// <param name="startTime"></param>
	/// <param name="endTime"></param>
	/// <returns>The next <see cref="DateTime"/> between start and endTimes or <see cref="DateTime.MaxValue"/> as 'never'</returns>
	public DateTime CalculateNextOccurrence(DateTime startTime, DateTime endTime) {
		Int32 baseYear = startTime.Year;
		Int32 baseMonth = startTime.Month - 1;
		Int32 baseDay = startTime.Day - 1;
		Int32 baseHour = startTime.Hour;
		Int32 baseMinute = startTime.Minute;
		Int32 baseSecond = startTime.Second;

		Int32 endYear = endTime.Year;
		Int32 endMonth = endTime.Month - 1;
		Int32 endDay = endTime.Day - 1;

		Int32 year = baseYear;
		Int32 month = baseMonth;
		Int32 day = baseDay;
		Int32 hour = baseHour;
		Int32 minute = baseMinute;
		Int32 second = baseSecond + 1;

		second = _seconds.Next(second);
		if (second == -1) {
			second = _seconds.FirstIndexSet;
			minute++;
		}

		minute = _minutes.Next(minute);
		if (minute == -1) {
			minute = _minutes.FirstIndexSet;
			hour++;
		}

		hour = _hours.Next(hour);
		if (hour == -1) {
			minute = _minutes.FirstIndexSet;
			hour = _hours.FirstIndexSet;
			day++;
		} else if (hour > baseHour) {
			minute = _minutes.FirstIndexSet;
		}

		day = _dayOfMonth.Next(day);

		RetryDayMonth:

		if (day == -1) {
			second = _seconds.FirstIndexSet;
			minute = _minutes.FirstIndexSet;
			hour = _hours.FirstIndexSet;
			day = _dayOfMonth.FirstIndexSet;
			month++;
		} else if (day > baseDay) {
			second = _seconds.FirstIndexSet;
			minute = _minutes.FirstIndexSet;
			hour = _hours.FirstIndexSet;
		}

		// Month
		month = _month.Next(month);

		if (month == -1) {
			second = _seconds.FirstIndexSet;
			minute = _minutes.FirstIndexSet;
			hour = _hours.FirstIndexSet;
			day = _dayOfMonth.FirstIndexSet;
			month = _month.FirstIndexSet;
			year++;
		} else if (month > baseMonth) {
			second = _seconds.FirstIndexSet;
			minute = _minutes.FirstIndexSet;
			hour = _hours.FirstIndexSet;
			day = _dayOfMonth.FirstIndexSet;
		}

		// The day field in a cron expression spans the entire range of days
		// in a month, which is from 1 to 31. However, the number of days in
		// a month tend to be variable depending on the month (and the year
		// in case of February). So a check is needed here to see if the
		// date is a border case. If the day happens to be beyond 28
		// (meaning that we're dealing with the suspicious range of 29-31)
		// and the date part has changed then we need to determine whether
		// the day still makes sense for the given year and month. If the
		// day is beyond the last possible value, then the day/month part
		// for the schedule is re-evaluated. So an expression like "0 0
		// 15,31 * *" will yield the following sequence starting on midnight
		// of Jan 1, 2000:
		// Jan 15, Jan 31, Feb 15, Mar 15, Apr 15, Apr 31, ...
		Boolean dateChanged = day != baseDay || month != baseMonth || year != baseYear;

		if (year >= 10000) return DateTime.MaxValue;

		if (day + 1 > 28 && dateChanged && day + 1 > Calendar.GetDaysInMonth(year, month + 1)) {
			if (year >= endYear && month >= endMonth && day >= endDay)
				return endTime;

			day = -1;
			goto RetryDayMonth;
		}

		DateTime nextTime = new(year, month + 1, day + 1, hour, minute, second, 0, startTime.Kind);

		if (nextTime >= endTime)
			return DateTime.MaxValue;

		// Day of week
		if (_dayOfWeek.IsSet((Int32)nextTime.DayOfWeek))
			return nextTime;

		return CalculateNextOccurrence(new DateTime(year, month + 1, day + 1, 23, 59, 59, 0, startTime.Kind), endTime);
	}

	public static Crontab Parse(String expression) {
		if (String.IsNullOrWhiteSpace(expression))
			throw new ArgumentNullException(nameof(expression));

		// TODO Add alias '@at <DateTime>' for a crontab that runs once only at the specified time
		// TODO Add alias '@every <>d <>h <>m <>s' for a crontab that runs every x days / hours / minutes / seconds
		// TODO Add extensions (@ after the default expression): @after <DateTime> -- @before(or @until) <DateTime> -- @times <number> (executed only x times)
		// TODO Add computed alias $RND for Random(min, max) for load distribution

		// any of the aliases?
		if (String.Equals(expression, "@yearly", StringComparison.OrdinalIgnoreCase) || String.Equals(expression, "@annually", StringComparison.OrdinalIgnoreCase))
			return Parse("0 0 0 1 1 *");
		if (String.Equals(expression, "@monthly", StringComparison.OrdinalIgnoreCase))
			return Parse("0 0 0 1 * *");
		if (String.Equals(expression, "@weekly", StringComparison.OrdinalIgnoreCase))
			return Parse("0 0 0 * * 0");
		if (String.Equals(expression, "@daily", StringComparison.OrdinalIgnoreCase) || String.Equals(expression, "@midnight", StringComparison.OrdinalIgnoreCase))
			return Parse("0 0 0 * * *");
		if (String.Equals(expression, "@hourly", StringComparison.OrdinalIgnoreCase))
			return Parse("0 0 * * * *");

		if (expression.StartsWith("@", StringComparison.Ordinal))
			throw new ArgumentException($"'{expression}' is an invalid crontab alias. Can only be one of: @yearly, @annually, @monthly, @weekly, @daily, @hourly", nameof(expression));

		String[] tokens = expression.Split(_cronPartSeparators, StringSplitOptions.RemoveEmptyEntries);

		// 6 tokens is with seconds, 5 is standard crontab format
		if (tokens.Length < 5 || tokens.Length > 6)
			throw new ArgumentException($"'{expression}' is an invalid crontab expression. It must contain 5 or 6 components of a schedule in the sequence of [seconds] minutes hours days months and days of week.", nameof(expression));

		Crontab cron = new(expression);

		Int32 tokenIdx = 0;
		cron._seconds.ParsePart(tokens.Length == 6 ? tokens[tokenIdx++] : "00");

		cron._minutes.ParsePart(tokens[tokenIdx++]);
		cron._hours.ParsePart(tokens[tokenIdx++]);
		cron._dayOfMonth.ParsePart(tokens[tokenIdx++]);
		cron._month.ParsePart(tokens[tokenIdx++]);
		cron._dayOfWeek.ParsePart(tokens[tokenIdx]);

		return cron;
	}

	[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
	public static Boolean TryParse(String cronDef, [NotNullWhen(true)] out Crontab? crontab) {
		try {
			crontab = Parse(cronDef);
			return true;
		}
		catch {
			crontab = null;
			return false;
		}
	}

	#region Overrides of Object

	/// <summary>
	/// Returns a string that represents the current object.
	/// </summary>
	/// <returns>
	/// A string that represents the current object.
	/// </returns>
	public override String ToString() => Expression;

	#endregion Overrides of Object

	public String ToDebugString() {
		StringBuilder sb = new();
		sb.Append(_seconds);
		sb.Append(".");
		sb.Append(_minutes);
		sb.Append(".");
		sb.Append(_hours);
		sb.Append(".");
		sb.Append(_dayOfMonth);
		sb.Append(".");
		sb.Append(_month);
		sb.Append(".");
		sb.Append(_dayOfWeek);
		sb.Append(".");
		return sb.ToString();
	}

	#region Equality members

	private Boolean Equals(Crontab other) =>
		_seconds.Equals(other._seconds)
		&& _minutes.Equals(other._minutes)
		&& _hours.Equals(other._hours)
		&& _dayOfMonth.Equals(other._dayOfMonth)
		&& _month.Equals(other._month)
		&& _dayOfWeek.Equals(other._dayOfWeek)
		&& PointInTime.Equals(other.PointInTime);

	/// <summary>
	/// Determines whether the specified object is equal to the current object.
	/// </summary>
	/// <returns>
	/// true if the specified object  is equal to the current object; otherwise, false.
	/// </returns>
	/// <param name="obj">The object to compare with the current object. </param>
	public override Boolean Equals(Object? obj) {
		if (ReferenceEquals(null, obj)) {
			return false;
		}

		if (ReferenceEquals(this, obj)) {
			return true;
		}

		return obj is Crontab crontab && Equals(crontab);
	}

	/// <summary>
	/// Serves as the default hash function.
	/// </summary>
	/// <returns>
	/// A hash code for the current object.
	/// </returns>
	public override Int32 GetHashCode() {
		unchecked {
			Int32 hashCode = _seconds.GetHashCode();
			hashCode = (hashCode * 397) ^ _minutes.GetHashCode();
			hashCode = (hashCode * 397) ^ _hours.GetHashCode();
			hashCode = (hashCode * 397) ^ _dayOfMonth.GetHashCode();
			hashCode = (hashCode * 397) ^ _month.GetHashCode();
			hashCode = (hashCode * 397) ^ _dayOfWeek.GetHashCode();
			return hashCode;
		}
	}

	public static Boolean operator ==(Crontab left, Crontab right) => Equals(left, right);

	public static Boolean operator !=(Crontab left, Crontab right) => !Equals(left, right);

	#endregion Equality members

	private sealed class CrontabPart {
		public Int32 FirstIndexSet { get; private set; }

		private readonly String _name;

		private readonly Int32 _minValue;

		private readonly Int32 _maxValue;

		private readonly String[]? _aliases;

		private readonly BitArray _bitmask;

		private String? _partExpression;

		internal CrontabPart(String name, Int32 minValue, Int32 maxValue, String[]? aliases) {
			_name = name;
			_minValue = minValue;
			_maxValue = maxValue;
			_aliases = aliases;
			_bitmask = new BitArray(maxValue - minValue + 1, true);
		}

		internal Int32 Next(Int32 baseIdx) {
			if (baseIdx < FirstIndexSet)
				return FirstIndexSet;

			for (Int32 i = baseIdx; i < _bitmask.Length; i++) {
				if (_bitmask[i]) return i;
			}

			return -1;
		}

		internal Boolean IsSet(Int32 idx) => _bitmask[idx];

		internal void ParsePart(String partExpression) {
			_partExpression = partExpression;
			try {
				// Special case 1: *
				if (String.Equals(partExpression, "*", StringComparison.Ordinal)) {
					_bitmask.SetAll(true);
					return;
				}

				// Must be an interval definition --> set everything to false and enable the specified
				_bitmask.SetAll(false);

				// Special case 2: Single number
				if (Int32.TryParse(partExpression, out Int32 val) && val >= _minValue && val <= _maxValue) {
					_bitmask.Set(val - _minValue, true);
					return;
				}

				// * is always first-last
				partExpression = partExpression.Replace("*", $"{_minValue}-{_maxValue}", StringComparison.Ordinal);

				// Can be a list separated by , (list may contain ranges(0-5,10-15) or steps(*/5,*/10))
				if (partExpression.Contains(',', StringComparison.Ordinal)) {
					String[] listElements = partExpression.Split(',');
					foreach (String listElement in listElements) {
						ParseRange(listElement);
					}
				} else {
					ParseRange(partExpression);
				}
			}
			finally {
				for (Int32 i = 0; i < _bitmask.Length; ++i)
					if (_bitmask[i]) {
						FirstIndexSet = i;
						break;
					}
			}
		}

		/// <summary>
		/// Can be a range separated by - (ranges may contain steps like 13-23/2 )
		/// or a single number
		/// </summary>
		private void ParseRange(String rangeExpression) {
			// contains steps ?
			Int32 stepIndex = rangeExpression.IndexOf('/', StringComparison.Ordinal);
			Int32 stepValue = -1;
			if (stepIndex > 0) {
				stepValue = ParseValue(rangeExpression.Substring(stepIndex + 1), false);
				if (stepValue <= 0 || stepValue > _maxValue) throw new ValueParseException($"Step value must be greater or equal to 1  and less than {_maxValue} in {_name} part '{_partExpression}'");
				rangeExpression = rangeExpression.Substring(0, stepIndex);
			}

			if (!rangeExpression.Contains('-', StringComparison.Ordinal)) {
				// Can only a single value, since * has been replaced to first-last
				Int32 value = ParseValue(rangeExpression);
				if (stepValue <= 0) {
					_bitmask.Set(value, true);
					return;
				}

				// Single value with step 2/5 means every 5th starting at 2 --> 2,7,12,17 ...
				for (Int32 i = value; i < _bitmask.Length; i += stepValue) {
					_bitmask.Set(i, true);
				}

				return;
			}

			String[] rangeParts = rangeExpression.Split('-');
			if (rangeParts.Length != 2) throw new ValueParseException($"Crontab range must have exactly two parts delimited with a '-' but was: {rangeExpression} in {_name} part '{_partExpression}'");
			Int32 startValue = ParseValue(rangeParts[0]);
			Int32 endValue = ParseValue(rangeParts[1]);

			if (endValue <= startValue) throw new ValueParseException($"Crontab range must be a positive integer range, but left value {startValue} <= {endValue} right value in {_name} part '{_partExpression}'");

			for (Int32 i = startValue; i <= endValue; i += stepValue <= 0 ? 1 : stepValue)
				_bitmask.Set(i, true);
		}

		private Int32 ParseValue(String value, Boolean convertToIndex = true) {
			if (value.Length == 0)
				throw new ValueParseException($"Crontab value cannot be empty in {_name} part '{_partExpression}'");
			Char firstChar = value[0];
			if (firstChar >= '0' && firstChar <= '9') {
				if (!Int32.TryParse(value, out Int32 val) || val < _minValue || val > _maxValue)
					throw new ValueParseException($"Crontab value '{value}' not in expected range {_minValue} - {_maxValue} (inclusive) in {_name} part '{_partExpression}'");
				return val - (convertToIndex ? _minValue : 0);
			}

			if (_aliases == null || _aliases.Length == 0)
				throw new ValueParseException($"Crontab value '{value}' not in expected range {_minValue} - {_maxValue} (inclusive) in {_name} part '{_partExpression}'");
			for (Int32 i = 0; i < _aliases.Length; ++i) {
				if (value.StartsWith(_aliases[i], StringComparison.InvariantCultureIgnoreCase)) {
					Int32 idx = i % _bitmask.Length;
					return convertToIndex ? idx : idx + _minValue;
				}
			}

			throw new ValueParseException($"Crontab value '{value}' not in expected range {_minValue} - {_maxValue} (inclusive) or a known alias like: {String.Join(", ", _aliases)} in {_name} part '{_partExpression}'");
		}

		#region Overrides of Object

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>
		/// A string that represents the current object.
		/// </returns>
		public override String ToString() {
			StringBuilder sb = new(_maxValue - _minValue);
			for (Int32 i = 0; i < _bitmask.Length; i++) {
				sb.Append(_bitmask[i] ? "1" : "0");
			}

			return sb.ToString();
		}

		#endregion Overrides of Object

		#region Equality members

		private Boolean Equals(CrontabPart other) {
			BitArray otherDef = other._bitmask;
			for (Int32 j = 0; j < _bitmask.Length; j++) {
				if (_bitmask[j] != otherDef[j])
					return false;
			}

			return true;
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current object.
		/// </summary>
		/// <returns>
		/// true if the specified object  is equal to the current object; otherwise, false.
		/// </returns>
		/// <param name="obj">The object to compare with the current object. </param>
		public override Boolean Equals(Object? obj) {
			if (ReferenceEquals(null, obj)) {
				return false;
			}

			if (ReferenceEquals(this, obj)) {
				return true;
			}

			return obj is CrontabPart crontabPart && Equals(crontabPart);
		}

		/// <summary>
		/// Serves as the default hash function.
		/// </summary>
		/// <returns>
		/// A hash code for the current object.
		/// </returns>
		public override Int32 GetHashCode() {
			return _bitmask.OfType<Boolean>().Count(b => b);
		}

		#endregion Equality members
	}
}