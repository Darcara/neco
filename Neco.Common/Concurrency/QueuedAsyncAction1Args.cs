namespace Neco.Common.Concurrency;

using System;
using System.Threading.Tasks;

public sealed class QueuedAsyncAction1Args<TArg1> : IQueuedAction {
	private readonly Func<TArg1, Task> _doMe;
	private readonly TArg1 _arg1;

	public QueuedAsyncAction1Args(Func<TArg1, Task> doMe, TArg1 arg1) {
		_doMe = doMe;
		_arg1 = arg1;
	}

	#region Implementation of IQueuedAction

	/// <inheritdoc />
	public Task InvokeAsync() => _doMe(_arg1);

	#endregion
}