namespace Neco.Test.Common.Data;

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Neco.Common;
using Neco.Common.Data;
using NUnit.Framework;

[TestFixture]
public class RandomDataStreamTests {
	[Test]
	public void InterfaceTests() {
		using Stream s = new RandomDataStream(1024);
		s.Length.Should().Be(1024);
		s.CanRead.Should().BeTrue();
		s.CanWrite.Should().BeFalse();
		s.CanSeek.Should().BeFalse();
		Assert.DoesNotThrow(() => s.Flush());
		Assert.DoesNotThrow(() => s.FlushAsync());
		Assert.Throws<NotSupportedException>(() => s.SetLength(0));
		Assert.Throws<NotSupportedException>(() => s.Seek(0, SeekOrigin.Begin));
		Assert.Throws<NotSupportedException>(() => s.Seek(0, SeekOrigin.End));
		Assert.Throws<NotSupportedException>(() => s.Seek(0, SeekOrigin.Current));
		Assert.Throws<NotSupportedException>(() => s.Write(Span<Byte>.Empty));
	}
	
	[TestCase(1024)]
	[TestCase(Int32.MaxValue)]
	[TestCase(Int32.MaxValue * 4L)]
	public void ReturnsRandomData(Int64 numBytes) {
		using Stream s = new RandomDataStream(numBytes);
		s.Length.Should().Be(numBytes);
		Int64 totalBytesRead = 0;
		Byte[] buffer = new Byte[MagicNumbers.MaxNonLohBufferSize];
		Int64 maxReads = 1 + numBytes / 4096;
		Int64 maxReadsCounter = 0;
		Int32 bytesRead;
		do {
			bytesRead = s.Read(buffer);
			totalBytesRead += bytesRead;
			++maxReadsCounter;
		} while (totalBytesRead < numBytes && maxReadsCounter < maxReads && bytesRead > 0);
		
		totalBytesRead.Should().Be(numBytes);
		s.Position.Should().Be(numBytes);
	}
	
	// No need for extremely large data here
	[TestCase(1024)]
	[TestCase(1024*1024)]
	public async Task ReturnsRandomDataAsync(Int64 numBytes) {
		await using Stream s = new RandomDataStream(numBytes);
		s.Length.Should().Be(numBytes);
		Int64 totalBytesRead = 0;
		Byte[] buffer = new Byte[MagicNumbers.MaxNonLohBufferSize];
		Int64 maxReads = 1 + numBytes / 4096;
		Int64 maxReadsCounter = 0;
		Int32 bytesRead;
		do {
			bytesRead = await s.ReadAsync(buffer);
			totalBytesRead += bytesRead;
			++maxReadsCounter;
		} while (totalBytesRead < numBytes && maxReadsCounter < maxReads && bytesRead > 0);
		
		totalBytesRead.Should().Be(numBytes);
		s.Position.Should().Be(numBytes);
	}
}