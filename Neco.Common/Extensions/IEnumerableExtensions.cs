namespace Neco.Common.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once InconsistentNaming
public static class IEnumerableExtensions {
	/// <summary>
	/// Calculates the minimum and maximum value in a single pass and returns the total count of non-null elements.
	/// </summary>
	public static Int32 MinMax<T>(this IEnumerable<T?> data, out T? min, out T? max) where T : IComparable<T> {
		ArgumentNullException.ThrowIfNull(data);
		Int32 count = 0;
		min = default(T);
		max = default(T);
		foreach (T? t in data) {
			if (t == null) continue;

			if (count == 0) {
				min = t;
				max = t;
			}

			if (t.CompareTo(min) < 0)
				min = t;
			if (t.CompareTo(max) > 0)
				max = t;
			++count;
		}

		return count;
	}

	///<summary>Finds the index of the first item matching an expression in an enumerable.</summary>
	///<param name="items">The enumerable to search.</param>
	///<param name="predicate">The expression to test the items against.</param>
	///<returns>The index of the first matching item, or -1 if no items match.</returns>
	public static Int32 FindIndex<T>(this IEnumerable<T> items, Func<T, Boolean> predicate) {
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(predicate);

		Int32 retVal = 0;
		foreach (T item in items) {
			if (predicate(item)) return retVal;
			retVal++;
		}

		return -1;
	}

	///<summary>Finds the index of the first occurrence of an item in an enumerable.</summary>
	///<param name="items">The enumerable to search.</param>
	///<param name="item">The item to find.</param>
	///<returns>The index of the first matching item, or -1 if the item was not found.</returns>
	public static Int32 IndexOf<T>(this IEnumerable<T> items, T? item) {
		return items.FindIndex(i => EqualityComparer<T>.Default.Equals(item, i));
	}

	/// <summary>
	/// Executes an action for each element in the source giving the index and the item itself
	/// </summary>
	public static void ForEach<T>(this IEnumerable<T>? source, Action<T> action) {
		if (source == null) return;
		ArgumentNullException.ThrowIfNull(action);
		foreach (T x1 in source) {
			action.Invoke(x1);
		}
	}

	/// <summary>
	/// Executes an action for each element in the source giving the index and the item itself
	/// </summary>
	public static void ForEach<T1, T2>(this IEnumerable<KeyValuePair<T1, T2>>? source, Action<T1, T2> action) {
		if (source == null) return;
		ArgumentNullException.ThrowIfNull(action);
		foreach (KeyValuePair<T1, T2> x1 in source) {
			action.Invoke(x1.Key, x1.Value);
		}
	}

	/// <summary>
	/// Executes an action for each element in the source giving the index and the item itself
	/// </summary>
	public static void ForEachIdx<T>(this IEnumerable<T>? source, Action<T, Int32> action) {
		if (source == null) return;
		ArgumentNullException.ThrowIfNull(action);

		Int32 idx = 0;
		foreach (T x1 in source) {
			action.Invoke(x1, idx++);
		}
	}

	/// <summary>
	/// Returns a random element while enumerating everything exactly once
	/// </summary>
	public static T? RandomElementOrDefault<T>(this IEnumerable<T> source) {
		ArgumentNullException.ThrowIfNull(source);
		Int32 elemCount = 0;
		T? representativeElem = default;
		foreach (T elem in source) {
			++elemCount;

			if (elemCount == 1) {
				representativeElem = elem;
				continue;
			}

			if (Random.Shared.Next(elemCount) == 0)
				representativeElem = elem;
		}

		return representativeElem;
	}

	/// <summary>
	/// Returns any number of randomly selected elements while enumerating exactly once
	/// </summary>
	public static T[] RandomElements<T>(this IEnumerable<T> source, Int32 numElements) {
		ArgumentNullException.ThrowIfNull(source);
		T[] reservoir = new T[numElements];
		Int32 elemCount = 0;
		foreach (T elem in source) {
			++elemCount;

			if (elemCount <= numElements) {
				reservoir[elemCount - 1] = elem;
				continue;
			}

			Int32 elementToReplace = Random.Shared.Next(elemCount);
			if (elementToReplace < numElements) {
				reservoir[elementToReplace] = elem;
			}
		}

		if (elemCount == 0) throw new InvalidOperationException("Sequence contains no elements");

		return reservoir;
	}

	/// <summary>
	/// Recursive depth first traversal
	/// </summary>
	public static IEnumerable<T> SelectRecursive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector) {
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(selector);
		foreach (T parent in source) {
			yield return parent;

			IEnumerable<T> children = selector(parent);
			foreach (T child in SelectRecursive(children, selector))
				yield return child;
		}
	}

	/// <summary>
	/// Recursive depth first traversal
	/// </summary>
	public static IEnumerable<T> SelectRecursiveStack<T>(this IEnumerable<T> source, Func<T, Stack<T>, IEnumerable<T>> selector, Stack<T>? path = null) {
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(selector);
		path ??= new Stack<T>();
		foreach (T parent in source) {
			yield return parent;

			IEnumerable<T> children = selector(parent, path);
			foreach (T child in SelectRecursiveStack(children, selector, path))
				yield return child;
		}
	}

	/// <summary>
	/// Filters all null elements so only non-null elements remain
	/// </summary>
	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class {
		ArgumentNullException.ThrowIfNull(source);
		return source.Where(t => t != null)!;
	}
	
	/// <summary>
	/// Copies the elements of an <see cref="IEnumerable{T}"/> into the supplied array
	/// </summary>
	/// <param name="source">The enumerable to copy</param>
	/// <param name="destination">The destination array</param>
	/// <param name="destinationOffset">The offset at which the elements will be inserted</param>
	public static void CopyTo<T>(this IEnumerable<T> source, T[] destination, Int32 destinationOffset) {
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(destination);
		foreach (T t in source) {
			destination[destinationOffset++] = t;
		}
	}
	
	/// <summary>
	/// Copies the elements of an <see cref="IEnumerable{T}"/> into the supplied span
	/// </summary>
	/// <param name="source">The enumerable to copy</param>
	/// <param name="destination">The destination span</param>
	public static void CopyTo<T>(this IEnumerable<T> source, Span<T> destination) where T : struct {
		ArgumentNullException.ThrowIfNull(source);
		Int32 offset = 0;
		foreach (T t in source) {
			destination[offset++] = t;
		}
	}
	
	public static Boolean ContainsAny<T>(this ICollection<T> source, IEnumerable<T> other)  {
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(other);
		foreach (T t in other) {
			if(source.Contains(t)) return true;
		}

		return false;
	}
	
	public static Boolean ContainsAll<T>(this ICollection<T> source, IEnumerable<T> other)  {
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(other);
		foreach (T t in other) {
			if(!source.Contains(t)) return false;
		}

		return true;
	}

	// public static Boolean TryFind<T>(this ICollection<T> source, Predicate<T> predicate, [NotNullWhen(true)]out T? result)  {
	// 	ArgumentNullException.ThrowIfNull(source);
	// 	result = source.FirstOrDefault(t => predicate(t));
	// }

}