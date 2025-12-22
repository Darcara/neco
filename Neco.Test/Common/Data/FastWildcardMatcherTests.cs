namespace Neco.Test.Common.Data;

using Neco.Common.Data;

[TestFixture]
public class FastWildcardMatcherTests {
	public Boolean DoMatch(String str, String pattern, StringComparison cmp = StringComparison.Ordinal) => FastWildcardMatcher.IsMatch(str, pattern, cmp);
	
	[TestCase("abcde", null)]
	[TestCase("abcde", "")]
	public void ThrowsArgumentRange(String str, String? pattern) {
		Assert.Throws<ArgumentOutOfRangeException>(() => DoMatch(str, pattern));
	}

	[TestCase("a", "?")]
	[TestCase("abc", "a?c")]
	[TestCase("abcde", "a?c?e")]
	[TestCase("abcde", "a???e")]
	[TestCase("abcde", "?bcde")]
	[TestCase("abcde", "abcd?")]
	[TestCase("", "*")]
	[TestCase(" ", "*")]
	[TestCase(" ", "**")]
	[TestCase(" ", "***")]
	[TestCase("aaa", "a*a")]
	[TestCase("abc", "a*c")]
	[TestCase("abcde", "a*e")]
	[TestCase("abcde", "a**e")]
	[TestCase("abcde", "*bcde")]
	[TestCase("abcde", "abcd*")]
	[TestCase("abcdefg", "*bc*fg")]
	[TestCase("abc/def/ghi", "abc*/ghi")]
	[TestCase("abc/def/ghi", "abc/*/ghi")]
	[TestCase("abc/def/ghi", "*/ghi")]
	[TestCase("aabbccaabbddaabbee", "a*b*a*e")]
	[TestCase("aabbccaabbddaabbee", "a*b*a*ee")]
	[TestCase("aa", "a*a")]
	[TestCase("abc", "a*bc")]
	[TestCase("abc", "*abc")]
	[TestCase("abc", "abc*")]
	[TestCase("abc", "*a*")]
	[TestCase("abc", "*b*")]
	[TestCase("abc", "*c*")]
	[TestCase("abcde", "a*b*c*d*e")]
	[TestCase("ab", "a?*")]
	[TestCase("abcde", "a?c*e")]
	[TestCase("abcde", "a*c?e")]
	[TestCase("abcde", "a?*de")]
	[TestCase("aabbccaabbddaabbee", "a*b?cca*b*ee")]
	[TestCase(" ", "?*")]
	[TestCase(" ", "*?")]
	[TestCase(" ", "*?*")]
	[TestCase("  ", "*?*?*")]
	[TestCase(" ", " ")]
	[TestCase("[5510]HEREFORD.FMS", "*?*.*?*")]
	[TestCase("[5510]HEREFORD.FMS", "*?*.?*")]
	[TestCase("[5510]HEREFORD.FMS", "*?.?*")] // expected failure: https://github.com/fastwildcard/fastwildcard/issues/46
	[TestCase("1xutilisation Cambridgeshireiz2", "1x*i?2")] // expected failure for now: https://github.com/fastwildcard/fastwildcard/issues/45
	public void IsMatch(String str, String pattern) {
		Assert.That(DoMatch(str, pattern), Is.True);
	}

	[TestCase("ab", "a?c")]
	[TestCase("abc", "a?")]
	[TestCase("abc", " ")]
	[TestCase("bbcde", "a?cde")]
	[TestCase("bacde", "?bcde")]
	[TestCase("bbcde", "abcd?")]
	[TestCase("abbde", "a*cde")]
	[TestCase("bbcde", "a*cde")]
	[TestCase("bacde", "*bcde")]
	[TestCase("bbcde", "abcd*")]
	[TestCase("abc", "a*bc*de")]
	[TestCase("abbde", "abcde")]
	[TestCase(" ", "  ")]
	[TestCase("  ", " ")]
	public void IsNotMatch(String str, String pattern) {
		Assert.That(DoMatch(str, pattern), Is.False);
	}

	[TestCase("Abc", "a?c")]
	public void IsMatch_Ordinal_DoesNotMatchDifferentCase(String str, String pattern) {
		Assert.That(DoMatch(str, pattern, StringComparison.Ordinal), Is.False);
	}

	[TestCase("abc", "a?c")]
	[TestCase("Abc", "a?c")]
	public void IsMatch_OrdinalIgnoreCase_MatchesOrdinalAndDifferentCase(String str, String pattern) {
		Assert.That(DoMatch(str, pattern, StringComparison.OrdinalIgnoreCase), Is.True);
	}
}