namespace Neco.Common.Extensions;

using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

public delegate void InspectStreamDelegate(ReadOnlySpan<Byte> data);

public static class StreamExtensions {
	public static Int64 CopyTo(this PipeReader source, Stream destination, Int32 bufferSize = MagicNumbers.MaxNonLohBufferSize) {
		ArgumentNullException.ThrowIfNull(destination);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

		Int64 totalBytesCopied = 0;
		SpinWait spinner = new();

		while (true) {
			ReadResult readResult;
			while (!source.TryRead(out readResult)) {
				spinner.SpinOnce();
			}

			spinner.Reset();

			if (readResult.IsCanceled || readResult.IsCompleted || readResult.Buffer.Length == 0) break;

			foreach (ReadOnlyMemory<byte> readOnlyMemory in readResult.Buffer) {
				destination.Write(readOnlyMemory.Span);
				totalBytesCopied += readOnlyMemory.Span.Length;
			}

			source.AdvanceTo(readResult.Buffer.End);
		}

		return totalBytesCopied;
	}

	public static Int64 CopyTo(this Stream source, IBufferWriter<Byte> destination, Int32 bufferSize = MagicNumbers.MaxNonLohBufferSize) {
		ArgumentNullException.ThrowIfNull(destination);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

		if (!source.CanRead) throw new NotSupportedException("Destination stream not readable");

		Byte[] buffer = ArrayPool<Byte>.Shared.Rent(bufferSize);
		try {
			Int64 totalBytesCopied = 0;
			Int32 bytesRead;
			while ((bytesRead = source.Read(buffer, 0, buffer.Length)) != 0) {
				Span<Byte> destinationSpan = destination.GetSpan(bytesRead);
				buffer.AsSpan(0, bytesRead).CopyTo(destinationSpan);
				destination.Advance(bytesRead);
				totalBytesCopied += bytesRead;
			}

			return totalBytesCopied;
		}
		finally {
			ArrayPool<Byte>.Shared.Return(buffer);
		}
	}

	public static void CopyPartiallyTo(this Stream source, Stream destination, Int64 length, Int32 bufferSize = MagicNumbers.MaxNonLohBufferSize) {
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(destination);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
		
		if (!destination.CanWrite) throw new NotSupportedException("Destination stream not writable");
		if (!source.CanRead) throw new NotSupportedException("Destination stream not readable");
		
		Int64 remaining = length;
		Byte[] buffer = ArrayPool<Byte>.Shared.Rent((Int32)Math.Min(length, bufferSize));
		try {
			while (remaining > 0) {
				Int32 toRead = (Int32)Math.Min(remaining, bufferSize);
				Int32 actuallyRead = source.Read(buffer, 0, toRead);
				destination.Write(buffer, 0, actuallyRead);
				remaining -= actuallyRead;
			}
		}
		finally {
			ArrayPool<Byte>.Shared.Return(buffer);
		}
	}
	
	/// <inheritdoc cref="Stream.CopyTo(Stream, Int32)"/>
	/// <returns>Number of bytes copied</returns>
	public static Int64 CopyToAndInspect(this Stream source, Stream destination, InspectStreamDelegate inspectCallback, Int32 bufferSize = MagicNumbers.MaxNonLohBufferSize) {
		ArgumentNullException.ThrowIfNull(destination);
		ArgumentNullException.ThrowIfNull(inspectCallback);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

		if (!destination.CanWrite) throw new NotSupportedException("Destination stream not writable");
		if (!source.CanRead) throw new NotSupportedException("Destination stream not readable");

		Byte[] buffer = ArrayPool<Byte>.Shared.Rent(bufferSize);
		try {
			Int64 totalBytesCopied = 0;
			Int32 bytesRead;
			while ((bytesRead = source.Read(buffer, 0, buffer.Length)) != 0) {
				destination.Write(buffer, 0, bytesRead);
				totalBytesCopied += bytesRead;
				inspectCallback.Invoke(new ReadOnlySpan<Byte>(buffer, 0, bytesRead));
			}

			return totalBytesCopied;
		}
		finally {
			ArrayPool<Byte>.Shared.Return(buffer);
		}
	}

	/// <inheritdoc cref="Stream.CopyToAsync(Stream, Int32, CancellationToken)"/>
	/// <returns>Number of bytes copied</returns>
	public static async Task<Int64> CopyToAndInspectAsync(this Stream source, Stream destination, InspectStreamDelegate inspectCallback, Int32 bufferSize = MagicNumbers.MaxNonLohBufferSize, CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(destination);
		ArgumentNullException.ThrowIfNull(inspectCallback);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

		if (!destination.CanWrite) throw new NotSupportedException("Destination stream not writable");
		if (!source.CanRead) throw new NotSupportedException("Destination stream not readable");

		Byte[] buffer = ArrayPool<Byte>.Shared.Rent(bufferSize);
		Memory<Byte> bufferMem = buffer.AsMemory();
		try {
			Int64 totalBytesCopied = 0;
			Int32 bytesRead;
			while ((bytesRead = await source.ReadAsync(bufferMem, cancellationToken)) != 0) {
				await destination.WriteAsync(bufferMem.Slice(0, bytesRead), cancellationToken).ConfigureAwait(false);
				totalBytesCopied += bytesRead;
				inspectCallback.Invoke(new ReadOnlySpan<Byte>(buffer, 0, bytesRead));
			}

			return totalBytesCopied;
		}
		finally {
			ArrayPool<Byte>.Shared.Return(buffer);
		}
	}
}