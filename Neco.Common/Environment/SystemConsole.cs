namespace Neco.Common.Environment;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

public static class SystemConsole {
	private static Int64 _shutdownInvokeCount;

	/// <summary>
	/// TRUE if any of the shutdown-signals has been received
	/// </summary>
	public static Boolean IsShutdownInvoked => Interlocked.Read(ref _shutdownInvokeCount) > 0;

	private static readonly ConcurrentBag<Action> _shutdownHandlers = new();

	/// Registers event handlers for CTRL-C and similar constructs to callback in the event the user want to close the application through the console.
	[SuppressMessage("Reliability", "CA2000:Dispose Objects", Justification = "Lifetime of application")]
	public static void RegisterCtrlCHandler(Action action, Boolean cancelDefaultProcessingIfPossible = true) {
		lock (_shutdownHandlers) {
			_shutdownHandlers.Add(action);
		}

		// For console apps on windows and linux
		Console.CancelKeyPress += (a, b) => {
			b.Cancel = cancelDefaultProcessingIfPossible;

			if (Interlocked.Exchange(ref _shutdownInvokeCount, 1) == 0)
				action.Invoke();
		};

		// For NetCore
		AppDomain.CurrentDomain.ProcessExit += (sender, args) => {
			if (Interlocked.Exchange(ref _shutdownInvokeCount, 1) == 0)
				action.Invoke();
		};

		// Net Core on Linux?
		AssemblyLoadContext.Default.Unloading += _ => {
			if (Interlocked.Exchange(ref _shutdownInvokeCount, 1) == 0)
				action.Invoke();
		};

		// Posix Signals on Linux
		PosixSignalRegistration.Create(PosixSignal.SIGTERM, ctx => {
			ctx.Cancel = cancelDefaultProcessingIfPossible;
			if (Interlocked.Exchange(ref _shutdownInvokeCount, 1) == 0)
				action.Invoke();
		});
		
		PosixSignalRegistration.Create(PosixSignal.SIGQUIT, ctx => {
			ctx.Cancel = cancelDefaultProcessingIfPossible;
			if (Interlocked.Exchange(ref _shutdownInvokeCount, 1) == 0)
				action.Invoke();
		});
	}

	public static void SimulateCtrlC() {
		if (Interlocked.Exchange(ref _shutdownInvokeCount, 1) != 0) return;
		List<Exception>? exceptions = null;
		lock (_shutdownHandlers) {
			foreach (Action shutdownHandler in _shutdownHandlers) {
				try {
					shutdownHandler.Invoke();
				}
				catch (Exception e) {
					exceptions ??= [];
					exceptions.Add(e);
				}
			}
		}

		if (exceptions == null) return;
		throw new AggregateException("Failed to call one ore more shutdown handler", exceptions);
	}
}