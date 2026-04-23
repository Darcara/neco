namespace Neco.Common;

using System.Text;

/// <summary>
/// Contains commonly used magic numbers and constants.
/// </summary>
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
}