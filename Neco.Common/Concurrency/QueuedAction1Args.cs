namespace Neco.Common.Concurrency;

using System;
using System.Threading.Tasks;

public sealed class QueuedAction1Args<TArg1> : IQueuedAction {
	private readonly Action<TArg1> _doMe;
	private readonly TArg1 _arg1;

	public QueuedAction1Args(Action<TArg1> doMe, TArg1 arg1) {
		_doMe = doMe;
		_arg1 = arg1;
	}

	#region Implementation of IQueuedAction

	/// <inheritdoc />
	public Task InvokeAsync() {
		_doMe(_arg1);
		return Task.CompletedTask;
	}

	#endregion
}