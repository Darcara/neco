namespace Neco.Common.Processing;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A data/memory container for <see cref="IMiddleware{TData}"/> within <see cref="MiddlewarePipeline{TData}"/>.
/// </summary>
public interface IContext {
	/// <summary>
	/// Stores data using the data type itself as the key.
	/// </summary>
	/// <typeparam name="TKeyAndData">The type of data to store, which is also used as the key.</typeparam>
	/// <param name="data">The data to store.</param>
	void SetData<TKeyAndData>(TKeyAndData data) where TKeyAndData : notnull;

	/// <summary>
	/// Stores data with an explicit key.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TData">The type of the data being stored.</typeparam>
	/// <param name="key">The key used to retrieve the data later.</param>
	/// <param name="data">The data to store.</param>
	void SetData<TKey, TData>(TKey key, TData data) where TKey : notnull;

	/// <summary>
	/// Removes data associated with a specific key.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <param name="key">The key of the data to remove.</param>
	void ClearData<TKey>(TKey key) where TKey : notnull;

	/// <summary>
	/// Retrieves data stored with an explicit key, throwing an exception if not found.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TData">The type of the data to retrieve.</typeparam>
	/// <param name="key">The key used to look up the data.</param>
	/// <returns>The data associated with the key.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when the key is not found in the context.</exception>
	TData GetData<TKey, TData>(TKey key) where TKey : notnull;

	/// <summary>
	/// Retrieves data using the type itself as the key, throwing an exception if not found.
	/// </summary>
	/// <typeparam name="TKeyAndData">The type of data to retrieve, which is also used as the key.</typeparam>
	/// <param name="key">The key used to look up the data.</param>
	/// <returns>The data associated with the type key.</returns>
	/// <exception cref="KeyNotFoundException">Thrown when the type key is not found in the context.</exception>
	TKeyAndData GetData<TKeyAndData>(TKeyAndData key) where TKeyAndData : notnull;

	/// <summary>
	/// Retrieves data stored with an explicit key, returning a default value if not found.
	/// </summary>
	/// <typeparam name="TKey">The type of the key.</typeparam>
	/// <typeparam name="TData">The type of the data to retrieve.</typeparam>
	/// <param name="key">The key used to look up the data.</param>
	/// <param name="valueIfNotFound">The value to return if the key is not found.</param>
	/// <returns>The data associated with the key, or the default value if not found.</returns>
	TData? GetDataOrDefault<TKey, TData>(TKey key, TData? valueIfNotFound=default) where TKey : notnull;

		/// <summary>
		/// Attempts to retrieve data stored with an explicit key.
		/// </summary>
		/// <typeparam name="TKey">The type of the key.</typeparam>
		/// <typeparam name="TData">The type of the data to retrieve.</typeparam>
		/// <param name="key">The key used to look up the data.</param>
		/// <param name="data">When this method returns, contains the data associated with the key if found; otherwise, the default value.</param>
		/// <returns><see langword="true"/> if the data is found; otherwise, <see langword="false"/>.</returns>
		Boolean TryGetData<TKey, TData>(TKey key, [NotNullWhen(true)] out TData? data) where TKey : notnull;
}