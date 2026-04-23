namespace Neco.Common.Concurrency;

/// <summary>
/// A disposable wrapper that automatically returns a borrowed object to its balancer when disposed.
/// </summary>
/// <typeparam name="T">The type of the wrapped object.</typeparam>
/// <remarks>
/// This class is typically obtained from <see cref="IObjectBalancer{T}.GetDisposable"/> or 
/// <see cref="IObjectBalancer{T}.GetDisposableAsync"/>. It enables using the using statement or
/// using declaration pattern to automatically return objects to the pool.
/// </remarks>
public sealed class BalancedObject<T> : IDisposable {
	private readonly IObjectBalancer<T> _balancer;
	
	/// <summary>
	/// Gets the underlying object borrowed from the balancer.
	/// </summary>
	public readonly T TheObject;

	internal BalancedObject(IObjectBalancer<T> balancer, T theObject) {
		_balancer = balancer;
		TheObject = theObject;
	}

	/// <summary>
	/// Returns the borrowed object to the balancer.
	/// </summary>
	/// <inheritdoc />
	public void Dispose() {
		_balancer.Return(TheObject);
	}
}