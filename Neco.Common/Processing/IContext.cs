namespace Neco.Common.Processing;

using System;
using System.Diagnostics.CodeAnalysis;

public interface IContext {
	void SetData<TKeyAndData>(TKeyAndData data) where TKeyAndData : notnull;
	void SetData<TKey, TData>(TKey key, TData data) where TKey : notnull;
	void ClearData<TKey>(TKey key) where TKey : notnull;
	TData GetData<TKey, TData>(TKey key) where TKey : notnull;
	TKeyAndData GetData<TKeyAndData>(TKeyAndData key) where TKeyAndData : notnull;
	TData? GetDataOrDefault<TKey, TData>(TKey key) where TKey : notnull;
	Boolean TryGetData<TKey, TData>(TKey key, [NotNullWhen(true)] out TData? data) where TKey : notnull;
}