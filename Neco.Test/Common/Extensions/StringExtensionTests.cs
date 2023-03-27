namespace Neco.Test.Common.Extensions;

using System;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
public class StringExtensionTests {

	[TestCase("ß", "ß")]
	[TestCase("ö", "Ö")]
	[TestCase("a", "A")]
	[TestCase("aaa", "Aaa")]
	[TestCase("Aaa", "Aaa")]
	[TestCase("aaA", "AaA")]
	[TestCase("aaA bBb", "AaA bBb")]
	[TestCase("", "")]
	public void ToUpperCapitalizesCorrectly(String input, String expected) {
		Assert.That(input.FirstCharToUpper(), Is.EqualTo(expected));
	}
	
	[Test]
	public void ToUpperFailsOnNull() {
		String nullString = null!;
		Assert.Throws<ArgumentNullException>(() => nullString.FirstCharToUpper());
	}
	
	[TestCase("ß", "ß")]
	[TestCase("Ö", "ö")]
	[TestCase("A", "a")]
	[TestCase("aaa", "aaa")]
	[TestCase("Aaa", "aaa")]
	[TestCase("AaA", "aaA")]
	[TestCase("AaA bBb", "aaA bBb")]
	[TestCase("", "")]
	public void ToLowerCapitalizesCorrectly(String input, String expected) {
		Assert.That(input.FirstCharToLower(), Is.EqualTo(expected));
	}
	
	[Test]
	public void ToLowerFailsOnNull() {
		String nullString = null!;
		Assert.Throws<ArgumentNullException>(() => nullString.FirstCharToLower());
	}
}