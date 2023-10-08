namespace Neco.Test.Common.Helper;

using System;
using FluentAssertions;
using Neco.Common.Helper;
using NUnit.Framework;

[TestFixture]
public class BitHelperTests {
	[Test]
	public void SetsBitsCorrectlyForInt16() {
		Int16 val = 0;
		BitHelper.SetBit(ref val, 5);
		val.Should().Be(32);
		BitHelper.IsBitSet(val, 5).Should().BeTrue();

		BitHelper.ToggleBit(ref val, 6);
		val.Should().Be(64 + 32);
		BitHelper.IsBitSet(val, 6).Should().BeTrue();

		BitHelper.ClearBit(ref val, 5);
		val.Should().Be(64);
		BitHelper.IsBitSet(val, 5).Should().BeFalse();

		BitHelper.Bit(ref val, 7, true);
		val.Should().Be(128 + 64);
		BitHelper.IsBitSet(val, 7).Should().BeTrue();
		BitHelper.Bit(ref val, 7, false);
		val.Should().Be(64);
		BitHelper.IsBitSet(val, 7).Should().BeFalse();
	}
	
	[Test]
	public void SetsBitsCorrectlyForUInt16() {
		UInt16 val = 0;
		BitHelper.SetBit(ref val, 5);
		val.Should().Be(32);
		BitHelper.IsBitSet(val, 5).Should().BeTrue();

		BitHelper.ToggleBit(ref val, 6);
		val.Should().Be(64 + 32);
		BitHelper.IsBitSet(val, 6).Should().BeTrue();

		BitHelper.ClearBit(ref val, 5);
		val.Should().Be(64);
		BitHelper.IsBitSet(val, 5).Should().BeFalse();

		BitHelper.Bit(ref val, 7, true);
		val.Should().Be(128 + 64);
		BitHelper.IsBitSet(val, 7).Should().BeTrue();
		BitHelper.Bit(ref val, 7, false);
		val.Should().Be(64);
		BitHelper.IsBitSet(val, 7).Should().BeFalse();
	}
}