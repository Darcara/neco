namespace Neco.Common.Extensions;

using System;

public static class StringExtensions {
	public static String FirstCharToUpper(this String input) {
		return input switch {
			null => throw new ArgumentNullException(nameof(input)),
			"" => input,
			_ => String.Concat(input[..1].ToUpperInvariant(), input.AsSpan(1)),
		};
	}

	public static String FirstCharToLower(this String input) {
		return input switch {
			null => throw new ArgumentNullException(nameof(input)),
			"" => input,
			_ => String.Concat(input[..1].ToLowerInvariant(), input.AsSpan(1)),
		};
	}
}