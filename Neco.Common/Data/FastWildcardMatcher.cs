namespace Neco.Common.Data;

using System.Runtime.CompilerServices;

/// <summary>
/// Fast Wildcard ('*' and '?') matching
/// </summary>
// Originally based on: https://github.com/fastwildcard/fastwildcard
public static class FastWildcardMatcher {
	private const Char _singleWildcardCharacter = '?';
	private const Char _multiWildcardCharacter = '*';

	/// <summary>
	/// Returns if the input string <paramref name="str"/> matches the given wildcard pattern <paramref name="pattern"/>.
	/// </summary>
	/// <param name="str">Input string to match on</param>
	/// <param name="pattern">Wildcard pattern to match with</param>
	/// <param name="strComparison">How to perform string comparision. Defaults to <see cref="StringComparison.Ordinal"/></param>
	/// <returns>True if a match is found, false otherwise</returns>
	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static Boolean IsMatch(ReadOnlySpan<Char> str, ReadOnlySpan<Char> pattern, StringComparison strComparison = StringComparison.Ordinal) {
		// Pattern must contain something
		ArgumentOutOfRangeException.ThrowIfZero(pattern.Length, nameof(pattern));

		// Multi character wildcard matches everything
		if (pattern.Length == 1 && pattern[0] == _multiWildcardCharacter) {
			return true;
		}

		// Empty string does not match
		if (str.Length == 0) {
			return false;
		}

		Int32 strIndex = 0;

		for (Int32 patternIndex = 0; patternIndex < pattern.Length; patternIndex++) {
			Char patternCh = pattern[patternIndex];
			ReadOnlySpan<Char> patternChSpan = pattern.Slice(patternIndex, 1);

			if (strIndex == str.Length) {
				// At end of pattern for this longer string so always matches '*'
				if (patternCh == _multiWildcardCharacter && patternIndex == pattern.Length - 1) {
					return true;
				}

				return false;
			}

			// Character match
			ReadOnlySpan<Char> strCh = str.Slice(strIndex, 1);
			if (patternChSpan.Equals(strCh, strComparison)) {
				strIndex++;
				continue;
			}

			// Single wildcard match
			if (patternCh == _singleWildcardCharacter) {
				strIndex++;
				continue;
			}

			// No match
			if (patternCh != _multiWildcardCharacter) {
				return false;
			}

			// Multi character wildcard - last character in the pattern
			if (patternIndex == pattern.Length - 1) {
				return true;
			}

			// Match pattern to input string character-by-character until the next wildcard (or end of string if there is none)
			Int32 patternChMatchStartIndex = patternIndex + 1;

			Int32 nextWildcardIndex = pattern.Slice(patternChMatchStartIndex).IndexOfAny(_multiWildcardCharacter, _singleWildcardCharacter);
			Int32 patternChMatchEndIndex = nextWildcardIndex == -1
				? pattern.Length - 1
				: nextWildcardIndex + patternChMatchStartIndex - 1;

			Int32 comparisonLength = patternChMatchEndIndex - patternIndex;

			ReadOnlySpan<Char> comparison = pattern.Slice(patternChMatchStartIndex, comparisonLength);
			Int32 skipToStringIndex = str.Slice(strIndex).IndexOf(comparison, strComparison) + strIndex;


			// Handle repeated instances of the same character at end of pattern
			if (comparisonLength == 1 && nextWildcardIndex == -1) {
				Int32 skipCandidateIndex = 0;
				while (skipCandidateIndex == 0) {
					Int32 skipToStringIndexNew = skipToStringIndex + 1;
					skipCandidateIndex = str.Slice(skipToStringIndexNew).IndexOf(comparison, strComparison);

					if (skipCandidateIndex == 0) {
						skipToStringIndex = skipToStringIndexNew;
					}
				}
			}

			if (skipToStringIndex == -1) {
				return false;
			}

			strIndex = skipToStringIndex;
		}

		// Pattern processing completed but rest of input string was not
		if (strIndex < str.Length) {
			return false;
		}

		return true;
	}
}