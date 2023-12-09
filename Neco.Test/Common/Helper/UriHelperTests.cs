namespace Neco.Test.Common.Helper;

using System;
using System.Collections.Generic;
using FluentAssertions;
using Neco.Common.Helper;
using NUnit.Framework;

[TestFixture]
public class UriHelperTests {
	public static IEnumerable<TestCaseData> RelativeTestCasesPositive() {
		yield return new("", " "){TestName = "Empty"};
		yield return new("/", " "){TestName = "Root"};
		yield return new("/?a=1&b=2", "?a=1&b=2"){TestName = "OptionalFirstSlash"};
		yield return new("/?a=1&b=2", "/?b=2&a=1"){TestName = "OrderOfQueryParameters"};
		yield return new("/?a=1&%=2", "/?%=2&a=1"){TestName = "OrderOfQueryParameters-Special1"};
		yield return new("/?a=1&=2", "/?=2&a=1"){TestName = "OrderOfQueryParameters-Special2"};
		yield return new("/?a=1& =2", "/? =2&a=1"){TestName = "OrderOfQueryParameters-Special3"};
		yield return new("/?a=1&%25=2", "/?%25=2&a=1"){TestName = "OrderOfQueryParameters-Special4"};
		yield return new("/?a=1&%25=2", "/?%25=2&a=1"){TestName = "OrderOfQueryParameters-Special4"};
	}
	public static IEnumerable<TestCaseData> AbsoluteTestCasesPositive() {
		yield return new("http://example.org", "https://www.example.org"){TestName = "Ignores-www"};
		yield return new("http://www.example.org", "https://www.example.org"){TestName = "DifferentSchema"};
		yield return new("http://www.example.org", "http://www.example.org  "){TestName = "OptionalSpacesAtTheEnd"};
		yield return new("http://www.example.org", "http://www.example.org/"){TestName = "OptionalSlashAfterHost"};
		yield return new("http://www.example.org", "http://www.example.org/  "){TestName = "OptionalSpacesAfterHostSlash"};
		yield return new("http://www.abæcdöef.org", "http://www.xn--abcdef-qua4k.org/"){TestName = "PunyCode-Partial"};
		yield return new("http://MajiでKoiする5秒前.org", "http://xn--MajiKoi5-783gue6qz075azm5e.org/"){TestName = "PunyCode-Japanese"};
		yield return new("http://ドメイン名例", "http://xn--eckwd4c7cu47r2wf/"){TestName = "PunyCode-Complete"};
	}
	
	public static IEnumerable<TestCaseData> RelativeTestCasesNegative() {
		yield return new("/?a=1b=2", "/?b=1&a=2"){TestName = null};
	}
	public static IEnumerable<TestCaseData> AbsoluteTestCasesNegative() {
		yield return new("http://www.example.org", "http://www.example.ôrg/"){TestName = null};
	}

	[TestCase("/test.php?b=2&a=1&sid=xxxxx#abc", "/test.php?a=1&b=2")]
	[TestCase("https://www.abæcdöef.org/test.php?b=2&a=1&sid=xxxxx#abc", "xn--abcdef-qua4k.org/test.php?a=1&b=2")]
	public void NormalizesUriCorrectly(String uriString, String expected) {
		Uri u = new(uriString, UriKind.RelativeOrAbsolute);
		String normalized = UriHelper.NormalizeUri(u);
		Assert.That(normalized, Is.EqualTo(expected));
	}

	[TestCaseSource(nameof(AbsoluteTestCasesPositive))]
	[TestCaseSource(nameof(RelativeTestCasesPositive))]
	public void HashesEquivalentAbsoluteUriCorrectly(String u1, String u2) {
		Uri uri1 = new(!u1.StartsWith("http") ? $"http://example.org{u1}" : u1, UriKind.Absolute);
		Uri uri2 = new(!u2.StartsWith("http") ? $"http://example.org{u2}" : u2, UriKind.Absolute);
		UInt64 hash1 = UriHelper.HashUri(uri1);
		UInt64 hash2 = UriHelper.HashUri(uri2);

		hash1.Should().Be(hash2);
	}
	
	[TestCaseSource(nameof(RelativeTestCasesPositive))]
	public void HashesEquivalentRelativeUriCorrectly(String u1, String u2) {
		Uri uri1 = new(u1, UriKind.Relative);
		Uri uri2 = new(u2, UriKind.Relative);
		UInt64 hash1 = UriHelper.HashUri(uri1);
		UInt64 hash2 = UriHelper.HashUri(uri2);

		hash1.Should().Be(hash2);
	}
	
	[TestCaseSource(nameof(AbsoluteTestCasesNegative))]
	[TestCaseSource(nameof(RelativeTestCasesNegative))]
	public void HashesDifferingAbsoluteUriCorrectly(String u1, String u2) {
		Uri uri1 = new(u1.StartsWith("/") ? $"http://example.org{u1}" : u1, UriKind.Absolute);
		Uri uri2 = new(u2.StartsWith("/") ? $"http://example.org{u2}" : u2, UriKind.Absolute);
		UInt64 hash1 = UriHelper.HashUri(uri1);
		UInt64 hash2 = UriHelper.HashUri(uri2);

		hash1.Should().NotBe(hash2);
	}
	
	[TestCaseSource(nameof(RelativeTestCasesNegative))]
	public void HashesDifferingRelativeUriCorrectly(String u1, String u2) {
		Uri uri1 = new(u1, UriKind.Relative);
		Uri uri2 = new(u2, UriKind.Relative);
		UInt64 hash1 = UriHelper.HashUri(uri1);
		UInt64 hash2 = UriHelper.HashUri(uri2);

		hash1.Should().NotBe(hash2);
	}
}