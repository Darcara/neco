namespace Neco.Test.Common.Data.Auth;

using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using FluentAssertions;
using Neco.Common.Data.Auth;
using NUnit.Framework;

[TestFixture]
public class Pbkdf2HasherTests {
	[Test]
	[SuppressMessage("ReSharper", "EqualExpressionComparison")]
	public void IsCorrectlyImplemented() {
		Pbkdf2Hasher hasher = new();
		hasher.Id.Should().NotBeEmpty();
		hasher.ToString().Should().NotBeEmpty();
		hasher.ToString().Should().Be(hasher.Id);

		hasher.Equals(hasher).Should().BeTrue();
		(hasher == hasher).Should().BeTrue();
		(hasher != hasher).Should().BeFalse();

		Pbkdf2Hasher hasher2 = new();
		hasher.Equals(hasher2).Should().BeTrue();
		(hasher == hasher2).Should().BeTrue();
		(hasher != hasher2).Should().BeFalse();

		hasher.GetHashCode().Should().NotBe(0);
		hasher.GetHashCode().Should().Be(hasher2.GetHashCode());
	}

	[Test]
	public void HashesAndValidatesCorrectly() {
		Pbkdf2Hasher hasher = new();

		String hash = hasher.HashPassword(String.Empty, "somePassword");
		hash.Should().NotBeNull();
		hash.Should().NotBeEmpty();

		hasher.VerifyPassword(String.Empty, "somePassword", hash).Should().BeTrue();
		hasher.VerifyPassword(String.Empty, "SomePassword", hash).Should().BeFalse();
		hasher.VerifyPassword(String.Empty, String.Empty, hash).Should().BeFalse();
	}

	[Test]
	public void FalseOnInvalidInput() {
		Pbkdf2Hasher hasher = new();
		hasher.VerifyPassword(String.Empty, "SomePassword", "Totally#Not#Base#64!").Should().BeFalse();
	}

	[Test]
	public void ResistentAgainstCraftedPayloads_Empty() {
		Pbkdf2Hasher hasher = new();
		hasher.VerifyPassword(String.Empty, "SomePassword", String.Empty).Should().BeFalse();
		hasher.VerifyPassword(String.Empty, "SomePassword", String.Empty).Should().BeFalse();
	}

	[Test]
	public void ResistentAgainstCraftedPayloads_SaltSize() {
		Pbkdf2Hasher hasher = new();
		var evilByteArraySaltSize = new Byte[40];
		RandomNumberGenerator.Fill(evilByteArraySaltSize);
		evilByteArraySaltSize[0] = 254;
		hasher
			.VerifyPassword(String.Empty, "SomePassword", Convert.ToBase64String(evilByteArraySaltSize))
			.Should()
			.BeFalse();
	}

	[Test]
	public void ResistentAgainstCraftedPayloads_NegativeIterations() {
		Pbkdf2Hasher hasher = new();
		var evilByteArrayIterationsNegative = new Byte[40];
		evilByteArrayIterationsNegative[0] = 0;
		BinaryPrimitives.WriteInt32LittleEndian(evilByteArrayIterationsNegative.AsSpan(1), 0);
		hasher
			.VerifyPassword(String.Empty, "SomePassword", Convert.ToBase64String(evilByteArrayIterationsNegative))
			.Should()
			.BeFalse();

		BinaryPrimitives.WriteInt32LittleEndian(evilByteArrayIterationsNegative.AsSpan(1), -1);
		hasher
			.VerifyPassword(String.Empty, "SomePassword", Convert.ToBase64String(evilByteArrayIterationsNegative))
			.Should()
			.BeFalse();
	}
}