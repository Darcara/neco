namespace Neco.Common.Extensions;

using System;

public static class RandomExtensions {
	public static UInt64 NextUInt64(this Random rnd) => (UInt64)rnd.NextInt64();
	public static UInt32 NextUInt32(this Random rnd) => (UInt32)rnd.Next();
}