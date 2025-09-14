namespace Neco.Common.Data.Hash;

using System.Security.Cryptography;

public interface IIncrementalHash : IDisposable {
	void AppendData(Byte[] data);
	void AppendData(Byte[] data, Int32 offset, Int32 count);
	void AppendData(ReadOnlySpan<Byte> data);
	IIncrementalHash Clone();
	Byte[] GetCurrentHash();

	/// <inheritdoc cref="IncrementalHash.GetCurrentHash(Span{Byte})"/>
	Int32 GetCurrentHash(Span<Byte> destination);

	Byte[] GetHashAndReset();
	Int32 GetHashAndReset(Span<Byte> destination);

	/// <inheritdoc cref="IncrementalHash.TryGetCurrentHash"/>
	Boolean TryGetCurrentHash(Span<Byte> destination, out Int32 bytesWritten);

	/// <inheritdoc cref="IncrementalHash.TryGetHashAndReset"/>
	Boolean TryGetHashAndReset(Span<Byte> destination, out Int32 bytesWritten);

	HashAlgorithmName AlgorithmName { get; }
	Int32 HashLengthInBytes { get; }

	void Reset();
}

public static class IIncrementalHashExtensions {
	public static Byte[] Hash(this IIncrementalHash hasher, ReadOnlySpan<Byte> data) {
		hasher.AppendData(data);
		return hasher.GetHashAndReset();
	}
	
	public static Int32 Hash(this IIncrementalHash hasher, ReadOnlySpan<Byte> data, Span<Byte> destination) {
		hasher.AppendData(data);
		return hasher.GetHashAndReset(destination);
	}

}