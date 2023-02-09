// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Neco.Common.ObjectMethodExecutor;

using System;
using System.Linq.Expressions;

internal readonly struct CoercedAwaitableInfo {
	public AwaitableInfo AwaitableInfo { get; }
	public Expression CoercerExpression { get; }
	public Type CoercerResultType { get; }
	public Boolean RequiresCoercion => CoercerExpression != null;

	public CoercedAwaitableInfo(AwaitableInfo awaitableInfo) {
		AwaitableInfo = awaitableInfo;
		CoercerExpression = null;
		CoercerResultType = null;
	}

	public CoercedAwaitableInfo(Expression coercerExpression, Type coercerResultType, AwaitableInfo coercedAwaitableInfo) {
		CoercerExpression = coercerExpression;
		CoercerResultType = coercerResultType;
		AwaitableInfo = coercedAwaitableInfo;
	}

	public static Boolean IsTypeAwaitable(Type type, out CoercedAwaitableInfo info) {
		if (AwaitableInfo.IsTypeAwaitable(type, out AwaitableInfo directlyAwaitableInfo)) {
			info = new CoercedAwaitableInfo(directlyAwaitableInfo);
			return true;
		}

		// It's not directly awaitable, but maybe we can coerce it.
		// Currently we support coercing FSharpAsync<T>.
		if (ObjectMethodExecutorFSharpSupport.TryBuildCoercerFromFSharpAsyncToAwaitable(type,
			    out Expression coercerExpression,
			    out Type coercerResultType)) {
			if (AwaitableInfo.IsTypeAwaitable(coercerResultType, out AwaitableInfo coercedAwaitableInfo)) {
				info = new CoercedAwaitableInfo(coercerExpression, coercerResultType, coercedAwaitableInfo);
				return true;
			}
		}

		info = default(CoercedAwaitableInfo);
		return false;
	}
}