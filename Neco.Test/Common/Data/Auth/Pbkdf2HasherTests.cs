namespace Neco.Test.Common.Data.Auth;

using System;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using FluentAssertions;
using Neco.Common.Data.Auth;
using Neco.Common.Helper;
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
		
		Assert.Throws<ArgumentOutOfRangeException>(() => new Pbkdf2Hasher(new Byte[15]));
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
	public void PepperLeadsToDifferentPasswordHashes() {
		Pbkdf2Hasher hasher1 = new();
		
		Byte[] customPepper = new Byte[16];
		RandomNumberGenerator.Fill(customPepper);
		Pbkdf2Hasher hasher2 = new(customPepper);

		String hash1 = hasher1.HashPassword(String.Empty, "somePassword");
		String hash2 = hasher2.HashPassword(String.Empty, "somePassword");
		hash1.Should().NotBeNull();
		hash1.Should().NotBeEmpty();
		hash1.Should().NotBe(hash2);

		hasher1.VerifyPassword(String.Empty, "somePassword", hash1).Should().BeTrue();
		hasher1.VerifyPassword(String.Empty, "somePassword", hash2).Should().BeFalse();
		hasher2.VerifyPassword(String.Empty, "somePassword", hash1).Should().BeFalse();
		hasher2.VerifyPassword(String.Empty, "somePassword", hash2).Should().BeTrue();
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
		Byte[] evilByteArraySaltSize = new Byte[40];
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
		Byte[] evilByteArrayIterationsNegative = new Byte[40];
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

	/*
	 On AMD Ryzen 9 3900X 12-Core Processor
	 Hash-12 37 ops in 5,033.413ms = clean per operation: 136,038.159µs or 7.351op/s with GC 0/0/0
	 Hash-24 37 ops in 5,014.144ms = clean per operation: 135,517.360µs or 7.379op/s with GC 0/0/0

	 Verify-Wrong-12 38 ops in 5,135.704ms = clean per operation: 135,150.079µs or 7.399op/s with GC 0/0/0
	 Verify-Correct-12 37 ops in 5,007.440ms = clean per operation: 135,336.195µs or 7.389op/s with GC 0/0/0
	*/
	[Test]
	[Category("Benchmark")]
	public void PerformanceEstimate() {
		Pbkdf2Hasher hasher = new();
		
		PerformanceHelper.GetPerformanceRough("Hash-12", static h => h.HashPassword(String.Empty, "somePassword"), hasher);
		PerformanceHelper.GetPerformanceRough("Hash-24", static h => h.HashPassword(String.Empty, "somePasswordsomePassword"), hasher);

		String hash = hasher.HashPassword(String.Empty, "somePassword");
		PerformanceHelper.GetPerformanceRough("Verify-Wrong-12", static h => h.hasher.VerifyPassword(String.Empty, "somepassword", h.hash), (hasher, hash));
		PerformanceHelper.GetPerformanceRough("Verify-Correct-12", static h => h.hasher.VerifyPassword(String.Empty, "somePassword", h.hash), (hasher, hash));
	}
}