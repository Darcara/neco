namespace Neco.Common.Processing.Threading;

/// <summary>
/// Schedules invocations of a function by calculating the delay between two invocations from the END of the invocation
/// </summary>
internal sealed class ScheduledFixedRateAction : AScheduledAction {
	private readonly TimeSpan _executionPeriod;
	private readonly Int64 _executionPeriodTicks;

	public ScheduledFixedRateAction(SimpleScheduler scheduler, TimeProvider time, String name, TimeSpan executionPeriod, SyncOrAsyncAction theTask, TimeSpan waitUntilFirstExecution) : base(scheduler, time, name, theTask, waitUntilFirstExecution) {
		_executionPeriod = executionPeriod;
		_executionPeriodTicks = TicksFromTimespan(executionPeriod);
	}

	/// <inheritdoc />
	public override Int64 CalculateNextExecutionTimestamp() {
		Int64 nextExecution = LastExecutionFinishedTimestamp + _executionPeriodTicks;
		if(NextOverride > TimeSpan.Zero)
			nextExecution = LastExecutionFinishedTimestamp + TicksFromTimespan(NextOverride);
		return nextExecution;
	}
}