namespace Neco.Test.Common.Helper;

using System;
using Neco.Common.Helper;
using NUnit.Framework;

[TestFixture]
public class TimeSpanHelperTests {
	[Test]
	public void StringToTimeSpan()
	{
		Assert.That(TimeSpanHelper.FromString(null), Is.EqualTo(TimeSpan.Zero));
		Assert.That(TimeSpanHelper.FromString(String.Empty), Is.EqualTo(TimeSpan.Zero));
		Assert.That(TimeSpanHelper.FromString(""), Is.EqualTo(TimeSpan.Zero));
		Assert.That(TimeSpanHelper.FromString("1x"), Is.EqualTo(TimeSpan.Zero));
		Assert.That(TimeSpanHelper.FromString("1dm"), Is.EqualTo(TimeSpan.Zero));
		Assert.That(TimeSpanHelper.FromString("0d"), Is.EqualTo(TimeSpan.Zero));
		Assert.That(TimeSpanHelper.FromString("never"), Is.EqualTo(TimeSpan.MaxValue));
		Assert.That(TimeSpanHelper.FromString("infinite"), Is.EqualTo(TimeSpan.MaxValue));

		Assert.That(TimeSpanHelper.FromString("1ms"), Is.EqualTo(TimeSpan.FromMilliseconds(1)));
		Assert.That(TimeSpanHelper.FromString("1000ms"), Is.EqualTo(TimeSpan.FromSeconds(1)));
			
		Assert.That(TimeSpanHelper.FromString(".5s"), Is.EqualTo(TimeSpan.FromSeconds(.5)));
		Assert.That(TimeSpanHelper.FromString(".5s"), Is.EqualTo(TimeSpan.FromMilliseconds(500)));
		Assert.That(TimeSpanHelper.FromString("1s"), Is.EqualTo(TimeSpan.FromSeconds(1)));
		Assert.That(TimeSpanHelper.FromString("1.25s"), Is.EqualTo(TimeSpan.FromSeconds(1.25)));
		Assert.That(TimeSpanHelper.FromString("1.25s"), Is.EqualTo(TimeSpan.FromMilliseconds(1250)));

		Assert.That(TimeSpanHelper.FromString("5m"), Is.EqualTo(TimeSpan.FromMinutes(5)));
		Assert.That(TimeSpanHelper.FromString("5m6ms"), Is.EqualTo(TimeSpan.FromMinutes(5) + TimeSpan.FromMilliseconds(6)));
		Assert.That(TimeSpanHelper.FromString("6ms5m"), Is.EqualTo(TimeSpan.FromMinutes(5) + TimeSpan.FromMilliseconds(6)));
		Assert.That(TimeSpanHelper.FromString("5h"), Is.EqualTo(TimeSpan.FromHours(5)));
		Assert.That(TimeSpanHelper.FromString("5d"), Is.EqualTo(TimeSpan.FromDays(5)));

		Assert.That(TimeSpanHelper.FromString("5d 4h 3ms"), Is.EqualTo(TimeSpan.FromDays(5) + TimeSpan.FromHours(4) + TimeSpan.FromMilliseconds(3)));
		Assert.That(TimeSpanHelper.FromString("5d4h3ms"), Is.EqualTo(TimeSpan.FromDays(5) + TimeSpan.FromHours(4) + TimeSpan.FromMilliseconds(3)));
		Assert.That(TimeSpanHelper.FromString("  5   d   4   h   3    ms  "), Is.EqualTo(TimeSpan.FromDays(5) + TimeSpan.FromHours(4) + TimeSpan.FromMilliseconds(3)));
		Assert.That(TimeSpanHelper.FromString("5d4h0m3ms"), Is.EqualTo(TimeSpan.FromDays(5) + TimeSpan.FromHours(4) + TimeSpan.FromMilliseconds(3)));

		Assert.That(TimeSpanHelper.FromString("5d 4h 3.5s"), Is.EqualTo(TimeSpan.FromDays(5) + TimeSpan.FromHours(4) + TimeSpan.FromMilliseconds(3500)));
		Assert.That(TimeSpanHelper.FromString("5d 4h .5s"), Is.EqualTo(TimeSpan.FromDays(5) + TimeSpan.FromHours(4) + TimeSpan.FromMilliseconds(500)));
		Assert.That(TimeSpanHelper.FromString("5d 4h 0.5s"), Is.EqualTo(TimeSpan.FromDays(5) + TimeSpan.FromHours(4) + TimeSpan.FromMilliseconds(500)));
	}
}