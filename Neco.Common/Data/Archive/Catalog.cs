namespace Neco.Common.Data.Archive;

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using Neco.Common.Data.Hash;
using Neco.Common.Extensions;

/// <summary>
/// A very simple 2-file archive. Append-only!
/// </summary>
/// <para>A Catalog consists of two files. The .cat-File contains the metadata (offset, length, name) of the entries, while the .bin file contains all data simple appended after each other.</para>
public sealed class Catalog : IDisposable, IAsyncDisposable {
	internal const Byte EntrySeparator = 0x0A; //0x0A is line feed or \n
	internal const Byte ColumnSeparator = 0x20; //0x20 is space
	private readonly String _catalogFile;
	private readonly String _dataFile;
	private readonly FileStream _catalogFileStream;
	private readonly StandardFormat _writeOffsetFormat = StandardFormat.Parse("X16");
	private readonly FileStream _dataFileStream;
	private readonly CatalogOptions _options;
	private readonly List<FileEntry> _entries = new();
	public IReadOnlyList<FileEntry> Entries => _entries;
	private Boolean _isDisposed;

	private Catalog(String catalogFile, String dataFile, FileStream catalogFileStream, FileStream dataFileStream, CatalogOptions options) {
		_catalogFile = catalogFile;
		_dataFile = dataFile;
		_catalogFileStream = catalogFileStream;
		_dataFileStream = dataFileStream;
		_options = options;
	}

	/// <summary>
	/// Creates a new Catalog
	/// </summary>
	/// <exception cref="ArchiveException">If an archive already exists</exception>
	public static Catalog CreateNew(String baseName, CatalogOptions? options = null) {
		options?.Validate();
		String catalogFile = Path.ChangeExtension(baseName, "cat");
		String dataFile = Path.ChangeExtension(baseName, "bin");
		if (File.Exists(catalogFile)) throw new ArchiveException($"Catalog file already present: {catalogFile}");
		if (File.Exists(dataFile)) throw new ArchiveException($"Data file already present: {dataFile}");

		FileStream catalogFileStream = new(catalogFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, MagicNumbers.DefaultStreamBufferSize, false);
		FileStream dataFileStream = new(dataFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, MagicNumbers.DefaultStreamBufferSize, false);

		options ??= CatalogOptions.Default;
		catalogFileStream.WriteByte(CatalogOptions.OptionsIndicator);
		catalogFileStream.Write(Encoding.ASCII.GetBytes(options.Features.ToString()));
		catalogFileStream.WriteByte(EntrySeparator);
		catalogFileStream.Flush();
		Catalog cat = new(catalogFile, dataFile, catalogFileStream, dataFileStream, options);
		return cat;
	}

	/// <summary>
	/// Opens an existing Catalog
	/// </summary>
	/// <exception cref="ArchiveException">If not catalog exists with the given name</exception>
	public static Catalog OpenExisting(String baseName, BasicCatalogOptions? options = null) {
		String catalogFile = Path.ChangeExtension(baseName, "cat");
		String dataFile = Path.ChangeExtension(baseName, "bin");

		FileInfo catalogInfo = new(catalogFile);
		if (!catalogInfo.Exists)
			throw new ArchiveException($"Missing catalog file: {catalogFile}");

		FileInfo dataInfo = new(dataFile);
		if (!dataInfo.Exists) throw new ArchiveException($"Missing catalog file: {dataFile}");

		FileStream catalogFileStream = new(catalogFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None, MagicNumbers.DefaultStreamBufferSize, false);
		FileStream dataFileStream = new(dataFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None, MagicNumbers.DefaultStreamBufferSize, false);

		Catalog cat = new(catalogFile, dataFile, catalogFileStream, dataFileStream, CatalogOptions.FromCatalog(catalogFileStream, options ?? BasicCatalogOptions.DefaultBasic) ?? CatalogOptions.DefaultFrom(options));
		try {
			cat.ReadEntries(cat._entries, dataInfo.Length);
		}
		catch (Exception) {
			cat.Dispose();
			throw;
		}

		return cat;
	}

	/// <summary>
	/// Checks if a catalog exist
	/// </summary>
	public static Boolean Exists(String baseName) {
		String catalogFile = Path.ChangeExtension(baseName, "cat");
		String dataFile = Path.ChangeExtension(baseName, "bin");

		return File.Exists(catalogFile) || File.Exists(dataFile);
	}
    
	private static String DefaultRealativeEntryNameGenerator(DirectoryInfo baseDirectory, FileInfo fileToAppend) {
		return Path.GetRelativePath(baseDirectory.FullName, fileToAppend.FullName);
	}

	public void AppendFolder(String folder, String searchPattern, EnumerationOptions enumerationOptions, Func<FileInfo, Boolean>? includeFilePredicate = null, Func<DirectoryInfo, FileInfo, String>? entryNameGenerator = null) {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		DirectoryInfo baseDirectory = new(folder);
		IEnumerable<FileInfo> filesToAppend = baseDirectory.EnumerateFiles(searchPattern, enumerationOptions);
		if (includeFilePredicate != null) filesToAppend = filesToAppend.Where(includeFilePredicate);
		AppendEntries(filesToAppend, baseDirectory, entryNameGenerator ?? DefaultRealativeEntryNameGenerator);
	}

	public void AppendFolder(String folder, String searchPattern, SearchOption searchOption, Func<FileInfo, Boolean>? includeFilePredicate = null, Func<DirectoryInfo, FileInfo, String>? entryNameGenerator = null) {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		DirectoryInfo baseDirectory = new(folder);
		IEnumerable<FileInfo> filesToAppend = baseDirectory.EnumerateFiles(searchPattern, searchOption);
		if (includeFilePredicate != null) filesToAppend = filesToAppend.Where(includeFilePredicate);
		AppendEntries(filesToAppend, baseDirectory, entryNameGenerator ?? DefaultRealativeEntryNameGenerator);
	}

	private void AppendEntries(IEnumerable<FileInfo> files, DirectoryInfo baseDirectory, Func<DirectoryInfo, FileInfo, String> entryNameGenerator) {
		foreach (FileInfo fileInfo in files) {
			if (!fileInfo.Exists) continue;
			String entryName = entryNameGenerator(baseDirectory, fileInfo);
			using FileStream inputStream = fileInfo.OpenRead();
			AppendEntry(inputStream, entryName);
		}
	}

	public void AppendEntry(String filename) => AppendEntry(new FileInfo(filename));

	public void AppendEntry(FileInfo file) {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if (!file.Exists) throw new FileNotFoundException("Unable to append non-existing file", file.FullName);
		using FileStream inputStream = file.OpenRead();
		AppendEntry(inputStream, file.Name);
	}

	public void AppendEntry(Stream inputStream, String entryName) {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		Int64 dataStreamOffset = _dataFileStream.Seek(0, SeekOrigin.End);

		static void Nop(Byte[] data, Int32 offset, Int32 length) {
		}

		HashAlgorithm? hasher = null;
		InspectStreamDelegate inspector = Nop;
		if (_options.Features.HasFlag(CatalogFeatures.ChecksumPerEntry)) {
			hasher = new WyHashFinal3();
			hasher.Initialize();
			Debug.Assert(hasher.HashSize == 64);

			void Hash(Byte[] data, Int32 offset, Int32 length) {
				hasher.TransformBlock(data, offset, length, null, 0);
			}

			inspector = Hash;
		}

		Boolean compressed = false;
		Int64 uncompressedBytesCopied;
		Boolean shouldCompress = _options.CompressionLookup == null || _options.CompressionLookup.DoesFileCompress(Path.GetExtension(entryName)) == FileCompression.Compressible;
		if (_options.Features.HasFlag(CatalogFeatures.CompressionPerEntryOptimal) && shouldCompress) {
			compressed = true;
			using BrotliStream stream = new(_dataFileStream, CompressionLevel.Optimal, true);
			uncompressedBytesCopied = inputStream.CopyToAndInspect(stream, inspector);
			stream.Flush();
		} else if (_options.Features.HasFlag(CatalogFeatures.CompressionPerEntrySmallest) && shouldCompress) {
			compressed = true;
			using BrotliStream stream = new(_dataFileStream, CompressionLevel.SmallestSize, true);
			uncompressedBytesCopied = inputStream.CopyToAndInspect(stream, inspector);
			stream.Flush();
		} else {
			uncompressedBytesCopied = inputStream.CopyToAndInspect(_dataFileStream, inspector);
		}

		_dataFileStream.Flush();

		Int64 checksum = 0;
		if (_options.Features.HasFlag(CatalogFeatures.ChecksumPerEntry)) {
			hasher!.TransformFinalBlock(Array.Empty<Byte>(), 0, 0);
			Byte[] hash = hasher.Hash!;
			Debug.Assert(hash.Length == 8);
			checksum = BitConverter.ToInt64(hash);
			hasher.Dispose();
		}

		// delete uncompressed so it is not saved and no longer available
		if (compressed && !_options.Features.HasFlag(CatalogFeatures.UncompressedFileSize)) {
			uncompressedBytesCopied = 0;
		}

		FileEntry fileEntry = new(entryName, dataStreamOffset, _dataFileStream.Position - dataStreamOffset, compressed, uncompressedBytesCopied, checksum);

		Write(fileEntry);

		_entries.Add(fileEntry);
	}

	public Byte[] GetDataAsArray(Int32 index) => GetDataAsArray(_entries[index]);

	public Byte[] GetDataAsArray(FileEntry entry) {
		ArgumentOutOfRangeException.ThrowIfGreaterThan(entry.Length, Int32.MaxValue);
		using Stream dataStream = GetDataAsStream(entry);
		if (!entry.IsCompressed || entry.UncompressedLength != 0) {
			Byte[] buffer = new Byte[Math.Max(entry.Length, entry.UncompressedLength)];
			dataStream.ReadExactly(buffer);
			return buffer;
		}

		using MemoryStream memStream = new();
		dataStream.CopyTo(memStream);
		return memStream.ToArray();
	}

	public Stream GetDataAsStream(Int32 index) => GetDataAsStream(_entries[index]);

	public Stream GetDataAsStream(FileEntry entry) {
		BoundedReadOnlyStream rawDataStream = new(_dataFileStream, entry.Offset, entry.Length, false);
		if (!entry.IsCompressed)
			return rawDataStream;
		return new BrotliStream(rawDataStream, CompressionMode.Decompress, false);
	}

	private void Write(FileEntry fileEntry) {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		// We assume fileEntry has been constructed correctly for the feature set
		// This means we can write the non-default fields without further feature checks

		// 16 bytes offset + 1 byte space + 1 byte nameid 'N' + <name> + 1 byte newline = 19
		// Features: 1 bytes compressed + 16 bytes uncompressed length + 16 bytes checksum + 3 spaces= 36
		Byte[] buffer = ArrayPool<Byte>.Shared.Rent(19 + 36 + fileEntry.Name.Length * 4);

		// Write start of line: Offset into data file as hex
		Utf8Formatter.TryFormat(fileEntry.Offset, buffer, out Int32 offsetBytesWritten, _writeOffsetFormat);
		buffer[offsetBytesWritten++] = ColumnSeparator;

		if (fileEntry.IsCompressed) {
			buffer[offsetBytesWritten++] = (Byte)'C';
			buffer[offsetBytesWritten++] = ColumnSeparator;
		}

		if (fileEntry.IsCompressed && fileEntry.UncompressedLength != 0) {
			buffer[offsetBytesWritten++] = (Byte)'U';
			Utf8Formatter.TryFormat(fileEntry.UncompressedLength, buffer.AsSpan(offsetBytesWritten), out Int32 bytesWritten, _writeOffsetFormat);
			offsetBytesWritten += bytesWritten;
			buffer[offsetBytesWritten++] = ColumnSeparator;
		}

		if (fileEntry.Checksum != 0) {
			buffer[offsetBytesWritten++] = (Byte)'H';
			Utf8Formatter.TryFormat(fileEntry.Checksum, buffer.AsSpan(offsetBytesWritten), out Int32 bytesWritten, _writeOffsetFormat);
			offsetBytesWritten += bytesWritten;
			buffer[offsetBytesWritten++] = ColumnSeparator;
		}

		// Write last part of line: Name
		buffer[offsetBytesWritten++] = (Byte)'N';
		Utf8.FromUtf16(fileEntry.Name, buffer.AsSpan(offsetBytesWritten), out _, out Int32 nameBytesWritten);
		buffer[offsetBytesWritten + nameBytesWritten] = EntrySeparator;

		_catalogFileStream.Seek(0, SeekOrigin.End);
		_catalogFileStream.Write(buffer, 0, offsetBytesWritten + nameBytesWritten + 1);
		_catalogFileStream.Flush();
		ArrayPool<Byte>.Shared.Return(buffer);
	}

	private void ReadEntries(List<FileEntry> entries, Int64 dataFileLength) {
		Debug.Assert(entries.Count == 0);

		Int32 readByteOffset = 0;
		Int32 writeByteOffsetOffset = 0;
		// Int64 offset = 0;
		Int64 lineNumber = 0;
		// String? lastName = null;
		FileEntry lastEntry = default;
		Byte[] data = new Byte[MagicNumbers.MaxNonLohBufferSize];
		Span<Byte> dataSpan = data.AsSpan();
		Span<Char> stringBuffer = new(new Char[MagicNumbers.MaxNonLohBufferSize / sizeof(Char)]);

		while (true) {
			Int32 numberOfBytesRead = _catalogFileStream.Read(data, writeByteOffsetOffset, data.Length - writeByteOffsetOffset);
			writeByteOffsetOffset += numberOfBytesRead;
			if (numberOfBytesRead == 0)
				break;

			Span<Byte> availableDataSpan = dataSpan.Slice(readByteOffset, writeByteOffsetOffset);
			while (true) {
				Int32 newLineIndex = availableDataSpan.IndexOf(EntrySeparator);
				if (newLineIndex == -1) {
					availableDataSpan.CopyTo(dataSpan);
					writeByteOffsetOffset = availableDataSpan.Length;
					readByteOffset = 0;
					break;
				}

				++lineNumber;
				Debug.Assert(newLineIndex != 0);
				readByteOffset += newLineIndex + 1;
				Span<Byte> line = availableDataSpan.Slice(0, newLineIndex);
				availableDataSpan = availableDataSpan.Slice(newLineIndex + 1);

				FileEntry lineEntry = ParseLine(line, stringBuffer, lineNumber);
				if (lastEntry != default) {
					Int64 length = lineEntry.Offset - lastEntry.Offset;
					ArchiveException.ThrowIfNegative(length);
					entries.Add(new FileEntry(lastEntry, length));
				}

				lastEntry = lineEntry;
			}
		}

		if (lastEntry != default) {
			Int64 length = dataFileLength - lastEntry.Offset;
			ArchiveException.ThrowIfNegative(length);
			entries.Add(new FileEntry(lastEntry, length));
		}
	}

	private static FileEntry ParseLine(ReadOnlySpan<Byte> line, Span<Char> stringBuffer, Int64 lineNumber) {
		if (line[0] == CatalogOptions.OptionsIndicator) return default(FileEntry);

		if (!Utf8Parser.TryParse(line.Slice(0, 16), out Int64 binaryPosition, out Int32 bytesConsumed, 'X') || bytesConsumed != 16)
			ArchiveException.Throw($"Failed to parse binary position in line {lineNumber}");

		if(line.Length < 17 || line[16] != ' ')
			ArchiveException.Throw($"Failed to parse short line {lineNumber}"); 
		line = line.Slice(17);

		Boolean isCompressed = false;
		Int64 checksum = 0;
		Int64 uncompressedLength = 0;
		while (line.Length > 0) {
			switch (line[0]) {
				case (Byte)'N':
					OperationStatus status = Utf8.ToUtf16(line.Slice(1), stringBuffer, out bytesConsumed, out Int32 charsWritten, false);
					if (status != OperationStatus.Done)
						ArchiveException.Throw($"Failed to parse file name in line {lineNumber}: {status}");
					Debug.Assert(bytesConsumed == line.Length - 1);

					String name = new(stringBuffer.Slice(0, charsWritten));
					return new FileEntry(name, binaryPosition, 0, isCompressed, uncompressedLength, checksum);

				case (Byte)'C':
					isCompressed = true;
					line = line.Slice(2);
					break;
				case (Byte)'U':
					if (!Utf8Parser.TryParse(line.Slice(1, 16), out uncompressedLength, out bytesConsumed, 'X') || bytesConsumed != 16)
						ArchiveException.Throw($"Failed to parse uncompressed length in line {lineNumber}");
					line = line.Slice(bytesConsumed + 2);
					break;
				case (Byte)'H':
					if (!Utf8Parser.TryParse(line.Slice(1, 16), out checksum, out bytesConsumed, 'X') || bytesConsumed != 16)
						ArchiveException.Throw($"Failed to parse checksum in line {lineNumber}");
					line = line.Slice(bytesConsumed + 2);
					break;
				default:
					ArchiveException.Throw($"Unknown DataType {line[0]}=={(char)line[0]} in line {lineNumber}");
					break;
			}
		}

		ArchiveException.Throw($"Missing entry name while parsing line {lineNumber}");
		return default(FileEntry);
	}

	#region IDisposable

	/// <inheritdoc />
	public void Dispose() {
		if (_isDisposed) return;
		_dataFileStream.Dispose();
		_catalogFileStream.Dispose();
		_isDisposed = true;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync() {
		if (_isDisposed) return;
		await _dataFileStream.DisposeAsync();
		await _catalogFileStream.DisposeAsync();
		_isDisposed = true;
	}

	#endregion
}