namespace Neco.Common.Concurrency;

using System;
using System.Threading.Tasks;

public sealed class QueuedAsyncAction2Args<TArg1, TArg2> : IQueuedAction {
	private readonly Func<TArg1, TArg2, Task> _doMe;
	private readonly TArg1 _arg1;
	private readonly TArg2 _arg2;

	public QueuedAsyncAction2Args(Func<TArg1, TArg2, Task> doMe, TArg1 arg1, TArg2 arg2) {
		_doMe = doMe;
		_arg1 = arg1;
		_arg2 = arg2;
	}

	#region Implementation of IQueuedAction

	/// <inheritdoc />
	public Task InvokeAsync() {
		return _doMe(_arg1, _arg2);
	}

	#endregion
}