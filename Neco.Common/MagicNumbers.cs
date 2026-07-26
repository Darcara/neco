namespace Neco.Common;

using System.Diagnostics.CodeAnalysis;
using System.Text;

/// <summary>
/// Contains commonly used magic numbers and constants.
/// </summary>
[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
public static class MagicNumbers {
	/// <summary>
	/// The maximum recommended buffer size that avoids allocation on the Large Object Heap (LOH).
	/// </summary>
	/// <remarks>
	/// Objects larger than or equal to 85,000 bytes are considered large objects and allocated on the large object heap.
	/// This constant is set to 80 KiB to provide a safe margin.
	/// </remarks>
	/// <value>80 KiB or 81,920 bytes</value>
	public const Int32 MaxNonLohBufferSize = 80 * 1024;

	/// <summary>
	/// The default buffer size for stream operations.
	/// </summary>
	/// <remarks>
	/// This size balances memory usage with I/O efficiency and is suitable for most stream-based operations.
	/// </remarks>
	/// <value>8 KiB or 8,192 bytes</value>
	public const Int32 DefaultStreamBufferSize = 8192;

	/// <summary>
	/// Provides UTF-8 encoding without the BOM (Byte Order Mark) preamble.
	/// </summary>
	/// <remarks>
	/// This is useful when encoding strings to UTF-8 without the three-byte BOM prefix that <see cref="Encoding.UTF8"/> includes by default.
	/// </remarks>
	public static readonly UTF8Encoding Utf8NoBom = new(false);

	/// <summary>
	/// Well known and often used String-constants
	/// </summary>
	/// <seealso cref="WellKnownChars"/>
	public static class WellKnownStrings {
		/// <summary>
		/// NO-BREAK SPACE is a space character that prevents an automatic line break.
		/// </summary>
		/// <value>U+00A0 or &amp;nbsp;</value>
		public const String NonBreakingSpace = " ";

		/// <summary>
		/// NARROW NO-BREAK SPACE (NNBSP) is a non-breaking space that is narrower than the standard non-breaking space.
		/// </summary>
		/// <value>U+202F</value>
		public const String NarrowNonBreakingSpace = @" ";

		/// <summary>
		/// A space equal to the size of a single numerical digit.
		/// </summary>
		/// <value>U+2007 or &amp;numsp;</value>
		public const String NumericSpace = @" ";
	}

	/// <summary>
	/// Well known and often used Character-constants
	/// </summary>
	/// <seealso cref="WellKnownStrings"/>
	public static class WellKnownChars {
		/// <inheritdoc cref="WellKnownStrings.NonBreakingSpace"/>
		public const Char NonBreakingSpace = ' ';

		/// <inheritdoc cref="WellKnownStrings.NarrowNonBreakingSpace"/>
		public const Char NarrowNonBreakingSpace = ' ';

		/// <inheritdoc cref="WellKnownStrings.NumericSpace"/>
		public const Char NumericSpace = ' ';
	}
}