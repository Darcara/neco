namespace Neco.Common.Processing.Threading;

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// TODO Execution groups for consecutively / concurrent processing
[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
public sealed class SimpleScheduler : IScheduler {
	private readonly CancellationTokenSource _cts = new();
	private readonly PriorityQueue<IScheduledAction, Int64> _scheduledActions = new();
	private readonly TimeProvider _timeProvider;
	private readonly ILogger<SimpleScheduler> _logger;

	private static readonly Meter _meter = new("Neco.SimpleScheduler", "1.0.0");
	private static readonly Counter<Int64> _metricErrors = _meter.CreateCounter<Int64>(_meter.Name.ToLowerInvariant() + ".errors");
	private static readonly UpDownCounter<Int32> _metricScheduled = _meter.CreateUpDownCounter<Int32>(_meter.Name.ToLowerInvariant() + ".scheduled");
	private static readonly UpDownCounter<Int32> _metricExecuting = _meter.CreateUpDownCounter<Int32>(_meter.Name.ToLowerInvariant() + ".executing");

	public SimpleScheduler(TimeProvider timeProvider, ILogger<SimpleScheduler> logger) {
		_timeProvider = timeProvider;
		_logger = logger;
		_ = Task.Run(ScheduleActions, _cts.Token);
	}

	private IScheduled AddScheduledTask(IScheduledAction scheduled) {
		ObjectDisposedException.ThrowIf(_cts.IsCancellationRequested, this);
		Int64 nextExecution = scheduled.CalculateNextExecutionTimestamp();
		lock (_scheduledActions) {
			_scheduledActions.Enqueue(scheduled, nextExecution);
		}
		_logger.LogDebug("Scheduled action added {ActionName}@{ActionNextExecutionTime}. There are now {ScheduledActionsCount} actions sheduled", scheduled.Name, _timeProvider.GetUtcNow() + _timeProvider.GetElapsedTime(_timeProvider.GetTimestamp(), nextExecution), _scheduledActions.Count);
		_metricScheduled.Add(1);
		return scheduled.Subscription;
	}

	internal void RemoveAction(IScheduledAction actionToRemove) {
		lock (_scheduledActions) {
			if (_scheduledActions.Remove(actionToRemove, out _, out _)) {
				_logger.LogDebug("Scheduled action removed {ActionName}. There are now {ScheduledActionsCount} actions sheduled", actionToRemove.Name, _scheduledActions.Count);
				_metricScheduled.Add(-1);
			}
		}
	}

	private void FireUnhandledError(IScheduled action, Exception ex) => OnUnhandledError?.Invoke(action, ex);

	private async Task ScheduleActions() {
		CancellationToken token = _cts.Token;

		while (!token.IsCancellationRequested) {
			await Task.Delay(100, token).ConfigureAwait(false);
			Boolean anyDequeued;
			do {
				anyDequeued = false;
				IScheduledAction? task;
				lock (_scheduledActions) {
					if (!_scheduledActions.TryPeek(out IScheduledAction? maybeAction, out Int64 currentPriority)) continue;
					Int64 nextPriority = maybeAction.CalculateNextExecutionTimestamp();
					if (nextPriority > _timeProvider.GetTimestamp()) {
						// reschedule if difference is greater than one millisecond
						if (Math.Abs(nextPriority - currentPriority) > _timeProvider.TimestampFrequency / 1000) {
							_scheduledActions.DequeueEnqueue(maybeAction, nextPriority);
							// order might have changed 
							anyDequeued = true;
						}

						continue;
					}

					if (!_scheduledActions.TryDequeue(out task, out _)) continue;
				}

				_ = Task.Run(() => ExecuteAction(task, token), token);
				anyDequeued = true;
			} while (anyDequeued && !token.IsCancellationRequested);
		}
	}

	private async Task ExecuteAction(IScheduledAction action, CancellationToken token) {
		_metricExecuting.Add(1);
		try {
			action.BeforeExecution();
			if (action.ActionToExecute.IsAsync)
				await action.ActionToExecute.InvokeAsync().ConfigureAwait(false);
			else
#pragma warning disable CA1849
				action.ActionToExecute.Invoke();
#pragma warning restore CA1849
		}
		catch (SchedulingExecutionException see) {
			if (see.Retry < TimeSpan.Zero || see.Retry == TimeSpan.MaxValue) {
				_metricErrors.Add(1);
				FireUnhandledError(action.Subscription, see);
			} else {
				action.OverrideNextOnce(see.Retry);
			}
		}
		catch (Exception ex) {
			_metricErrors.Add(1);
			FireUnhandledError(action.Subscription, ex);
		}
		finally {
			_metricExecuting.Add(-1);
			action.AfterExecution();
			if (!token.IsCancellationRequested) {
				Int64 timeUntilNextExecution = action.CalculateNextExecutionTimestamp();
				if (timeUntilNextExecution > 0 && timeUntilNextExecution < Int64.MaxValue) {
					lock (_scheduledActions) {
						_scheduledActions.Enqueue(action, timeUntilNextExecution);
					}
				} else {
					_logger.LogDebug("Removing scheduled action {ActionName}, next execution time out of bounds", action.Name);
					action.Subscription.Dispose();
				}
			}
		}
	}

	#region Implementation of IScheduler

	/// <inheritdoc />
	public event Action<IScheduled, Exception>? OnUnhandledError;

	/// <inheritdoc />
	public IScheduled Schedule(String name, TimeSpan executionPeriod, SyncOrAsyncAction action, TimeSpan? waitUntilFirstExecution = null) {
		TimeSpan initialWait = waitUntilFirstExecution == null || waitUntilFirstExecution < TimeSpan.Zero ? TimeSpan.Zero : waitUntilFirstExecution.Value;
		IScheduledAction scheduled = new ScheduledFixedRateAction(this, _timeProvider, name, executionPeriod, action, initialWait);
		return AddScheduledTask(scheduled);
	}

	/// <inheritdoc />
	public IScheduled Schedule(String name, Crontab cron, SyncOrAsyncAction action, TimeSpan? waitUntilFirstExecution = null) {
		TimeSpan initialWait = waitUntilFirstExecution == null || waitUntilFirstExecution < TimeSpan.Zero ? TimeSpan.Zero : waitUntilFirstExecution.Value;
		IScheduledAction scheduled = new ScheduledCronAction(this, _timeProvider, name, cron, action, initialWait);
		return AddScheduledTask(scheduled);
	}

	/// <inheritdoc />
	public IScheduled ExecuteOnceAt(String name, DateTime at, SyncOrAsyncAction action) {
		return AddScheduledTask(new SingleExecution(this, _timeProvider, name, at - _timeProvider.GetUtcNow(), action));
	}

	/// <inheritdoc />
	public IScheduled ExecuteOnceIn(String name, TimeSpan inTime, SyncOrAsyncAction action) {
		return AddScheduledTask(new SingleExecution(this, _timeProvider, name, inTime, action));
	}

	/// <inheritdoc />
	public void RemoveScheduledTask(IScheduled task) {
		ArgumentNullException.ThrowIfNull(task);
		task.Dispose();
	}

	#endregion

	#region IDisposable

	/// <inheritdoc />
	public void Dispose() {
		_cts.Cancel();
		_cts.Dispose();
		lock (_scheduledActions) {
			_scheduledActions.Clear();
		}
	}

	#endregion
}