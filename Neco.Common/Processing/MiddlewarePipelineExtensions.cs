namespace Neco.Common.Processing;

using System;

public static class MiddlewarePipelineExtensions {
	/// <summary>
	/// Branches the pipeline if the given predicate returns true. The branched Pipeline will NOT rejoin the original pipeline.
	/// </summary>
	/// <returns>The original (non-branched) <see cref="MiddlewarePipeline{TData}"/></returns>
	public static MiddlewarePipeline<TData> MapWhen<TData>(this MiddlewarePipeline<TData> pipeline, Predicate<TData> predicate, Action<MiddlewarePipeline<TData>> configure) {
		ArgumentNullException.ThrowIfNull(pipeline);
		ArgumentNullException.ThrowIfNull(predicate);
		ArgumentNullException.ThrowIfNull(configure);
		
		MiddlewarePipeline<TData> branch = new();
		configure(branch);
		
		// Not a middleware to keep the stacktrace clean
		pipeline.AppendMiddleware(next => ctx => {
			if (predicate(ctx))
				return branch.CallAsync(ctx);
			return next(ctx);
		});
		return pipeline;
	}

	/// <summary>
	/// Conditionally creates an intermediate branch in the pipeline that is rejoined to the main pipeline.
	/// </summary>
	/// <returns>The original (non-branched) <see cref="MiddlewarePipeline{TData}"/></returns>
	public static MiddlewarePipeline<TData> UseWhen<TData>(this MiddlewarePipeline<TData> pipeline, Predicate<TData> predicate, Action<MiddlewarePipeline<TData>> configure) {
		ArgumentNullException.ThrowIfNull(pipeline);
		ArgumentNullException.ThrowIfNull(predicate);
		ArgumentNullException.ThrowIfNull(configure);
		
		MiddlewarePipeline<TData> branch = new();
		configure(branch);
		
		// Not a middleware to keep the stacktrace clean
		pipeline.AppendMiddleware(next => {
			// this is called only once during pipeline build
			branch.AppendMiddleware(_ => next);
			branch.Build();
			return ctx => {
				if (predicate(ctx))
					return branch.CallAsync(ctx);
				return next(ctx);
			};
		});
		return pipeline;
	}
}