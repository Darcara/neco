namespace Neco.Common.Data.Archive;

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text.Unicode;
using Neco.Common.Extensions;

public record BasicCatalogOptions {
	public static readonly CatalogOptions DefaultBasic = new() {
		CompressionLookup = StaticFileCompressionLookup.Instance,
	};

	public IFileCompressionLookup? CompressionLookup { get; init; }
}

public record CatalogOptions : BasicCatalogOptions {
	public static readonly CatalogOptions Default = new() {
		Features = CatalogFeatures.None,
		CompressionLookup = StaticFileCompressionLookup.Instance,
	};

	internal const Byte OptionsIndicator = (Byte)'#';

	public CatalogFeatures Features { get; init; }

	internal static CatalogOptions? FromCatalog(FileStream fileStream, BasicCatalogOptions baseOptions) {
		Debug.Assert(fileStream.Position == 0);
		Int64 catalogDataStartPosition = 0;
		Byte[] buffer = ArrayPool<Byte>.Shared.Rent(2048);
		Char[] charBuffer = ArrayPool<Char>.Shared.Rent(2048);
		try {
			Int32 bytesRead = fileStream.ReadAtLeast(buffer, buffer.Length, false);

			if (bytesRead < 1 || buffer[0] != OptionsIndicator) return null;

			Int32 lineEndIndex = buffer.IndexOf(Catalog.EntrySeparator);
			if (lineEndIndex <= 0 || lineEndIndex >= bytesRead) return null;

			// Slice 1 to remove OptionsIndicator
			Span<Byte> span = buffer.AsSpan().Slice(1, lineEndIndex-1);
			Utf8.ToUtf16(span, charBuffer, out Int32 _, out Int32 charsWritten);
			if (charsWritten == 0) return null;
			Span<Char> charSpan = charBuffer.AsSpan(0, charsWritten);

			if (!Enum.TryParse(charSpan, out CatalogFeatures features))
				return null;

			return new CatalogOptions {
				CompressionLookup = baseOptions.CompressionLookup,
				Features = features,
			};
		}
		finally {
			ArrayPool<Byte>.Shared.Return(buffer);
			ArrayPool<Char>.Shared.Return(charBuffer);
			fileStream.Seek(catalogDataStartPosition, SeekOrigin.Begin);
		}
	}

	public static CatalogOptions DefaultFrom(BasicCatalogOptions? options) {
		if (options == null) return Default;
		return Default with {
			CompressionLookup = options.CompressionLookup,
		};
	}

	public void Validate() {
		if (Features.HasFlag(CatalogFeatures.CompressionPerEntryOptimal) && Features.HasFlag(CatalogFeatures.CompressionPerEntrySmallest))
			throw new ArgumentException("Both compression options cannot be specified at the same time.");
	}
}

[Flags]
public enum CatalogFeatures {
	/// No extra features are enabled, only offset and name are present in the .cat file
	None = 0,

	/// Generates a wyhash3 64Bit checksum of the original/uncompressed data
	ChecksumPerEntry = 0b1,

	/// Uses Brotly-4 compression.
	CompressionPerEntryOptimal = 0b10,

	/// Uses Brotli-11 compression. Warning very slow.
	CompressionPerEntrySmallest = 0b100,

	/// Saves the uncompressed file size. Skipped if the entry is not compressed.
	UncompressedFileSize = 0b1000,
}