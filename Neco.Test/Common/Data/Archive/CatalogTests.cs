namespace Neco.Test.Common.Data.Archive;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using FluentAssertions;
using Neco.Common;
using Neco.Common.Data;
using Neco.Common.Data.Archive;
using Neco.Common.Data.Hash;
using Neco.Common.Extensions;
using Neco.Common.Helper;
using NUnit.Framework;

[TestFixture]
public class CatalogTests {
	private const String _testBaseName = "test";
	internal static String TestCatalogName => Path.ChangeExtension(_testBaseName, ".cat");
	private static String TestDataName => Path.ChangeExtension(_testBaseName, ".bin");

	private static IEnumerable<CatalogOptions?> CatalogOptionsTestCases {
		get {
			yield return null;
			yield return new CatalogOptions() { Features = CatalogFeatures.ChecksumPerEntry };
			yield return new CatalogOptions() { Features = CatalogFeatures.ChecksumPerEntry | CatalogFeatures.CompressionPerEntryOptimal };
			yield return new CatalogOptions() { Features = CatalogFeatures.ChecksumPerEntry | CatalogFeatures.CompressionPerEntrySmallest };
			yield return new CatalogOptions() { Features = CatalogFeatures.ChecksumPerEntry | CatalogFeatures.CompressionPerEntryOptimal | CatalogFeatures.UncompressedFileSize };
			yield return new CatalogOptions() { Features = CatalogFeatures.UncompressedFileSize };
		}
	}

	[SetUp]
	public static void BeforeEachTest() {
		File.Delete(TestCatalogName);
		File.Delete(TestDataName);
	}

	[Test]
	public void InterfaceTests() {
		Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
		File.Create(TestCatalogName).Dispose();
		Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
		File.Create(TestDataName).Dispose();
		Assert.DoesNotThrow(() => {
			Catalog cat = Catalog.OpenExisting(_testBaseName);
			cat.DisposeAsync().GetResultBlocking();
			cat.Dispose();
			cat.DisposeAsync().GetResultBlocking();
		});

		Assert.Throws<ArchiveException>(() => Catalog.CreateNew(_testBaseName).Dispose());
		File.Delete(TestCatalogName);
		Assert.Throws<ArchiveException>(() => Catalog.CreateNew(_testBaseName).Dispose());
		File.Delete(TestDataName);
		Assert.DoesNotThrow(() => Catalog.CreateNew(_testBaseName).Dispose());
		BeforeEachTest();

		using (Catalog catalog = Catalog.CreateNew(_testBaseName)) {
			Assert.Throws<FileNotFoundException>(() => catalog.AppendEntry("fileDoesNotExist.xyz"));
		}

		BeforeEachTest();
		Assert.Throws<ArgumentException>(() => Catalog.CreateNew(_testBaseName, new CatalogOptions() { Features = CatalogFeatures.CompressionPerEntryOptimal | CatalogFeatures.CompressionPerEntrySmallest }));
	}

	[Test]
	public void InvalidCatalogFile_NonHexOffset() {
		File.WriteAllText(TestCatalogName, "01234X6789ABCDEF Nfilename.txt\n");
		File.WriteAllText(TestDataName, String.Empty);
		Exception? ex = Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
		Console.WriteLine(ex);
	}
	
	[Test]
	public void InvalidCatalogFile_NonHexChecksum() {
		File.WriteAllText(TestCatalogName, "0123456789ABCDEF H1X34567890ABCDEF Nfilename.txt\n");
		File.WriteAllText(TestDataName, String.Empty);
		Exception? ex = Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
		Console.WriteLine(ex);
	}
	
	[Test]
	public void InvalidCatalogFile_NonUnCompressedLength() {
		File.WriteAllText(TestCatalogName, "0123456789ABCDEF U1X34567890ABCDEF Nfilename.txt\n");
		File.WriteAllText(TestDataName, String.Empty);
		Exception? ex = Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
		Console.WriteLine(ex);
	}
	
	[Test]
	public void InvalidCatalogFile_NoName() {
		File.WriteAllText(TestCatalogName, "0123456789ABCDEF\n");
		File.WriteAllText(TestDataName, String.Empty);
		Exception? ex = Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
		Console.WriteLine(ex);
		
		File.WriteAllText(TestCatalogName, "0123456789ABCDEF \n");
		File.WriteAllText(TestDataName, String.Empty);
		ex = Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
		Console.WriteLine(ex);
	}

	[Test]
	public void InvalidCatalogFile_NoNameIndicator() {
		File.WriteAllText(TestCatalogName, "0000000000000000 filename.txt\n");
		File.WriteAllText(TestDataName, String.Empty);
		Exception? ex = Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
		Console.WriteLine(ex);
	}

	[Test]
	public void InvalidCatalogFile_WrongOrderOfOffsets() {
		File.WriteAllText(TestCatalogName, "0000000000000001 Nfilename1.txt\n0000000000000000 Nfilename2.txt\n");
		File.WriteAllText(TestDataName, String.Empty);
		Exception? ex = Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
		Console.WriteLine(ex);
	}

	[Test]
	public void InvalidCatalogFile_NonUtf8Name() {
		Byte[] invalidutf8 = new Byte[27];
		"0123456789ABCDEF filena"u8.CopyTo(invalidutf8);
		invalidutf8[23] = 0xF0;
		invalidutf8[24] = 0x90;
		invalidutf8[25] = 0x80;
		invalidutf8[26] = 0x0A;

		File.WriteAllBytes(TestCatalogName, invalidutf8);
		File.WriteAllText(TestDataName, String.Empty);
		Exception? ex = Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
		Console.WriteLine(ex);
	}

	internal static void CreateTestCatalog(CatalogOptions? options = null) {
		Assert.That(TestCatalogName, Does.Not.Exist);
		Assert.That(TestDataName, Does.Not.Exist);
		Assert.That(Catalog.Exists(TestCatalogName), Is.False);

		using Catalog catalog = Catalog.CreateNew(_testBaseName, options);
		Assert.That(TestCatalogName, Does.Exist);
		Assert.That(TestDataName, Does.Exist);
		Assert.That(Catalog.Exists(TestCatalogName), Is.True);

		catalog.AppendEntry(typeof(CatalogTests).Assembly.Location);
		catalog.Entries.Count.Should().Be(1);
		catalog.GetDataAsArray(0).Should().BeEquivalentTo(File.ReadAllBytes(typeof(CatalogTests).Assembly.Location));
		
		catalog.AppendEntry(new FileInfo(typeof(Catalog).Assembly.Location));
		catalog.Entries.Count.Should().Be(2);
		catalog.GetDataAsArray(1).Should().BeEquivalentTo(File.ReadAllBytes(typeof(Catalog).Assembly.Location));
		
		VerifyFeatures(catalog.Entries, options?.Features ?? CatalogFeatures.None);
	}

	private static void VerifyFeatures(IReadOnlyList<FileEntry> entries, CatalogFeatures features) {
		for (int index = 0; index < entries.Count; index++) {
			FileEntry entry = entries[index];
			File.Exists(entry.Name).Should().BeTrue($"Entry #{index}");
			String msg = $"Entry #{index} = {entry.Name}";

			byte[] data = File.ReadAllBytes(entry.Name);

			Boolean shouldBeCompressed = features.HasFlag(CatalogFeatures.CompressionPerEntryOptimal) || features.HasFlag(CatalogFeatures.CompressionPerEntrySmallest);
			entry.IsCompressed.Should().Be(shouldBeCompressed, msg);

			String checksum = ((Int64)WyHashFinal3.HashOneOffLong(data)).ToString("X16");

			if (!shouldBeCompressed)
				entry.Length.Should().Be(data.Length, msg);

			if (features.HasFlag(CatalogFeatures.ChecksumPerEntry))
				entry.Checksum.ToString("X16").Should().Be(checksum, msg);
			else
				entry.Checksum.Should().Be(0, msg);

			if (features.HasFlag(CatalogFeatures.UncompressedFileSize) || !shouldBeCompressed)
				entry.UncompressedLength.Should().Be(data.Length, msg);
			else
				entry.UncompressedLength.Should().Be(0, msg);
		}
	}

	[TestCaseSource(nameof(CatalogOptionsTestCases))]
	public void CreatesNewCatalogAndCanAppend(CatalogOptions? options) => CreateTestCatalog(options);

	[TestCaseSource(nameof(CatalogOptionsTestCases))]
	public void CanReadExistingArchive(CatalogOptions? options) {
		CreateTestCatalog(options);

		using Catalog readCat = Catalog.OpenExisting(_testBaseName);
		readCat.Entries.Count.Should().Be(2);
		CatalogOptions? readCatOptions = ReflectionHelper.GetFieldOrPropertyValue<CatalogOptions?>(readCat, "_options");
		(readCatOptions?.Features).Should().Be(options?.Features ?? CatalogFeatures.None);
		VerifyFeatures(readCat.Entries, readCatOptions!.Features);
		
		readCat.Entries[0].Offset.Should().Be(0);
		readCat.Entries[0].Name.Should().Be(Path.GetFileName(typeof(CatalogTests).Assembly.Location));
		Int64 zeroLength = new FileInfo(readCat.Entries[0].Name).Length;
		if (readCatOptions.Features.HasFlag(CatalogFeatures.UncompressedFileSize))
			zeroLength = readCat.Entries[0].UncompressedLength;

		readCat.Entries[1].Offset.Should().Be(readCat.Entries[0].Length);
		readCat.Entries[1].Name.Should().Be(Path.GetFileName(typeof(Catalog).Assembly.Location));
		Int64 oneLength = new FileInfo(readCat.Entries[1].Name).Length;
		if (readCatOptions.Features.HasFlag(CatalogFeatures.UncompressedFileSize))
			oneLength = readCat.Entries[1].UncompressedLength;

		using (MemoryStream memStream = new()) {
			using Stream stream1 = readCat.GetDataAsStream(0);
			stream1.CopyTo(memStream);
			Assert.That(memStream.Position, Is.EqualTo(zeroLength));
			Byte[] stream1ExpectedData = File.ReadAllBytes(typeof(CatalogTests).Assembly.Location);
			Assert.That(stream1ExpectedData, Is.EquivalentTo(memStream.ToArray()));
		}

		Byte[] stream2ExpectedData = File.ReadAllBytes(typeof(Catalog).Assembly.Location);
		Byte[] stream2Data = readCat.GetDataAsArray(1);
		Assert.That(stream2ExpectedData, Has.Length.EqualTo(oneLength));
		Assert.That(stream2Data, Has.Length.EqualTo(oneLength));
		Assert.That(stream2ExpectedData.AsSpan().SequenceEqual(stream2Data));
	}

	[Test]
	public void CreateFolderArchive() {
		Int32 simpleCount;
		using (Catalog catalog = Catalog.CreateNew(_testBaseName)) {
			catalog.AppendFolder(".", "*.*", SearchOption.AllDirectories, includeFilePredicate: (filename, _) => new FileInfo(filename).Name != TestCatalogName && new FileInfo(filename).Name != TestDataName);
			simpleCount = catalog.Entries.Count;
		}

		Int64 simpleSize = new FileInfo(TestDataName).Length;

		BeforeEachTest();
		Int32 optionsCount;
		using (Catalog catalog = Catalog.CreateNew(_testBaseName)) {
			catalog.AppendFolder(".", "*.*", new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = true }, includeFilePredicate: (filename, _) => new FileInfo(filename).Name != TestCatalogName && new FileInfo(filename).Name != TestDataName);
			optionsCount = catalog.Entries.Count;
		}

		Int64 optionsSize = new FileInfo(TestDataName).Length;

		simpleCount.Should().Be(optionsCount);
		simpleSize.Should().Be(optionsSize);
	}

	[Test]
	public void CanReadAndAppendSynchronously() {
		Assert.That(TestCatalogName, Does.Not.Exist);
		Assert.That(TestDataName, Does.Not.Exist);
		CatalogOptions options = CatalogOptions.Default with { Features = CatalogFeatures.None };

		using Catalog catalog = Catalog.CreateNew(_testBaseName, options);
		Assert.That(TestCatalogName, Does.Exist);
		Assert.That(TestDataName, Does.Exist);

		catalog.AppendEntry(typeof(CatalogTests).Assembly.Location);
		catalog.Entries.Count.Should().Be(1);
		catalog.GetDataAsArray(0).Should().BeEquivalentTo(File.ReadAllBytes(typeof(CatalogTests).Assembly.Location));
		
		catalog.AppendEntry(new FileInfo(typeof(Catalog).Assembly.Location));
		catalog.Entries.Count.Should().Be(2);
		catalog.GetDataAsArray(1).Should().BeEquivalentTo(File.ReadAllBytes(typeof(Catalog).Assembly.Location));
		catalog.GetDataAsArray(0).Should().BeEquivalentTo(File.ReadAllBytes(typeof(CatalogTests).Assembly.Location));

		catalog.AppendEntry(new FileInfo(typeof(TestAttribute).Assembly.Location));
		catalog.Entries.Count.Should().Be(3);
		catalog.GetDataAsArray(2).Should().BeEquivalentTo(File.ReadAllBytes(typeof(TestAttribute).Assembly.Location));
		catalog.GetDataAsArray(1).Should().BeEquivalentTo(File.ReadAllBytes(typeof(Catalog).Assembly.Location));
		catalog.GetDataAsArray(0).Should().BeEquivalentTo(File.ReadAllBytes(typeof(CatalogTests).Assembly.Location));
		
		VerifyFeatures(catalog.Entries, options.Features);
	}

	[Test]
	public void DoesNotCompressIfFileDoesNotCompress() {
		MockCompressionLookup lookup = new();
		CatalogOptions options = new() { CompressionLookup = lookup, Features = CatalogFeatures.CompressionPerEntryOptimal};
		using Catalog catalog = Catalog.CreateNew(_testBaseName, options);
		Assert.That(TestCatalogName, Does.Exist);
		Assert.That(TestDataName, Does.Exist);

		lookup.NextResult = FileCompression.Incompressible;
		catalog.AppendEntry(typeof(CatalogTests).Assembly.Location);
		catalog.Entries.Count.Should().Be(1);
		catalog.GetDataAsArray(0).Should().BeEquivalentTo(File.ReadAllBytes(typeof(CatalogTests).Assembly.Location));
		
		lookup.NextResult = FileCompression.Compressible;
		catalog.AppendEntry(new FileInfo(typeof(Catalog).Assembly.Location));
		catalog.Entries.Count.Should().Be(2);
		catalog.GetDataAsArray(1).Should().BeEquivalentTo(File.ReadAllBytes(typeof(Catalog).Assembly.Location));
		catalog.GetDataAsArray(0).Should().BeEquivalentTo(File.ReadAllBytes(typeof(CatalogTests).Assembly.Location));

		catalog.Entries[0].IsCompressed.Should().BeFalse();
		catalog.Entries[1].IsCompressed.Should().BeTrue();
	}
	
	[Test]
	public void DoesNotCompressIfFileTooSmall() {
		MockCompressionLookup lookup = new();
		CatalogOptions options = new() { CompressionLookup = lookup, Features = CatalogFeatures.CompressionPerEntryOptimal};
		using Catalog catalog = Catalog.CreateNew(_testBaseName, options);
		Assert.That(TestCatalogName, Does.Exist);
		Assert.That(TestDataName, Does.Exist);

		lookup.NextResult = FileCompression.Compressible;
		using MemoryStream ms = new("Less than 32b does not compress"u8.ToArray());
		catalog.AppendEntry(ms, "somefile.txt");
		catalog.Entries.Count.Should().Be(1);
		catalog.GetDataAsArray(0).LongLength.Should().Be(ms.Length);
	}

	[Test]
	[Ignore("CurrentlyNotThreadSafe")]
	public void CanReadInterleavedWithAdding() {
		CreateTestCatalog();
		using Catalog catalog = Catalog.OpenExisting(_testBaseName);
		catalog.Entries.Count.Should().Be(2);

		using Stream readStream = catalog.GetDataAsStream(1);

		Pipe pipe = new();
		Task t = Task.Run(() => {
			try {
				catalog.AppendEntry(pipe.Reader.AsStream(), "fromStream");
			}
			finally {
				pipe.Writer.Complete();
				pipe.Reader.Complete();
			}
		});

		Int32 bytesRead;
		Byte[] buffer = new Byte[MagicNumbers.DefaultStreamBufferSize];
		using MemoryStream memStream = new();
		while ((bytesRead = readStream.Read(buffer, 0, MagicNumbers.DefaultStreamBufferSize)) != 0) {
			memStream.Write(buffer, 0, bytesRead);
			Span<Byte> span = pipe.Writer.GetSpan(bytesRead);
			buffer.AsSpan(0, bytesRead).CopyTo(span);
			pipe.Writer.Advance(bytesRead);
			pipe.Writer.FlushAsync().GetResultBlocking();
		}

		pipe.Writer.Complete();
		t.GetAwaiter().GetResult();

		Byte[] data2 = catalog.GetDataAsArray(2);
		Assert.That(buffer, Is.EquivalentTo(data2));
	}
}

internal class MockCompressionLookup : IFileCompressionLookup {
	public FileCompression NextResult { get; set; } = FileCompression.Unknown;
	
	#region Implementation of IFileCompressionLookup

	/// <inheritdoc />
	public FileCompression DoesFileCompress(String fileExtension, FileCompression assumedDefault = FileCompression.Compressible) {
		return NextResult;
	}

	#endregion
} 