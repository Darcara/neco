// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Neco.Common.ObjectMethodExecutor;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Provides a common awaitable structure that <see cref="ObjectMethodExecutor.ExecuteAsync"/> can
/// return, regardless of whether the underlying value is a System.Task, an FSharpAsync, or an
/// application-defined custom awaitable.
/// </summary>
public readonly struct ObjectMethodExecutorAwaitable {
	private readonly Object _customAwaitable;
	private readonly Func<Object, Object> _getAwaiterMethod;
	private readonly Func<Object, Boolean> _isCompletedMethod;
	private readonly Func<Object, Object> _getResultMethod;
	private readonly Action<Object, Action> _onCompletedMethod;
	private readonly Action<Object, Action> _unsafeOnCompletedMethod;

	// Perf note: since we're requiring the customAwaitable to be supplied here as an object,
	// this will trigger a further allocation if it was a value type (i.e., to box it). We can't
	// fix this by making the customAwaitable type generic, because the calling code typically
	// does not know the type of the awaitable/awaiter at compile-time anyway.
	//
	// However, we could fix it by not passing the customAwaitable here at all, and instead
	// passing a func that maps directly from the target object (e.g., controller instance),
	// target method (e.g., action method info), and params array to the custom awaiter in the
	// GetAwaiter() method below. In effect, by delaying the actual method call until the
	// upstream code calls GetAwaiter on this ObjectMethodExecutorAwaitable instance.
	// This optimization is not currently implemented because:
	// [1] It would make no difference when the awaitable was an object type, which is
	//     by far the most common scenario (e.g., System.Task<T>).
	// [2] It would be complex - we'd need some kind of object pool to track all the parameter
	//     arrays until we needed to use them in GetAwaiter().
	// We can reconsider this in the future if there's a need to optimize for ValueTask<T>
	// or other value-typed awaitables.

	public ObjectMethodExecutorAwaitable(
		Object customAwaitable,
		Func<Object, Object> getAwaiterMethod,
		Func<Object, Boolean> isCompletedMethod,
		Func<Object, Object> getResultMethod,
		Action<Object, Action> onCompletedMethod,
		Action<Object, Action> unsafeOnCompletedMethod) {
		_customAwaitable = customAwaitable;
		_getAwaiterMethod = getAwaiterMethod;
		_isCompletedMethod = isCompletedMethod;
		_getResultMethod = getResultMethod;
		_onCompletedMethod = onCompletedMethod;
		_unsafeOnCompletedMethod = unsafeOnCompletedMethod;
	}

	public Awaiter GetAwaiter() {
		Object customAwaiter = _getAwaiterMethod(_customAwaitable);
		return new Awaiter(customAwaiter, _isCompletedMethod, _getResultMethod, _onCompletedMethod, _unsafeOnCompletedMethod);
	}

	public readonly struct Awaiter : ICriticalNotifyCompletion {
		private readonly Object _customAwaiter;
		private readonly Func<Object, Boolean> _isCompletedMethod;
		private readonly Func<Object, Object> _getResultMethod;
		private readonly Action<Object, Action> _onCompletedMethod;
		private readonly Action<Object, Action> _unsafeOnCompletedMethod;

		public Awaiter(
			Object customAwaiter,
			Func<Object, Boolean> isCompletedMethod,
			Func<Object, Object> getResultMethod,
			Action<Object, Action> onCompletedMethod,
			Action<Object, Action> unsafeOnCompletedMethod) {
			_customAwaiter = customAwaiter;
			_isCompletedMethod = isCompletedMethod;
			_getResultMethod = getResultMethod;
			_onCompletedMethod = onCompletedMethod;
			_unsafeOnCompletedMethod = unsafeOnCompletedMethod;
		}

		public Boolean IsCompleted => _isCompletedMethod(_customAwaiter);

		public Object GetResult() => _getResultMethod(_customAwaiter);

		public void OnCompleted(Action continuation) {
			_onCompletedMethod(_customAwaiter, continuation);
		}

		public void UnsafeOnCompleted(Action continuation) {
			// If the underlying awaitable implements ICriticalNotifyCompletion, use its UnsafeOnCompleted.
			// If not, fall back on using its OnCompleted.
			//
			// Why this is safe:
			// - Implementing ICriticalNotifyCompletion is a way of saying the caller can choose whether it
			//   needs the execution context to be preserved (which it signals by calling OnCompleted), or
			//   that it doesn't (which it signals by calling UnsafeOnCompleted). Obviously it's faster *not*
			//   to preserve and restore the context, so we prefer that where possible.
			// - If a caller doesn't need the execution context to be preserved and hence calls UnsafeOnCompleted,
			//   there's no harm in preserving it anyway - it's just a bit of wasted cost. That's what will happen
			//   if a caller sees that the proxy implements ICriticalNotifyCompletion but the proxy chooses to
			//   pass the call on to the underlying awaitable's OnCompleted method.

			Action<Object, Action> underlyingMethodToUse = _unsafeOnCompletedMethod ?? _onCompletedMethod;
			underlyingMethodToUse(_customAwaiter, continuation);
		}
	}
}