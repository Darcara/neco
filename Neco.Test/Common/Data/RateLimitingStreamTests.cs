namespace Neco.Test.Common.Data;

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using FluentAssertions;
using Neco.Common.Data;
using Neco.Common.Extensions;
using Neco.Test.Mocks;
using NUnit.Framework;

[TestFixture]
public class RateLimitingStreamTests {
	private static (RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) Create() {
		TokenBucketRateLimiterOptions options = new() {
			AutoReplenishment = false,
			QueueLimit = 10,
			ReplenishmentPeriod = TimeSpan.FromMilliseconds(1),
			TokenLimit = 10,
			TokensPerPeriod = 1,
			QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
		};
		TokenBucketRateLimiter rateLimiter = new(options);
		Byte[] innerStreamData = Enumerable.Range(0, 256).Select(i => (Byte)i).ToArray();
		MemoryStream innerStream = new(innerStreamData);
		RateLimitedStream stream = new(innerStream, rateLimiter, rateLimiter, disposeStream: true, disposeReadRateLimiter: true, disposeWriteRateLimiter: true);
		return (stream, rateLimiter);
	}

	[Test]
	[SuppressMessage("ReSharper", "MustUseReturnValue")]
	[SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
	public void InterfaceTests() {
		Byte[] buffer = new Byte[1024];
		(RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) = Create();

		stream.ReadByte().Should().Be(0);
		stream.Read(Array.Empty<Byte>(), 0, 0).Should().Be(0);
		stream.Read(Span<Byte>.Empty).Should().Be(0);
		stream.ReadAsync(Array.Empty<Byte>(), 0, 0).GetResultBlocking().Should().Be(0);
		stream.ReadAsync(Memory<Byte>.Empty).GetResultBlocking().Should().Be(0);
		// Requesting 1024 bytes, but only 10 max in rate limiter
		Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(buffer));
		
		// Read rest of tokens
		stream.Read(buffer.AsSpan(0, 9));
		// Queue max read
		ValueTask<Int32> readTask1 = stream.ReadAsync(buffer.AsMemory(0, 10));
		readTask1.IsCompletedSuccessfully.Should().BeFalse();
		// Will fail because QueueLength=10 is exausted
		ValueTask<Int32> readTask2 = stream.ReadAsync(buffer.AsMemory(0, 10));
		readTask2.IsCompletedSuccessfully.Should().BeTrue();
		readTask2.GetAwaiter().GetResult().Should().Be(0);
		Thread.Sleep(rateLimiter.ReplenishmentPeriod);
		rateLimiter.TryReplenish();
		Assert.That(() => readTask1.IsCompletedSuccessfully, Is.True.After(100, 1));

		Assert.DoesNotThrow(() => stream.Flush());
		Assert.DoesNotThrowAsync(() => stream.FlushAsync());

		Assert.DoesNotThrow(() => stream.Write(Array.Empty<Byte>(), 0, 0));
		Assert.DoesNotThrow(() => stream.Write(Span<Byte>.Empty));
		Assert.DoesNotThrowAsync(() => stream.WriteAsync(Array.Empty<Byte>(), 0, 0));
		Assert.DoesNotThrowAsync(() => stream.WriteAsync(Memory<Byte>.Empty).AsTask());
		Assert.Throws<NotSupportedException>(() => stream.BeginWrite(Array.Empty<Byte>(), 0, 0, null, null));
		Assert.Throws<NotSupportedException>(() => stream.EndWrite(null!));
		Assert.Throws<NotSupportedException>(() => stream.BeginRead(Array.Empty<Byte>(), 0, 0, null, null));
		Assert.Throws<NotSupportedException>(() => stream.EndRead(null!));

		stream.Length.Should().Be(256);
		stream.Position.Should().Be(20); // we read 1+9+10 bytes earlier
		stream.CanRead.Should().BeTrue();
		stream.CanWrite.Should().BeTrue();
		stream.CanSeek.Should().BeTrue();
		stream.Seek(0, SeekOrigin.Begin);
		stream.Position.Should().Be(0);
		rateLimiter.TryReplenish();
		stream.ReadByte().Should().Be(0);
		stream.Position.Should().Be(1);
		stream.Position = 0;
		stream.Position.Should().Be(0);

		stream.SetLength(64);
		stream.Length.Should().Be(64);


		Assert.Throws<ArgumentNullException>(() => new RateLimitedStream(null!, null));
		Assert.Throws<ArgumentOutOfRangeException>(() => new RateLimitedStream(stream, null, blockSize:-1));
		
		stream.Dispose();
		rateLimiter.Dispose();
	}

	[Test]
	public void RateLimitsAsyncReadMemoryAsync() {
		Byte[] buffer = new Byte[1024];
		(RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) = Create();

		ValueTask task = stream.ReadExactlyAsync(buffer.AsMemory(0, 10));
		Assert.That(() => task.IsCompletedSuccessfully, Is.True.After(1000, 50));
		task = stream.ReadExactlyAsync(buffer.AsMemory(0, 10));
		task.IsCompletedSuccessfully.Should().BeFalse();
		Assert.That(() => {
			rateLimiter.TryReplenish();
			return task.IsCompletedSuccessfully;
		}, Is.True.After(1000, 50));
	}

	[Test]
	public void RateLimitsAsyncReadByteArray() {
		Byte[] buffer = new Byte[1024];
		(RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) = Create();

		Task<int> task = stream.ReadAsync(buffer, 0, 10);
		Assert.That(() => task.IsCompletedSuccessfully, Is.True.After(1000, 50));
		task.Result.Should().Be(10);

		task = stream.ReadAsync(buffer, 0, 10);
		task.IsCompletedSuccessfully.Should().BeFalse();
		Assert.That(() => {
			rateLimiter.TryReplenish();
			return task.IsCompletedSuccessfully;
		}, Is.True.After(1000, 50));
	}

	[Test]
	public void RateLimitsSyncReadByteArray() {
		Byte[] buffer = new Byte[1024];
		(RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) = Create();
		Int32 bytesRead = stream.Read(buffer, 0, 10);
		bytesRead.Should().Be(10);

		Task<Int32> task = Task.Run(() => stream.Read(buffer, 0, 10));
		task.IsCompletedSuccessfully.Should().BeFalse();
		Assert.That(() => {
			rateLimiter.TryReplenish();
			return task.IsCompletedSuccessfully;
		}, Is.True.After(1000, 50));
	}

	[Test]
	public void RateLimitsSyncReadSpan() {
		Byte[] buffer = new Byte[1024];
		(RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) = Create();
		Int32 bytesRead = stream.Read(buffer.AsSpan(0, 10));
		bytesRead.Should().Be(10);

		Task<Int32> task = Task.Run(() => stream.Read(buffer.AsSpan(0, 10)));
		task.IsCompletedSuccessfully.Should().BeFalse();
		Assert.That(() => {
			rateLimiter.TryReplenish();
			return task.IsCompletedSuccessfully;
		}, Is.True.After(1000, 50));
	}

	[Test]
	public void RateLimitsSyncReadByte() {
		(RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) = Create();
		for (int i = 0; i < 10; ++i) {
			stream.ReadByte().Should().BeInRange(0, 255);
		}

		Task<Int32> task = Task.Run(() => stream.ReadByte());
		task.IsCompletedSuccessfully.Should().BeFalse();
		Assert.That(() => {
			rateLimiter.Dispose();
			return task.IsCompletedSuccessfully ? task.Result : -100;
		}, Is.EqualTo(-1).After(1000, 50));
	}

	[Test]
	public void RateLimitsSyncWriteMemoryAsync() {
		Byte[] buffer = new Byte[1024];
		(RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) = Create();
		stream.Write(buffer, 0, 10);

		ValueTask task = stream.WriteAsync(buffer.AsMemory(0, 10), CancellationToken.None);
		task.IsCompletedSuccessfully.Should().BeFalse();
		Assert.That(() => {
			rateLimiter.TryReplenish();
			return task.IsCompletedSuccessfully;
		}, Is.True.After(1000, 50));
	}

	[Test]
	public void RateLimitsSyncWriteByteArrayAsync() {
		Byte[] buffer = new Byte[1024];
		(RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) = Create();
		stream.Write(buffer, 0, 10);

		Task task = stream.WriteAsync(buffer, 0, 10, CancellationToken.None);
		task.IsCompletedSuccessfully.Should().BeFalse();
		Assert.That(() => {
			rateLimiter.TryReplenish();
			return task.IsCompletedSuccessfully;
		}, Is.True.After(1000, 50));
	}

	[Test]
	public void RateLimitsSyncWriteByteArray() {
		Byte[] buffer = new Byte[1024];
		(RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) = Create();
		stream.Write(buffer, 0, 10);

		Task task = Task.Run(() => stream.Write(buffer, 0, 10));
		task.IsCompletedSuccessfully.Should().BeFalse();
		Assert.That(() => {
			rateLimiter.TryReplenish();
			return task.IsCompletedSuccessfully;
		}, Is.True.After(1000, 50));
	}

	[Test]
	public void RateLimitsSyncWriteSpan() {
		Byte[] buffer = new Byte[1024];
		(RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) = Create();
		stream.Write(buffer.AsSpan(0, 10));

		Task task = Task.Run(() => stream.Write(buffer.AsSpan(0, 10)));
		task.IsCompletedSuccessfully.Should().BeFalse();
		Assert.That(() => {
			rateLimiter.TryReplenish();
			return task.IsCompletedSuccessfully;
		}, Is.True.After(1000, 50));
	}

	[Test]
	public void RateLimitsWriteByte() {
		(RateLimitedStream stream, TokenBucketRateLimiter rateLimiter) = Create();
		for (int i = 0; i < 10; ++i) {
			stream.WriteByte(42);
		}

		Task task = Task.Run(() => stream.WriteByte(5));
		task.IsCompletedSuccessfully.Should().BeFalse();
		Assert.That(() => {
			rateLimiter.Dispose();
			return task.IsCompletedSuccessfully;
		}, Is.True.After(1000, 50));
	}

	[Test]
	public void RateLimiterDisposesStream() {
		MemoryStream innerStream = new();
		innerStream.CanRead.Should().BeTrue();

		RateLimitedStream rs = new(innerStream, null, disposeStream: true, disposeReadRateLimiter: true, disposeWriteRateLimiter: true);
		rs.Dispose();
		innerStream.CanRead.Should().BeFalse();
	}

	[Test]
	public void RateLimiterDisposesOnlyOnce() {
		MemoryStream innerStream = new();
		innerStream.CanRead.Should().BeTrue();

		RateLimitedStream rs = new(innerStream, null, disposeStream: true, disposeReadRateLimiter: true, disposeWriteRateLimiter: true);
		rs.Dispose();

		RateLimiterMock readMock = new();

		rs = new(innerStream, readMock, readMock, disposeStream: true, disposeReadRateLimiter: true, disposeWriteRateLimiter: true);
		rs.Dispose();

		readMock.DisposeCount.Should().Be(1);
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void RateLimiterDisposesOnlySpecified(Boolean disposeRead, Boolean disposeWrite) {
		MemoryStream innerStream = new();
		innerStream.CanRead.Should().BeTrue();

		RateLimiterMock readMock = new();
		RateLimiterMock writeMock = new();
		RateLimitedStream rs = new(innerStream, readMock, writeMock, disposeStream: true, disposeReadRateLimiter: disposeRead, disposeWriteRateLimiter: disposeWrite);
		rs.Dispose();

		readMock.DisposeCount.Should().Be(disposeRead ? 1 : 0);
		writeMock.DisposeCount.Should().Be(disposeWrite ? 1 : 0);
	}
}