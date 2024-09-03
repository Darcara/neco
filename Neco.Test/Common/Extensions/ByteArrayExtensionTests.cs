namespace Neco.Test.Common.Extensions;

using System;
using System.Linq;
using FluentAssertions;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
public class ByteArrayExtensionTests {
	[Test]
	public void StringHexSingleLine() {
		Byte[] array = {0x34, 0xFF, 0x00, 0xAB, 0xC5};
		Assert.That(array.ToStringHexSingleLine(), Is.EqualTo("34FF00ABC5"));
		Assert.That(array.ToStringHexSingleLine(2), Is.EqualTo("00ABC5"));
		Assert.That(array.ToStringHexSingleLine(2, 2), Is.EqualTo("00AB"));
	}
	
	[Test]
	public void StringHexMultiLine() {
		Byte[] array = Enumerable.Range(0,32).Select(x => (Byte)x).ToArray();
		Assert.That(array.ToStringHexMultiLine(), Is.EqualTo($"00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F {Environment.NewLine}10 11 12 13 14 15 16 17 18 19 1A 1B 1C 1D 1E 1F "));
		Assert.That(array.ToStringHexMultiLine(2), Does.StartWith($"00 01 {Environment.NewLine}02 03 {Environment.NewLine}"));
		Assert.That(array.ToStringHexMultiLine(2, "___"), Does.StartWith($"00___01___{Environment.NewLine}02___03___{Environment.NewLine}"));
	}
	
	[Test]
	public void StringHexDump() {
		Assert.That(ByteArrayExtensions.ToStringHexDump(null), Is.EqualTo("null"));
		Assert.That(Array.Empty<Byte>().ToStringHexDump(), Is.EqualTo("empty"));
		
		Byte[] array = Enumerable.Range(0x18,30).Select(x => (Byte)x).ToArray();
		Assert.That(new ReadOnlySpan<Byte>(array).ToStringHexDump(), Is.EqualTo($"18191A1B1C1D1E1F2021222324252627 ........ !\"#$%&'{Environment.NewLine}28292A2B2C2D2E2F303132333435     ()*+,-./012345"));
		Assert.That(array.AsSpan().ToStringHexDump(), Is.EqualTo($"18191A1B1C1D1E1F2021222324252627 ........ !\"#$%&'{Environment.NewLine}28292A2B2C2D2E2F303132333435     ()*+,-./012345"));
		Assert.That(array.ToStringHexDump(2), Does.StartWith($"1819 ..{Environment.NewLine}1A1B ..{Environment.NewLine}"));
		Assert.That(array.ToStringHexDump(2, 8), Does.StartWith($"2021  !{Environment.NewLine}2223 \"#{Environment.NewLine}"));
	}

	[Test]
	public void Matches() {
		Byte[] array1 = Enumerable.Range(0,32).Select(x => (Byte)x).ToArray();
		Byte[] array2 = Enumerable.Range(0,32).Select(x => (Byte)x).ToArray();
		Byte[] array3 = Enumerable.Range(1,32).Select(x => (Byte)x).ToArray();
		
		array1.Matches(array1).Should().BeTrue();
		array1.Matches(array2).Should().BeTrue();
		array2.Matches(array1).Should().BeTrue();
		array1.Matches(array2, 2, 2).Should().BeTrue();
		array1.Matches(array2, 2, 2, 10).Should().BeTrue();
		
		array1.Matches(array3).Should().BeFalse();
		array1.Matches(array2, 2, 2, 100).Should().BeFalse();
		array1.Matches(array2, 2, 3, 10).Should().BeFalse();
		
		array1.Matches(null, 2, 3, 10).Should().BeFalse();
		array1.Matches(array2, 2, 3, 10).Should().BeFalse();
		ByteArrayExtensions.Matches(null, array1).Should().BeFalse();
	}

	[Test]
	public void CopyTo() {
		Byte[] arraySource = Enumerable.Range(0,32).Select(x => (Byte)x).ToArray();
		Byte[] arrayDestination = new Byte[arraySource.Length * 2];
		Byte[] arrayExpected2 = Enumerable.Repeat((Byte)0, 32).Concat(Enumerable.Range(0,16).Select(x => (Byte)x).Concat(Enumerable.Repeat((Byte)0, 16))).ToArray();
		
		Array.Fill(arrayDestination, (Byte)0);
		arraySource.CopyTo(0, arrayDestination, arraySource.Length, arraySource.Length/2);
		arrayDestination.Should().BeEquivalentTo(arrayExpected2);
	}
}