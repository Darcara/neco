namespace Neco.Common.Data.Hash;

using System.Security.Cryptography;

public sealed class IncrementalHashWrapper : IIncrementalHash {
	private readonly IncrementalHash _hash;

	public IncrementalHashWrapper(IncrementalHash hash) {
		_hash = hash;
	}

	/// <inheritdoc />
	public void AppendData(Byte[] data) {
		_hash.AppendData(data);
	}

	/// <inheritdoc />
	public void AppendData(Byte[] data, Int32 offset, Int32 count) {
		_hash.AppendData(data, offset, count);
	}

	/// <inheritdoc />
	public void AppendData(ReadOnlySpan<Byte> data) {
		_hash.AppendData(data);
	}

	/// <inheritdoc />
	public IIncrementalHash Clone() => new IncrementalHashWrapper(_hash.Clone());

	/// <inheritdoc />
	public void Dispose() {
		_hash.Dispose();
	}

	/// <inheritdoc />
	public Byte[] GetCurrentHash() => _hash.GetCurrentHash();

	/// <inheritdoc />
	public Int32 GetCurrentHash(Span<Byte> destination) => _hash.GetCurrentHash(destination);

	/// <inheritdoc />
	public Byte[] GetHashAndReset() => _hash.GetHashAndReset();

	/// <inheritdoc />
	public Int32 GetHashAndReset(Span<Byte> destination) => _hash.GetHashAndReset(destination);

	/// <inheritdoc />
	public Boolean TryGetCurrentHash(Span<Byte> destination, out Int32 bytesWritten) => _hash.TryGetCurrentHash(destination, out bytesWritten);

	/// <inheritdoc />
	public Boolean TryGetHashAndReset(Span<Byte> destination, out Int32 bytesWritten) => _hash.TryGetHashAndReset(destination, out bytesWritten);

	/// <inheritdoc />
	public HashAlgorithmName AlgorithmName => _hash.AlgorithmName;

	/// <inheritdoc />
	public Int32 HashLengthInBytes => _hash.HashLengthInBytes;

	/// <inheritdoc />
	public void Reset() {
		_hash.GetHashAndReset();
	}
}