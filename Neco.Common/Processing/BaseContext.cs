namespace Neco.Common.Processing;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A thread-safe base implementation of <see cref="IContext"/> that uses a concurrent dictionary for data storage.
/// </summary>
/// <remarks>
/// This class is suitable for use in multi-threaded scenarios, such as middleware pipelines or request processing.
/// </remarks>
public class BaseContext : IContext {
	private readonly ConcurrentDictionary<Object, Object> _features = new();

	/// <inheritdoc />
	public void SetData<TKeyAndData>(TKeyAndData data) where TKeyAndData : notnull => SetData(typeof(TKeyAndData), data);

	/// <inheritdoc />
	public void SetData<TKey, TData>(TKey key, TData data) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(data);
		_features[key] = data;
	}

	/// <inheritdoc />
	public void ClearData<TKey>(TKey key) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key);
		_features.Remove(key, out _);
	}

	/// <inheritdoc />
	public TData GetData<TKey, TData>(TKey key) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key);
		return (TData)_features[key];
	}

	/// <inheritdoc />
	public TKeyAndData GetData<TKeyAndData>(TKeyAndData key) where TKeyAndData : notnull {
		ArgumentNullException.ThrowIfNull(key);
		return (TKeyAndData)_features[key];
	}

	/// <inheritdoc />
	[return: NotNullIfNotNull(nameof(valueIfNotFound))]
	public TData? GetDataOrDefault<TKey, TData>(TKey key, TData? valueIfNotFound=default) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key);
		if (_features.TryGetValue(key, out Object? obj)) {
			return (TData)obj;
		}

		return valueIfNotFound;
	}

	/// <inheritdoc />
	public Boolean TryGetData<TKey, TData>(TKey key, [NotNullWhen(true)] out TData? data) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key);
		if (_features.TryGetValue(key, out Object? obj)) {
			data = (TData)obj;
			return true;
		}

		data = default(TData);
		return false;
	}
}