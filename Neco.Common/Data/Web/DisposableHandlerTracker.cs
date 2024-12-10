namespace Neco.Common.Data.Web;

using System;
using System.Collections.Generic;
using System.Net.Http;

/// <summary>
/// Weakly tracks created message Handler that should be disposed
/// </summary>
internal readonly struct DisposableHandlerTracker : IDisposable {
	private readonly WeakReference _lifetimeReference;
	public readonly HttpMessageHandler Handler;
	public readonly Int64 HandlerId;
	public readonly String HandlerName;
	public readonly Int64 ClientsCreated;
	public readonly List<IDisposable> Disposables;

	public DisposableHandlerTracker(ActiveHandlerTracker activeHandlerTracker) {
		_lifetimeReference = new WeakReference(activeHandlerTracker.Handler);
		Handler = activeHandlerTracker.Handler.InnerHandler!;
		HandlerId = activeHandlerTracker.Id;
		HandlerName = activeHandlerTracker.Name;
		Disposables = activeHandlerTracker.Disposables;
		ClientsCreated = activeHandlerTracker.ClientsCreated;
	}

	public Boolean CanDispose => !_lifetimeReference.IsAlive;

	#region IDisposable

	/// <inheritdoc />
	public void Dispose() {
		Handler.Dispose();
		Disposables.ForEach(disposable => disposable.Dispose());
	}

	#endregion
}