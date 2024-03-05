namespace Neco.Common.Extensions;

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public delegate void InspectStreamDelegate(Byte[] data, Int32 offset, Int32 length);

public static class StreamExtensions {
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
				inspectCallback.Invoke(buffer, 0, bytesRead);
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
				inspectCallback.Invoke(buffer, 0, bytesRead);
			}

			return totalBytesCopied;
		}
		finally {
			ArrayPool<Byte>.Shared.Return(buffer);
		}
	}
}