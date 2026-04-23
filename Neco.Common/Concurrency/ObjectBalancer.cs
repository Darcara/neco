namespace Neco.Common.Concurrency;

/// <summary>
/// Provides factory methods for creating object balancers that manage a pool of reusable objects.
/// </summary>
/// <remarks>
/// Object balancers are useful for managing expensive resources that can be reused.
/// In contrast to object-pools an effort is made to balance the access to the cached
/// objects in some manner (usually something like RoundRobin).
/// </remarks>
public static class ObjectBalancer {
	/// <summary>
	/// Creates an object balancer of a fixed size from an enumerable collection of objects.
	/// </summary>
	/// <typeparam name="T">The type of objects to balance.</typeparam>
	/// <param name="objects">The collection of objects to manage in the balancer.</param>
	/// <returns>An <see cref="IObjectBalancer{T}"/> instance managing the provided objects.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="objects"/> is <see langword="null"/>.</exception>
	public static IObjectBalancer<T> Static<T>(IEnumerable<T> objects) {
		ArgumentNullException.ThrowIfNull(objects);
		return new StaticObjectBalancer<T>(objects);
	}

	/// <summary>
	/// Creates a static object balancer by generating a specified number of objects using a factory function.
	/// </summary>
	/// <typeparam name="T">The type of objects to balance.</typeparam>
	/// <param name="numberOfObjects">The number of objects to create.</param>
	/// <param name="objectFactory">A factory function that creates instances of type <typeparamref name="T"/>.</param>
	/// <returns>An <see cref="IObjectBalancer{T}"/> instance managing the generated objects.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="objectFactory"/> is <see langword="null"/>.</exception>
	public static IObjectBalancer<T> Static<T>(Int32 numberOfObjects, Func<T> objectFactory) {
		ArgumentNullException.ThrowIfNull(objectFactory);
		return new StaticObjectBalancer<T>(Enumerable.Range(0, numberOfObjects).Select(_ => objectFactory()));
	}
}