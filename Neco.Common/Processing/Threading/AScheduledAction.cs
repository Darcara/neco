namespace Neco.Common.Processing.Threading;

using System.Diagnostics;

[DebuggerDisplay("{" + nameof(Name) + "}")]
internal abstract class AScheduledAction : IScheduledAction {
	protected readonly SimpleScheduler Scheduler;
	protected readonly TimeProvider Time;
	protected Int64 LastExecutionStartTimestamp;
	protected DateTimeOffset LastExecutionStart;
	protected Int64 LastExecutionFinishedTimestamp;
	protected DateTimeOffset LastExecutionFinished;
	protected TimeSpan NextOverride { get; set; }

	/// <inheritdoc />
	public String Name { get; }

	/// <inheritdoc />
	public IScheduled Subscription { get; }

	/// <inheritdoc />
	public SyncOrAsyncAction ActionToExecute { get; }

	/// <inheritdoc />
	public void BeforeExecution() {
		LastExecutionStartTimestamp = Time.GetTimestamp();
		LastExecutionStart = Time.GetUtcNow();
		NextOverride = TimeSpan.Zero; 
	}

	/// <inheritdoc />
	public void AfterExecution() {
		LastExecutionFinishedTimestamp = Time.GetTimestamp();
		LastExecutionFinished = Time.GetUtcNow();
	}

	protected AScheduledAction(SimpleScheduler scheduler, TimeProvider time, String name, SyncOrAsyncAction action, TimeSpan waitUntilFirstExecution) {
		Name = name;
		Scheduler = scheduler;
		Time = time;
		ActionToExecute = action;
		NextOverride = waitUntilFirstExecution;
		Subscription = new ScheduledSubscription(scheduler, this);
		LastExecutionStartTimestamp = time.GetTimestamp();
		LastExecutionFinishedTimestamp = time.GetTimestamp();
	}
	
	/// <summary> Returns StopwatchTicks (as these are not TimeSpanTicks)</summary>
	protected Int64 TicksFromTimespan(TimeSpan t) => (Int64)(t.TotalSeconds * Time.TimestampFrequency);

	#region Implementation of IScheduledFunction

	/// <inheritdoc />
	public abstract Int64 CalculateNextExecutionTimestamp();

	/// <inheritdoc />
	public virtual void OverrideNextOnce(TimeSpan timeToNextExecution) {
		NextOverride = timeToNextExecution;
	}

	#endregion

	#region Overrides of Object

	public override String ToString() => Name;

	#endregion
}