namespace Neco.Test.Common.Data;

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Neco.Common;
using Neco.Common.Data;
using NUnit.Framework;

[TestFixture]
public class ZeroDataStreamTests {
	[Test]
	public void InterfaceTests() {
		using Stream s = new ZeroDataStream(1024);
		s.Length.Should().Be(1024);
		s.CanRead.Should().BeTrue();
		s.CanWrite.Should().BeFalse();
		s.CanSeek.Should().BeTrue();
		Assert.DoesNotThrow(() => s.Flush());
		Assert.DoesNotThrow(() => s.FlushAsync());
		s.Seek(100, SeekOrigin.Begin);
		s.Position.Should().Be(100);
		s.Seek(100, SeekOrigin.Current);
		s.Position.Should().Be(200);
		s.Seek(-10, SeekOrigin.Current);
		s.Position.Should().Be(190);
		s.Seek(-100, SeekOrigin.End);
		s.Position.Should().Be(1024-100);
		Assert.Throws<IOException>(() => s.Seek(-1, SeekOrigin.Begin));
		Assert.Throws<IOException>(() => s.Seek(1024+1, SeekOrigin.Begin));
		Assert.Throws<IOException>(() => s.Seek(1, SeekOrigin.End));
		Assert.Throws<IOException>(() => s.Seek(-(1024+1), SeekOrigin.End));
		s.Seek(100, SeekOrigin.Begin);
		Assert.Throws<IOException>(() => s.Seek(-101, SeekOrigin.Current));
		Assert.Throws<IOException>(() => s.Seek(1024 + 1 -100, SeekOrigin.Current));
		Assert.Throws<ArgumentOutOfRangeException>(() => s.Seek(0, (SeekOrigin)100));
		Assert.Throws<NotSupportedException>(() => s.SetLength(0));
		Assert.Throws<NotSupportedException>(() => s.Write(Span<Byte>.Empty));
	}
	
	[TestCase(1024)]
	[TestCase(Int32.MaxValue)]
	[TestCase(Int32.MaxValue * 4L)]
	public void ReturnsZeroedData(Int64 numBytes) {
		using Stream s = new ZeroDataStream(numBytes);
		s.Length.Should().Be(numBytes);
		Int64 totalBytesRead = 0;
		Byte[] buffer = new Byte[MagicNumbers.MaxNonLohBufferSize];
		Int64 maxReads = 1 + numBytes / 4096;
		Int64 maxReadsCounter = 0;
		Int32 bytesRead;
		do {
			bytesRead = s.Read(buffer);
			Span<byte> trimmed = buffer.AsSpan(0, bytesRead).Trim((Byte)0);
			Assert.IsTrue(trimmed.Length == 0);
			totalBytesRead += bytesRead;
			++maxReadsCounter;
		} while (totalBytesRead < numBytes && maxReadsCounter < maxReads && bytesRead > 0);
		
		totalBytesRead.Should().Be(numBytes);
		s.Position.Should().Be(numBytes);
	}
	
	// No need for extremely large data here
	[TestCase(1024)]
	[TestCase(1024*1024)]
	public async Task ReturnsZeroedDataAsync(Int64 numBytes) {
		await using Stream s = new ZeroDataStream(numBytes);
		s.Length.Should().Be(numBytes);
		Int64 totalBytesRead = 0;
		Byte[] buffer = new Byte[MagicNumbers.MaxNonLohBufferSize];
		Int64 maxReads = 1 + numBytes / 4096;
		Int64 maxReadsCounter = 0;
		Int32 bytesRead;
		do {
			bytesRead = await s.ReadAsync(buffer);
			buffer.AsSpan(0, bytesRead).Trim((Byte)0).Length.Should().Be(0);
			totalBytesRead += bytesRead;
			++maxReadsCounter;
		} while (totalBytesRead < numBytes && maxReadsCounter < maxReads && bytesRead > 0);
		
		totalBytesRead.Should().Be(numBytes);
		s.Position.Should().Be(numBytes);
	}
}