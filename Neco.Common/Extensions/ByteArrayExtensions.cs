namespace Neco.Common.Extensions;

using System;
using System.Text;

public static class ByteArrayExtensions {
	/// <summary>
	/// <para>Converts a byte array into a readable string of the form AABBCCDDEEFF</para>
	/// <para>See <see cref="Convert.ToHexString(byte[])"/></para>
	/// </summary>
	public static String ToStringHexSingleLine(this Byte[] bytes, Int32 offset = 0, Int32 length = -1) => Convert.ToHexString(bytes, offset, length < 0 ? bytes.Length - offset : length);

	public static String ToStringHexMultiLine(this Byte[] bArr, Int32 size = 16, String spacer = " ") {
		StringBuilder sb = new("");
		for (Int32 i = 0; i < bArr.Length; ++i) {
			if (i % size == 0 && i > 0)
				sb.AppendLine();

			sb.Append(bArr[i].ToString("X2"));
			sb.Append(spacer);
		}

		return sb.ToString();
	}

	// public static String ToStringHexDump(this Byte[] bArr, Int32 size = 16, Int32 offset = 0, Int32 length = -1) => new ReadOnlySpan<Byte>(bArr, offset, length > 0 ? length : bArr.Length).ToStringHexDump(size);
	// public static String ToStringHexDump(this ReadOnlySpan<Byte> bArr, Int32 size = 16) {

	public static String ToStringHexDump(this Span<Byte> bArr, Int32 size = 16) => bArr.ToArray().ToStringHexDump(size);
	public static String ToStringHexDump(this ReadOnlySpan<Byte> bArr, Int32 size = 16) => bArr.ToArray().ToStringHexDump(size);

	public static String ToStringHexDump(this Byte[]? bArr, Int32 size = 16, Int32 offset = -1, Int32 length = -1) {
		if (bArr == null)
			return "null";
		if (bArr.Length == 0)
			return "empty";
		if (offset < 0)
			offset = 0;
		if (length <= 0 || length > bArr.Length)
			length = bArr.Length;

		if (offset + length > bArr.Length)
			length = bArr.Length - offset;

		StringBuilder sb = new("");
		Byte[] tArr = new Byte[1];
		Int32 cnt = 0;
		for (Int32 i = offset; i < offset + length; ++i) {
			if (cnt % size == 0 && cnt > 0) {
				sb.Append(' ');
				for (Int32 j = i - size; j < i; ++j) {
					if (bArr[j] < 32)
						sb.Append('.');
					else {
						tArr[0] = bArr[j];
						sb.Append(Encoding.Default.GetString(tArr));
					}
				}

				sb.AppendLine();
			}

			sb.Append(bArr[i].ToString("X2"));
			++cnt;
		}

		for (Int32 i = 0; i < (size - (length % size)) % size; ++i)
			sb.Append("  ");
		sb.Append(' ');
		for (Int32 j = Math.Max(0, (offset + length) - (length % size == 0 ? size : length % size)); j < Math.Min(bArr.Length, offset + length); ++j) {
			if (bArr[j] < 32)
				sb.Append('.');
			else {
				tArr[0] = bArr[j];
				sb.Append(Encoding.Default.GetString(tArr));
			}
		}

		return sb.ToString();
	}

	public static void CopyTo(this Byte[] source, Int32 idxSource, Byte[] target, Int32 idxTarget, Int32 length) {
		Array.Copy(source, idxSource, target, idxTarget, length);
	}

	/// <summary>
	/// Checks to see if the contents of two byte arrays are equal
	/// </summary>
	/// <param name="array1">The first array</param>
	/// <param name="array2">The second array</param>
	/// <param name="offset1">The starting index for the first array</param>
	/// <param name="offset2">The starting index for the second array</param>
	/// <param name="count">The number of bytes to check</param>
	/// <returns>True if the arrays are equal, false if they aren't</returns>
	public static Boolean Matches(this Byte[]? array1, Byte[]? array2, Int32 offset1 = 0, Int32 offset2 = 0, Int32 count = -1) {
		ArgumentOutOfRangeException.ThrowIfNegative(offset1);
		ArgumentOutOfRangeException.ThrowIfNegative(offset2);
		if (ReferenceEquals(array1, array2)) return true;
		if (array1 == null || array2 == null) return false;
		Int32 count1 = count >= 0 ? count : array1.Length - offset1;
		Int32 count2 = count >= 0 ? count : array2.Length - offset2;
		if(count1 != count2 || offset1 + count1 > array1.Length || offset2 + count2 > array2.Length) return false;

		return array1.AsSpan(offset1, count1).SequenceEqual(new ReadOnlySpan<Byte>(array2, offset2, count2));
	}
}