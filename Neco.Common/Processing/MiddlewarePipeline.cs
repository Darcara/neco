namespace Neco.Common.Processing;

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

/// <summary>
/// A delegate that represents a middleware function or the next step in a <see cref="MiddlewarePipeline{TData}"/>.
/// </summary>
/// <typeparam name="TData">The type of data being processed.</typeparam>
/// <param name="context">The pipeline data.</param>
[SuppressMessage("Design", "MA0048:File name must match type name")]
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
public delegate Task MiddlewareDelegate<in TData>(TData context);

/// <summary>
/// A simple middleware pipeline for processing data through a series of middleware components.
/// </summary>
/// <typeparam name="TData">The type of data that every middleware mutates or processes.</typeparam>
/// <remarks>
/// The pipeline is built lazily on the first call and caches the result for subsequent calls.
/// The <see cref="AppendMiddleware"/> method is preferred over <see cref="AppendMiddlewareSuboptimal(IMiddleware{TData})"/>
/// as it produces less stack trace pollution.
/// </remarks>
public class MiddlewarePipeline<TData> {
	private readonly MiddlewareDelegate<TData> _endOfPipeline;
	private readonly List<Func<MiddlewareDelegate<TData>, MiddlewareDelegate<TData>>> _middlewares = new();
	private MiddlewareDelegate<TData>? _pipeline;

	/// <summary>
	/// Initializes a new instance of the <see cref="MiddlewarePipeline{TData}"/> class with a no-op end of pipeline.
	/// </summary>
	public MiddlewarePipeline() {
		_endOfPipeline = _ => Task.CompletedTask;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MiddlewarePipeline{TData}"/> class with a specified end-of-pipeline delegate.
	/// </summary>
	/// <param name="endOfPipeline">The delegate to execute when the end of pipeline is reached.</param>
	public MiddlewarePipeline(MiddlewareDelegate<TData> endOfPipeline) {
		_endOfPipeline = endOfPipeline;
	}

	/// <summary>
	/// Appends a middleware function to the end of the middleware pipeline.
	/// </summary>
	/// <remarks>
	/// This is the preferred method for adding middleware as it results in less stack trace pollution compared to
	/// <see cref="AppendMiddlewareSuboptimal(IMiddleware{TData})"/>.
	/// </remarks>
	/// <param name="chainer">A function that takes a <see cref="MiddlewareDelegate{TData}"/> (the next middleware)
	/// and returns a new <see cref="MiddlewareDelegate{TData}"/> (the current middleware).</param>
	/// <returns>This pipeline instance, for method chaining.</returns>
	/// <example>
	/// Use like: <code>next => ctx => FunctionCall(next, ctx)</code>
	/// or <code>next => async ctx => FunctionCallAsync(next, ctx)</code>
	/// </example>
	public MiddlewarePipeline<TData> AppendMiddleware(Func<MiddlewareDelegate<TData>, MiddlewareDelegate<TData>> chainer) {
		_middlewares.Add(chainer);
		return this;
	}

	/// <summary>
	/// Appends an implementation of <see cref="IMiddleware{TData}"/> to the end of the middleware pipeline.
	/// </summary>
	/// <remarks>
	/// This method is less efficient than <see cref="AppendMiddleware"/> and should only be used when you have
	/// an <see cref="IMiddleware{TData}"/> instance that cannot be expressed as a lambda or local function.
	/// </remarks>
	/// <param name="middleware">The middleware instance to append.</param>
	/// <returns>This pipeline instance, for method chaining.</returns>
	public MiddlewarePipeline<TData> AppendMiddlewareSuboptimal(IMiddleware<TData> middleware) {
		_middlewares.Add(next => ctx => middleware.Handle(next, ctx));
		return this;
	}

	/// <summary>
	/// Appends a middleware function to the end of the middleware pipeline.
	/// </summary>
	/// <remarks>
	/// This method is less efficient than <see cref="AppendMiddleware"/> and should only be used when you have
	/// a function that must be wrapped.
	/// </remarks>
	/// <param name="middleware">The function to append. Takes a next delegate and context data.</param>
	/// <returns>This pipeline instance, for method chaining.</returns>
	public MiddlewarePipeline<TData> AppendMiddlewareSuboptimal(Func<MiddlewareDelegate<TData>, TData, Task> middleware) {
		_middlewares.Add(next => ctx => middleware.Invoke(next, ctx));
		return this;
	}

	/// <summary>
	/// Builds the complete middleware pipeline from the appended middleware functions.
	/// </summary>
	/// <returns>The built <see cref="MiddlewareDelegate{TData}"/> that can be invoked to run the pipeline.</returns>
	/// <exception cref="InvalidOperationException">Thrown if the pipeline has already been built.</exception>
	/// <remarks>
	/// This method is called automatically during the first invocation of <see cref="CallAsync"/> or <see cref="Call"/> and does not need
	/// to be called manually.
	/// </remarks>
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
	/// Asynchronously executes the pipeline with the given data, building the pipeline if necessary.
	/// </summary>
	/// <param name="data">The data object to pass through the pipeline.</param>
	/// <remarks>
	/// The pipeline is built automatically on the first call if it hasn't been built yet. Subsequent calls reuse the cached pipeline.
	/// </remarks>
	public Task CallAsync(TData data) {
		if (_pipeline == null) Build();
		return _pipeline(data);
	}

	/// <summary>
	/// Synchronously executes the pipeline with the given data, building the pipeline if necessary.
	/// </summary>
	/// <param name="data">The data object to pass through the pipeline.</param>
	/// <remarks>
	/// This method blocks until the pipeline completes. The pipeline is built automatically on the first call if it hasn't been built yet.
	/// </remarks>
	public void Call(TData data) {
		if (_pipeline == null) Build();
		_pipeline(data).ConfigureAwait(false).GetAwaiter().GetResult();
	}
}