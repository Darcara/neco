namespace Neco.Common.Data;

using System;
using System.IO;

/// <summary>
/// Creates a stream that returns '0' at every position
/// </summary>
public sealed class ZeroDataStream : Stream {
	/// <summary>
	/// Creates a stream that returns '0' at every position
	/// </summary>
	/// <param name="sizeToGenerate">The <see cref="Length"/> of the stream.</param>
	public ZeroDataStream(Int64 sizeToGenerate) {
		Length = sizeToGenerate;
	}

	#region Overrides of Stream

	/// <inheritdoc />
	public override void Flush() {
	}

	/// <inheritdoc />
	public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) {
		Int32 bytesToProvide = (Int32)Math.Min(1024 * 64, Math.Min(count, Length - Position));
		buffer.AsSpan(offset, bytesToProvide).Clear();
		Position += bytesToProvide;
		return bytesToProvide;
	}

	/// <inheritdoc />
	public override Int64 Seek(Int64 offset, SeekOrigin origin) {
		switch (origin) {
			case SeekOrigin.Begin: {
				if (offset < 0 || offset > Length) throw new IOException($"{nameof(offset)} with {nameof(SeekOrigin)}.{nameof(SeekOrigin.Begin)} is out of range");
				Position = offset;
			}
				break;
			case SeekOrigin.Current: {
				Int64 newPosition = Position + offset;
				if (newPosition < 0 || newPosition > Length) throw new IOException($"{nameof(offset)} with {nameof(SeekOrigin)}.{nameof(SeekOrigin.Current)} is out of range");
				Position = newPosition;
			}
				break;
			case SeekOrigin.End: {
				Int64 newPosition = Length + offset;
				if (newPosition < 0 || newPosition > Length) throw new IOException($"{nameof(offset)} with {nameof(SeekOrigin)}.{nameof(SeekOrigin.End)} is out of range");
				Position = newPosition;
			}
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
		}

		return Position;
	}

	/// <inheritdoc />
	public override void SetLength(Int64 value) => throw new NotSupportedException();

	/// <inheritdoc />
	public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();

	/// <inheritdoc />
	public override Boolean CanRead => true;

	/// <inheritdoc />
	public override Boolean CanSeek => true;

	/// <inheritdoc />
	public override Boolean CanWrite => false;

	/// <inheritdoc />
	public override Int64 Length { get; }

	/// <inheritdoc />
	public override Int64 Position { get; set; }

	#endregion
}