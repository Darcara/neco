namespace Neco.Common;

using System;

public static class MagicNumbers {
	/// Objects larger than or equal to 85000 bytes are considered a large object and is allocated on the large object heap.
	/// <value>80 KiB or 81920 Bytes</value> 
	public const Int32 MaxNonLohBufferSize = 80 * 1024;

	/// The default Buffer size for many streams and stream operations
	/// <value>8 KiB or 8192 Bytes</value>
	public const Int32 DefaultStreamBufferSize = 8192;
}