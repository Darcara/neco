namespace Neco.Test.Common;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Neco.Common.Concurrency;
using Neco.Test.Mocks;
using NUnit.Framework;

[TestFixture]
public class ActionQueueTests : ATest {
	[Test]
	public async Task CompletedSync0Tasks() {
		using SimpleActionQueue aq = new(GetLogger<SimpleActionQueue>());
		Int32 i = 0;
		aq.Enqueue(() => Interlocked.Increment(ref i));
		await aq.WaitUntilEmpty();
		Assert.That(i, Is.EqualTo(1));
	}

	[Test]
	public async Task CompletedSync1Tasks() {
		using SimpleActionQueue aq = new(GetLogger<SimpleActionQueue>());
		List<Int32> container = new();
		aq.Enqueue(someContainer => someContainer.Add(42), container);
		await aq.WaitUntilEmpty();
		Assert.That(container, Has.Count.EqualTo(1));
		Assert.That(container, Has.ItemAt(0).EqualTo(42));
	}

	[Test]
	public async Task CompletedSync2Tasks() {
		using SimpleActionQueue aq = new(GetLogger<SimpleActionQueue>());
		List<Int32> container = new();
		aq.Enqueue((someContainer, someNumber) => someContainer.Add(someNumber), container, 55);
		await aq.WaitUntilEmpty();
		Assert.That(container, Has.Count.EqualTo(1));
		Assert.That(container, Has.ItemAt(0).EqualTo(55));
	}

	[Test]
	public async Task CompletedAsync0Tasks() {
		using SimpleActionQueue aq = new(GetLogger<SimpleActionQueue>());
		Int32 i = 0;
		aq.Enqueue(async () => {
			await Task.Yield();
			Interlocked.Increment(ref i);
		});
		await aq.WaitUntilEmpty();
		Assert.That(i, Is.EqualTo(1));
	}

	[Test]
	public async Task CompletedAsync1Tasks() {
		using SimpleActionQueue aq = new(GetLogger<SimpleActionQueue>());
		await using MemoryStream stream = new();
		aq.Enqueue(async (someStream) => {
			await someStream.WriteAsync(new Byte[]{42});
		}, stream);
		await aq.WaitUntilEmpty();
		stream.Position = 0;
		Assert.That(stream.Length, Is.EqualTo(1));
		Assert.That(stream.ReadByte(), Is.EqualTo(42));
	}

	[Test]
	public async Task CompletedASync2Tasks() {
		using SimpleActionQueue aq = new(GetLogger<SimpleActionQueue>());
		await using MemoryStream stream = new();
		aq.Enqueue(async (someStream, someByte) => {
			await someStream.WriteAsync(new Byte[]{someByte});
		}, stream, (Byte)55);
		await aq.WaitUntilEmpty();
		await aq.WaitUntilEmpty();
		stream.Position = 0;
		Assert.That(stream.Length, Is.EqualTo(1));
		Assert.That(stream.ReadByte(), Is.EqualTo(55));
	}

	[Test]
	public async Task CatchesAndLogsException() {
		Int32 numUncaughtExceptions = 0;
		TaskScheduler.UnobservedTaskException += (_, _) => numUncaughtExceptions += 1;

		LoggerMock<SimpleActionQueue> loggerMock = new(GetLogger<SimpleActionQueue>());
		using SimpleActionQueue aq = new(loggerMock);
		Int32 i = 0;
		aq.Enqueue((Action)(() => {
			Interlocked.Increment(ref i);
			if(true)throw new Exception("Exception from enqueued Task");
		}));
		await aq.WaitUntilEmpty();

		Assert.That(i, Is.EqualTo(1));
		Assert.That(numUncaughtExceptions, Is.Zero);
		Assert.That(loggerMock.NumberOfLogCalls, Is.EqualTo(2));
	}
}