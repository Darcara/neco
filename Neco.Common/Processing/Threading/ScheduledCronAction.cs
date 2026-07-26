namespace Neco.Common.Processing.Threading;

internal sealed class ScheduledCronAction : AScheduledAction {
	private readonly Crontab _cron;

	/// <inheritdoc />
	public ScheduledCronAction(SimpleScheduler scheduler, TimeProvider time, String name, Crontab cron, SyncOrAsyncAction theTask, TimeSpan waitUntilFirstExecution) : base(scheduler, time, name, theTask, waitUntilFirstExecution) {
		_cron = cron;
	}

	#region Overrides of AScheduledAction

	/// <inheritdoc />
	public override Int64 CalculateNextExecutionTimestamp() {
		if (NextOverride > TimeSpan.Zero)
			return LastExecutionFinishedTimestamp + TicksFromTimespan(NextOverride);

		DateTime next = _cron.CalculateNextOccurrence(LastExecutionFinished.UtcDateTime);
		return TicksFromTimespan(next - Time.GetUtcNow().UtcDateTime);
	}

	#endregion
}