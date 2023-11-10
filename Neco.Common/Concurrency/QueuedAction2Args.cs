namespace Neco.Common.Concurrency;

using System;
using System.Threading.Tasks;

internal sealed class QueuedAction2Args<TArg1, TArg2> : IQueuedAction {
	private readonly Action<TArg1, TArg2> _doMe;
	private readonly TArg1 _arg1;
	private readonly TArg2 _arg2;

	public QueuedAction2Args(Action<TArg1, TArg2> doMe, TArg1 arg1, TArg2 arg2) {
		_doMe = doMe;
		_arg1 = arg1;
		_arg2 = arg2;
	}

	#region Implementation of IQueuedAction

	/// <inheritdoc />
	public Task InvokeAsync() {
		_doMe(_arg1, _arg2);
		return Task.CompletedTask;
	}

	#endregion
}