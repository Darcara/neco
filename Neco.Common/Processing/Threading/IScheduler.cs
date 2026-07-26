namespace Neco.Common.Processing.Threading;

/// <summary>
/// A generic scheduler
/// </summary>
public interface IScheduler : IDisposable {
	public event Action<IScheduled, Exception> OnUnhandledError;

	/// <summary>
	/// 
	/// </summary>
	/// <param name="action"></param>
	/// <param name="waitUntilFirstExecution">Period until the first execution</param>
	/// <param name="executionPeriod">Period begins after the scheduled task has executed</param>
	/// <param name="name"></param>
	/// <returns></returns>
	public IScheduled Schedule(String name, TimeSpan executionPeriod, SyncOrAsyncAction action, TimeSpan? waitUntilFirstExecution = null);
	public IScheduled Schedule(String name, Crontab cron, SyncOrAsyncAction action, TimeSpan? waitUntilFirstExecution = null);
	public IScheduled ExecuteOnceAt(String name, DateTime at, SyncOrAsyncAction action);
	public IScheduled ExecuteOnceIn(String name, TimeSpan inTime, SyncOrAsyncAction action);

	public void RemoveScheduledTask(IScheduled task);

}