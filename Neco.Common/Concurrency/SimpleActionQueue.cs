namespace Neco.Common.Concurrency;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public sealed class SimpleActionQueue : IActionQueue {
	private readonly ILogger<SimpleActionQueue> _logger;
	private readonly Channel<IQueuedAction> _fileInfosToProcess = Channel.CreateUnbounded<IQueuedAction>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = false });
	private Int32 _numberOfItemsQueued;
	private TaskCompletionSource? _tcs;

	public SimpleActionQueue(ILogger<SimpleActionQueue> logger) {
		_logger = logger;
		Task.Factory.StartNew(QueueWorker, this);
	}

	private static async Task QueueWorker(Object? state) {
		SimpleActionQueue queue = state as SimpleActionQueue ?? throw new ArgumentException($"{nameof(QueueWorker)} expected {nameof(state)} to be {nameof(SimpleActionQueue)} but was {state?.GetType().FullName}", nameof(state));
		ChannelReader<IQueuedAction> reader = queue._fileInfosToProcess;
		ILogger<SimpleActionQueue> logger = queue._logger;
		await Task.Delay(100);
		await foreach (IQueuedAction queuedTask in reader.ReadAllAsync()) {
			var sw = Stopwatch.StartNew();
			try {
				await queuedTask.InvokeAsync();
			}
			catch (Exception e) {
				logger.LogError(e, "Failed to execute queued task");
			}

			logger.LogTrace("Task finished after {Elapsed}", sw.Elapsed);

			Int32 newNumberEnqueued = Interlocked.Decrement(ref queue._numberOfItemsQueued);
			if (newNumberEnqueued == 0 && queue._tcs != null) {
				lock (queue._fileInfosToProcess) {
					queue._tcs?.TrySetResult();
					queue._tcs = null;
				}
			}
		}
	}

	/// <inheritdoc />
	public Task WaitUntilEmpty() {
		if (_numberOfItemsQueued == 0) return Task.CompletedTask;
		lock (_fileInfosToProcess) {
			_tcs ??= new TaskCompletionSource();
			return _tcs.Task;
		}
	}

	/// <inheritdoc />
	public void Enqueue(Action doMe) {
		Interlocked.Increment(ref _numberOfItemsQueued);
		_fileInfosToProcess.Writer.TryWrite(new QueuedAction0Args(doMe));
	}

	/// <inheritdoc />
	public void Enqueue(Func<Task> doMe) {
		Interlocked.Increment(ref _numberOfItemsQueued);
		_fileInfosToProcess.Writer.TryWrite(new QueuedAsyncAction0Args(doMe));
	}

	/// <inheritdoc />
	public void Enqueue<TArg1>(Action<TArg1> doMe, TArg1 arg1) {
		Interlocked.Increment(ref _numberOfItemsQueued);
		_fileInfosToProcess.Writer.TryWrite(new QueuedAction1Args<TArg1>(doMe, arg1));
	}

	/// <inheritdoc />
	public void Enqueue<TArg1>(Func<TArg1, Task> doMe, TArg1 arg1) {
		Interlocked.Increment(ref _numberOfItemsQueued);
		_fileInfosToProcess.Writer.TryWrite(new QueuedAsyncAction1Args<TArg1>(doMe, arg1));
	}

	/// <inheritdoc />
	public void Enqueue<TArg1, TArg2>(Action<TArg1, TArg2> doMe, TArg1 arg1, TArg2 arg2) {
		Interlocked.Increment(ref _numberOfItemsQueued);
		_fileInfosToProcess.Writer.TryWrite(new QueuedAction2Args<TArg1, TArg2>(doMe, arg1, arg2));
	}

	/// <inheritdoc />
	public void Enqueue<TArg1, TArg2>(Func<TArg1, TArg2, Task> doMe, TArg1 arg1, TArg2 arg2) {
		Interlocked.Increment(ref _numberOfItemsQueued);
		_fileInfosToProcess.Writer.TryWrite(new QueuedAsyncAction2Args<TArg1, TArg2>(doMe, arg1, arg2));
	}

	#region IDisposable

	/// <inheritdoc />
	public void Dispose() {
		lock (_fileInfosToProcess) {
			_tcs?.TrySetResult();
			_tcs = null;
			_numberOfItemsQueued = 0;
		}

		_fileInfosToProcess.Writer.TryComplete();
	}

	#endregion
}