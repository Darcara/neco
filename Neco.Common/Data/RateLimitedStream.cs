namespace Neco.Common.Data;

using System;
using System.IO;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Neco.Common.Extensions;

public class RateLimitedStream : Stream {
	private readonly Stream _wrappedStream;
	private readonly RateLimiter? _readRateLimiter;
	private readonly RateLimiter? _writeRateLimiter;
	private readonly Int32 _blockSize;
	private readonly Boolean _disposeStream;
	private readonly Boolean _disposeReadRateLimiter;
	private readonly Boolean _disposeWriteRateLimiter;

	/// <summary>
	/// Wraps the given stream and rate limits reading and writing
	/// </summary>
	/// <param name="wrappedStream"></param>
	/// <param name="readRateLimiter">RateLimiter for all read operations. If null no rate limiting is done</param>
	/// <param name="writeRateLimiter">RateLimiter for all write operations. If null no rate limiting is done</param>
	/// <param name="blockSize">The size in bytes that will be requested from the read/write rate limiters. Each rate limiter must be able to provide at least this amount in one call.</param>
	/// <param name="disposeStream">true to leave the <see cref="wrappedStream"/> open after disposing the this <see cref="RateLimitedStream"/> object; otherwise, false.</param>
	/// <param name="disposeReadRateLimiter">true to leave the <see cref="readRateLimiter"/> after disposing the this <see cref="RateLimitedStream"/> object; otherwise, false.</param>
	/// <param name="disposeWriteRateLimiter">true to leave the <see cref="writeRateLimiter"/> open after disposing the this <see cref="RateLimitedStream"/> object; otherwise, false.</param>
	public RateLimitedStream(Stream wrappedStream, RateLimiter? readRateLimiter, RateLimiter? writeRateLimiter = null, Int32 blockSize = MagicNumbers.DefaultStreamBufferSize, Boolean disposeStream = false, Boolean disposeReadRateLimiter = false, Boolean disposeWriteRateLimiter = false) {
		ArgumentNullException.ThrowIfNull(wrappedStream);
		if (blockSize <= 0) throw new ArgumentOutOfRangeException(nameof(blockSize));
		_wrappedStream = wrappedStream;
		_readRateLimiter = readRateLimiter;
		_writeRateLimiter = writeRateLimiter;
		_blockSize = blockSize;
		_disposeStream = disposeStream;
		_disposeReadRateLimiter = disposeReadRateLimiter;
		_disposeWriteRateLimiter = disposeWriteRateLimiter;
	}

	// Returns 0 if 0 is requested, or if the rate limiter will never issue tokens again
	private Int32 GetTokens(RateLimiter? rateLimiter, Int32 tokensWanted) {
		if (rateLimiter == null) return tokensWanted;
		if (tokensWanted == 0) return 0;
		Int32 tokensToAquire = Math.Min(_blockSize, tokensWanted);
		using (RateLimitLease lease = rateLimiter.AttemptAcquire(tokensToAquire)) {
			if (lease.IsAcquired) return tokensWanted;
			// TODO: use MetadataName.RetryAfter
		}

		using RateLimitLease asyncLease = rateLimiter.AcquireAsync(tokensWanted, CancellationToken.None).GetResultBlocking();
		return asyncLease.IsAcquired ? tokensWanted : 0;
	}

	private ValueTask<Int32> GetTokensAsync(RateLimiter? rateLimiter, Int32 tokensWanted, CancellationToken cancellationToken) {
		if (rateLimiter == null) return ValueTask.FromResult(tokensWanted);
		if (tokensWanted == 0) return ValueTask.FromResult(0);
		Int32 tokensToAquire = Math.Min(_blockSize, tokensWanted);
		using (RateLimitLease lease = rateLimiter.AttemptAcquire(tokensToAquire)) {
			if (lease.IsAcquired) return ValueTask.FromResult(tokensWanted);
			// TODO: use MetadataName.RetryAfter if available
		}

		// Probability is admittedly low, but we try to stay sync as long as possible
		ValueTask<RateLimitLease> vt = rateLimiter.AcquireAsync(tokensWanted, cancellationToken);
		if (vt.IsCompletedSuccessfully) {
			return vt.GetAwaiter().GetResult().IsAcquired ? ValueTask.FromResult(tokensWanted) : ValueTask.FromResult(0);
		}

		return AwaitAsync(tokensToAquire, vt);
	}

	private static async ValueTask<Int32> AwaitAsync(Int32 bytes, ValueTask<RateLimitLease> vt) {
		using RateLimitLease asyncLease = await vt.ConfigureAwait(false);
		return asyncLease.IsAcquired ? bytes : 0;
	}

	#region Overrides of Stream

	/// <inheritdoc />
	public override Task<Int32> ReadAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken) {
		ValueTask<Int32> bytesToReadTask = GetTokensAsync(_readRateLimiter, count, cancellationToken);
		if (bytesToReadTask.IsCompletedSuccessfully) {
			Int32 bytesToRead = bytesToReadTask.GetAwaiter().GetResult();
			if (bytesToRead == 0) return Task.FromResult(0);
			return _wrappedStream.ReadAsync(buffer, offset, bytesToRead, cancellationToken);
		}

		return ReadAsyncCore(bytesToReadTask, buffer, offset, cancellationToken);
	}

	private async Task<Int32> ReadAsyncCore(ValueTask<Int32> bytesToReadTask, Byte[] buffer, Int32 offset, CancellationToken cancellationToken) {
		Int32 bytesToRead = await bytesToReadTask;
		if (bytesToRead == 0) return 0;
		return await _wrappedStream.ReadAsync(buffer, offset, bytesToRead, cancellationToken);
	}

	/// <inheritdoc />
	public override ValueTask<Int32> ReadAsync(Memory<Byte> buffer, CancellationToken cancellationToken = default) {
		ValueTask<Int32> bytesToReadTask = GetTokensAsync(_readRateLimiter, buffer.Length, cancellationToken);
		if (bytesToReadTask.IsCompletedSuccessfully) {
			Int32 bytesToRead = bytesToReadTask.GetAwaiter().GetResult();
			if (bytesToRead == 0) return ValueTask.FromResult(0);
			return _wrappedStream.ReadAsync(buffer.Slice(0, bytesToRead), cancellationToken);
		}

		return ReadAsyncCore(bytesToReadTask, buffer, cancellationToken);
	}

	private async ValueTask<Int32> ReadAsyncCore(ValueTask<Int32> bytesToReadTask, Memory<Byte> buffer, CancellationToken cancellationToken) {
		Int32 bytesToRead = await bytesToReadTask;
		if (bytesToRead == 0) return 0;
		return await _wrappedStream.ReadAsync(buffer.Slice(0, bytesToRead), cancellationToken);
	}

	/// <exception cref="NotSupportedException">This call is not supported</exception>
	public override IAsyncResult BeginRead(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback? callback, Object? state) => throw new NotSupportedException();

	/// <exception cref="NotSupportedException">This call is not supported</exception>
	// Necessary, because we delegated BeginRead
	public override Int32 EndRead(IAsyncResult asyncResult) => throw new NotSupportedException();

	/// <inheritdoc />
	public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) {
		Int32 bytesToRead = GetTokens(_readRateLimiter, count);
		if (bytesToRead == 0) return 0;
		return _wrappedStream.Read(buffer, offset, bytesToRead);
	}

	/// <inheritdoc />
	public override Int32 Read(Span<Byte> buffer) {
		Int32 bytesToRead = GetTokens(_readRateLimiter, buffer.Length);
		if (bytesToRead == 0) return 0;
		return _wrappedStream.Read(buffer.Slice(0, bytesToRead));
	}

	/// <inheritdoc />
	public override Int32 ReadByte() {
		Int32 bytesToRead = GetTokens(_readRateLimiter, 1);
		if (bytesToRead == 0) return -1;
		return _wrappedStream.ReadByte();
	}

	/// <inheritdoc />
	public override async Task WriteAsync(Byte[] buffer, Int32 offset, Int32 count, CancellationToken cancellationToken) {
		Int32 totalBytesWritten = 0;
		do {
			Int32 bytesToWrite = await GetTokensAsync(_writeRateLimiter, count - totalBytesWritten, cancellationToken);
			if (bytesToWrite == 0) return;
			await _wrappedStream.WriteAsync(buffer, offset + totalBytesWritten, bytesToWrite, cancellationToken);
			totalBytesWritten += bytesToWrite;
		} while (totalBytesWritten < count);
	}

	/// <inheritdoc />
	public override async ValueTask WriteAsync(ReadOnlyMemory<Byte> buffer, CancellationToken cancellationToken = default) {
		Int32 totalBytesWritten = 0;
		do {
			Int32 bytesToWrite = await GetTokensAsync(_writeRateLimiter, buffer.Length - totalBytesWritten, cancellationToken);
			if (bytesToWrite == 0) return;
			await _wrappedStream.WriteAsync(buffer.Slice(totalBytesWritten, bytesToWrite), cancellationToken);
			totalBytesWritten += bytesToWrite;
		} while (totalBytesWritten < buffer.Length);
	}

	/// <exception cref="NotSupportedException">This call is not supported</exception>
	public override IAsyncResult BeginWrite(Byte[] buffer, Int32 offset, Int32 count, AsyncCallback? callback, Object? state) => throw new NotSupportedException();

	/// <exception cref="NotSupportedException">This call is not supported</exception>
	// Necessary, because we delegated BeginWrite
	public override void EndWrite(IAsyncResult asyncResult) => throw new NotSupportedException();

	/// <inheritdoc />
	public override void Write(ReadOnlySpan<Byte> buffer) {
		Int32 totalBytesWritten = 0;
		do {
			Int32 bytesToWrite = GetTokens(_writeRateLimiter, buffer.Length - totalBytesWritten);
			if (bytesToWrite == 0) return;
			_wrappedStream.Write(buffer.Slice(totalBytesWritten, bytesToWrite));
			totalBytesWritten += bytesToWrite;
		} while (totalBytesWritten < buffer.Length);
	}

	/// <inheritdoc />
	public override void Write(Byte[] buffer, Int32 offset, Int32 count) {
		Int32 totalBytesWritten = 0;
		do {
			Int32 bytesToWrite = GetTokens(_writeRateLimiter, count - totalBytesWritten);
			if (bytesToWrite == 0) return;
			_wrappedStream.Write(buffer, offset + totalBytesWritten, bytesToWrite);
			totalBytesWritten += bytesToWrite;
		} while (totalBytesWritten < count);
	}

	/// <inheritdoc />
	public override void WriteByte(Byte value) {
		Int32 bytesToWrite = GetTokens(_writeRateLimiter, 1);
		if (bytesToWrite == 0) return;
		_wrappedStream.WriteByte(value);
	}

	/// <inheritdoc />
	public override Boolean CanRead => _wrappedStream.CanRead;

	/// <inheritdoc />
	public override Boolean CanSeek => _wrappedStream.CanSeek;

	/// <inheritdoc />
	public override Boolean CanWrite => _wrappedStream.CanWrite;

	/// <inheritdoc />
	public override Int64 Length => _wrappedStream.Length;

	/// <inheritdoc />
	public override Int64 Position {
		get => _wrappedStream.Position;
		set => _wrappedStream.Position = value;
	}

	/// <inheritdoc />
	public override Int64 Seek(Int64 offset, SeekOrigin origin) => _wrappedStream.Seek(offset, origin);

	/// <inheritdoc />
	public override void SetLength(Int64 value) => _wrappedStream.SetLength(value);

	/// <inheritdoc />
	public override void Flush() => _wrappedStream.Flush();

	/// <inheritdoc />
	protected override void Dispose(Boolean disposing) {
		if (disposing) {
			if (_disposeStream)
				_wrappedStream.Dispose();

			if (_disposeReadRateLimiter)
				_readRateLimiter?.Dispose();

			if (_disposeWriteRateLimiter) {
				// prevend double dispose
				if (!_disposeReadRateLimiter || !ReferenceEquals(_readRateLimiter, _writeRateLimiter))
					_writeRateLimiter?.Dispose();
			}
		}

		base.Dispose(disposing);
	}

	#endregion
}