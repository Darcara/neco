namespace Neco.Test.Common.Data.Archive;

using System;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Neco.Common;
using Neco.Common.Data.Archive;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
public class CatalogTests {
	private const String _testBaseName = "test";
	private String TestCatalogName => Path.ChangeExtension(_testBaseName, ".cat");
	private String TestDataName => Path.ChangeExtension(_testBaseName, ".bin");

	[SetUp]
	public void BeforeEachTest() {
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
			cat.DisposeAsync().GetAwaiter().GetResult();
			cat.Dispose();
			cat.DisposeAsync().GetAwaiter().GetResult();
		});
		
		Assert.Throws<ArchiveException>(() => Catalog.CreateNew(_testBaseName).Dispose());
		File.Delete(TestCatalogName);
		Assert.Throws<ArchiveException>(() => Catalog.CreateNew(_testBaseName).Dispose());
		File.Delete(TestDataName);
		Assert.DoesNotThrow(() => Catalog.CreateNew(_testBaseName).Dispose());
		BeforeEachTest();
		
		using Catalog catalog = Catalog.CreateNew(_testBaseName);
		Assert.Throws<FileNotFoundException>(() => catalog.AppendEntry("fileDoesNotExist.xyz"));
	}

	[Test]
	public void InvalidCatalogFile_NonHexOffset() {
		File.WriteAllText(TestCatalogName,"01234X6789ABCDEF filename.txt\n");
		File.WriteAllText(TestDataName,String.Empty);
		Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
	}
	
	[Test]
	public void InvalidCatalogFile_WrongOrderOfOffsets() {
		File.WriteAllText(TestCatalogName,"0000000000000001 filename1.txt\n0000000000000000 filename2.txt\n");
		File.WriteAllText(TestDataName,String.Empty);
		Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
	}
	
	[Test]
	public void InvalidCatalogFile_NonUtf8Name() {
		byte[] invalidutf8 = new Byte[27];
		"0123456789ABCDEF filena"u8.CopyTo(invalidutf8);
		invalidutf8[23] = 0xF0;
		invalidutf8[24] = 0x90;
		invalidutf8[25] = 0x80;
		invalidutf8[26] = 0x0A;
			
		File.WriteAllBytes(TestCatalogName,invalidutf8);
		File.WriteAllText(TestDataName,String.Empty);
		Assert.Throws<ArchiveException>(() => Catalog.OpenExisting(_testBaseName).Dispose());
	}


	private void CreateTestCatalog() {
		Assert.That(TestCatalogName, Does.Not.Exist);
		Assert.That(TestDataName, Does.Not.Exist);

		using Catalog catalog = Catalog.CreateNew(_testBaseName);
		Assert.That(TestCatalogName, Does.Exist);
		Assert.That(TestDataName, Does.Exist);

		catalog.AppendEntry(typeof(CatalogTests).Assembly.Location);
		catalog.Entries.Count.Should().Be(1);
		catalog.AppendEntry(new FileInfo(typeof(Catalog).Assembly.Location));
		catalog.Entries.Count.Should().Be(2);
	}

	[Test]
	public void CreatesNewCatalogAndCanAppend() => CreateTestCatalog();

	[Test]
	public void CanReadExistingArchive() {
		CreateTestCatalog();

		using Catalog readCat = Catalog.OpenExisting(_testBaseName);
		readCat.Entries.Count.Should().Be(2);

		readCat.Entries[0].Offset.Should().Be(0);
		readCat.Entries[0].Length.Should().Be(new FileInfo(typeof(CatalogTests).Assembly.Location).Length);
		readCat.Entries[0].Name.Should().Be(Path.GetFileName(typeof(CatalogTests).Assembly.Location));

		readCat.Entries[1].Offset.Should().Be(readCat.Entries[0].Length);
		readCat.Entries[1].Length.Should().Be(new FileInfo(typeof(Catalog).Assembly.Location).Length);
		readCat.Entries[1].Name.Should().Be(Path.GetFileName(typeof(Catalog).Assembly.Location));

		using (MemoryStream memStream = new()) {
			using Stream stream1 = readCat.GetDataAsStream(0);
			stream1.CopyTo(memStream);
			Assert.That(memStream.Position, Is.EqualTo(readCat.Entries[0].Length));
			Byte[] stream1ExpectedData = File.ReadAllBytes(typeof(CatalogTests).Assembly.Location);
			Assert.That(stream1ExpectedData, Is.EquivalentTo(memStream.ToArray()));
		}

		Byte[] stream2ExpectedData = File.ReadAllBytes(typeof(Catalog).Assembly.Location);
		Byte[] stream2Data = readCat.GetDataAsArray(1);
		Assert.That(stream2ExpectedData, Has.Length.EqualTo(readCat.Entries[1].Length));
		Assert.That(stream2Data, Has.Length.EqualTo(readCat.Entries[1].Length));
		Assert.That(stream2ExpectedData.AsSpan().SequenceEqual(stream2Data));
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
			Span<byte> span = pipe.Writer.GetSpan(bytesRead);
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