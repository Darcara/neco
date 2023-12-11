namespace Neco.Common.Data.Archive;

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Unicode;
using System.Threading.Tasks;
using Neco.Common.Extensions;

/// <summary>
/// A very simple 2-file archive. Append-only!
/// </summary>
/// <para>A Catalog consists of two files. The .cat-File contains the metadata (offset, length, name) of the entries, while the .bin file contains all data simple appended after each other.</para>
public sealed class Catalog : IDisposable, IAsyncDisposable {
	private const Byte _entrySeparator = 0x0A; //0x0A is line feed or \n
	private const Byte _columnSeparator = 0x20; //0x20 is space
	private readonly StandardFormat _writeOffsetFormat = StandardFormat.Parse("X16");
	
	private readonly String _catalogFile;
	private readonly String _dataFile;
	private readonly List<FileEntry> _entries = new();
	private Int64? _catalogFileLength;
	private Int64? _dataFileLength;
	private Boolean _isDisposed;
	private FileStream _catalogFileStream;
	private FileStream _dataFileStream;

	public IReadOnlyList<FileEntry> Entries => _entries;

	private Catalog(String baseName) : this(Path.ChangeExtension(baseName, "cat"), Path.ChangeExtension(baseName, "bin")) {
	}

	private Catalog(String catalogFile, String dataFile) {
		_catalogFile = catalogFile;
		_dataFile = dataFile;
	}

	public static Catalog CreateNew(String baseName) {
		Catalog cat = new(baseName);
		cat.InitNew();
		return cat;
	}

	public static Catalog OpenExisting(String baseName) {
		Catalog cat = new(baseName);
		cat.InitExisting();
		return cat;
	}

	private void InitNew() {
		if (File.Exists(_catalogFile)) throw new ArchiveException($"Catalog file already present: {_catalogFile}");
		if (File.Exists(_dataFile)) throw new ArchiveException($"Data file already present: {_dataFile}");

		_catalogFileStream = new FileStream(_catalogFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, MagicNumbers.DefaultStreamBufferSize, false);
		_dataFileStream = new FileStream(_dataFile, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, MagicNumbers.DefaultStreamBufferSize, false);
	}

	private void InitExisting() {
		FileInfo catalogfile = new(_catalogFile);
		if (catalogfile.Exists) _catalogFileLength = catalogfile.Length;
		else throw new ArchiveException($"Missing catalog file: {_catalogFile}");

		FileInfo datafile = new(_dataFile);
		if (datafile.Exists) _dataFileLength = datafile.Length;
		else throw new ArchiveException($"Missing catalog file: {_dataFile}");

		_catalogFileStream = new FileStream(_catalogFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None, MagicNumbers.DefaultStreamBufferSize, false);
		_dataFileStream = new FileStream(_dataFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None, MagicNumbers.DefaultStreamBufferSize, false);
		try {
			ReadEntriesFromCatalog();
		}
		catch (Exception) {
			Dispose();
			throw;
		}
	}

	public void AppendEntry(String filename) => AppendEntry(new FileInfo(filename));

	public void AppendEntry(FileInfo file) {
		ObjectDisposedException.ThrowIf(_isDisposed, this);
		if(!file.Exists) throw new FileNotFoundException("Unable to append non-existing file", file.FullName);
		using FileStream inputStream = file.OpenRead();
		AppendEntry(inputStream, file.Name);
	}

	public void AppendEntry(Stream inputStream, String entryName) {
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		_dataFileStream.Seek(0, SeekOrigin.End);
		
		Int64 dataStreamOffset = _dataFileStream.Position;
		Int64 bytesCopied = inputStream.CopyToAndCount(_dataFileStream);
		_dataFileStream.Flush();

		FileEntry fileEntry = new(entryName, dataStreamOffset, bytesCopied);

		Byte[] buffer = ArrayPool<Byte>.Shared.Rent(18 + fileEntry.Name.Length * 4);
		Utf8Formatter.TryFormat(fileEntry.Offset, buffer, out Int32 offsetBytesWritten, _writeOffsetFormat);
		buffer[offsetBytesWritten] = _columnSeparator;
		Utf8.FromUtf16(fileEntry.Name, buffer.AsSpan(offsetBytesWritten+1), out _, out Int32 nameBytesWritten);
		buffer[offsetBytesWritten + 1 + nameBytesWritten] = _entrySeparator;
		
		_catalogFileStream.Seek(0, SeekOrigin.End);
		_catalogFileStream.Write(buffer, 0, offsetBytesWritten + 1 + nameBytesWritten + 1);
		_catalogFileStream.Flush();
		ArrayPool<Byte>.Shared.Return(buffer);
		
		_entries.Add(fileEntry);
	}

	public Byte[] GetDataAsArray(Int32 index) => GetDataAsArray(_entries[index]);

	public Byte[] GetDataAsArray(FileEntry entry) {
		ArgumentOutOfRangeException.ThrowIfGreaterThan(entry.Length, Int32.MaxValue);
		using BoundedReadOnlyStream dataStream = new(_dataFileStream, entry.Offset, entry.Length, false);
		Byte[] buffer = new Byte[entry.Length];
		dataStream.ReadExactly(buffer);
		return buffer;
	}

	public Stream GetDataAsStream(Int32 index) => GetDataAsStream(_entries[index]);

	public Stream GetDataAsStream(FileEntry entry) => new BoundedReadOnlyStream(_dataFileStream, entry.Offset, entry.Length, false);

	private void ReadEntriesFromCatalog() {
		Debug.Assert(_entries.Count == 0);

		Int32 readByteOffset = 0;
		Int32 writeByteOffsetOffset = 0;
		Int64 offset = 0;
		Int64 lineNumber = 0;
		String? lastName = null;
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
				Int32 newLineIndex = availableDataSpan.IndexOf(_entrySeparator);
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

				String name = ParseLine(line, stringBuffer, lineNumber, out Int64 binaryPosition);
				if (lastName != null) {
					Int64 length = binaryPosition - offset;
					ArchiveException.ThrowIfNegative(length);
					_entries.Add(new FileEntry(lastName, offset, length));
				}

				offset = binaryPosition;
				lastName = name;
			}
		}

		if (lastName != null) {
			Int64 length = _dataFileLength!.Value - offset;
			ArchiveException.ThrowIfNegative(length);
			_entries.Add(new FileEntry(lastName, offset, length));
		}
	}

	private static String ParseLine(ReadOnlySpan<Byte> line, Span<Char> stringBuffer, Int64 lineNumber, out Int64 binaryPosition) {
		if (!Utf8Parser.TryParse(line.Slice(0, 16), out binaryPosition, out Int32 bytesConsumed, 'X') || bytesConsumed != 16)
			ArchiveException.Throw($"Failed to parse binary position in line {lineNumber}");

		OperationStatus status = Utf8.ToUtf16(line.Slice(17), stringBuffer, out bytesConsumed, out Int32 charsWritten, false);
		if (status != OperationStatus.Done)
			ArchiveException.Throw($"Failed to parse file name line {lineNumber}: {status}");
		Debug.Assert(bytesConsumed == line.Length - 1 - 16);

		return new String(stringBuffer.Slice(0, charsWritten));
	}
	

	#region IDisposable

	/// <inheritdoc />
	public void Dispose() {
		if (_isDisposed) return;
		_catalogFileStream.Dispose();
		_dataFileStream.Dispose();
		_isDisposed = true;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync() {
		if (_isDisposed) return;
		await _catalogFileStream.DisposeAsync();
		await _dataFileStream.DisposeAsync();
		_isDisposed = true;
	}

	#endregion
	
}