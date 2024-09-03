namespace Neco.Test.Common.Extensions;

using System;
using FluentAssertions;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
public class NumericExtensionTests {
	[Test]
	public void Multipliers() {
		5.KB().Should().Be(5 * 1000);
		5D.KB().Should().Be(5 * 1000);
		5L.KB().Should().Be(5 * 1000);
		5.KiB().Should().Be(5 * 1024);
		5D.KiB().Should().Be(5 * 1024);
		5L.KiB().Should().Be(5 * 1024);

		5.MB().Should().Be(5 * 1000 * 1000);
		5D.MB().Should().Be(5 * 1000 * 1000);
		5L.MB().Should().Be(5 * 1000 * 1000);
		5.MiB().Should().Be(5 * 1024 * 1024);
		5D.MiB().Should().Be(5 * 1024 * 1024);
		5L.MiB().Should().Be(5 * 1024 * 1024);
		
		5.GB().Should().Be(5L * 1000 * 1000 * 1000);
		5D.GB().Should().Be(5L * 1000 * 1000 * 1000);
		5L.GB().Should().Be(5L * 1000 * 1000 * 1000);
		5.GiB().Should().Be(5L * 1024 * 1024 * 1024);
		5D.GiB().Should().Be(5L * 1024 * 1024 * 1024);
		5L.GiB().Should().Be(5L * 1024 * 1024 * 1024);

		5.TB().Should().Be(5L * 1000 * 1000 * 1000 * 1000);
		5D.TB().Should().Be(5L * 1000 * 1000 * 1000 * 1000);
		5L.TB().Should().Be(5L * 1000 * 1000 * 1000 * 1000);
		5.TiB().Should().Be(5L * 1024 * 1024 * 1024 * 1024);
		5D.TiB().Should().Be(5L * 1024 * 1024 * 1024 * 1024);
		5L.TiB().Should().Be(5L * 1024 * 1024 * 1024 * 1024);

		5.SecondsInTicks().Should().Be(50000000);
		5.SecondsInMs().Should().Be(5000);
		5.MinutesInMs().Should().Be(300_000);
		5.HoursInMs().Should().Be(3600*5*1000);
	}

	[Test]
	public void Clamping() {
		5L.ToInt32Clamped().Should().Be(5);
		(5L + Int32.MaxValue).ToInt32Clamped().Should().Be(Int32.MaxValue);
		(Int32.MinValue - 5L).ToInt32Clamped().Should().Be(Int32.MinValue);
		
		5UL.ToUInt32Clamped().Should().Be(5);
		(5UL + UInt32.MaxValue).ToUInt32Clamped().Should().Be(UInt32.MaxValue);
	}

	[Test]
	public void Filesize() {
		5.3.TiB().ToFileSize().Should().Be("5.30 TiB");
		5.3.TiB().ToFileSize(useSiPrefix: true).Should().Be("5.83 TB");
		
		5.3.GiB().ToFileSize().Should().Be("5.30 GiB");
		5.3.GiB().ToFileSize(useSiPrefix: true).Should().Be("5.69 GB");
		
		5.3.MiB().ToFileSize().Should().Be("5.30 MiB");
		5.3.MiB().ToFileSize(useSiPrefix: true).Should().Be("5.56 MB");
		
		5.3.KiB().ToFileSize().Should().Be("5.30 KiB");
		5.3.KiB().ToFileSize(useSiPrefix: true).Should().Be("5.43 KB");
		
		1024D.ToFileSize().Should().Be("1.00 KiB");
		1023D.ToFileSize().Should().Be("1023 Bytes");
		1024D.ToFileSize(useSiPrefix: true).Should().Be("1.02 KB");
		1000D.ToFileSize(useSiPrefix: true).Should().Be("1.00 KB");
		999D.ToFileSize(useSiPrefix: true).Should().Be("999 Bytes");

		Int16.MaxValue.ToFileSize().Should().Be("32.00 KiB");
		UInt16.MaxValue.ToFileSize().Should().Be("64.00 KiB");
		Int32.MaxValue.ToFileSize().Should().Be("2.00 GiB");
		UInt32.MaxValue.ToFileSize().Should().Be("4.00 GiB");
		Int64.MaxValue.ToFileSize().Should().Be("8388608.00 TiB");
		UInt64.MaxValue.ToFileSize().Should().Be("16777216.00 TiB");
		Int128.MaxValue.ToFileSize().Should().Be("154742504910673000000000000.00 TiB");
		UInt128.MaxValue.ToFileSize().Should().Be("309485009821345000000000000.00 TiB");
		Single.MaxValue.ToFileSize().Should().Be("309484991374601000000000000.00 TiB");
		Double.MaxValue.ToFileSize().Should().Be("163499238157084000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000.00 TiB");
	}
}