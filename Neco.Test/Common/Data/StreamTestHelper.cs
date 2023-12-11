namespace Neco.Test.Common.Data;

using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;

public static class StreamTestHelper {
	public static void CanReadSeak(Stream s) {
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
}