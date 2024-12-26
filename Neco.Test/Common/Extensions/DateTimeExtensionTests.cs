namespace Neco.Test.Common.Extensions;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using FluentAssertions;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
public class DateTimeExtensionTests {
	[Test]
	public void ToIso8601() {
		DateTime dateTime = new(2020, 1, 1, 12, 0, 0, DateTimeKind.Local);

		String isoByFormat = dateTime.ToString("O");
		String isoByExtension = dateTime.ToIso8601();

		Console.WriteLine($"ISO8601 by format string: {isoByFormat}");
		Console.WriteLine($"ISO8601 by extension    : {isoByExtension}");

		isoByExtension.Should().EndWith("Z");
		isoByExtension.Should().NotContainAny(":", "+");
	}

	[Test]
	public void ToSortableString() {
		DateTime dateTime = new(2020, 1, 2, 3, 4, 5, 6, 7, DateTimeKind.Local);
		DateTime utc = dateTime.ToUniversalTime();

		String sortable = dateTime.ToSortableString();

		Console.WriteLine(sortable);

		sortable.Should().NotContainAny(Path.GetInvalidPathChars().Select(c => c.ToString()));
		sortable.Should().NotContainAny(Path.GetInvalidFileNameChars().Select(c => c.ToString()));
		sortable.Should().Be($"{utc.Year}-{utc.Month:00}-{utc.Day:00}-{utc.Hour:00}{utc.Minute:00}{utc.Second:00}");
	}

	[Test]
	public void FromUnixTime() {
		DateTime.UnixEpoch.ToUnixTime().Should().Be(0L);
		DateTime.UnixEpoch.ToUnixTimeSeconds().Should().Be(0L);

		DateTime.UnixEpoch.AddMilliseconds(-500).ToUnixTime().Should().Be(-500L);
		DateTime.UnixEpoch.AddMilliseconds(-500).ToUnixTimeSeconds().Should().Be(-0L);

		DateTime.UnixEpoch.AddSeconds(-5).ToUnixTime().Should().Be(-5000L);
		DateTime.UnixEpoch.AddSeconds(-5).ToUnixTimeSeconds().Should().Be(-5L);

		DateTime.UnixEpoch.AddMilliseconds(-5010).ToUnixTime().Should().Be(-5010L);
		DateTime.UnixEpoch.AddMilliseconds(-5010).ToUnixTimeSeconds().Should().Be(-5L);

		DateTime.UnixEpoch.AddMicroseconds(-5010010).ToUnixTime().Should().Be(-5010L);
		DateTime.UnixEpoch.AddMicroseconds(-5010010).ToUnixTimeSeconds().Should().Be(-5L);


		DateTime.UnixEpoch.AddMilliseconds(500).ToUnixTime().Should().Be(500L);
		DateTime.UnixEpoch.AddMilliseconds(500).ToUnixTimeSeconds().Should().Be(0L);

		DateTime.UnixEpoch.AddSeconds(5).ToUnixTime().Should().Be(5000L);
		DateTime.UnixEpoch.AddSeconds(5).ToUnixTimeSeconds().Should().Be(5L);

		DateTime.UnixEpoch.AddMilliseconds(5010).ToUnixTime().Should().Be(5010L);
		DateTime.UnixEpoch.AddMilliseconds(5010).ToUnixTimeSeconds().Should().Be(5L);

		DateTime.UnixEpoch.AddMicroseconds(5010010).ToUnixTime().Should().Be(5010L);
		DateTime.UnixEpoch.AddMicroseconds(5010010).ToUnixTimeSeconds().Should().Be(5L);
	}

	[Test]
	public void ChangeTime() {
		DateTime dateTime = new(2020, 1, 2, 3, 4, 5, 6, 7, DateTimeKind.Local);
		DateTime changed = dateTime.ChangeTime(4, 5, 6, 7, 8);

		changed.DayOfYear.Should().Be(dateTime.DayOfYear);
		changed.Year.Should().Be(dateTime.Year);
		changed.Hour.Should().Be(4);
		changed.Minute.Should().Be(5);
		changed.Second.Should().Be(6);
		changed.Millisecond.Should().Be(7);
		changed.Microsecond.Should().Be(8);
		changed.Kind.Should().Be(DateTimeKind.Local);

		changed = dateTime.ChangeTime(24);
		changed.Day.Should().Be(dateTime.Day + 1);
		changed.Hour.Should().Be(0);
	}

	[Test]
	public void WeekOfYear() {
		new DateTime(2019, 12, 23, 0, 0, 0, DateTimeKind.Utc).WeekOfYear().Should().Be(52);
		new DateTime(2019, 12, 30, 0, 0, 0, DateTimeKind.Utc).WeekOfYear().Should().Be(1);
		new DateTime(2020, 01, 01, 0, 0, 0, DateTimeKind.Utc).WeekOfYear().Should().Be(1);
		
		new DateTime(2020, 12, 23, 0, 0, 0, DateTimeKind.Utc).WeekOfYear().Should().Be(52);
		new DateTime(2020, 12, 30, 0, 0, 0, DateTimeKind.Utc).WeekOfYear().Should().Be(53);
		new DateTime(2021, 01, 01, 0, 0, 0, DateTimeKind.Utc).WeekOfYear().Should().Be(53);
	}

	[Test]
	public void IsOnSameDay() {
		DateOnly day = new(2020, 1, 2);
		day.ToDateTime(TimeOnly.MinValue).IsOnSameDay(day.ToDateTime(new TimeOnly(12,12))).Should().BeTrue();
	}

	[Test]
	public void Only() {
		DateTime dateTime = new(2020, 1, 2, 3, 4, 5, 6, 7, DateTimeKind.Local);
		dateTime.DateOnly().Year.Should().Be(2020);
		dateTime.DateOnly().Month.Should().Be(1);
		dateTime.DateOnly().Day.Should().Be(2);
		
		dateTime.TimeOnly().Hour.Should().Be(3);
		dateTime.TimeOnly().Minute.Should().Be(4);
		dateTime.TimeOnly().Second.Should().Be(5);
		dateTime.TimeOnly().Millisecond.Should().Be(6);
		dateTime.TimeOnly().Microsecond.Should().Be(7);
	}

	[Test]
	public void FirstDayOfWeek() {
		DateTime dateTime = new(2020, 1, 2, 3, 4, 5, 6, 7, DateTimeKind.Local);

		dateTime.FirstDayOfWeek().DayOfWeek.Should().Be(DayOfWeek.Monday);
		dateTime.FirstDayOfWeek(DayOfWeek.Sunday).DayOfWeek.Should().Be(DayOfWeek.Sunday);
		
		dateTime.FirstDayOfWeekLocal(CultureInfo.InvariantCulture).DayOfWeek.Should().Be(DayOfWeek.Sunday);
		dateTime.FirstDayOfWeekLocal(CultureInfo.InvariantCulture.DateTimeFormat).DayOfWeek.Should().Be(DayOfWeek.Sunday);
	}
}