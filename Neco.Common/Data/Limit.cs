namespace Neco.Common.Data;

using System.Diagnostics;

/// <summary>
/// Action-Wrapper to limit the executons of often-called Actions
/// </summary>
public static class Limit {
	
	/// <summary>
	/// Executes an <see cref="Action"/> exactly once.
	/// </summary>
	/// <param name="a">The action to execute</param>
	/// <returns>A wrapper action that will execute the original action only once when called the first time</returns>
	public static Action Once(Action a) {
		Boolean hasBeenCalled = false;
		return () => {
			if (hasBeenCalled) return;
			hasBeenCalled = true;
			a();
		};
	}

	/// <summary>
	/// Executes an <see cref="Action"/> exactly once.
	/// </summary>
	/// <param name="a">The action to execute</param>
	/// <param name="arg">An argument for the action</param>
	/// <returns>A wrapper action that will execute the original action only once when called the first time</returns>
	public static Action Once<T>(Action<T> a, T arg) {
		Boolean hasBeenCalled = false;
		return () => {
			if (hasBeenCalled) return;
			hasBeenCalled = true;
			a(arg);
		};
	}

	/// <summary>
	/// Executes an <see cref="Action"/> exactly once.
	/// </summary>
	/// <param name="a">The action to execute</param>
	/// <param name="arg1">The first argument for the action</param>
	/// <param name="arg2">The second argument for the action</param>
	/// <returns>A wrapper action that will execute the original action only once when called the first time</returns>
	public static Action Once<TArg1, TArg2>(Action<TArg1, TArg2> a, TArg1 arg1, TArg2 arg2) {
		Boolean hasBeenCalled = false;
		return () => {
			if (hasBeenCalled) return;
			hasBeenCalled = true;
			a(arg1, arg2);
		};
	}

	/// <summary>
	/// Executes an <see cref="Action"/> once when called initially and then only when called after the delay has elapsed.
	/// </summary>
	/// <param name="delay">The time between invocations</param>
	/// <param name="a">The action to execute</param>
	/// <returns>A wrapper action that will execute the original action</returns>
	public static Action Every(TimeSpan delay, Action a) {
		Int64 lastCall = 0;
		return () => {
			if (Stopwatch.GetElapsedTime(lastCall) < delay) return;
			lastCall = Stopwatch.GetTimestamp();
			a();
		};
	}

	/// <summary>
	/// Executes an <see cref="Action"/> once when called initially and then only when called after the delay has elapsed.
	/// </summary>
	/// <param name="delay">The time between invocations</param>
	/// <param name="a">The action to execute</param>
	/// <param name="arg">An argument for the action</param>
	/// <returns>A wrapper action that will execute the original action</returns>
	public static Action Every<T>(TimeSpan delay, Action<T> a, T arg) {
		Int64 lastCall = 0;
		return () => {
			if (Stopwatch.GetElapsedTime(lastCall) < delay) return;
			lastCall = Stopwatch.GetTimestamp();
			a(arg);
		};
	}
	
	/// <summary>
	/// Executes an <see cref="Action"/> once when called initially and then only when called after the delay has elapsed.
	/// </summary>
	/// <param name="delay">The time between invocations</param>
	/// <param name="a">The action to execute</param>
	/// <param name="arg1">The first argument for the action</param>
	/// <param name="arg2">The second argument for the action</param>
	/// <returns>A wrapper action that will execute the original action</returns>
	public static Action Every<TArg1, TArg2>(TimeSpan delay, Action<TArg1, TArg2> a, TArg1 arg1, TArg2 arg2) {
		Int64 lastCall = 0;
		return () => {
			if (Stopwatch.GetElapsedTime(lastCall) < delay) return;
			lastCall = Stopwatch.GetTimestamp();
			a(arg1, arg2);
		};
	}
}