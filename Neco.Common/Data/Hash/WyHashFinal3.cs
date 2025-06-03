namespace Neco.Common.Data.Hash;

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using Neco.Common.Helper;

/// <summary>
/// Originally named _wyp
/// </summary>
public readonly struct WyHashSecret {
	// the default secret parameters
	public readonly UInt64 Item0 = 0xa0761d6478bd642fUL;
	public readonly UInt64 Item1 = 0xe7037ed1a0b428dbUL;
	public readonly UInt64 Item2 = 0x8ebc6af09c88c6e3UL;
	public readonly UInt64 Item3 = 0x589965cc75374cc3UL;

	public WyHashSecret() {
	}

	public WyHashSecret(UInt64 item0, UInt64 item1, UInt64 item2, UInt64 item3) {
		Item0 = item0;
		Item1 = item1;
		Item2 = item2;
		Item3 = item3;
	}

	public WyHashSecret(ReadOnlySpan<UInt64> seedItems) {
		Debug.Assert(seedItems.Length == 4);
		Item0 = seedItems[0];
		Item1 = seedItems[1];
		Item2 = seedItems[2];
		Item3 = seedItems[3];
	}

	#region Equality members

	public Boolean Equals(WyHashSecret other) => Item0 == other.Item0 && Item1 == other.Item1 && Item2 == other.Item2 && Item3 == other.Item3;

	/// <inheritdoc />
	public override Boolean Equals(Object? obj) => obj is WyHashSecret other && Equals(other);

	/// <inheritdoc />
	public override Int32 GetHashCode() {
		unchecked {
			Int32 hashCode = Item0.GetHashCode();
			hashCode = (hashCode * 397) ^ Item1.GetHashCode();
			hashCode = (hashCode * 397) ^ Item2.GetHashCode();
			hashCode = (hashCode * 397) ^ Item3.GetHashCode();
			return hashCode;
		}
	}

	public static Boolean operator ==(WyHashSecret left, WyHashSecret right) => left.Equals(right);

	public static Boolean operator !=(WyHashSecret left, WyHashSecret right) => !left.Equals(right);

	#endregion
}

[Obsolete("Use System.IO.Hashing.XxHash3 instead.")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed unsafe class WyHashFinal3 : AIncrementalHash {
	private static readonly Byte[] _seedLookupBytes = [15, 23, 27, 29, 30, 39, 43, 45, 46, 51, 53, 54, 57, 58, 60, 71, 75, 77, 78, 83, 85, 86, 89, 90, 92, 99, 101, 102, 105, 106, 108, 113, 114, 116, 120, 135, 139, 141, 142, 147, 149, 150, 153, 154, 156, 163, 165, 166, 169, 170, 172, 177, 178, 180, 184, 195, 197, 198, 201, 202, 204, 209, 210, 212, 216, 225, 226, 228, 232, 240];
	private static readonly UInt64 _seedLookupBytesLength = (UInt64)_seedLookupBytes.Length;

	private readonly UInt64 _originalSeed;
	private readonly WyHashSecret _originalSecret;
	private readonly Byte[] _state = GC.AllocateUninitializedArray<Byte>(64, true);
	private UInt64 _stateSeed, _stateSee1, _stateSee2;
	private Int32 _byteBuffered;
	private UInt64 _totalBytesProcessed;

	[Obsolete("Use System.IO.Hashing.XxHash3 instead.")]
	public WyHashFinal3() : this(0xAC8FDFCE8D7ED9DFUL, new WyHashSecret()) {
	}

	public WyHashFinal3(UInt64 seed, WyHashSecret? secret = null) : base(new HashAlgorithmName("WyHashFinal3"), 8) {
		_originalSeed = seed;
		_originalSecret = secret ?? new WyHashSecret();

		Reset();
	}

	public static void MakeSecret(UInt64 seed, Span<UInt64> secret) {
		Debug.Assert(secret.Length == 4);
		for (Int32 i = 0; i < 4; i++) {
			Boolean ok;
			do {
				ok = true;
				secret[i] = 0;
				for (Int32 j = 0; j < 64; j += 8) secret[i] |= ((UInt64)_seedLookupBytes[_wyrand(&seed) % _seedLookupBytesLength]) << j;
				if (secret[i] % 2 == 0) {
					ok = false;
					continue;
				}

				for (Int32 j = 0; j < i; j++) {
					if (Popcnt.X64.IsSupported) {
						if (Popcnt.X64.PopCount(secret[j] ^ secret[i]) != 32) {
							ok = false;
							break;
						}
					} else {
						//manual popcount
						UInt64 x = secret[j] ^ secret[i];
						x -= (x >> 1) & 0x5555555555555555;
						x = (x & 0x3333333333333333) + ((x >> 2) & 0x3333333333333333);
						x = (x + (x >> 4)) & 0x0f0f0f0f0f0f0f0f;
						x = (x * 0x0101010101010101) >> 56;
						if (x != 32) {
							ok = false;
							break;
						}
					}
				}
			} while (!ok);
		}
	}

	/// <summary>
	/// Perform a MUM (MUltiply and Mix) operation. Multiplies 2 unsigned 64-bit integers, then combines the
	/// hi and lo bits of the resulting 128-bit integer using XOR 
	/// </summary>
	/// <param name="x">First 64-bit integer</param>
	/// <param name="y">Second 64-bit integer</param>
	/// <returns>Result of the MUM (MUltiply and Mix) operation</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static UInt64 _wymix(UInt64 x, UInt64 y) {
		UInt64 hi = Math.BigMul(x, y, out UInt64 lo);
		return hi ^ lo;
	}

	//read functions
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static UInt64 _wyr8(Byte* p) => *(UInt64*)(p);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static UInt64 _wyr4(Byte* p) => *(UInt32*)(p);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static UInt64 _wyr3(Byte* p, UInt64 k) => (((UInt64)p[0]) << 16) | (((UInt64)p[k >> 1]) << 8) | p[k - 1];

	//wyhash main function
	private static UInt64 _wyhash(Byte* key, UInt64 len, UInt64 seed, WyHashSecret secret) {
		Byte* p = key;
		seed ^= secret.Item0;
		UInt64 a, b;
		if ((len <= 16)) {
			if ((len >= 4)) {
				a = (_wyr4(p) << 32) | _wyr4(p + ((len >> 3) << 2));
				b = (_wyr4(p + len - 4) << 32) | _wyr4(p + len - 4 - ((len >> 3) << 2));
			} else if ((len > 0)) {
				a = _wyr3(p, len);
				b = 0;
			} else a = b = 0;
		} else {
			UInt64 i = len;
			if ((i > 48)) {
				UInt64 see1 = seed, see2 = seed;
				do {
					seed = _wymix(_wyr8(p) ^ secret.Item1, _wyr8(p + 8) ^ seed);
					see1 = _wymix(_wyr8(p + 16) ^ secret.Item2, _wyr8(p + 24) ^ see1);
					see2 = _wymix(_wyr8(p + 32) ^ secret.Item3, _wyr8(p + 40) ^ see2);
					p += 48;
					i -= 48;
				} while ((i > 48));

				seed ^= see1 ^ see2;
			}

			while ((i > 16)) {
				seed = _wymix(_wyr8(p) ^ secret.Item1, _wyr8(p + 8) ^ seed);
				i -= 16;
				p += 16;
			}

			a = _wyr8(p + i - 16);
			b = _wyr8(p + i - 8);
		}

		return _wymix(secret.Item1 ^ len, _wymix(a ^ secret.Item1, b ^ seed));
	}

	//a useful 64bit-64bit mix function to produce deterministic pseudo random numbers that can pass BigCrush and PractRand
	private static UInt64 _wyhash64(UInt64 a, UInt64 b) {
		a ^= 0xa0761d6478bd642fUL;
		b ^= 0xe7037ed1a0b428dbUL;
		UInt64 a1 = Math.BigMul(a, b, out UInt64 b1);
		return _wymix(a1 ^ 0xa0761d6478bd642fUL, b1 ^ 0xe7037ed1a0b428dbUL);
	}

	//The wyrand PRNG that pass BigCrush and PractRand
	private static UInt64 _wyrand(UInt64* seed) {
		*seed += 0xa0761d6478bd642fUL;
		return _wymix(*seed, *seed ^ 0xe7037ed1a0b428dbUL);
	}

	//convert any 64 bit pseudo random numbers to uniform distribution [0,1). It can be combined with wyrand, wyhash64 or wyhash.
	private static Double _wy2u01(UInt64 r) {
		const Double wynorm = 1.0 / (1UL << 52);
		return (r >> 12) * wynorm;
	}

	//convert any 64 bit pseudo random numbers to APPROXIMATE Gaussian distribution. It can be combined with wyrand, wyhash64 or wyhash.
	private static Double _wy2gau(UInt64 r) {
		const Double wynorm = 1.0 / (1UL << 20);
		return ((r & 0x1fffff) + ((r >> 21) & 0x1fffff) + ((r >> 42) & 0x1fffff)) * wynorm - 3.0;
	}

#if(!WYHASH_32BIT_MUM)
	//fast range integer random number generation on [0,k) credit to Daniel Lemire. May not work when WYHASH_32BIT_MUM=1. It can be combined with wyrand, wyhash64 or wyhash.
	private static UInt64 _wy2u0k(UInt64 r, UInt64 k) {
		UInt64 _ = Math.BigMul(r, k, out UInt64 k1);
		return k1;
	}
#endif

	[Obsolete("Use System.IO.Hashing.XxHash3 instead.")]
	public static Byte[] HashOneOff(ReadOnlySpan<Byte> data, UInt64 seed = 0xAC8FDFCE8D7ED9DFUL, WyHashSecret? secret = null) {
		fixed (Byte* ptr = data) {
			UInt64 hash = _wyhash(ptr, (UInt64)data.Length, seed, secret ?? new WyHashSecret());
			Byte[] bytes = new Byte[sizeof(UInt64)];
			Unsafe.As<Byte, UInt64>(ref bytes[0]) = hash;
			return bytes;
		}
	}

	[Obsolete("Use System.IO.Hashing.XxHash3 instead.")]
	public static void HashOneOff(ReadOnlySpan<Byte> data, Span<Byte> hashOutput, UInt64 seed = 0xAC8FDFCE8D7ED9DFUL, WyHashSecret? secret = null) {
		Debug.Assert(hashOutput.Length == 8);
		fixed (Byte* ptr = data) {
			UInt64 hash = _wyhash(ptr, (UInt64)data.Length, seed, secret ?? new WyHashSecret());
			Unsafe.As<Byte, UInt64>(ref hashOutput[0]) = hash;
		}
	}

	/// <summary>
	/// Hashes the data into an unsigned long. Beware of endianess if comparing with <see cref="HashOneOff(System.ReadOnlySpan{byte},ulong,System.Nullable{Neco.Common.Data.Hash.WyHashSecret})">HashOneOff</see>
	/// </summary>
	[Obsolete("Use System.IO.Hashing.XxHash3 instead.")]
	public static UInt64 HashOneOffLong(ReadOnlySpan<Byte> data, UInt64 seed = 0xAC8FDFCE8D7ED9DFUL, WyHashSecret? secret = null) {
		fixed (Byte* ptr = data) {
			return _wyhash(ptr, (UInt64)data.Length, seed, secret ?? new WyHashSecret());
		}
	}

	[Obsolete("Use System.IO.Hashing.XxHash3 instead.")]
	public static UInt64 HashOneOffLong(Byte* ptr, UInt64 len, UInt64 seed = 0xAC8FDFCE8D7ED9DFUL, WyHashSecret? secret = null) => _wyhash(ptr, len, seed, secret ?? new WyHashSecret());

	[Obsolete("Use System.IO.Hashing.XxHash3 instead.")]
	public static Byte[] HashOneOff(Byte[] array, Int32 ibStart, Int32 cbSize, UInt64 seed = 0xAC8FDFCE8D7ED9DFUL, WyHashSecret? secret = null) => HashOneOff(array.AsSpan(ibStart, cbSize), seed, secret);

	[Obsolete("Use System.IO.Hashing.XxHash3 instead.")]
	public static void HashOneOff(Byte[] array, Int32 ibStart, Int32 cbSize, Byte[] hashOutput, UInt64 seed = 0xAC8FDFCE8D7ED9DFUL, WyHashSecret? secret = null) {
		HashOneOff(array.AsSpan(ibStart, cbSize), hashOutput, seed, secret);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void HashIncrementalBlock(Byte* p) {
		_stateSeed = _wymix(_wyr8(p) ^ _originalSecret.Item1, _wyr8(p + 8) ^ _stateSeed);
		_stateSee1 = _wymix(_wyr8(p + 16) ^ _originalSecret.Item2, _wyr8(p + 24) ^ _stateSee1);
		_stateSee2 = _wymix(_wyr8(p + 32) ^ _originalSecret.Item3, _wyr8(p + 40) ^ _stateSee2);
	}

	/// <inheritdoc cref="IncrementalHash.AppendData(byte[], Int32, Int32)" />
	public void AppendData(Byte[] data, Int32 offset, Int32 count) => AppendData(new ReadOnlySpan<Byte>(data, offset, count));

	/// <inheritdoc cref="IncrementalHash.AppendData(ReadOnlySpan{Byte})" />
	public override void AppendData(ReadOnlySpan<Byte> data) {
		if (_byteBuffered == 0) {
			Int32 bytesRemaining = data.Length;
			if (bytesRemaining > 48) {
				fixed (Byte* ptr = data) {
					Byte* p = ptr;
					do {
						HashIncrementalBlock(p);
						bytesRemaining -= 48;
						_totalBytesProcessed += 48;
						p += 48;
					} while (bytesRemaining > 48);

					if (bytesRemaining < 16) {
						data
							.Slice(data.Length - 16, 16)
							.CopyTo(_state.AsSpan(bytesRemaining));
						_byteBuffered = bytesRemaining;
						return;
					}
				}
			}

			if (bytesRemaining > 0) {
				// Copy rest
				data
					.Slice(data.Length - bytesRemaining, bytesRemaining)
					.CopyTo(_state.AsSpan(16));
				_byteBuffered = bytesRemaining;
			}
		} else {
			// Fill buffer as much as possible
			Int32 maxBytesToFill = 48 - _byteBuffered;
			Int32 bytesToCopy = data.Length > maxBytesToFill ? maxBytesToFill : data.Length;
			data
				.Slice(0, bytesToCopy)
				.CopyTo(_state.AsSpan(16 + _byteBuffered));
			_byteBuffered += bytesToCopy;

			Int32 bytesRemaining = data.Length - bytesToCopy;
			if (_byteBuffered < 48 || bytesRemaining == 0) return;

			fixed (Byte* ptr = _state.AsSpan(16)) {
				HashIncrementalBlock(ptr);
				_totalBytesProcessed += 48;

				if (bytesRemaining < 16) {
					Int32 numToCopy = 16 - bytesRemaining;
					_state
						.AsSpan(64 - numToCopy, numToCopy)
						.CopyTo(_state.AsSpan(16 - numToCopy));
				}
			}

			_byteBuffered = 0;
			if (bytesRemaining > 0) AppendData(data.Slice(bytesToCopy, bytesRemaining));
		}
	}

	/// <inheritdoc />
	public override IIncrementalHash Clone() => throw new NotImplementedException();

	/// <inheritdoc />
	public override Int32 GetCurrentHash(Span<Byte> destination) {
		if (_totalBytesProcessed == 0) {
			HashOneOff(_state.AsSpan( 16, _byteBuffered), destination, _originalSeed, _originalSecret);
			return HashLengthInBytes;
		}

		_stateSeed ^= _stateSee1 ^ _stateSee2;

		UInt64 i = (UInt64)_byteBuffered;
		fixed (Byte* ptr = _state) {
			Byte* p = ptr;
			p += 16;
			while ((i > 16)) {
				_stateSeed = _wymix(_wyr8(p) ^ _originalSecret.Item1, _wyr8(p + 8) ^ _stateSeed);
				i -= 16;
				p += 16;
			}

			UInt64 a = _wyr8(p + i - 16);
			UInt64 b = _wyr8(p + i - 8);


			UInt64 finalHash = _wymix(_originalSecret.Item1 ^ ((UInt64)_byteBuffered + _totalBytesProcessed), _wymix(a ^ _originalSecret.Item1, b ^ _stateSeed));
			BinaryPrimitives.WriteUInt64LittleEndian(destination, finalHash);
			return HashLengthInBytes;
		}
	}

	/// <inheritdoc cref="HashAlgorithm.Initialize" />
	public override void Reset() {
		_byteBuffered = 0;
		_totalBytesProcessed = 0;
		_stateSeed = _originalSeed ^ _originalSecret.Item0;
		_stateSee1 = _stateSeed;
		_stateSee2 = _stateSeed;
	}

	/// <para>With old Bmi2.X64.MultiplyNoFlags<br/>
	/// wyhash-64-final3 44,282,796 ops in 5,000.001ms = clean per operation: 0.081µs or 12,415,099.974op/s with GC 0/0/0<br/>
	/// wyhash-64-final3 TotalCPUTime per operation: 5,000.000ms or clean 12,415,102.062op/s for a factor of 1.000</para>
	/// <para>With Math.BigMul Implementation:<br/>
	/// wyhash-64-final3 45,014,739 ops in 5,000.001ms = clean per operation: 0.079µs or 12,696,560.045op/s with GC 0/0/0<br/>
	/// wyhash-64-final3 TotalCPUTime per operation: 5,000.000ms or clean 12,696,561.835op/s for a factor of 1.000</para>
	public static void PerfTest() {
		Byte[] hashme = GC.AllocateUninitializedArray<Byte>(1024, true);
		Byte[] hash = GC.AllocateUninitializedArray<Byte>(8, true);
		Random.Shared.NextBytes(hashme);
		PerformanceHelper.GetPerformanceRough("wyhash-64-final3", () => { HashOneOff(hashme, hash); });
	}
}