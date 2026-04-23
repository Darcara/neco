namespace Neco.Common.Concurrency;

using System.Threading.Tasks;

/// <summary>
/// An object balancer that manages a pool of reusable objects.
/// </summary>
/// <typeparam name="T">The type of objects managed by the balancer.</typeparam>
/// <remarks>
/// Implementations of this interface are responsible for distributing access to pooled objects
/// and ensuring they are properly returned for reuse. All pooled objects can optionally implement
/// <see cref="IDisposable"/>, which will be called when the balancer is disposed.
/// In contrast to object-pools an effort is made to balance the access to the cached
/// objects in some manner (usually something like RoundRobin).
/// </remarks>
public interface IObjectBalancer<T> : IDisposable {
	/// <summary>
	/// Synchronously retrieves an object from the pool.
	/// </summary>
	/// <returns>An object of type <typeparamref name="T"/> from the pool.</returns>
	/// <remarks>
	/// This method blocks if no objects are immediately available until one becomes available.
	/// The object must be returned to the pool using the <see cref="Return"/> method.
	/// </remarks>
	T Get();

	/// <summary>
	/// Asynchronously retrieves an object from the pool.
	/// </summary>
	/// <returns>A task that represents the asynchronous retrieval operation, containing an object of type <typeparamref name="T"/>.</returns>
	/// <remarks>
	/// This method is useful for async contexts where blocking would be inappropriate.
	/// The object must be returned to the pool using the <see cref="Return"/> method.
	/// </remarks>
	ValueTask<T> GetAsync();

	/// <summary>
	/// Synchronously retrieves an object from the pool wrapped in a disposable container.
	/// </summary>
	/// <returns>A <see cref="BalancedObject{T}"/> that automatically returns the object to the pool when disposed.</returns>
	/// <remarks>
	/// This is a convenience method that returns a disposable wrapper. When the wrapper is disposed,
	/// the underlying object is automatically returned to the pool.
	/// This method blocks if no objects are immediately available until one becomes available.
	/// </remarks>
	BalancedObject<T> GetDisposable();

	/// <summary>
	/// Asynchronously retrieves an object from the pool wrapped in a disposable container.
	/// </summary>
	/// <returns>A task that represents the asynchronous retrieval operation, containing a <see cref="BalancedObject{T}"/>.</returns>
	/// <remarks>
	/// This is a convenience method for async contexts that returns a disposable wrapper. When the wrapper is disposed,
	/// the underlying object is automatically returned to the pool.
	/// </remarks>
	ValueTask<BalancedObject<T>> GetDisposableAsync();

	/// <summary>
	/// Returns an object to the pool for reuse by other consumers.
	/// </summary>
	/// <param name="obj">The object to return to the pool.</param>
	/// <remarks>
	/// This method should be called after an object obtained via <see cref="Get"/> or <see cref="GetAsync"/>
	/// is no longer needed. For disposable wrappers obtained via <see cref="GetDisposable"/> or <see cref="GetDisposableAsync"/>,
	/// this method is called automatically when the wrapper is disposed.
	/// </remarks>
	void Return(T obj);
}