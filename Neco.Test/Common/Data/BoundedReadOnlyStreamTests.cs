namespace Neco.Test.Common.Data;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FluentAssertions;
using Neco.Common.Data;
using NUnit.Framework;

[TestFixture]
public class BoundedReadOnlyStreamTests {
	[Test]
	public void InterfaceTests() {
		using MemoryStream baseStream = new MemoryStream(new Byte[2048]);
		using Stream s = new BoundedReadOnlyStream(baseStream, 512, 1024, true);
		StreamTestHelper.CanReadSeak(s);
	}

	private Stream ConstructStream() {
		Byte[] buffer = new Byte[2048];
		for (Int32 i = 0; i < 512; i++) {
			buffer[i] = 0;
		}
		for (Int32 i = 512; i < 512+1024; i++) {
			buffer[i] = 1;
		}
		for (Int32 i = 512+1024; i < buffer.Length; i++) {
			buffer[i] = 0;
		}

		buffer[512] = 42;
		buffer[512 + 1024 - 1] = 43;
		
		MemoryStream baseStream = new(buffer);
		
		return new BoundedReadOnlyStream(baseStream, 512, 1024, true);
		
	}
	
	[Test]
	public void CanRead() {
		using Stream s = ConstructStream();
		s.ReadByte().Should().Be(42);
		s.ReadByte().Should().Be(1);
		Byte[] buffer = new Byte[1024];
		Int32 bytesRead = s.Read(buffer, 0, 1024 - 3);
		bytesRead.Should().Be(1024 - 3);
		buffer.AsSpan(0, 1024 - 3).Trim((Byte)1).Length.Should().Be(0);
		s.ReadByte().Should().Be(43);

		s.Seek(1, SeekOrigin.Begin);
		bytesRead = s.Read(buffer.AsSpan(1, 1024 - 2));
		bytesRead.Should().Be(1024 - 2);
		buffer.AsSpan(1, 1024 - 2).Trim((Byte)1).Length.Should().Be(0);

		s.Seek(-10, SeekOrigin.End);
		bytesRead = s.Read(buffer.AsSpan());
		bytesRead.Should().Be(10);
		
		bytesRead = s.Read(buffer.AsSpan());
		bytesRead.Should().Be(0);
		
		bytesRead = s.Read(buffer, 0, 1024);
		bytesRead.Should().Be(0);
	}
	
	[Test]
	public async Task CanReadAsync() {
		await using Stream s = ConstructStream();
		s.ReadByte().Should().Be(42);
		Byte[] buffer = new Byte[1024];
		Int32 bytesRead = await s.ReadAsync(buffer, 0, 1024 - 2);
		bytesRead.Should().Be(1024 - 2);
		buffer.AsSpan(0, 1024 - 2).Trim((Byte)1).Length.Should().Be(0);
		s.ReadByte().Should().Be(43);

		s.Seek(1, SeekOrigin.Begin);
		bytesRead = await s.ReadAsync(buffer.AsMemory(1, 1024 - 2));
		bytesRead.Should().Be(1024 - 2);
		buffer.AsSpan(1, 1024 - 2).Trim((Byte)1).Length.Should().Be(0);
		
		s.Seek(-10, SeekOrigin.End);
		bytesRead = await s.ReadAsync(buffer.AsMemory());
		bytesRead.Should().Be(10);

		bytesRead = await s.ReadAsync(buffer.AsMemory());
		bytesRead.Should().Be(0);
		
		bytesRead = await s.ReadAsync(buffer, 0, 1024);
		bytesRead.Should().Be(0);
		
		s.ReadByte().Should().Be(-1);
	}
}