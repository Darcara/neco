namespace Neco.Common.Processing;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

public class BaseContext {
	public Dictionary<Object, Object> Features { get; } = new();

	public void SetData<TKeyAndData>(TKeyAndData data) where TKeyAndData : notnull => SetData(typeof(TKeyAndData), data);
	public void SetData<TKey, TData>(TKey key, TData data) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key, nameof(key));
		ArgumentNullException.ThrowIfNull(data, nameof(key));
		Features[key] = data;
	}

	public void ClearData<TKey>(TKey key) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key, nameof(key));
		Features.Remove(key);
	}

	public TData GetData<TKey, TData>(TKey key) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key, nameof(key));
		return (TData)Features[key];
	}

	public TData? GetDataOrDefault<TKey, TData>(TKey key) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key, nameof(key));
		if (Features.TryGetValue(key, out Object? obj)) {
			return (TData?)obj;
		}

		return default(TData);
	}

	public Boolean TryGetData<TKey, TData>(TKey key, [NotNullWhen(true)] out TData? data) where TKey : notnull {
		ArgumentNullException.ThrowIfNull(key, nameof(key));
		if (Features.TryGetValue(key, out Object? obj)) {
			data = (TData)obj;
			return true;
		}

		data = default(TData);
		return false;
	}
}