namespace Neco.Test.Common.Data;

using System;
using FluentAssertions;
using Neco.Common.Data;
using NUnit.Framework;

[TestFixture]
public class FileCompressionLookupTests {
	[Test]
	public void StaticLookupKnowsIncompressible() {
		StaticFileCompressionLookup.Instance.DoesFileCompress(".zip").Should().Be(FileCompression.Incompressible);
		StaticFileCompressionLookup.Instance.DoesFileCompress("zip").Should().Be(FileCompression.Incompressible);
		StaticFileCompressionLookup.Instance.DoesFileCompress(".ZIP").Should().Be(FileCompression.Incompressible);
		StaticFileCompressionLookup.Instance.DoesFileCompress("ZIP").Should().Be(FileCompression.Incompressible);
	}
	
	[Test]
	public void StaticLookupDoesNotKnowCompressible() {
		StaticFileCompressionLookup.Instance.DoesFileCompress(".txt", FileCompression.Unknown).Should().Be(FileCompression.Unknown);
		StaticFileCompressionLookup.Instance.DoesFileCompress("txt", FileCompression.Unknown).Should().Be(FileCompression.Unknown);
		StaticFileCompressionLookup.Instance.DoesFileCompress(".TXT", FileCompression.Unknown).Should().Be(FileCompression.Unknown);
		StaticFileCompressionLookup.Instance.DoesFileCompress("TXT", FileCompression.Unknown).Should().Be(FileCompression.Unknown);
	}
	
	[Test]
	public void StaticLookupUsesDefaultReturnCorrectly() {
		StaticFileCompressionLookup.Instance.DoesFileCompress(".txt").Should().Be(FileCompression.Compressible);
	}
	
	[Test]
	public void StaticLookupReturnsDefaultForNoExtension() {
		StaticFileCompressionLookup.Instance.DoesFileCompress(".").Should().Be(FileCompression.Compressible);
		StaticFileCompressionLookup.Instance.DoesFileCompress("").Should().Be(FileCompression.Compressible);
	}
	[Test]
	public void StaticLookupThrowsOnNull() {
		Assert.Throws<ArgumentNullException>(() => StaticFileCompressionLookup.Instance.DoesFileCompress(null));
	}
}