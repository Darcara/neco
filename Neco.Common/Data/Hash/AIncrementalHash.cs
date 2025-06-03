namespace Neco.Common.Data.Hash;

using System.Security.Cryptography;

public abstract class AIncrementalHash : IIncrementalHash {
	protected AIncrementalHash(HashAlgorithmName algorithmName, Int32 hashLengthInBytes) {
		AlgorithmName = algorithmName;
		HashLengthInBytes = hashLengthInBytes;
	}

	#region Implementation of IIncrementalHash

	/// <inheritdoc />
	public HashAlgorithmName AlgorithmName { get; }

	/// <inheritdoc />
	public Int32 HashLengthInBytes { get; }

	/// <inheritdoc />
	public void AppendData(Byte[] data) => AppendData(new ReadOnlySpan<Byte>(data));

	/// <inheritdoc />
	public void AppendData(Byte[] data, Int32 offset, Int32 count) => AppendData(new ReadOnlySpan<Byte>(data, offset, count));

	/// <inheritdoc />
	public Byte[] GetCurrentHash() {
		Byte[] hashBytes = new Byte[HashLengthInBytes];
		GetCurrentHash(hashBytes);
		return hashBytes;
	}

	/// <inheritdoc />
	public Byte[] GetHashAndReset() {
		Byte[] hashBytes = new Byte[HashLengthInBytes];
		GetCurrentHash(hashBytes);
		Reset();
		return hashBytes;
	}

	/// <inheritdoc />
	public Int32 GetHashAndReset(Span<Byte> destination) {
		Int32 bytesWritten = GetCurrentHash(destination);
		Reset();
		return bytesWritten;
	}

	/// <inheritdoc />
	public Boolean TryGetCurrentHash(Span<Byte> destination, out Int32 bytesWritten) {
		if (destination.Length < HashLengthInBytes) {
			bytesWritten = 0;
			return false;
		}
		bytesWritten = GetCurrentHash(destination);
		return true;
	}

	/// <inheritdoc />
	public Boolean TryGetHashAndReset(Span<Byte> destination, out Int32 bytesWritten) {
		if (destination.Length < HashLengthInBytes) {
			bytesWritten = 0;
			return false;
		}
		bytesWritten = GetCurrentHash(destination);
		Reset();
		return true;
	}

	/// <inheritdoc />
	public void Dispose() {
	}

	#endregion

	#region Abstract Implementation of IIncrementalHash

	/// <inheritdoc />
	public abstract void AppendData(ReadOnlySpan<Byte> data);

	/// <inheritdoc />
	public abstract IIncrementalHash Clone();

	/// <inheritdoc />
	public abstract Int32 GetCurrentHash(Span<Byte> destination);
	/// <inheritdoc />
	public abstract void Reset();

	#endregion
}