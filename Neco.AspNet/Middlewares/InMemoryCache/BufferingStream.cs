namespace Neco.AspNet.Middlewares.InMemoryCache;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public class BufferingStream : Stream {
	private readonly Int64 _maxBytesToBuffer;
	private readonly Int32 _segmentSize;
	private readonly Func<BufferingStream, DateTimeOffset> _onFirstWrite;
	private Int64 _length;
	private Boolean _shouldBufferStream = true;

	public readonly Stream OriginalStream;
	public DateTimeOffset? FirstWriteTime { get; private set; }
	public Boolean IsBufferingDisabled => !_shouldBufferStream;

	private List<Byte[]>? _dataSegments;
	private Byte[]? _currentBuffer;
	private Int32 _currentPosition;

	public BufferingStream(Stream originalStream, Int64 maxBytesToBuffer, Int32 segmentSize, Func<BufferingStream, DateTimeOffset> onFirstWrite) {
		OriginalStream = originalStream;
		_maxBytesToBuffer = maxBytesToBuffer;
		_segmentSize = segmentSize;
		_onFirstWrite = onFirstWrite;
	}

	public void DisableBuffering() {
		_shouldBufferStream = false;
	}

	public List<Byte[]> GetDataSegments() {
		if (_dataSegments == null) throw new InvalidOperationException();
		if (_currentBuffer != null && _currentPosition > 0) {
			Byte[] partialBuffer = new Byte[_currentPosition];
			Array.Copy(_currentBuffer, 0, partialBuffer, 0, _currentPosition);
			_dataSegments.Add(partialBuffer);
			_currentPosition = 0;
			_currentBuffer = null;
		}
		return _dataSegments;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void StartResponse() {
		if (FirstWriteTime != null)
			FirstWriteTime = _onFirstWrite(this);
	}

	private void WriteToBuffer(ReadOnlySpan<Byte> data) {
		if(data.Length == 0) return;
		_length += data.Length;
		_dataSegments ??= new List<Byte[]>();
		_currentBuffer ??= new Byte[_segmentSize];

		Int32 bytesWritten = 0;
		while (bytesWritten < data.Length) {
			if (_currentPosition >= _segmentSize) {
				_dataSegments.Add(_currentBuffer);
				_currentBuffer = new Byte[_segmentSize];
				_currentPosition = 0;
			}

			Int32 maxToCopy = Math.Min(_segmentSize-_currentPosition, data.Length-bytesWritten);
			data.Slice(bytesWritten, maxToCopy).CopyTo(_currentBuffer.AsSpan(_currentPosition));
			bytesWritten += maxToCopy;
			_currentPosition += maxToCopy;
		}
	}

	#region Overrides of Stream

	/// <inheritdoc />
	public override void Flush() {
		try {
			StartResponse();
			OriginalStream.Flush();
		}
		catch {
			DisableBuffering();
			throw;
		}
	}

	/// <inheritdoc />
	public override async Task FlushAsync(CancellationToken cancellationToken) {
		try {
			StartResponse();
			await OriginalStream.FlushAsync(cancellationToken);
		}
		catch {
			DisableBuffering();
			throw;
		}
	}

	/// <inheritdoc />
	public override void Write(Byte[] buffer, Int32 offset, Int32 count) {
		try {
			StartResponse();
			OriginalStream.Write(buffer, offset, count);
		}
		catch {
			DisableBuffering();
			throw;
		}


		if (_shouldBufferStream) {
			if (Length + count > _maxBytesToBuffer) {
				DisableBuffering();
			} else {
				WriteToBuffer(buffer.AsSpan(offset, count));
			}
		}
	}

	/// <inheritdoc />
	public override void WriteByte(Byte value) {
		try {
			StartResponse();
			OriginalStream.WriteByte(value);
		}
		catch {
			DisableBuffering();
			throw;
		}


		if (_shouldBufferStream) {
			if (Length + 1 > _maxBytesToBuffer) {
				DisableBuffering();
			} else {
				Byte[] buffer = ArrayPool<Byte>.Shared.Rent(1);
				buffer[0] = value;
				WriteToBuffer(buffer.AsSpan(0, 1));
			}
		}
	}

	/// <inheritdoc />
	public override async Task WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken) {
		try {
			StartResponse();
			await OriginalStream.WriteAsync(buffer, offset, count, cancellationToken);
		}
		catch {
			DisableBuffering();
			throw;
		}

		if (_shouldBufferStream) {
			if (Length + count > _maxBytesToBuffer) {
				DisableBuffering();
			} else {
				WriteToBuffer(buffer.AsSpan(offset, count));
			}
		}
	}

	/// <inheritdoc />
	public override async ValueTask WriteAsync(ReadOnlyMemory<Byte> buffer, CancellationToken cancellationToken = new()) {
		try {
			StartResponse();
			await OriginalStream.WriteAsync(buffer, cancellationToken);
		}
		catch {
			DisableBuffering();
			throw;
		}

		if (_shouldBufferStream) {
			if (Length + buffer.Length > _maxBytesToBuffer) {
				DisableBuffering();
			} else {
				WriteToBuffer(buffer.Span);
			}
		}
	}

	/// <inheritdoc />
	public override IAsyncResult BeginWrite(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback? callback, Object? state) {
		throw new InvalidOperationException();
	}

	/// <inheritdoc />
	public override void EndWrite(IAsyncResult asyncResult) {
		throw new InvalidOperationException();
	}

	/// <inheritdoc />
	public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => throw new InvalidOperationException();

	/// <inheritdoc />
	public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new InvalidOperationException();

	/// <inheritdoc />
	public override void SetLength(Int64 value) => throw new InvalidOperationException();

	/// <inheritdoc />
	public override Boolean CanRead => false;

	/// <inheritdoc />
	public override Boolean CanSeek => false;

	/// <inheritdoc />
	public override Boolean CanWrite => OriginalStream.CanWrite;

	/// <inheritdoc />
	public override Int64 Length => _length;

	/// <inheritdoc />
	public override Int64 Position {
		get => Length;
		set => throw new InvalidOperationException();
	}

	#endregion
}