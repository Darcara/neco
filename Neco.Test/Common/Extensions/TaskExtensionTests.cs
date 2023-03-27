namespace Neco.Test.Common.Extensions;

using System;
using System.Threading.Tasks;
using Neco.Common.Extensions;
using NUnit.Framework;

[TestFixture]
public class TaskExtensionTests {
	[Test]
	public async Task TransformTask() {
		String result = await Task.FromResult(42).Transform(i => i.ToString("X"));
		Assert.That(result, Is.EqualTo("2A"));
	}

	[Test]
	public async Task TransformValueTask() {
		String result = await ValueTask.FromResult(42).Transform(i => i.ToString("X"));
		Assert.That(result, Is.EqualTo("2A"));
	}

	[Test]
	public async Task TimeoutTask() {
		Int32 result = await Task.FromResult(42).TimeoutAfter(TimeSpan.FromSeconds(5));
		Assert.That(result, Is.EqualTo(42));
		Assert.That(await TestTask(100).TimeoutAfter(TimeSpan.FromSeconds(5)), Is.EqualTo(42));
		Assert.ThrowsAsync<TimeoutException>(() => TestTask(10000).AsTask().TimeoutAfter(TimeSpan.FromMilliseconds(100)));
	}

	[Test]
	public async Task TimeoutValueTask() {
		Int32 result = await ValueTask.FromResult(42).TimeoutAfter(TimeSpan.FromSeconds(5));
		Assert.That(result, Is.EqualTo(42));
		Assert.That(await TestTask(100).TimeoutAfter(TimeSpan.FromSeconds(5)), Is.EqualTo(42));
		try {
			await TestTask(10000).TimeoutAfter(TimeSpan.FromMilliseconds(100));
			Assert.Fail($"Expected {nameof(TimeoutException)}");
		}
		catch (TimeoutException) {
			Assert.Pass();
		}
	}

	private async ValueTask<Int32> TestTask(Int32 msDelay = 0) {
		await Task.Yield();
		if (msDelay > 0)
			await Task.Delay(msDelay);
		return 42;
	}
}