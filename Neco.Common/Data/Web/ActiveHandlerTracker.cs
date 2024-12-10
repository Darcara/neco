namespace Neco.Common.Data.Web;

using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Strongly tracks a handler, while it is in use
/// </summary>
/// <param name="handler">The actual created handler, that is given out</param>
/// <param name="disposables">A list of <see cref="IDisposable"/> that must be disposed together with the handler</param>
/// <param name="lifetime">The time before this handler is disposed</param>
/// <param name="name">The logical name of the message handler</param>
/// <param name="id"></param>
internal sealed class ActiveHandlerTracker(LifetimeTrackingHttpMessageHandlerDecorator handler, List<IDisposable> disposables, TimeSpan lifetime, String name, Int64 id) {
	private static readonly TimerCallback _timerCallback = state => ((ActiveHandlerTracker)state!).ExpiryTimerCallback();

	public readonly LifetimeTrackingHttpMessageHandlerDecorator Handler = handler;
	public readonly List<IDisposable> Disposables = disposables;
	public readonly TimeSpan Lifetime = lifetime;
	public readonly String Name = name;
	public readonly Int64 Id = id;

	private Int32 _isTimerInitialized;
	private Action<ActiveHandlerTracker>? _onExpiryCallback;

	public Int64 ClientsCreated;
	private Timer? _expiryTimer;

	public void StartExpiry(Action<ActiveHandlerTracker> onExpiryCallback) {
		if (Volatile.Read(ref _isTimerInitialized) != 0) return;
		if (Lifetime <= TimeSpan.Zero || Lifetime == Timeout.InfiniteTimeSpan) return;
		
		if (Interlocked.CompareExchange(ref _isTimerInitialized, 1, 0) == 0) {
			// using var suppressedFlow = ExecutionContext.SuppressFlow();
			_onExpiryCallback = onExpiryCallback;
			_expiryTimer = new Timer(_timerCallback, this, Lifetime, Timeout.InfiniteTimeSpan);
		}
	}

	private void ExpiryTimerCallback() {
		_expiryTimer?.Dispose();
		_expiryTimer = null;

		Action<ActiveHandlerTracker>? cb = _onExpiryCallback;
		if (cb != null) {
			_onExpiryCallback = null;
			cb.Invoke(this);
		}
	}
}