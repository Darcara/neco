namespace Neco.Common.Processing.Threading;

internal sealed class SingleExecution : AScheduledAction {
	private readonly TimeSpan _executeIn;
	private readonly Int64 _initialTimestamp;

	/// <inheritdoc />
	public SingleExecution(SimpleScheduler scheduler, TimeProvider time, String name, TimeSpan executeIn, SyncOrAsyncAction theTask) : base(scheduler, time, name, theTask, TimeSpan.Zero) {
		_executeIn = executeIn;
		_initialTimestamp = LastExecutionFinishedTimestamp;
	}

	#region Overrides of AScheduledAction

	/// <inheritdoc />
	public override Int64 CalculateNextExecutionTimestamp() {
		if (_initialTimestamp != LastExecutionFinishedTimestamp) return Int64.MaxValue;
		
		Int64 nextExecution = LastExecutionFinishedTimestamp + TicksFromTimespan(_executeIn);
		return nextExecution;
	}

	#endregion
}