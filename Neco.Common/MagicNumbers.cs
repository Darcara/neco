namespace Neco.Common;

using System;

public static class MagicNumbers {
	/// Objects larger than or equal to 85.000 bytes are considered a large object and is allocated on the large object heap.
	/// <value>80 KiB or 81.920 Bytes</value> 
	public const Int32 MaxNonLohBufferSize = 80 * 1024;
}