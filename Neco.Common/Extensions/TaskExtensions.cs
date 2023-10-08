namespace Neco.Common.Extensions;

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

public static class TaskExtensions {
	public static Task<TResult> Transform<TInput, TResult>(this Task<TInput> task, Func<TInput, TResult> mapping) {
		ArgumentNullException.ThrowIfNull(mapping);
		return task.ContinueWith(static (t, state) => ((Func<TInput, TResult>)state!).Invoke(t.Result), mapping, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion);
	}

	public static async ValueTask<TResult> Transform<TInput, TResult>(this ValueTask<TInput> task, Func<TInput, TResult> mapping) {
		ArgumentNullException.ThrowIfNull(mapping);
		TInput input = await task;
		return mapping.Invoke(input);
	}

	public static Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout) {
		if (task.IsCompleted || timeout < TimeSpan.Zero) return task;
		return Timeout(task, timeout).AsTask();
	}

	public static ValueTask<TResult> TimeoutAfter<TResult>(this ValueTask<TResult> task, TimeSpan timeout) {
		if (task.IsCompleted || timeout < TimeSpan.Zero) return task;
		return Timeout(task.AsTask(), timeout);
	}

	private static async ValueTask<TResult> Timeout<TResult>(Task<TResult> task, TimeSpan timeout) {
		using CancellationTokenSource timeoutCancellationTokenSource = new();
		Task completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
		if (completedTask == task) {
			timeoutCancellationTokenSource.Cancel();
			return await task;
		}

		throw new TimeoutException();
	}

	// see: https://stackoverflow.com/questions/72715689/valuetask-instances-should-not-have-their-result-directly-accessed-unless-the-in/72716573#72716573
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetResultBlocking(this in ValueTask valueTask) {
		if (valueTask.IsCompletedSuccessfully) {
			valueTask.GetAwaiter().GetResult();
			return;
		}

		valueTask.AsTask().GetAwaiter().GetResult();
	}

	// see: https://stackoverflow.com/questions/72715689/valuetask-instances-should-not-have-their-result-directly-accessed-unless-the-in/72716573#72716573
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T GetResultBlocking<T>(this in ValueTask<T> valueTask) {
		if (valueTask.IsCompletedSuccessfully) {
			return valueTask.GetAwaiter().GetResult();
		}

		return valueTask.AsTask().GetAwaiter().GetResult();
	}

	/// <remarks>Not required. Only provided as parity for ValueTask.GetResultBlocking</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetResultBlocking(this Task task) => task.GetAwaiter().GetResult();

	/// <remarks>Not required. Only provided as parity for ValueTask.GetResultBlocking</remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T GetResultBlocking<T>(this Task<T> task) => task.GetAwaiter().GetResult();
}