namespace Neco.Common.Data;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

public static class SequentialGuidGenerator {
	private const Int64 _startOf2000Ticks = 630822816000000000;
	private static readonly Int64 _startTicks = (DateTime.UtcNow.Ticks - _startOf2000Ticks) / 100;
	private static readonly Int64 _startTimestamp = Stopwatch.GetTimestamp();

	/// <summary>
	/// 10_000_000 DateTimeTicks per Second / PerfCounterTicksPerSecond = DateTimeTicks / PerfCounterTicks
	/// </summary>
	private static readonly Double _tickFrequency = 10_000_000D / Stopwatch.Frequency / 100;

	private static Int64 _lastTicks;

	/// <summary>
	/// Creates a <see cref="Guid"/>, so that every Guid generated after that is <see cref="Guid.op_GreaterThan">greather</see>
	/// </summary>
	/// <para>
	/// The returned Guids have an 48Bit timestamp in the beginning, that can be read with <see cref="FromSequentialGuid"/>.
	/// Since the id time is truncated to 10s of microseconds, the calculated time can be slightly before (max. 20 microseconds) <see cref="DateTime.UtcNow"/>.
	/// </para>
	public static Guid CreateSequentialBinary() {
		Guid g = Guid.NewGuid();

		// Since we have 6 bytes / 48 Bits of "time" in out guid we can represent
		// 8900 years in milliseconds (ticks / 10_000)
		// 89 years with 10s of microseconds (ticks / 100) <-- this is what we want to use
		// 8.9 years in microseconds (ticks / 10)
		Int64 ticks = (Int64)((Stopwatch.GetTimestamp() - _startTimestamp) * _tickFrequency + _startTicks) & 0xFFFFFFFFFFFF;
		// Since the time resolution is very low, two subsequent calls can end in the same tick-Timestamp
		Int64 lastTicks = Interlocked.Read(ref _lastTicks);
		if (ticks <= lastTicks) {
			// lastTicks will never be smaller than ticks, so incrementing it will always be greater than any other id generated
			ticks = Interlocked.Increment(ref _lastTicks);
		} else {
			Interlocked.CompareExchange(ref _lastTicks, ticks, lastTicks);
		}

		// Reinterpret Guid as a Span ob bytes
		Span<Guid> guidSpan = MemoryMarshal.CreateSpan(ref g, 1);
		Span<Byte> byteSpan = MemoryMarshal.AsBytes(guidSpan);

		// Work directly on GUID
		byteSpan[0] = (Byte)((ticks >> 40) & 0xFF);
		byteSpan[1] = (Byte)((ticks >> 32) & 0xFF);
		byteSpan[2] = (Byte)((ticks >> 24) & 0xFF);
		byteSpan[3] = (Byte)((ticks >> 16) & 0xFF);
		byteSpan[4] = (Byte)((ticks >> 8) & 0xFF);
		byteSpan[5] = (Byte)(ticks & 0xFF);

		return g;
	}

	/// <summary>
	/// Returns the timestamp bit from a <see cref="CreateSequentialBinary"/> generated Guid.
	/// </summary>
	/// <para>
	/// Since the id time is truncated to 10s of microseconds, the calculated time can be slightly before (max. 20 microseconds) <see cref="DateTime.UtcNow"/>.
	/// </para> 
	public static DateTime FromSequentialGuid(Guid g) {
		Span<Guid> guidSpan = MemoryMarshal.CreateSpan(ref g, 1);
		Span<Byte> byteSpan = MemoryMarshal.AsBytes(guidSpan);

		// Not really ticks but 10s of microseconds, see above
		Int64 ticksSince2000 = byteSpan[5] | (Int64)byteSpan[4] << 8 | (Int64)byteSpan[3] << 16 | (Int64)byteSpan[2] << 24 | (Int64)byteSpan[1] << 32 | (Int64)byteSpan[0] << 40;

		return new DateTime(ticksSince2000 * 100 + _startOf2000Ticks, DateTimeKind.Utc);
	}

	public static Byte[] ToMySqlUuid(Guid g) {
		Byte[] b = g.ToByteArray();
		for (Int32 i = 0; i < 6; i++) (b[10 + i], b[i]) = (b[i], b[10 + i]);
		return b;
	}

	public static Guid FromMySqlUuid(Byte[] b) {
		Guid g = new(b);
		// Reinterpret Guid as a Span ob bytes
		Span<Guid> guidSpan = MemoryMarshal.CreateSpan(ref g, 1);
		Span<Byte> byteSpan = MemoryMarshal.AsBytes(guidSpan);
		
		for (Int32 i = 0; i < 6; i++) (byteSpan[10 + i], byteSpan[i]) = (byteSpan[i], byteSpan[10 + i]);
		return g;
	}
}