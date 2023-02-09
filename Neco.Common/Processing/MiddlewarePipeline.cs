namespace Neco.Common.Processing;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

public interface IMiddleware<TData> {
	public Task Handle(MiddlewareDelegate<TData> next, TData context);
}

public delegate Task MiddlewareDelegate<in TData>(TData data);

/// <summary>
/// A very simple middleware pipeline.
/// </summary>
/// <typeparam name="TData">The type of data that every middleware mutates.</typeparam>
public class MiddlewarePipeline<TData> {
	private readonly MiddlewareDelegate<TData> _endOfPipeline;
	private readonly List<Func<MiddlewareDelegate<TData>, MiddlewareDelegate<TData>>> _middlewares = new();
	private MiddlewareDelegate<TData>? _pipeline;

	public MiddlewarePipeline() {
		_endOfPipeline = _ => Task.CompletedTask;
	}

	public MiddlewarePipeline(MiddlewareDelegate<TData> endOfPipeline) {
		_endOfPipeline = endOfPipeline;
	}

	/// <summary>Appends a middleware to the end of the middleware pipeline.</summary>
	/// <remarks>Prefer this method, as it reslts in the least stacktrace pollution</remarks>
	/// <example>
	/// Use like: <code>next => ctx => FunctionCall(next, ctx)</code>
	/// </example>
	public MiddlewarePipeline<TData> AppendMiddleware(Func<MiddlewareDelegate<TData>, MiddlewareDelegate<TData>> chainer) {
		_middlewares.Add(chainer);
		return this;
	}

	/// <summary>
	/// Appends an implementation of <see cref="IMiddleware{T}"/> to the end of the middleware pipeline.
	/// </summary>
	/// <remarks>Prefer <see cref="AppendMiddleware"/> for less stacktrace pollution</remarks>
	/// <param name="middleware">The instance of the <see cref="IMiddleware{T}"/> to append</param>
	public MiddlewarePipeline<TData> AppendMiddlewareSuboptimal(IMiddleware<TData> middleware) {
		_middlewares.Add(next => ctx => middleware.Handle(next, ctx));
		return this;
	}

	/// <summary>
	/// Appends a function to the end of the middleware pipeline.
	/// </summary>
	/// <remarks>Prefer <see cref="AppendMiddleware"/> for less stacktrace pollution</remarks>
	/// <param name="middleware">The function to append</param>
	public MiddlewarePipeline<TData> AppendMiddlewareSuboptimal(Func<MiddlewareDelegate<TData>, TData, Task> middleware) {
		_middlewares.Add(next => ctx => middleware.Invoke(next, ctx));
		return this;
	}

	/// <summary>
	/// Creates the pipeline from the <see cref="AppendMiddleware">appended</see> middleware-factories
	/// </summary>
	/// <returns>The , but prefer to use <see cref="CallAsync"/> instead</returns>
	/// <exception cref="InvalidOperationException">If the pipeline has already been built.</exception>
	[MemberNotNull(nameof(_pipeline))]
	public MiddlewareDelegate<TData> Build() {
		if (_pipeline != null) throw new InvalidOperationException("Pipeline has already been built");
		MiddlewareDelegate<TData> pipeline = _endOfPipeline;
		for (Int32 idx = _middlewares.Count - 1; idx >= 0; idx--) {
			pipeline = _middlewares[idx](pipeline);
		}

		_pipeline = pipeline;
		return _pipeline;
	}

	/// <summary>
	/// Executes the pipeline with the given data. Builds the pipeline before the first call if necessary.
	/// </summary>
	/// <param name="data">The data object to start the pipeline with</param>
	public Task CallAsync(TData data) {
		if (_pipeline == null) Build();
		return _pipeline(data);
	}

	/// <summary>
	/// Executes the pipeline with the given data. Builds the pipeline before the first call if necessary.
	/// </summary>
	/// <param name="data">The data object to start the pipeline with</param>
	public void Call(TData data) {
		if (_pipeline == null) Build();
		_pipeline(data).ConfigureAwait(false).GetAwaiter().GetResult();
	}
}