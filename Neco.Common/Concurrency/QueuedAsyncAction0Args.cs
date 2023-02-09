namespace Neco.Common.Concurrency;

using System;
using System.Threading.Tasks;

public sealed class QueuedAsyncAction0Args : IQueuedAction {
	private readonly Func<Task> _doMe;

	public QueuedAsyncAction0Args(Func<Task> doMe) {
		_doMe = doMe;
	}

	#region Implementation of IQueuedAction

	/// <inheritdoc />
	public Task InvokeAsync() => _doMe();

	#endregion
}