﻿namespace Neco.Common.Extensions;

using System;
using System.Globalization;

public static class NumericExtensions {
	#region Int64 clamping

	public static Int32 ToInt32Clamped(this Int64 source) {
		return source > Int32.MaxValue ? Int32.MaxValue : (source < Int32.MinValue ? Int32.MinValue : (Int32)source);
	}

	public static UInt32 ToUInt32Clamped(this UInt64 source) {
		return source > UInt32.MaxValue ? UInt32.MaxValue : (UInt32)source;
	}

	#endregion

	#region .ToFileSize

	public static String ToFileSize(this Int16 source, String format = "0.00", Boolean useSiPrefix = false) {
		return ToFileSize(Convert.ToDouble(source), format, useSiPrefix);
	}

	public static String ToFileSize(this UInt16 source, String format = "0.00", Boolean useSiPrefix = false) {
		return ToFileSize(Convert.ToDouble(source), format, useSiPrefix);
	}

	public static String ToFileSize(this Int32 source, String format = "0.00", Boolean useSiPrefix = false) {
		return ToFileSize(Convert.ToDouble(source), format, useSiPrefix);
	}

	public static String ToFileSize(this UInt32 source, String format = "0.00", Boolean useSiPrefix = false) {
		return ToFileSize(Convert.ToDouble(source), format, useSiPrefix);
	}

	public static String ToFileSize(this Int64 source, String format = "0.00", Boolean useSiPrefix = false) {
		return ToFileSize(Convert.ToDouble(source), format, useSiPrefix);
	}

	public static String ToFileSize(this UInt64 source, String format = "0.00", Boolean useSiPrefix = false) {
		return ToFileSize(Convert.ToDouble(source), format, useSiPrefix);
	}

	public static String ToFileSize(this Single source, String format = "0.00", Boolean useSiPrefix = false) {
		return ToFileSize(Convert.ToDouble(source), format, useSiPrefix);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="bytes"></param>
	/// <param name="format"></param>
	/// <param name="useSiPrefix">TRUE to devide by 1000 and use kB, MB etc. default=FALSE to devide by 1024 and use kiB, MiB, etc.</param>
	/// <returns></returns>
	public static String ToFileSize(this Double bytes, String format = "0.00", Boolean useSiPrefix = false) {
		Double factor = useSiPrefix ? 1000.0 : 1024.0;
		String i = useSiPrefix ? String.Empty : "i";

		Double kib = factor;
		Double mib = kib * factor;
		Double gib = mib * factor;
		Double tib = gib * factor;

		if (bytes >= tib) //TiB Range
			return $"{(bytes / tib).ToString(format, CultureInfo.InvariantCulture)} T{i}B";

		if (bytes >= gib) //GiB Range
			return $"{(bytes / gib).ToString(format, CultureInfo.InvariantCulture)} G{i}B";

		if (bytes >= mib) //MiB Range
			return $"{(bytes / mib).ToString(format, CultureInfo.InvariantCulture)} M{i}B";

		if (bytes >= kib) //KiB Range
			return $"{(bytes / kib).ToString(format, CultureInfo.InvariantCulture)} K{i}B";

		//Bytes
		return $"{bytes:0} Bytes";
	}

	#endregion

	#region .Kib .Mib .Gib

	public static Double KiB(this Double source) => source * 1024;

	public static Double MiB(this Double source) => source * 1024 * 1024;

	public static Double GiB(this Double source) => source * 1024 * 1024 * 1024;

	public static Double TiB(this Double source) => source * 1024 * 1024 * 1024 * 1024;

	public static Double KB(this Double source) => source * 1000;

	public static Double MB(this Double source) => source * 1000 * 1000;

	public static Double GB(this Double source) => source * 1000 * 1000 * 1000;

	public static Double TB(this Double source) => source * 1000 * 1000 * 1000 * 1000;

	public static Int64 KiB(this Int64 source) => source * 1024;

	public static Int64 MiB(this Int64 source) => source * 1024 * 1024;

	public static Int64 GiB(this Int64 source) => source * 1024 * 1024 * 1024;

	public static Int64 TiB(this Int64 source) => source * 1024 * 1024 * 1024 * 1024;

	public static Int64 KB(this Int64 source) => source * 1000;

	public static Int64 MB(this Int64 source) => source * 1000 * 1000;

	public static Int64 GB(this Int64 source) => source * 1000 * 1000 * 1000;

	public static Int64 TB(this Int64 source) => source * 1000 * 1000 * 1000 * 1000;

	#endregion
}