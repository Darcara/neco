namespace Neco.Test.Common.Helper;

using System;
using System.Numerics;
using FluentAssertions;
using Neco.Common.Extensions;
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

	[Test]
	public void SetsBitsCorrectlyForGeneric() {
		SetsBitsCorrectly<Byte>(4, 32, 64);
		SetsBitsCorrectly<SByte>(4, 32, 64);
		SetsBitsCorrectly<Int16>(4, 32, 64);
		SetsBitsCorrectly<UInt16>(4, 32, 64);
		SetsBitsCorrectly<Int32>(4, 32, 64);
		SetsBitsCorrectly<UInt32>(4, 32, 64);
		SetsBitsCorrectly<Int64>(4, 32, 64);
		SetsBitsCorrectly<UInt64>(4, 32, 64);

		SetsBitsCorrectly<Int128>(4, 32, 64);
		SetsBitsCorrectly<UInt128>(4, 32, 64);
	}

	public void SetsBitsCorrectly<T>(T num4, T num32, T num64) where T : INumberBase<T>, IShiftOperators<T, Int32, T>, IBitwiseOperators<T, T, T> {
		Console.WriteLine($"Testing {typeof(T).GetName()} with {num4.GetType()}");
		T val = T.Zero;

		BitHelper.SetBit(ref val, 5);
		Assert.That(val, Is.EqualTo(num32));
		val.Should().Be(num32);
		BitHelper.IsBitSet(val, 5).Should().BeTrue();

		BitHelper.ToggleBit(ref val, 6);
		val.Should().Be(num64 + num32);
		BitHelper.IsBitSet(val, 6).Should().BeTrue();

		BitHelper.ClearBit(ref val, 5);
		val.Should().Be(num64);
		BitHelper.IsBitSet(val, 5).Should().BeFalse();

		BitHelper.Bit(ref val, 2, true);
		val.Should().Be(num64 + num4);
		BitHelper.IsBitSet(val, 2).Should().BeTrue();
		BitHelper.Bit(ref val, 2, false);
		val.Should().Be(num64);
		BitHelper.IsBitSet(val, 2).Should().BeFalse();
	}
}