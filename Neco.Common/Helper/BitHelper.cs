namespace Neco.Common.Helper;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

public static class BitHelper {

	/// <summary>
	/// Check if a bit is set to <see cref="INumberBase{T}.One"/>
	/// </summary>
	/// <param name="num">The numeric value</param>
	/// <param name="bit">The bit to check</param>
	/// <returns>TRUE if the bit is set, FALSE otherwise</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static Boolean IsBitSet<T>(T num, Int32 bit) where T : INumberBase<T>, IShiftOperators<T, Int32, T>, IBitwiseOperators<T, T, T> {
		return (num & (T.One << bit)) != T.Zero;
	}

	/// <summary>
	/// Set a bit to <see cref="INumberBase{T}.One"/>
	/// </summary>
	/// <param name="num">The numeric value</param>
	/// <param name="bit">The bit to set</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void SetBit<T>(ref T num, Int32 bit) where T : INumberBase<T>, IShiftOperators<T, Int32, T>, IBitwiseOperators<T, T, T> {
		num |= T.One << bit;
	}

	/// <summary>
	/// Clear a bit and set it to <see cref="INumberBase{T}.Zero"/>
	/// </summary>
	/// <param name="num">The numeric value</param>
	/// <param name="bit">The bit to set</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void ClearBit<T>(ref T num, Int32 bit) where T : INumberBase<T>, IShiftOperators<T, Int32, T>, IBitwiseOperators<T, T, T> {
		num &= ~(T.One << bit);
	}

	/// <summary>
	/// Toggles a bit. Sets it to <see cref="INumberBase{T}.One"/> is it was unset, or to <see cref="INumberBase{T}.Zero"/> if it was set.
	/// </summary>
	/// <param name="num">The numeric value</param>
	/// <param name="bit">The bit to toggle</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void ToggleBit<T>(ref T num, Int32 bit) where T : INumberBase<T>, IShiftOperators<T, Int32, T>, IBitwiseOperators<T, T, T> {
		num ^= T.One << bit;
	}

	/// <summary>
	/// Sets or clears a bit.
	/// </summary>
	/// <param name="num">The numeric value</param>
	/// <param name="bit">The bit to set or clear</param>
	/// <param name="shouldBeSet">TRUE to set the bit, FALSE to clear it</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
	public static void Bit<T>(ref T num, Int32 bit, Boolean shouldBeSet) where T : INumberBase<T>, IShiftOperators<T, Int32, T>, IBitwiseOperators<T, T, T> {
		if (shouldBeSet)
			num |= T.One << bit;
		else
			num &= ~(T.One << bit);
	}


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