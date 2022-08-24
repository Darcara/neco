namespace Neco.Common.Concurrency;

using System;
using System.Threading.Tasks;

public sealed class QueuedAction0Args : IQueuedAction {
	private readonly Action _doMe;

	public QueuedAction0Args(Action doMe) {
		_doMe = doMe;
	}

	#region Implementation of IQueuedAction

	/// <inheritdoc />
	public Task InvokeAsync() {
		_doMe();
		return Task.CompletedTask;
	}

	#endregion
}