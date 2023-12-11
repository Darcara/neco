namespace Neco.Common.Data;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Wraps an underlying stream and provides read acces to a sub-range of it.
/// </summary>
public class BoundedReadOnlyStream : Stream {
	private readonly Stream _underlying;
	private readonly Int64 _startingOffset;
	private readonly Boolean _disposeUnderlyingOnClose;
	private Int64 _position;

	/// <summary>
	/// Creates a new bounded sub-stream
	/// </summary>
	/// <param name="underlying">The original stream holding the data</param>
	/// <param name="startingOffset">The offset into the underlying stream, from which this stream will read</param>
	/// <param name="length">The maximum amount of bytes</param>
	/// <param name="disposeUnderlyingOnClose">TRUE to close/dispose the underlying stream if this stream is closed/disposed; FALSE to leave it open</param>
	public BoundedReadOnlyStream(Stream underlying, Int64 startingOffset, Int64 length, Boolean disposeUnderlyingOnClose) {
		_underlying = underlying;
		_startingOffset = startingOffset;
		_disposeUnderlyingOnClose = disposeUnderlyingOnClose;
		Length = length;
		_underlying.Position = startingOffset;
	}

	/// <inheritdoc />
	public override void Flush() {
	}

	private void SeekUnderlyingToCorrectPosition() {
		if (_underlying.Position != Position + _startingOffset)
			_underlying.Seek(Position + _startingOffset, SeekOrigin.Begin);
	}

	/// <inheritdoc />
	public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) {
		if (Position >= Length) return 0;
		SeekUnderlyingToCorrectPosition();
		Int32 maxBytesToRead = (Int32)Math.Min(Length - Position, count);
		Int32 numBytesRead = _underlying.Read(buffer, offset, maxBytesToRead);
		Position += numBytesRead;
		return numBytesRead;
	}

	/// <inheritdoc />
	public override Int32 Read(Span<Byte> buffer) {
		if (Position >= Length) return 0;
		SeekUnderlyingToCorrectPosition();
		Int32 maxBytesToRead = (Int32)Math.Min(Length - Position, buffer.Length);
		if (maxBytesToRead < buffer.Length) buffer = buffer.Slice(0, maxBytesToRead);
		Int32 numBytesRead = _underlying.Read(buffer);
		Position += numBytesRead;
		return numBytesRead;
	}

	/// <inheritdoc />
	public override async Task<Int32> ReadAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken) {
		if (Position >= Length) return 0;
		SeekUnderlyingToCorrectPosition();
		Int32 maxBytesToRead = (Int32)Math.Min(Length - Position, count);
		Int32 numBytesRead = await _underlying.ReadAsync(buffer, offset, maxBytesToRead, cancellationToken);
		Position += numBytesRead;
		return numBytesRead;
	}

	/// <inheritdoc />
	public override async ValueTask<Int32> ReadAsync(Memory<Byte> buffer, CancellationToken cancellationToken = default) {
		if (Position >= Length) return 0;
		SeekUnderlyingToCorrectPosition();
		Int32 maxBytesToRead = (Int32)Math.Min(Length - Position, buffer.Length);
		if (maxBytesToRead < buffer.Length) buffer = buffer.Slice(0, maxBytesToRead);
		Int32 numBytesRead = await _underlying.ReadAsync(buffer, cancellationToken);
		Position += numBytesRead;
		return numBytesRead;
	}

	/// <inheritdoc />
	public override Int32 ReadByte() {
		if (Position >= Length) return -1;
		SeekUnderlyingToCorrectPosition();
		Int32 theByte = _underlying.ReadByte();
		Position += 1;
		return theByte;
	}

	/// <inheritdoc />
	public override Int64 Seek(Int64 offset, SeekOrigin origin) {
		if (!CanSeek) throw new InvalidOperationException("Seeking not supported by this stream.");

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
	public override void SetLength(Int64 value) {
		throw new NotSupportedException("Read only stream.");
	}

	/// <inheritdoc />
	public override void Write(Byte[] buffer, Int32 offset, Int32 count) {
		throw new NotSupportedException("Read only stream.");
	}

	/// <inheritdoc />
	public override Boolean CanRead => _underlying.CanRead;

	/// <inheritdoc />
	public override Boolean CanSeek => _underlying.CanSeek;

	/// <inheritdoc />
	public override Boolean CanWrite => false;

	/// <inheritdoc />
	public override Int64 Length { get; }

	/// <inheritdoc />
	public override Int64 Position {
		get => _position;
		set {
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Length);
			_position = value;
		}
	}

	/// <inheritdoc />
	public override void Close() {
		base.Close();
		if (_disposeUnderlyingOnClose)
			_underlying.Close();
	}
}