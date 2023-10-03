namespace Neco.Common.Data;

using System;
using System.IO;

/// <summary>
/// Creates a stream that returns <see cref="Random"/> data at every position
/// </summary>
public sealed class RandomDataStream : Stream {
	/// <summary>
	/// Creates a stream that returns <see cref="Random"/> data at every position
	/// </summary>
	/// <param name="sizeToGenerate">The <see cref="Length"/> of the stream.</param>
	public RandomDataStream(Int64 sizeToGenerate) {
		Length = sizeToGenerate;
	}

	#region Overrides of Stream

	/// <inheritdoc />
	public override void Flush() {
	}

	/// <inheritdoc />
	public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) {
		Int32 bytesToProvide = (Int32)Math.Min(1024*64, Math.Min(count, Length -Position));
		Random.Shared.NextBytes(buffer.AsSpan(offset, bytesToProvide));
		Position += bytesToProvide;
		return bytesToProvide;
	}

	/// <inheritdoc />
	public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();

	/// <inheritdoc />
	public override void SetLength(Int64 value) => throw new NotSupportedException();

	/// <inheritdoc />
	public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();

	/// <inheritdoc />
	public override Boolean CanRead => true;

	/// <inheritdoc />
	public override Boolean CanSeek => false;

	/// <inheritdoc />
	public override Boolean CanWrite => false;

	/// <inheritdoc />
	public override Int64 Length { get; }

	/// <inheritdoc />
	public override Int64 Position { get; set; }

	#endregion
}