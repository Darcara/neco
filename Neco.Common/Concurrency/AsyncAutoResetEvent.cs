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
	/// <param name="cancellationToken">The cancellation token used to cancel this wait.</param>
	public Task WaitAsync(CancellationToken cancellationToken = default) {
		if (cancellationToken.IsCancellationRequested) return Task.FromCanceled(cancellationToken);
		lock (_mutex) {
			if (_isSet) {
				_isSet = false;
				return Task.CompletedTask;
			}

			TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
			Task ret = tcs.Task;
			if (cancellationToken.CanBeCanceled) {
				CancellationTokenRegistration reg = cancellationToken.Register(t => ((TaskCompletionSource)t!).TrySetCanceled(), tcs);
				ret.ContinueWith((_, registration) => ((CancellationTokenRegistration)registration!).Dispose(), reg, default(CancellationToken), TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
			}
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

			do {
				if (!_queue.TryDequeue(out TaskCompletionSource? tcs)) {
					return;
				}

				tcs.TrySetResult();
			} while (true);
		}
	}
}