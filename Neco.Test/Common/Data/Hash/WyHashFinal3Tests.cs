namespace Neco.Test.Common.Data.Hash;

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using FluentAssertions;
using Neco.Common.Data.Hash;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
public class WyHashFinal3Tests : AHashTest {
	private Dictionary<Byte[], String> _testCases = null!;

	[SetUp]
	public void Setup() {
		_testCases = new Dictionary<Byte[], String>();

		_testCases.Add(new Byte[] { 0x0 }, "0F775638DBD5A222");
		_testCases.Add(new Byte[] { (Byte)'a' }, "67E865245A4EF86C");
		_testCases.Add(new Byte[] { (Byte)'A' }, "05FC89ED90EB8AA1");
		_testCases.Add(new Byte[] { (Byte)'B' }, "4A07019ACEDB1D9C");

		// 2 byte
		_testCases.Add(new Byte[] { 0x0, 0x0 }, "3BEADF7249175702");
		_testCases.Add(new[] { (Byte)'A', (Byte)'B' }, "F21076AD9D80DD85");
		_testCases.Add(new[] { (Byte)'a', (Byte)'b' }, "D8B6EBB873A72B17");
		_testCases.Add(new[] { (Byte)'A', (Byte)'B', (Byte)'B' }, "19D7C50FBABAAE13");
		_testCases.Add(new[] { (Byte)'a', (Byte)'b', (Byte)'c' }, "CFFF442DF28D80B4");

		//4 byte
		_testCases.Add(BitConverter.GetBytes(5), "57712475366AB5D0");
		_testCases.Add(BitConverter.GetBytes(1024), "9C475EC9A54B0BE4");
		_testCases.Add(BitConverter.GetBytes(Int32.MaxValue), "3EBBE358B278E0C0");

		_testCases.Add(Encoding.ASCII.GetBytes("1234567"), "4CC03C51D48BD125");
		_testCases.Add(Encoding.ASCII.GetBytes("SipHash"), "452F7B75445F24BE");
		//8 byte
		_testCases.Add(Encoding.ASCII.GetBytes("12345678"), "342F01FF657BDD28");
		_testCases.Add(Encoding.ASCII.GetBytes("Hello Wo"), "BFF4022CE157E0BF");

		_testCases.Add(Encoding.ASCII.GetBytes("123456789012345"), "BFBA8A20F39B6D18");
		//16 byte
		_testCases.Add(Encoding.ASCII.GetBytes("1234567890123456"), "5A8F36D0D4DB623E");
		_testCases.Add(Encoding.ASCII.GetBytes("  Hello World!  "), "BBA650C2BFC973F5");

		_testCases.Add(Encoding.ASCII.GetBytes("12345678901234567890123"), "881CE20B51F78C81");
		//24 byte
		_testCases.Add(Encoding.ASCII.GetBytes("123456789012345678901234"), "625D7DA38C3EC4AE");
		_testCases.Add(Encoding.ASCII.GetBytes("Hello World!Hello World!"), "AC0B6471A4E81598");
		_testCases.Add(Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwxyz"), "B2363ABAEC1467D6");

		_testCases.Add(Encoding.ASCII.GetBytes("1234567890123456789012345678901"), "48C8B05E4E753488");
		//32 byte and more
		_testCases.Add(Encoding.ASCII.GetBytes("12345678901234567890123456789012"), "192EB14D27E7BF9C");
		_testCases.Add(Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"), "6738BEB57F9486D9");
		_testCases.Add(Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy cog"), "2029B73A7B657396");
		_testCases.Add(Encoding.ASCII.GetBytes("The quick brown box jumps over the lazy dog"), "D17545E9FF08F5E4");
		_testCases.Add(Encoding.ASCII.GetBytes("The quick brown box jumps over the lazy cog"), "B9C1769540679C87");
		_testCases.Add(Encoding.ASCII.GetBytes("The quick onyx goblin jumps over the lazy dwarf."), "4114F95270D60436");

		_testCases.Add(Encoding.ASCII.GetBytes("123456789012345678901234567890123456789012345678901234567890123"), "D20C21034EE6D231");
		//64 byte and more
		_testCases.Add(Encoding.ASCII.GetBytes("1234567890123456789012345678901234567890123456789012345678901234"), "0F755BDE9A22E8BF");
		_testCases.Add(Encoding.ASCII.GetBytes("Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut l"), "897F2489D818CD6A");

		_testCases.Add(Encoding.ASCII.GetBytes("1234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567")
			, "5EE4964705DE8399");
		//128 byte and more
		_testCases.Add(Encoding.ASCII.GetBytes("12345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678")
			, "61FE5008A70CBAA0");
		_testCases.Add(Encoding.ASCII.GetBytes("Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam volu")
			, "20894D3D1AF22402");

		_testCases.Add(Encoding.ASCII.GetBytes("Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata")
			, "054D957611231AD6");
		//256
		_testCases.Add(Encoding.ASCII.GetBytes("Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata ")
			, "A87E60E4D62C6DD8");

		_testCases.Add(Encoding.ASCII.GetBytes("Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. St")
			, "EC37D450084AF1D7");
		//512
		_testCases.Add(Encoding.ASCII.GetBytes("Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Ste")
			, "8E3BF358529D1723");
	}

	[Test]
	public void Hash64BitAllAtOnce() {
		TestHashFunc(_testCases, () => new WyHashFinal3());
	}

	[Test]
	public void HashSeed() {
		Span<UInt64> secret = new UInt64[4].AsSpan();
		WyHashFinal3.MakeSecret(0x1234, secret);
		String oneOffWithSeed = WyHashFinal3.HashOneOff("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"u8, 0x5678, new WyHashSecret(secret)).ToStringHexSingleLine();
		Assert.That(oneOffWithSeed, Is.EqualTo("CE8DB201D959DFEA"));
	}

	[Test]
	public void SecretConstructsCorrectlry() {
		Span<UInt64> secret = new UInt64[4].AsSpan();
		WyHashFinal3.MakeSecret(0x1234, secret);
		WyHashSecret s1 = new WyHashSecret(secret);
		WyHashSecret s2 = new WyHashSecret(secret[0], secret[1], secret[2], secret[3]);
		WyHashSecret s = new WyHashSecret();

		Assert.That(s1, Is.EqualTo(s2));
		Assert.That(s2 == s1, Is.True);
		Assert.That(s1 != s, Is.True);
		Assert.That(s1.GetHashCode(), Is.EqualTo(s2.GetHashCode()));
		Assert.That(s1.GetHashCode(), Is.Not.EqualTo(s.GetHashCode()));
	}

	private static Object[] _wyHashFinal3TestCases = {
		// https://github.com/wangyi-fudan/wyhash/blob/master/test_vector.cpp
		new Object[] { "D3C4EEC56D98BC42", "", 0UL }, // 0
		new Object[] { "5115C303C98D5084", "a", 1UL }, // 1
		new Object[] { "B1ECC9CF8748C50B", "abc", 2UL }, // 3
		new Object[] { "9C7B2ADC22860A09", "1234567890", 7UL }, // 10 -- no official test result
		new Object[] { "7CA6088229F32F6E", "message digest", 3UL }, // 14 
		new Object[] { "B99571892EE4649A", "abcdefghijklmnopqrstuvwxyz", 4UL }, // 26 
		new Object[] { "5425C33932389991", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", 5UL }, // 62 
		new Object[] { "31EE30570E9B8861", "1234567890123456789012345678901234567890123456789012345678901234567890", 6UL }, // 70 
		// Custom tests
		new Object[] { "7A076621038702E9", "The quick onyx goblin jumps over the lazy dwarf", 0UL }, // 47
		new Object[] { "4114F95270D60436", "The quick onyx goblin jumps over the lazy dwarf.", 0UL }, // 48
		new Object[] { "7D5894BF567EAD48", "The quick onyx goblin jumps over the lazy dwarf!!", 0UL }, // 49
		new Object[] { "2BD9E43504A714D5", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed di", 0UL }, // 63
		new Object[] { "C4362D485AD2AC27", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed dia", 0UL }, // 64
		new Object[] { "323C98F61726BCDE", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam", 0UL }, // 65
		new Object[] { "FC5BB2B1BB3587F6", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt", 0UL }, // 95
		new Object[] { "31803957B5D832C0", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ", 0UL }, // 96
		new Object[] { "87042E837F351096", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt u", 0UL }, // 97
		new Object[] { "1271A0C7F8A400AA", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et do", 0UL }, // 111
		new Object[] { "E5AB51090BEA2B38", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dol", 0UL }, // 112
		new Object[] { "41CA7B9CD54013E0", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolo", 0UL }, // 113
		new Object[] { "5067C2A280D8EC7C", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliqu", 0UL }, // 127
		new Object[] { "FE637D99AB49B66A", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquy", 0UL }, // 128
		new Object[] { "CA7999E9CA6EFE68", "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquya", 0UL }, // 129
	};

	[TestCaseSource(nameof(_wyHashFinal3TestCases))]
	public void HashAllAtOnce(String expected, String data, UInt64 seed = 0) {
		Byte[] bytes = Encoding.ASCII.GetBytes(data);

		UInt64 longHash = WyHashFinal3.HashOneOffLong(bytes, seed);
		Assert.That(longHash, Is.EqualTo(BinaryPrimitives.ReverseEndianness(UInt64.Parse(expected, NumberStyles.HexNumber))), "HashOneLong must be equal to HashAlgorithm");
		Assert.That(BinaryPrimitives.ReverseEndianness(longHash).ToString("X16"), Is.EqualTo(expected), "HashOneLong must be equal to HashAlgorithm");
		Assert.That(BitConverter.GetBytes(longHash).ToStringHexSingleLine(), Is.EqualTo(expected), "HashOneLong must be equal to HashAlgorithm");

		
		String oneOffHash = WyHashFinal3.HashOneOff(bytes, seed).ToStringHexSingleLine();
		Assert.That(oneOffHash, Is.EqualTo(expected), "HashOneOff must be equal to HashAlgorithm");

		oneOffHash = WyHashFinal3.HashOneOff(bytes, 0, bytes.Length, seed).ToStringHexSingleLine();
		Assert.That(oneOffHash, Is.EqualTo(expected), "HashOneOff must be equal to HashAlgorithm");

		Byte[] hashBytes = new Byte[sizeof(UInt64)];
		WyHashFinal3.HashOneOff(bytes, hashBytes, seed);
		oneOffHash = hashBytes.ToStringHexSingleLine();
		Assert.That(oneOffHash, Is.EqualTo(expected), "HashOneOff must be equal to HashAlgorithm");

		hashBytes = new Byte[sizeof(UInt64)];
		WyHashFinal3.HashOneOff(bytes, 0, bytes.Length, hashBytes, seed);
		oneOffHash = hashBytes.ToStringHexSingleLine();
		Assert.That(oneOffHash, Is.EqualTo(expected), "HashOneOff must be equal to HashAlgorithm");

		TestAllAtOnce(expected, bytes, () => new WyHashFinal3(seed));
	}

	[TestCaseSource(nameof(_wyHashFinal3TestCases))]
	public void HashIncremental1Byte(String expected, String data, UInt64 seed = 0) {
		Byte[] bytes = Encoding.ASCII.GetBytes(data);

		String oneOffHash = WyHashFinal3.HashOneOff(bytes, seed).ToStringHexSingleLine();
		Assert.That(oneOffHash, Is.EqualTo(expected), "HashOneOff must be equal to HashAlgorithm");

		Byte[] testArray = new Byte[64];
		Random.Shared.NextBytes(testArray);

		using WyHashFinal3 hasher = new(seed);
		hasher.Initialize();
		for (Int32 i = 0; i < bytes.Length; ++i) {
			Int32 idx = Random.Shared.Next(0, 64);
			testArray[idx] = bytes[i];
			hasher.TransformBlock(testArray, idx, 1, null, 0);
		}

		hasher.TransformFinalBlock(testArray, 0, 0);
		String? hashReadable = hasher.Hash?.ToStringHexSingleLine();

		Assert.That(hashReadable, Is.EqualTo(expected), $"Default hashing for {bytes.ToStringHexSingleLine()} - ({bytes.Length} bytes) {Encoding.ASCII.GetString(bytes)}");
	}

	[TestCase(128, 1)]
	[TestCase(227840, 128*1024)]
	[TestCase(128, 128*1024)]
	public void HashesLargeDataConsistently(Int32 size, Int32 hashBlockSize) {
		Byte[] buffer = new Byte[size];
		Random.Shared.NextBytes(buffer);

		UInt64 longHash = BinaryPrimitives.ReverseEndianness(WyHashFinal3.HashOneOffLong(buffer));
		Byte[] byteHash = WyHashFinal3.HashOneOff(buffer);

		WyHashFinal3 hasher = new();
		hasher.Initialize();
		Int32 bytesHashed = 0;
		while (bytesHashed < size) {
			Int32 bytesToHash = Math.Min(hashBlockSize, size - bytesHashed);
			hasher.TransformBlock(buffer, bytesHashed, bytesToHash, null, 0);
			bytesHashed += bytesToHash;
		}
		
		bytesHashed.Should().Be(size);

		hasher.TransformFinalBlock(Array.Empty<Byte>(), 0, 0);

		longHash.ToString("X16").Should().Be(byteHash.ToStringHexSingleLine());
		longHash.ToString("X16").Should().Be(hasher.Hash?.ToStringHexSingleLine());
	}
}