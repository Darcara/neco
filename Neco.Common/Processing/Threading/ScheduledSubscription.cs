namespace Neco.Common.Processing.Threading;

/// <inheritdoc />
public sealed class ScheduledSubscription : IScheduled {
	private readonly SimpleScheduler _owningScheduler;
	private readonly IScheduledAction _scheduledAction;

	/// <inheritdoc />
	public String Name => _scheduledAction.Name;

	internal ScheduledSubscription(SimpleScheduler owningScheduler, IScheduledAction scheduledAction) {
		_owningScheduler = owningScheduler;
		_scheduledAction = scheduledAction;
	}

	/// <inheritdoc />
	public void Dispose() {
		_owningScheduler.RemoveAction(_scheduledAction);
	}

	/// <inheritdoc />
	public override String ToString() => _scheduledAction.ToString()!;
}