namespace Neco.Common.Processing;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public class BaseContext : IContext {
	private readonly Dictionary<Object, Object> _features = new();

	/// <inheritdoc />
	public void SetData<TKeyAndData>(TKeyAndData data) where TKeyAndData : notnull => SetData(typeof(TKeyAndData), data);

	/// <inheritdoc />
	public void SetData<TKey, TData>(TKey key, TData data) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key, nameof(key));
		ArgumentNullException.ThrowIfNull(data, nameof(key));
		_features[key] = data;
	}

	/// <inheritdoc />
	public void ClearData<TKey>(TKey key) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key, nameof(key));
		_features.Remove(key);
	}

	/// <inheritdoc />
	public TData GetData<TKey, TData>(TKey key) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key, nameof(key));
		return (TData)_features[key];
	}

	/// <inheritdoc />
	public TKeyAndData GetData<TKeyAndData>(TKeyAndData key) where TKeyAndData : notnull {
		ArgumentNullException.ThrowIfNull(key, nameof(key));
		return (TKeyAndData)_features[key];
	}

	/// <inheritdoc />
	public TData? GetDataOrDefault<TKey, TData>(TKey key) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key, nameof(key));
		if (_features.TryGetValue(key, out Object? obj)) {
			return (TData?)obj;
		}

		return default(TData);
	}

	/// <inheritdoc />
	public Boolean TryGetData<TKey, TData>(TKey key, [NotNullWhen(true)] out TData? data) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key, nameof(key));
		if (_features.TryGetValue(key, out Object? obj)) {
			data = (TData)obj;
			return true;
		}

		data = default(TData);
		return false;
	}
}