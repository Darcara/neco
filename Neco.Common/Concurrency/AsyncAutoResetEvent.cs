namespace Neco.Common.Concurrency;

using System.Threading;
using System.Threading.Tasks;

/// <inheritdoc cref="AutoResetEvent"/>
public sealed class AsyncAutoResetEvent {
	/// <summary>
	/// The queue of TCSs that other tasks are awaiting.
	/// </summary>
	private readonly Queue<TaskCompletionSource> _queue = new();

	/// <summary>
	/// The current state of the event.
	/// </summary>
	private Boolean _isSet;

	/// <summary>
	/// The object used for mutual exclusion.
	/// </summary>
	private readonly Lock _mutex = new();

	/// <summary>
	/// Creates an async-compatible auto-reset event.
	/// </summary>
	/// <param name="isSet">Whether the auto-reset event is initially set or unset.</param>
	public AsyncAutoResetEvent(Boolean isSet = false) {
		_isSet = isSet;
	}

	/// <summary>
	/// Asynchronously waits for this event to be set. If the event is set, this method will auto-reset it and return immediately, even if the cancellation token is already signalled. If the wait is canceled, then it will not auto-reset this event.
	/// </summary>
	public Task WaitAsync() => WaitAsync(Timeout.InfiniteTimeSpan, TimeProvider.System, CancellationToken.None);

	/// <inheritdoc cref="WaitAsync()"/>
	/// <param name="timeout"></param>
	/// <param name="timeProvider"></param>
	public Task WaitAsync(TimeSpan timeout, TimeProvider timeProvider) => WaitAsync(timeout, timeProvider, CancellationToken.None);

	/// <inheritdoc cref="WaitAsync()"/>
	/// <param name="cancellationToken">The cancellation token used to cancel this wait.</param>
	public Task WaitAsync(CancellationToken cancellationToken) => WaitAsync(Timeout.InfiniteTimeSpan, TimeProvider.System, cancellationToken);

	/// <inheritdoc cref="WaitAsync(CancellationToken)"/>
	/// <param name="timeout"></param>
	/// <param name="timeProvider"></param>
	/// <param name="cancellationToken">The cancellation token used to cancel this wait.</param>
	public Task WaitAsync(TimeSpan timeout, TimeProvider timeProvider, CancellationToken cancellationToken) {
		if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);
		lock (_mutex) {
			if (_isSet) {
				_isSet = false;
				return Task.CompletedTask;
			}

			TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
			Task ret = tcs.Task;
			CancellationTokenSource? cts = timeout > TimeSpan.Zero ? new CancellationTokenSource(timeout, timeProvider) : null;

			CancellationTokenRegistration regExternal = default;
			CancellationTokenRegistration regTimeout = default;

			if (cancellationToken.CanBeCanceled) {
				regExternal = cancellationToken.Register(t => ((TaskCompletionSource)t!).TrySetCanceled(cancellationToken), tcs);
			}

			if (cts != null) {
				regTimeout = cts.Token.Register(t => ((TaskCompletionSource)t!).TrySetCanceled(cts.Token), tcs);
			}

			_ = ret.ContinueWith(static (_, state) => {
				var (rExt, rTimeout, ctsLocal) = ((CancellationTokenRegistration, CancellationTokenRegistration, CancellationTokenSource?))state!;
				rExt.Dispose();
				rTimeout.Dispose();
				if (ctsLocal is not null) {
					ctsLocal.Cancel(false);
					ctsLocal.Dispose();
				}
			}, (regExternal, regTimeout, cts), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

			_queue.Enqueue(tcs);

			return ret;
		}
	}

	/// <summary>
	/// Sets the event, atomically completing a task returned by <see cref="WaitAsync(System.Threading.CancellationToken)"/>.
	/// </summary>
	public void Set() {
		lock (_mutex) {
			if (_queue.Count == 0) {
				_isSet = true;
				return;
			}

			// Dequeue waiting TCS until we find one that can be set
			while (_queue.TryDequeue(out TaskCompletionSource? tcs)) {
				if (tcs.TrySetResult()) return;
			}

			// Some TCS were in queue, but they were allready cancelled
			_isSet = true;
		}
	}
}