namespace Neco.Test.Common.Concurrency;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Neco.Common.Concurrency;

[TestFixture]
public class AsyncAutoResetEventTests {
	[Test]
	public async Task WaitAsyncAutoResets() {
		AsyncAutoResetEvent ev = new();
		ev.Set();

		// First wait completes immediately and resets the event.
		await ev.WaitAsync();

		// Subsequent wait should not complete until Set is called.
		Task waiter = ev.WaitAsync();
		Assert.That(waiter.IsCompleted, Is.False);

		ev.Set();
		await waiter;
	}

	[Test]
	public async Task SetReleasesOnlyOneWaiter() {
		AsyncAutoResetEvent ev = new();

		Task w1 = ev.WaitAsync();
		Task w2 = ev.WaitAsync();

		// Release one waiter.
		ev.Set();
		await Task.Yield(); // allow continuations to run
		Assert.That(w1.IsCompleted ^ w2.IsCompleted, Is.True, "Exactly one waiter should be completed");

		// Release the other.
		ev.Set();
		await Task.WhenAll(w1, w2);
	}

	[Test]
	public async Task WaitAsyncTimeoutCancels() {
		FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
		AsyncAutoResetEvent ev = new();

		Task waitTask = ev.WaitAsync(TimeSpan.FromSeconds(5), timeProvider);

		// advance past the timeout
		timeProvider.Advance(TimeSpan.FromSeconds(10));

		// allow any timers/continuations to run
		await Task.Yield();

		Assert.That(waitTask.IsCompleted, Is.True, "Task should be completed after timeout");
		Assert.That(waitTask.IsCanceled, Is.True, "Task should be canceled on timeout");
	}

	[Test]
	public async Task CancellationTokenCancels() {
		AsyncAutoResetEvent ev = new();
		using CancellationTokenSource cts = new();

		Task waitTask = ev.WaitAsync(cts.Token);
		Assert.That(waitTask.IsCompleted, Is.False);

		await cts.CancelAsync();
		await Task.Yield();

		Assert.That(waitTask.IsCompleted, Is.True);
		Assert.That(waitTask.IsCanceled, Is.True);
		
		ev.Set();
		Task waiter = ev.WaitAsync(CancellationToken.None);
		Assert.That(waiter.IsCompleted, Is.True);
		Assert.That(waiter.IsCompletedSuccessfully, Is.True);
	}

	[Test]
	public async Task TimeoutAndCancellationTokenCancelsOnToken() {
		FakeTimeProvider timeProvider = new(DateTimeOffset.UtcNow);
		AsyncAutoResetEvent ev = new();
		using CancellationTokenSource cts = new();

		Task waitTask = ev.WaitAsync(TimeSpan.FromSeconds(30), timeProvider, cts.Token);

		// cancel before the timeout fires
		await cts.CancelAsync();
		await Task.Yield();

		Assert.That(waitTask.IsCompleted, Is.True);
		Assert.That(waitTask.IsCanceled, Is.True);
	}
}