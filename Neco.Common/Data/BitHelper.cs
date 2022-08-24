namespace Neco.Common.Data;

using System;
using System.Runtime.CompilerServices;

public class BitHelper {
	public static Int32 ReadBigEndian(ReadOnlySpan<Byte> data, Int32 offset, out UInt16 value) {
		value = (UInt16)(data[offset] << 8);
		value += data[offset + 1];

		return offset + sizeof(Int16);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean IsBitSet(UInt16 num, Int32 bit) {
		return (num & (1 << bit)) != 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetBit(ref UInt16 num, Int32 bit) {
		num |= (UInt16)(1 << bit);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ClearBit(ref UInt16 num, Int32 bit) {
		num &= (UInt16)(~(1 << bit));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Bit(ref UInt16 num, Int32 bit, Boolean shouldBeSet) {
		if (shouldBeSet)
			num |= (UInt16)(1 << bit);
		else
			num &= (UInt16)(~(1 << bit));
	}
}