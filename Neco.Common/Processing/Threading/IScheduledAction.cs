namespace Neco.Common.Processing.Threading;

internal interface IScheduledAction {
	public String Name { get; }

	public IScheduled Subscription { get; }
	
	public SyncOrAsyncAction ActionToExecute { get; }

	public void BeforeExecution();
	public void AfterExecution();
	
	public Int64 CalculateNextExecutionTimestamp();

	void OverrideNextOnce(TimeSpan timeSpan);
}