namespace Neco.Common.Concurrency;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

[DebuggerDisplay("Count = {Count}")]
public class ConcurrentSet<T> : ICollection<T>, IReadOnlyCollection<T> where T : notnull {
	private readonly Dictionary<T, Byte> _store;

	public ConcurrentSet(Int32 capacity=0, IEqualityComparer<T>? comparer = null) {
		_store = new(capacity, comparer);
	}

	/// <inheritdoc cref="ISet{T}.Add" />
	public Boolean Add(T item) => _store.TryAdd(item, 0);

	#region Implementation of IEnumerable

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion

	#region Implementation of IEnumerable<out T>

	/// <inheritdoc />
	public IEnumerator<T> GetEnumerator() => _store.Keys.GetEnumerator();

	#endregion

	#region Implementation of ICollection<T>

	/// <inheritdoc />
	void ICollection<T>.Add(T item) => Add(item);

	/// <inheritdoc />
	public void Clear() => _store.Clear();

	/// <inheritdoc />
	public Boolean Contains(T item) => _store.ContainsKey(item);

	/// <inheritdoc />
	public void CopyTo(T[] array, Int32 arrayIndex) => _store.Keys.CopyTo(array, arrayIndex);

	/// <inheritdoc />
	public Boolean Remove(T item) => _store.Remove(item);

	/// <inheritdoc cref="ICollection{T}.Count" />
	public Int32 Count => _store.Count;

	/// <inheritdoc />
	public Boolean IsReadOnly => false;

	#endregion
}