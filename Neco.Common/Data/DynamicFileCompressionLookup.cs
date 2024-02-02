namespace Neco.Common.Data;

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using Neco.Common.Extensions;

/// <summary>
/// A lookup that knows nothing initially, but can be fed with files to check for compression, or compression-test-results, to build a proper lookup.
/// </summary>
public sealed class DynamicFileCompressionLookup : IFileCompressionLookup {
	private readonly ConcurrentDictionary<String, FileExtensionStatistic> _dynamicStatistics = new(StringComparer.OrdinalIgnoreCase);

	/// <inheritdoc />
	public FileCompression DoesFileCompress(String fileExtension, FileCompression assumedDefault = FileCompression.Compressible) {
		return assumedDefault;
	}

	// Compress a chunk from MIDDLE of the file 80?k
	// or https://stackoverflow.com/questions/7027022/how-to-efficiently-predict-if-data-is-compressible#
	public void AddCompressionEstimate(FileInfo file) {
		String extensionStr = StaticFileCompressionLookup.NormalizeFileExtension(file.Extension);
		if (extensionStr.Length == 0) return;

		if (_dynamicStatistics.TryGetValue(extensionStr, out FileExtensionStatistic statistics)) {
			if (statistics.SamplesCompressible + statistics.SamplesIncompressible >= 1000)
				return;
		}

		using FileStream fileStream = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

		Byte[] buf = ArrayPool<Byte>.Shared.Rent(MagicNumbers.MaxNonLohBufferSize);
		Int64 start = Math.Max(0, file.Length / 2 - MagicNumbers.MaxNonLohBufferSize);
		Int32 length = (Int32)Math.Min(buf.Length, Math.Min(file.Length, file.Length / 2 + MagicNumbers.MaxNonLohBufferSize));
		fileStream.Seek(start, SeekOrigin.Begin);
		fileStream.ReadExactly(buf, 0, length);

		AddCompressionEstimate(extensionStr, buf.AsSpan(0, length));
		ArrayPool<Byte>.Shared.Return(buf);
	}

	public void AddCompressionEstimate(String fileExtension, ReadOnlySpan<Byte> sampleData) {
		Byte[] buf = ArrayPool<Byte>.Shared.Rent(MagicNumbers.MaxNonLohBufferSize * 2);
		using MemoryStream compressedStream = new(buf);
		using BrotliStream compressorStream = new(compressedStream, CompressionLevel.SmallestSize);
		compressorStream.Write(sampleData);
		compressorStream.Flush();
		Double ratio = compressedStream.Length / (Double)sampleData.Length;

		String extensionStr = StaticFileCompressionLookup.NormalizeFileExtension(fileExtension);
		if (extensionStr.Length == 0) return;

		AddCompressionEstimate(extensionStr, ratio);
		
		ArrayPool<Byte>.Shared.Return(buf);
	}

	public void AddCompressionEstimate(String extensionStr, Double ratio) {
		_dynamicStatistics.AddOrUpdate(extensionStr
			, key => new FileExtensionStatistic(extensionStr, ratio)
			, (_, statistic) => new FileExtensionStatistic(statistic, ratio));

		// Console.WriteLine($"{extensionStr} => {ratio}");

		if (_dynamicStatistics[extensionStr].SamplesCompressible + _dynamicStatistics[extensionStr].SamplesIncompressible >= 1000)
			Console.WriteLine(_dynamicStatistics[extensionStr]);


	}



	private static void ScanDir(DirectoryInfo dir, DynamicFileCompressionLookup fcl) {
		try {
			dir.EnumerateFiles().ForEach(f => ScanFile(f, fcl));
			dir.EnumerateDirectories().ForEach(d => ScanDir(d, fcl));
		}
		catch (Exception e) {
			Console.WriteLine($"[DIR] {e.Message} for {dir.FullName}");
		}
	}

	private static void ScanFile(FileInfo file, DynamicFileCompressionLookup fcl) {
		try {
			if (String.IsNullOrWhiteSpace(file.Extension) || file.Length < 4096) return;
			if (fcl.DoesFileCompress(file.Extension, FileCompression.Unknown) != FileCompression.Unknown) return;
			fcl.AddCompressionEstimate(file);
		}
		catch (Exception e) {
			Console.WriteLine($"[FIL] {e.Message} for {file.FullName}");
		}
	}
}

internal struct FileExtensionStatistic() {
	public String Extension { get; }
	public Int32 SamplesCompressible { get; set; }
	public Int32 SamplesIncompressible { get; set; }
	public Double EstimatedCompressionRatio { get; set; }

	public FileExtensionStatistic(String extension, Double measuredRatio) : this() {
		Extension = extension;
		EstimatedCompressionRatio = measuredRatio;
		SamplesCompressible = measuredRatio < 0.95 ? 1 : 0;
		SamplesIncompressible = measuredRatio >= 0.95 ? 1 : 0;
	}

	public FileExtensionStatistic(FileExtensionStatistic oldValue, Double measuredRatio) : this() {
		Extension = oldValue.Extension;
		SamplesCompressible = oldValue.SamplesCompressible + (measuredRatio < 0.95 ? 1 : 0);
		SamplesIncompressible = oldValue.SamplesIncompressible + (measuredRatio >= 0.95 ? 1 : 0);
		EstimatedCompressionRatio = oldValue.EstimatedCompressionRatio + (measuredRatio - oldValue.EstimatedCompressionRatio) / (SamplesCompressible + SamplesIncompressible);
	}

	#region Overrides of ValueType

	/// <inheritdoc />
	public override String ToString() => $"{Extension,4} = {SamplesCompressible}zip + {SamplesIncompressible}not => {EstimatedCompressionRatio,7:P3}";

	#endregion
}