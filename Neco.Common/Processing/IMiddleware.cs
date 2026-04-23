namespace Neco.Common.Processing;

using System.Threading.Tasks;

/// <summary>
/// Defines the contract for a middleware component that processes data in a <see cref="MiddlewarePipeline{TData}"/>.
/// </summary>
/// <typeparam name="TData">The type of data that the middleware processes.</typeparam>
public interface IMiddleware<TData> {
	/// <summary>
	/// Processes the data and passes it to the next middleware in the pipeline.
	/// </summary>
	/// <param name="next">A delegate to the next middleware in the pipeline.</param>
	/// <param name="context">The pipeline data.</param>
	public Task Handle(MiddlewareDelegate<TData> next, TData context);
}