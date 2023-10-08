namespace Neco.Common.Helper;

using System;
using System.Runtime.CompilerServices;

public static class BitHelper {
	#region Int16

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean IsBitSet(Int16 num, Int32 bit) => (num & (1 << bit)) != 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetBit(ref Int16 num, Int32 bit) => num |= (Int16)(1 << bit);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ClearBit(ref Int16 num, Int32 bit) => num &= (Int16)(~(1 << bit));
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ToggleBit(ref Int16 num, Int32 bit) => num ^= (Int16)(1 << bit);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Bit(ref Int16 num, Int32 bit, Boolean shouldBeSet) {
		if (shouldBeSet)
			num |= (Int16)(1 << bit);
		else
			num &= (Int16)(~(1 << bit));
	}
	
	#endregion
	
	#region UInt16

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Boolean IsBitSet(UInt16 num, Int32 bit) => (num & (1 << bit)) != 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void SetBit(ref UInt16 num, Int32 bit) => num |= (UInt16)(1 << bit);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ClearBit(ref UInt16 num, Int32 bit) => num &= (UInt16)(~(1 << bit));
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ToggleBit(ref UInt16 num, Int32 bit) => num ^= (UInt16)(1 << bit);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Bit(ref UInt16 num, Int32 bit, Boolean shouldBeSet) {
		if (shouldBeSet)
			num |= (UInt16)(1 << bit);
		else
			num &= (UInt16)(~(1 << bit));
	}
	
	#endregion

}